using System.Linq;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
    }

    async Task QuickLoadAsync()
    {
        if (viewer == null) { Debug.LogWarning("HUD: viewer não setado."); return; }

        // Pega o modelo selecionado (ou o primeiro)
        string model = viewer.GetSelectedModelNamePublic();

        if (string.IsNullOrEmpty(model)) { SetLabel("Nenhum modelo."); return; }

        var variants = viewer.GetAvailableVariantsPublic(model);
        if (variants == null || variants.Count == 0) { SetLabel("Sem variantes."); return; }

        // Preferência: meshopt > draco > original (ajuste se quiser)
        string variant = variants.Contains("meshopt") ? "meshopt" :
                         variants.Contains("draco")   ? "draco"   :
                         variants[0];

        SetLabel($"Carregando {model} ({variant})…");
        bool ok = await viewer.LoadAsync(model, variant);
        
        if (ok)
        {
            splitView?.SetCompareActive(false); // <- mostra a View Camera
            splitView?.ForceCleanupCameras(); // <- limpa estados para evitar travamentos
            _currentLoadedModel = model;
            _currentLoadedVariant = variant;
            UpdateCurrentModelLabel();
        }
        else
        {
            SetLabel("Falha ao carregar.");
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

        string model = dropdownModel.options.Count > 0 ? dropdownModel.options[dropdownModel.value].text : null;
        Debug.Log($"[HUD] BuildVariantChips for model: '{model}' (dropdown value: {dropdownModel.value}, options count: {dropdownModel.options.Count})");
        
        var variants = string.IsNullOrEmpty(model) || model == "(sem modelos)" ?
                       new System.Collections.Generic.List<string>() : viewer.GetAvailableVariantsPublic(model);

        Debug.Log($"[HUD] Found {variants.Count} variants for model '{model}': [{string.Join(", ", variants)}]");

        foreach (var v in variants)
        {
            // Filtrar apenas variantes válidas
            if (string.IsNullOrEmpty(v) || v.Contains("/") || v.Contains("\\"))
            {
                Debug.Log($"[HUD] Skipping invalid variant: '{v}'");
                continue;
            }

            var chip = Instantiate(variantChipPrefab, variantChipContainer);
            chip.gameObject.SetActive(true);
            var label = chip.GetComponentInChildren<TMPro.TMP_Text>();
            if (label) label.SetText(v);
            chip.isOn = false;
            chip.onValueChanged.AddListener(on => OnVariantChipChanged(v, on));
            Debug.Log($"[HUD] Created chip for variant: '{v}'");
        }
        
        Debug.Log($"[HUD] BuildVariantChips completed. Total chips created: {variants.Count}");
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
            if (viewer == null) 
            {
                Debug.LogError("[HUD] viewer is null!");
                return;
            }

        string model = dropdownModel.options.Count > 0 ? dropdownModel.options[dropdownModel.value].text : null;
        Debug.Log($"[HUD] Selected model: '{model}' (dropdown value: {dropdownModel.value})");
        
        if (string.IsNullOrEmpty(model) || model == "(sem modelos)") 
        { 
            SetLabel("Sem modelos."); 
            Debug.LogError($"[HUD] No valid model selected: '{model}'");
            return; 
        }

        Debug.Log($"[HUD] Selected variants count: {_selectedVariants.Count} - [{string.Join(", ", _selectedVariants)}]");
        
        if (_selectedVariants.Count == 0) 
        { 
            SetLabel("Selecione 1 ou 2 variantes."); 
            Debug.LogWarning("[HUD] No variants selected");
            return; 
        }

        if (_selectedVariants.Count == 1)
        {
            // modo 1×
            var v = _selectedVariants[0];
            SetLabel($"Carregando {model} ({v})…");
            bool ok = await viewer.LoadOnlyAsync(model, v);
            if (ok)
            {
                splitView?.SetCompareActive(false);   // <- mostra a View Camera
                splitView?.ForceCleanupCameras(); // <- limpa estados para evitar travamentos
                splitView?.ClearLabels();
                NotifyModelLoaded(model, v);
                SetLabel($"Atual: {model} ({v})");
            }
            else
            {
                SetLabel("Falha ao carregar.");
            }
            
            HideComparePanel(); // Fecha painel apenas no modo 1x
        }
        else
        {
            // modo 2× (split)
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

            var a = _selectedVariants[0];
            var b = _selectedVariants[1];
            Debug.Log($"[HUD] Setting up comparison: {model} ({a}) × ({b})");

            compareLoader.modelA = model; compareLoader.variantA = a;
            compareLoader.modelB = model; compareLoader.variantB = b;
            
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
            
            SetLabel($"Preview: {model} ({a}) × ({b})");
            Debug.Log($"[HUD] Split mode setup completed successfully");
            
            // Agora destravar a câmera após carregamento
            UIInputLock.Unlock(this);
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
}