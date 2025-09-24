using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UI = UnityEngine.UI;
using UApp = UnityEngine.Application;

public class WizardController : MonoBehaviour
{
    public enum Step { Import, AskCompress, Compressing, AskRun, Running, Done, AskOverwriteImport, AskOverwriteCompress }

    [Header("Refs")]
    public ModelViewer viewer;          // arraste seu SceneRoot (ModelViewer)
    public Metrics metrics;             // arraste o GO Metrics
    public HUDController hudController; // referência para controle mútuo de visibilidade
    [Header("UI")]
    public GameObject panel;            // um painel único
    public TMP_Text titleText;          // título do passo
    public TMP_Text bodyText;           // texto/descrição
    public UI.Button primaryButton;     // "Selecionar…", "Sim/Executar", "OK/Concluir"
    public UI.Button secondaryButton;   // "Pular" / "Não"
    public UI.Button backButton;        // opcional (pode desabilitar)
    
    [Header("Visibility")]
    public bool startHidden = true;
#if UNITY_EDITOR
    public bool useEditorFilePicker = true; // abre OpenFilePanel no Editor
#endif

    private Step _step = Step.Import;
    private string _importSourcePath;   // caminho do arquivo escolhido
    private string _modelName;          // deduzido do arquivo
    private string[] _runOrder = new[] { "original", "meshopt", "draco" };

    void Awake()
    {
        primaryButton.onClick.AddListener(() => _ = OnPrimaryAsync());
        secondaryButton.onClick.AddListener(() => _ = OnSecondaryAsync());
        if (backButton) backButton.onClick.AddListener(() => Go(Step.Import));
        Go(Step.Import);
    }

    void Go(Step s)
    {
        _step = s;
        switch (s)
        {
            case Step.Import:
                SetUI("1) Importar modelo",
                    "Selecione um arquivo .glb para importar.\n" +
                    "Ele será copiado para: StreamingAssets/Models/<Nome>/original/model.glb");
                SetButtons("Selecionar .glb…", "Pular", back:false);
                break;

            case Step.AskCompress:
                SetUI($"2) Comprimir \"{_modelName}\"?",
                    "Deseja gerar variantes Draco e Meshopt agora?");
                SetButtons("Sim, comprimir", "Não, pular", back:true);
                break;

            case Step.Compressing:
                SetUI("Comprimindo…", "Gerando Draco e Meshopt, aguarde…");
                SetButtons("Aguarde…", "", back:false, interact:false);
                break;

            case Step.AskRun:
                SetUI("3) Rodar testes?",
                    $"Rodar métricas para {_modelName} nas variantes disponíveis (original / meshopt / draco)?");
                SetButtons("Rodar testes", "Pular", back:true);
                break;

            case Step.Running:
                SetUI("Executando testes…", "Coletando métricas e salvando no CSV, aguarde…");
                SetButtons("Aguarde…", "", back:false, interact:false);
                break;

            case Step.Done:
                SetUI("Concluído 🎉", "Fluxo finalizado. Você pode recomeçar ou fechar.");
                SetButtons("Reiniciar", "Fechar painel", back:false);
                break;

            case Step.AskOverwriteImport:
                SetUI($"Modelo \"{_modelName}\" já existe", 
                    $"O modelo \"{_modelName}\" já foi importado anteriormente.\nDeseja sobrescrever?");
                SetButtons("Sim, sobrescrever", "Não, cancelar", back:true);
                break;

            case Step.AskOverwriteCompress:
                SetUI($"\"{_modelName}\" já comprimido", 
                    $"O modelo \"{_modelName}\" já possui variantes comprimidas.\nDeseja comprimir novamente?");
                SetButtons("Sim, recomprimir", "Não, pular", back:true);
                break;
        }
    }

    void SetUI(string title, string body)
    {
        if (panel) panel.SetActive(true);
        if (titleText) titleText.SetText(title);
        if (bodyText)  bodyText.SetText(body);
    }

    void SetButtons(string primary, string secondary, bool back, bool interact = true)
    {
        if (primaryButton) {
            var t = primaryButton.GetComponentInChildren<TMP_Text>();
            if (t) t.SetText(primary);
            primaryButton.interactable = interact && !string.IsNullOrEmpty(primary);
            primaryButton.gameObject.SetActive(!string.IsNullOrEmpty(primary));
        }
        if (secondaryButton) {
            var t = secondaryButton.GetComponentInChildren<TMP_Text>();
            if (t) t.SetText(secondary);
            secondaryButton.gameObject.SetActive(!string.IsNullOrEmpty(secondary));
            secondaryButton.interactable = interact && !string.IsNullOrEmpty(secondary);
        }
        if (backButton) backButton.gameObject.SetActive(back);
    }

