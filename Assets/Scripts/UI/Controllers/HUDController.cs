using System.Linq;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using PolyDiet.UI.Controllers;

public class HUDController : MonoBehaviour
{
    [Header("Refs")]
    public WizardController wizard;
    public ModelViewer viewer;
    public SimpleOrbitCamera orbitCamera;

    [Header("UI")]
    public Button buttonToggleWizard;
    public Button buttonQuickLoad;
    public TextMeshProUGUI quickLoadLabel; // opcional: mostra status
    public Button buttonResetCamera;
    public Button buttonReports; // Botão para abrir painel de relatórios
    public GameObject reportsPanel; // Referência ao ReportsPanel
    
    // Model Selector removido - agora usa apenas comparePanel com chips

    [Header("Compare (chips)")]
    public GameObject comparePanel;
    public TMP_Dropdown dropdownModel;     // o de modelo, dentro do ComparePanel
    public Transform variantChipContainer; // VariantChips
    public Toggle variantChipPrefab;       // ChipVariant (INATIVO)
    public Button buttonCompareLoad, buttonCompareClose;

    public CompareLoader compareLoader;    // _Systems/CompareRoot
    public CompareSplitView splitView;     // arraste o mesmo do CompareRoot

    // Estado atual do modelo renderizado
    private string _currentLoadedModel;
    private string _currentLoadedVariant;

    // estado interno
    readonly System.Collections.Generic.List<string> _selectedVariants = new();
    
    // Controle de carregamento para evitar múltiplos carregamentos simultâneos
    private bool _isLoading = false;

    void Awake()
    {
        if (buttonToggleWizard) buttonToggleWizard.onClick.AddListener(() => wizard?.Toggle());
        if (buttonQuickLoad)    buttonQuickLoad.onClick.AddListener(ShowComparePanel); // "Preview" abre o painel inteligente
        if (buttonResetCamera)  buttonResetCamera.onClick.AddListener(() => ResetCamera());
        
        
        // Compare panel
        if (buttonCompareLoad)  buttonCompareLoad.onClick.AddListener(() => _ = OnCompareConfirmAsync());
        if (buttonCompareClose) buttonCompareClose.onClick.AddListener(HideComparePanel);
        
        // Hide comparePanel initially
        if (comparePanel) comparePanel.SetActive(false);
    }

    void Start()
    {
        // Inicializa o label com o estado atual
        UpdateCurrentModelLabel();
        
        // Configurar listener do botão de relatórios
        if (buttonReports != null)
        {
            buttonReports.onClick.AddListener(OnClickReports);
        }
        
        // Garantir que o painel de relatórios inicie desativado
        if (reportsPanel != null)
        {
            reportsPanel.SetActive(false);
        }
    }

    void SetLabel(string s) { if (quickLoadLabel) quickLoadLabel.SetText(s); }

    void UpdateCurrentModelLabel()
    {
        if (quickLoadLabel == null) return;
        
        if (string.IsNullOrEmpty(_currentLoadedModel))
        {
            quickLoadLabel.SetText("Nenhum modelo carregado");
        }
        else
        {
            string variantText = string.IsNullOrEmpty(_currentLoadedVariant) ? "original" : _currentLoadedVariant;
            quickLoadLabel.SetText($"Atual: {_currentLoadedModel} ({variantText})");
        }
    }

    // Método público para outros componentes notificarem quando um modelo é carregado
    public void NotifyModelLoaded(string modelName, string variant)
    {
        _currentLoadedModel = modelName;
        _currentLoadedVariant = variant;
        UpdateCurrentModelLabel();
        
        // Ajusta a câmera automaticamente para o novo modelo
        if (orbitCamera != null)
        {
            orbitCamera.FrameTarget();
        }
    }

