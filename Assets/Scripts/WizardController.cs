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
    public enum Step { Import, AskCompress, Compressing, AskRun, Running, Done }

    [Header("Refs")]
    public ModelViewer viewer;          // arraste seu SceneRoot (ModelViewer)
    public Metrics metrics;             // arraste o GO Metrics
    [Header("UI")]
    public GameObject panel;            // um painel único
    public TMP_Text titleText;          // título do passo
    public TMP_Text bodyText;           // texto/descrição
    public UI.Button primaryButton;     // “Selecionar…”, “Sim/Executar”, “OK/Concluir”
    public UI.Button secondaryButton;   // “Pular” / “Não”
    public UI.Button backButton;        // opcional (pode desabilitar)
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
        }
    }

    async Task OnSecondaryAsync()
    {
        switch (_step)
        {
            case Step.Import:
                // pular import: apenas re-scan e ir pra pergunta de compress
                viewer.RescanModels();
                _modelName = viewer.GetSelectedModelName(); // usa primeiro disponível
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

    // ============ PASSO 2: COMPRIMIR (DRACO + MESHOPT) ============

    async Task DoCompressBothAsync()
    {
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
        // roda para as variantes na ordem (pula as que não existirem)
        foreach (var v in _runOrder)
        {
            // checa se a variante existe
            var variants = viewer.GetAvailableVariants(_modelName);
            if (!variants.Contains(v)) continue;

            bodyText?.SetText($"Rodando {_modelName} ({v})…");
            // carrega + métricas (o ModelViewer já chama Metrics.*)
            bool ok = await viewer.LoadAsync(_modelName, v);

            // upsert no CSV já é feito dentro do Metrics.WriteCsv() (ver patch abaixo)
            // limpa caches entre execuções para não “contaminar”
            await ClearBetweenRunsAsync();

            if (!ok)
            {
                bodyText?.SetText($"Falha ao carregar {_modelName} ({v}). Pulando.");
            }
        }
        bodyText?.SetText("Testes concluídos. CSV atualizado.");
        await Task.Yield();
    }

    static async Task ClearBetweenRunsAsync()
    {
        // descarrega GLTFs da cena se o caller não o fizer
        var op = Resources.UnloadUnusedAssets();
        while (!op.isDone) await Task.Yield();
        GC.Collect();
        await Task.Delay(100);
    }
}