    async Task OnPrimaryAsync()
    {
        switch (_step)
        {
            case Step.Import:
                await DoImportAsync();
                break;

            case Step.AskCompress:
                Go(Step.Compressing);
                await DoCompressBothAsync();
                Go(Step.AskRun);
                break;

            case Step.AskRun:
                Go(Step.Running);
                await DoRunTestsAsync();
                Go(Step.Done);
                break;

            case Step.Done:
                Go(Step.Import);
                break;

            case Step.AskOverwriteImport:
                // Usuário confirmou sobrescrever - procede com import sem abrir seletor
                await DoImportWithoutFilePickerAsync();
                break;

            case Step.AskOverwriteCompress:
                // Usuário confirmou recomprimir - procede com compressão
                Go(Step.Compressing);
                await DoCompressBothAsync();
                Go(Step.AskRun);
                break;
        }
    }

    async Task OnSecondaryAsync()
    {
        switch (_step)
        {
            case Step.Import:
                // pular import: apenas re-scan e ir pra pergunta de compress
                viewer.RescanModels();
                _modelName = viewer.GetSelectedModelNamePublic(); // usa primeiro disponível
                Go(Step.AskCompress);
                break;

            case Step.AskCompress:
                // pular compress e seguir para testes
                Go(Step.AskRun);
                break;

            case Step.AskRun:
                Go(Step.Done);
                break;

            case Step.Done:
                if (panel) panel.SetActive(false);
                break;

            case Step.AskOverwriteImport:
                // Usuário cancelou sobrescrever - volta para import
                Go(Step.Import);
                break;

            case Step.AskOverwriteCompress:
                // Usuário cancelou recomprimir - pula para testes
                Go(Step.AskRun);
                break;
        }
        await Task.Yield();
    }

    // ================== PASSO 1: IMPORTAR ==================

    async Task DoImportAsync()
    {
#if UNITY_EDITOR
        if (useEditorFilePicker)
        {
            string path = UnityEditor.EditorUtility.OpenFilePanel("Selecione .glb", "", "glb");
            if (!string.IsNullOrEmpty(path) && File.Exists(path))
                _importSourcePath = path;
        }
        else
#endif
        {
            // fallback simples: usa primeiro arquivo arrumado manualmente
            _importSourcePath = null;
        }

        if (string.IsNullOrEmpty(_importSourcePath))
        {
            bodyText?.SetText("Nenhum arquivo selecionado. Coloque seu .glb em StreamingAssets/Models/<Nome>/original/model.glb e clique ‘Pular’ para continuar.");
            return;
        }

        // Deduza o nome do modelo do arquivo
        _modelName = Path.GetFileNameWithoutExtension(_importSourcePath);
        
        // Verifica se o modelo já existe
        if (viewer.ModelExists(_modelName))
        {
            Go(Step.AskOverwriteImport);
            return;
        }

        string destRoot = Path.Combine(UApp.streamingAssetsPath, "Models", _modelName, "original");
        Directory.CreateDirectory(destRoot);
        string dest = Path.Combine(destRoot, "model.glb");

        // Copia
        File.Copy(_importSourcePath, dest, overwrite: true);

        // Atualiza ModelViewer
        viewer.RescanModels();

        SetUI("Importado ✅", $"Modelo \"{_modelName}\" foi copiado para:\n{dest}");
        SetButtons("Continuar", "", back:true);
        Go(Step.AskCompress);
        await Task.Yield();
    }

    // Import sem abrir seletor de arquivo (usado quando usuário confirma sobrescrever)
    async Task DoImportWithoutFilePickerAsync()
    {
        if (string.IsNullOrEmpty(_importSourcePath))
        {
            bodyText?.SetText("Erro: caminho do arquivo não encontrado.");
            Go(Step.Import);
            return;
        }

        // Deduza o nome do modelo do arquivo
        _modelName = Path.GetFileNameWithoutExtension(_importSourcePath);
        
        string destRoot = Path.Combine(UApp.streamingAssetsPath, "Models", _modelName, "original");
        Directory.CreateDirectory(destRoot);
        string dest = Path.Combine(destRoot, "model.glb");

        // Copia
        File.Copy(_importSourcePath, dest, overwrite: true);

        // Atualiza ModelViewer
        viewer.RescanModels();

        SetUI("Importado ✅", $"Modelo \"{_modelName}\" foi sobrescrito em:\n{dest}");
        SetButtons("Continuar", "", back:true);
        Go(Step.AskCompress);
        await Task.Yield();
    }