    // Reset da câmera para posição padrão
    void ResetCamera()
    {
        Debug.Log("[HUD] Reset camera button pressed");
        Debug.Log($"[HUD] orbitCamera: {(orbitCamera != null ? "OK" : "NULL")}");
        Debug.Log($"[HUD] splitView: {(splitView != null ? "OK" : "NULL")}");

        bool isInComparisonMode = false;
        if (splitView != null && splitView.compositeImage != null)
        {
            isInComparisonMode = splitView.compositeImage.gameObject.activeInHierarchy;
            Debug.Log($"[HUD] compositeImage active: {isInComparisonMode}");
        }

        if (isInComparisonMode)
        {
            Debug.Log("[HUD] In comparison mode - resetting all cameras together");
            if (splitView != null)
            {
                splitView.ResetAllComparisonCameras();
            }
            else
            {
                Debug.LogError("[HUD] splitView is null!");
            }
        }
        else
        {
            Debug.Log("[HUD] In normal mode - resetting main camera only");
            // Modo normal: apenas reset da câmera principal
            if (orbitCamera != null)
            {
                Debug.Log("[HUD] Calling orbitCamera.FrameTarget()");
                orbitCamera.ResetAngles(30f, 20f); // Reseta ângulos para padrão
                orbitCamera.FrameTarget(); // Enquadra o modelo
                Debug.Log("[HUD] orbitCamera reset completed");
            }
            else
            {
                Debug.LogError("[HUD] orbitCamera is null!");
            }
        }
    }

    // ============ MODEL SELECTOR (REMOVIDO - agora usa comparePanel) ============

    // ======== COMPARE PANEL (CHIP SYSTEM) ========

    void ShowComparePanel()
    {
        if (!comparePanel || viewer == null) 
        {
            Debug.LogError($"[HUD] ShowComparePanel failed: comparePanel={comparePanel != null}, viewer={viewer != null}");
            return;
        }

        // popula modelos
        var models = viewer.GetAllAvailableModels();
        
        dropdownModel.ClearOptions();
        dropdownModel.AddOptions(models.Count > 0 ? models : new System.Collections.Generic.List<string> { "(sem modelos)" });
        dropdownModel.interactable = models.Count > 0;
        
        // Garantir que o primeiro modelo válido seja selecionado
        if (models.Count > 0)
        {
            dropdownModel.value = 0;
            dropdownModel.RefreshShownValue();
        }
        
        dropdownModel.onValueChanged.RemoveAllListeners();
        dropdownModel.onValueChanged.AddListener(_ => BuildVariantChips());

        BuildVariantChips();
        comparePanel.SetActive(true);
        UIInputLock.Lock(this); // trava câmera com painel aberto
    }

    public void HideComparePanel()
    {
        if (comparePanel) comparePanel.SetActive(false);
        
        // Se estava em modo Compare, desativa e limpa
        if (splitView != null && splitView.compositeImage != null && 
            splitView.compositeImage.gameObject.activeInHierarchy)
        {
            Debug.Log("[HUD] Exiting Compare mode - cleaning up");
            splitView.SetCompareActive(false);
            if (compareLoader != null) compareLoader.Clear();
        }
        
        UIInputLock.Unlock(this);
    }

    void BuildVariantChips()
    {
        _selectedVariants.Clear();
        foreach (Transform c in variantChipContainer) Destroy(c.gameObject);

        if (variantChipContainer == null)
        {
            Debug.LogError("[HUD] variantChipContainer is null!");
            return;
        }

        if (variantChipPrefab == null)
        {
            Debug.LogError("[HUD] variantChipPrefab is null!");
            return;
        }

        string model = GetSelectedModelName();
        Debug.Log($"[HUD] BuildVariantChips for model: '{model}' (dropdown value: {dropdownModel.value}, options count: {dropdownModel.options.Count})");
        
        var variants = string.IsNullOrEmpty(model) || model == "(sem modelos)" ?
                       new System.Collections.Generic.List<string>() : viewer.GetAvailableVariantsPublic(model);

        Debug.Log($"[HUD] Found {variants.Count} variants for model '{model}': [{string.Join(", ", variants)}]");

        foreach (var variant in variants)
        {
            CreateVariantChip(variant);
        }
        
        Debug.Log($"[HUD] BuildVariantChips completed. Total chips created: {variants.Count}");
    }

    /// <summary>
    /// Cria um chip individual para uma variante
    /// </summary>
    /// <param name="variant">Nome da variante</param>
    private void CreateVariantChip(string variant)
    {
        // Filtrar apenas variantes válidas
        if (string.IsNullOrEmpty(variant) || variant.Contains("/") || variant.Contains("\\"))
        {
            Debug.Log($"[HUD] Skipping invalid variant: '{variant}'");
            return;
        }

        var chip = Instantiate(variantChipPrefab, variantChipContainer);
        chip.gameObject.SetActive(true);
        var label = chip.GetComponentInChildren<TMPro.TMP_Text>();
        if (label) label.SetText(variant);
        chip.isOn = false;
        chip.onValueChanged.AddListener(on => OnVariantChipChanged(variant, on));
        Debug.Log($"[HUD] Created chip for variant: '{variant}'");
    }

    void OnVariantChipChanged(string variant, bool on)
    {
        Debug.Log($"[HUD] OnVariantChipChanged: variant='{variant}', on={on}, current selected count={_selectedVariants.Count}");
        
        if (on)
        {
            if (_selectedVariants.Count >= 2)
            {
                // estourou limite → desliga imediatamente o recém-clicado
                var t = FindToggleByVariant(variant);
                if (t) t.isOn = false;
                SetLabel("Máx. 2 variantes.");
                Debug.Log($"[HUD] Rejected variant '{variant}' - already 2 selected");
                return;
            }
            if (!_selectedVariants.Contains(variant)) 
            {
                _selectedVariants.Add(variant);
                Debug.Log($"[HUD] Added variant '{variant}' to selection. Total selected: {_selectedVariants.Count}");
            }
        }
        else
        {
            _selectedVariants.Remove(variant);
            Debug.Log($"[HUD] Removed variant '{variant}' from selection. Total selected: {_selectedVariants.Count}");
        }
        
        Debug.Log($"[HUD] Current selected variants: [{string.Join(", ", _selectedVariants)}]");
    }

    Toggle FindToggleByVariant(string v)
    {
        foreach (Transform c in variantChipContainer)
        {
            var txt = c.GetComponentInChildren<TMPro.TMP_Text>();
            if (txt && txt.text == v) return c.GetComponent<Toggle>();
        }
        return null;
    }

    async Task OnCompareConfirmAsync()
    {
        Debug.Log("[HUD] OnCompareConfirmAsync started");
        
        // Verificar se já há um carregamento em andamento
        if (_isLoading)
        {
            Debug.LogWarning("[HUD] Carregamento já em andamento! Ignorando nova solicitação.");
            SetLabel("Carregamento em andamento...");
            return;
        }
        
        _isLoading = true;
        Debug.Log("[HUD] Carregamento iniciado - bloqueando novos carregamentos");
        
        // Desabilitar botão durante carregamento
        if (buttonCompareLoad) buttonCompareLoad.interactable = false;
        
        try
        {
            // Validar requisição de comparação
            if (!ValidateCompareRequest())
            {
                return;
            }

            string model = GetSelectedModelName();
            Debug.Log($"[HUD] Selected model: '{model}'");
            
            if (_selectedVariants.Count == 1)
            {
                await LoadSingleVariantAsync(model, _selectedVariants[0]);
            }
            else
            {
                await LoadComparisonModeAsync(model, _selectedVariants[0], _selectedVariants[1]);
            }
        }
        finally
        {
            _isLoading = false;
            Debug.Log("[HUD] Carregamento finalizado - liberando bloqueio");
            
            // Reabilitar botão após carregamento
            if (buttonCompareLoad) buttonCompareLoad.interactable = true;
        }
    }

    /// <summary>
    /// Valida se a requisição de comparação é válida
    /// </summary>
    /// <returns>True se válida, false caso contrário</returns>
    private bool ValidateCompareRequest()
    {
        if (viewer == null) 
        {
            Debug.LogError("[HUD] viewer is null!");
            return false;
        }

        string model = GetSelectedModelName();
        Debug.Log($"[HUD] Selected model: '{model}'");
        
        if (string.IsNullOrEmpty(model) || model == "(sem modelos)") 
        { 
            SetLabel("Sem modelos."); 
            Debug.LogError($"[HUD] No valid model selected: '{model}'");
            return false; 
        }

        Debug.Log($"[HUD] Selected variants count: {_selectedVariants.Count} - [{string.Join(", ", _selectedVariants)}]");
        
        if (_selectedVariants.Count == 0) 
        { 
            SetLabel("Selecione 1 ou 2 variantes."); 
            Debug.LogWarning("[HUD] No variants selected");
            return false; 
        }

        return true;
    }

    /// <summary>
    /// Carrega uma única variante (modo 1x)
    /// </summary>
    /// <param name="modelName">Nome do modelo</param>
    /// <param name="variant">Variante a carregar</param>
    private async Task LoadSingleVariantAsync(string modelName, string variant)
    {
        SetLabel($"Carregando {modelName} ({variant})…");
        bool ok = await viewer.LoadOnlyAsync(modelName, variant);
        if (ok)
        {
            splitView?.SetCompareActive(false);   // <- mostra a View Camera
            splitView?.ForceCleanupCameras(); // <- limpa estados para evitar travamentos
            splitView?.ClearLabels();
            NotifyModelLoaded(modelName, variant);
            SetLabel($"Atual: {modelName} ({variant})");
        }
        else
        {
            SetLabel("Falha ao carregar.");
        }
        
        HideComparePanel(); // Fecha painel apenas no modo 1x
    }