    // ============ PASSO 2: COMPRIMIR (DRACO + MESHOPT) ============

    async Task DoCompressBothAsync()
    {
        // Verifica se já tem variantes comprimidas
        if (viewer.HasCompressedVariants(_modelName))
        {
            Go(Step.AskOverwriteCompress);
            return;
        }

        // Gera para o modelo atual (se já existir, reescreve)
        var ok = await viewer.CompressBothAsyncFor(_modelName);
        viewer.RescanModels();
        bodyText?.SetText(ok ? "Compressão OK (Draco e Meshopt gerados)." :
                               "Compressão falhou (ver Console). Você pode seguir sem compressão.");
        await Task.Yield();
    }

    // ============ PASSO 3: RODAR TESTES ============

    async Task DoRunTestsAsync()
    {
        foreach (var v in _runOrder)
        {
            var variants = viewer.GetAvailableVariantsPublic(_modelName);
            if (!variants.Contains(v)) continue;

            string path = viewer.ResolvePath(_modelName, v);

            // Begin (antes do load)
            Metrics.Instance?.BeginLoad(_modelName, v, path);

            // Carrega (sem métricas)
            bool ok = await viewer.LoadOnlyAsync(_modelName, v);

            // End do load
            if (Metrics.Instance != null) await Metrics.Instance.EndLoad(ok);

            if (!ok)
            {
                bodyText?.SetText($"Falha ao carregar \"{_modelName}\" ({v}). Pulando…");
                continue;
            }

            // Medição com contagem regressiva
            float secs = Metrics.Instance ? Metrics.Instance.fpsWindowSeconds : 5f;
            if (Metrics.Instance != null)
            {
                await Metrics.Instance.MeasureFpsWindowWithCallback(secs, (remaining) =>
                {
                    bodyText?.SetText($"Rodando \"{_modelName}\" ({v}) — medindo {remaining:0.0}s…");
                });
                Metrics.Instance.WriteCsv();
            }

            // Limpar cache entre runs
            await ClearBetweenRunsAsync();
        }

        bodyText?.SetText("Testes concluídos. CSV atualizado.");
    }


    static async Task ClearBetweenRunsAsync()
    {
        // descarrega GLTFs da cena se o caller não o fizer
        var op = Resources.UnloadUnusedAssets();
        while (!op.isDone) await Task.Yield();
        GC.Collect();
        await Task.Delay(100);
    }

    CanvasGroup _cg;

    void Start() {
        if (panel == null) return;
        _cg = panel.GetComponent<CanvasGroup>() ?? panel.AddComponent<CanvasGroup>();
        if (startHidden) HideImmediate();
    }

    // Mostrar/ocultar com fade
    public async Task Fade(bool show, float dur = 0.2f) {
        if (panel == null) return;
        if (_cg == null) _cg = panel.AddComponent<CanvasGroup>();
        if (show && !panel.activeSelf) panel.SetActive(true);

        float a0 = _cg.alpha, a1 = show ? 1f : 0f, t = 0f;
        _cg.blocksRaycasts = true; _cg.interactable = true;
        while (t < dur) {
            await Task.Yield();
            t += Time.unscaledDeltaTime;
            _cg.alpha = Mathf.Lerp(a0, a1, t / dur);
        }
        _cg.alpha = a1;
        _cg.blocksRaycasts = show;
        _cg.interactable   = show;
        if (!show) panel.SetActive(false);
    }

    public void Toggle() { _ = Fade(!(panel?.activeSelf ?? false)); }

    public void Show()   
    { 
        // Fecha o preview se estiver aberto
        if (hudController != null && hudController.modelSelectorPanel != null && hudController.modelSelectorPanel.activeSelf)
        {
            hudController.HideModelSelector();
        }
        _ = Fade(true);  
    }
    public void Hide()   { _ = Fade(false); }
    void   HideImmediate(){
        if (panel == null) return;
        var cg = panel.GetComponent<CanvasGroup>() ?? panel.AddComponent<CanvasGroup>();
        cg.alpha = 0; cg.blocksRaycasts = false; cg.interactable = false;
        panel.SetActive(false);
    }

}