    /// <summary>
    /// Carrega duas variantes para comparação (modo 2x split)
    /// </summary>
    /// <param name="modelName">Nome do modelo</param>
    /// <param name="variantA">Primeira variante</param>
    /// <param name="variantB">Segunda variante</param>
    private async Task LoadComparisonModeAsync(string modelName, string variantA, string variantB)
    {
        Debug.Log("[HUD] Entering 2x split mode");
        
        if (compareLoader == null) 
        { 
            SetLabel("CompareLoader não ligado."); 
            Debug.LogError("[HUD] compareLoader is null!");
            HideComparePanel();
            return; 
        }
        if (splitView == null)
        {
            SetLabel("SplitView não ligado.");
            Debug.LogError("[HUD] splitView is null!");
            HideComparePanel();
            return;
        }

        Debug.Log($"[HUD] Setting up comparison: {modelName} ({variantA}) × ({variantB})");

        compareLoader.modelA = modelName; compareLoader.variantA = variantA;
        compareLoader.modelB = modelName; compareLoader.variantB = variantB;
        
        Debug.Log("[HUD] Clearing existing models before Compare mode");
        // Limpa modelos anteriores do viewer para evitar renderizar instâncias duplicadas
        if (viewer != null) 
        {
            viewer.ClearLoadedModels();
        }
        
        Debug.Log("[HUD] Activating split view and loading both models");
        splitView.SetCompareActive(true);    // <- ativa overlay + RTs
        
        // Fechar painel UI mas manter câmera travada durante carregamento
        if (comparePanel) comparePanel.SetActive(false);
        
        // Aguardar carregamento antes de destravar câmera
        await compareLoader.LoadBothAsync();
        
        SetLabel($"Preview: {modelName} ({variantA}) × ({variantB})");
        Debug.Log($"[HUD] Split mode setup completed successfully");
        
        // Agora destravar a câmera após carregamento
        UIInputLock.Unlock(this);
    }

    /// <summary>
    /// Obtém o nome do modelo selecionado no dropdown
    /// </summary>
    /// <returns>Nome do modelo ou null se nenhum selecionado</returns>
    public string GetSelectedModelName()
    {
        return dropdownModel.options.Count > 0 ? dropdownModel.options[dropdownModel.value].text : null;
    }

    /// <summary>
    /// Manipula erro de carregamento de modelo
    /// </summary>
    public void HandleModelLoadError(string modelName, string variant, string errorMessage)
    {
        Debug.Log($"[HUD] Model load error: {modelName} ({variant}) - {errorMessage}");
        
        // Mostrar mensagem de erro mais específica para o usuário
        if (errorMessage.Contains("parsing") || errorMessage.Contains("JSON"))
        {
            SetLabel($"❌ Arquivo GLTF corrompido: {modelName} ({variant})");
        }
        else if (errorMessage.Contains("não encontrado"))
        {
            SetLabel($"❌ Arquivo não encontrado: {modelName} ({variant})");
        }
        else
        {
            SetLabel($"❌ Erro ao carregar: {modelName} ({variant})");
        }
        
        // Log detalhado para debug
        Debug.LogError($"[HUD] Model load error: {modelName} ({variant}) - {errorMessage}");
    }
    
    /// <summary>
    /// Callback para o botão de relatórios
    /// </summary>
    public void OnClickReports()
    {
        if (reportsPanel != null)
        {
            bool isActive = reportsPanel.activeSelf;
            reportsPanel.SetActive(!isActive);
            
            if (!isActive)
            {
                Debug.Log("[HUD] Abrindo painel de relatórios");
                
                // Atualizar UI do painel de relatórios
                var reportsController = reportsPanel.GetComponent<ReportsPanelController>();
                if (reportsController != null)
                {
                    reportsController.RefreshUI();
                }
            }
            else
            {
                Debug.Log("[HUD] Fechando painel de relatórios");
            }
        }
        else
        {
            Debug.LogWarning("[HUD] ReportsPanel não configurado!");
        }
    }
}