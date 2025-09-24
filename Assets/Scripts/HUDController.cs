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

    [Header("UI")]
    public Button buttonToggleWizard;
    public Button buttonQuickLoad;
    public TextMeshProUGUI quickLoadLabel; // opcional: mostra status
    
    [Header("Model Selector")]
    public GameObject modelSelectorPanel;
    public TMP_Dropdown modelDropdown;
    public TMP_Dropdown variantDropdown;
    public Button buttonLoadSelected;
    public Button buttonCloseSelector;

    // Estado atual do modelo renderizado
    private string _currentLoadedModel;
    private string _currentLoadedVariant;

    void Awake()
    {
        if (buttonToggleWizard) buttonToggleWizard.onClick.AddListener(() => wizard?.Toggle());
        if (buttonQuickLoad)    buttonQuickLoad.onClick.AddListener(() => _ = QuickLoadAsync());
        
        // Model selector
        if (buttonQuickLoad) buttonQuickLoad.onClick.AddListener(() => ShowModelSelector());
        if (buttonLoadSelected) buttonLoadSelected.onClick.AddListener(() => _ = LoadSelectedModelAsync());
        if (buttonCloseSelector) buttonCloseSelector.onClick.AddListener(() => HideModelSelector());
        
        // Model dropdown change
        if (modelDropdown) modelDropdown.onValueChanged.AddListener(_ => UpdateVariantDropdown());
        
        // Hide selector initially
        if (modelSelectorPanel) modelSelectorPanel.SetActive(false);
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
    }

    // ============ MODEL SELECTOR ============

    void ShowModelSelector()
    {
        if (modelSelectorPanel == null) return;
        
        // Fecha o wizard se estiver aberto
        if (wizard != null && wizard.panel != null && wizard.panel.activeSelf)
        {
            wizard.Hide();
        }
        
        PopulateModelDropdown();
        SyncWithModelViewer();
        modelSelectorPanel.SetActive(true);
    }

    public void HideModelSelector()
    {
        if (modelSelectorPanel) modelSelectorPanel.SetActive(false);
    }

    void SyncWithModelViewer()
    {
        if (viewer == null || modelDropdown == null || variantDropdown == null) return;

        // Sincroniza com o modelo atualmente selecionado no ModelViewer
        string currentModel = viewer.GetCurrentSelectedModel();
        string currentVariant = viewer.GetCurrentSelectedVariant();

        if (!string.IsNullOrEmpty(currentModel))
        {
            // Encontra o índice do modelo atual no dropdown
            for (int i = 0; i < modelDropdown.options.Count; i++)
            {
                if (modelDropdown.options[i].text == currentModel)
                {
                    modelDropdown.value = i;
                    break;
                }
            }
            
            // Atualiza as variantes
            UpdateVariantDropdown();
            
            // Sincroniza com a variante atual
            if (!string.IsNullOrEmpty(currentVariant))
            {
                for (int i = 0; i < variantDropdown.options.Count; i++)
                {
                    if (variantDropdown.options[i].text == currentVariant)
                    {
                        variantDropdown.value = i;
                        variantDropdown.RefreshShownValue();
                        break;
                    }
                }
            }
        }
    }

    void PopulateModelDropdown()
    {
        if (modelDropdown == null || viewer == null) return;

        var models = viewer.GetAllAvailableModels();
        modelDropdown.ClearOptions();
        
        if (models.Count == 0)
        {
            modelDropdown.AddOptions(new System.Collections.Generic.List<string> { "(sem modelos)" });
            modelDropdown.interactable = false;
        }
        else
        {
            modelDropdown.AddOptions(models);
            modelDropdown.interactable = true;
            UpdateVariantDropdown();
        }
    }

    void UpdateVariantDropdown()
    {
        if (variantDropdown == null || modelDropdown == null || viewer == null) return;

        string selectedModel = modelDropdown.options[modelDropdown.value].text;
        if (string.IsNullOrEmpty(selectedModel) || selectedModel == "(sem modelos)")
        {
            variantDropdown.ClearOptions();
            variantDropdown.AddOptions(new System.Collections.Generic.List<string> { "-" });
            variantDropdown.interactable = false;
            return;
        }

        var variants = viewer.GetAvailableVariantsPublic(selectedModel);
        variantDropdown.ClearOptions();
        
        if (variants.Count == 0)
        {
            variantDropdown.AddOptions(new System.Collections.Generic.List<string> { "(sem variantes)" });
            variantDropdown.interactable = false;
        }
        else
        {
            variantDropdown.AddOptions(variants);
            variantDropdown.interactable = true;
        }
    }

    async Task LoadSelectedModelAsync()
    {
        if (modelDropdown == null || variantDropdown == null || viewer == null) return;

        string model = modelDropdown.options[modelDropdown.value].text;
        string variant = variantDropdown.options[variantDropdown.value].text;

        if (string.IsNullOrEmpty(model) || model == "(sem modelos)" ||
            string.IsNullOrEmpty(variant) || variant == "(sem variantes)" || variant == "-")
        {
            SetLabel("Selecione um modelo e variante válidos.");
            return;
        }

        SetLabel($"Carregando {model} ({variant})…");
        bool ok = await viewer.LoadOnlyAsync(model, variant);
        
        if (ok)
        {
            _currentLoadedModel = model;
            _currentLoadedVariant = variant;
            UpdateCurrentModelLabel();
            
            // Sincroniza o ModelViewer com a seleção atual
            viewer.SetSelectedModel(model);
            viewer.SetSelectedVariant(variant);
        }
        else
        {
            SetLabel("Falha ao carregar.");
        }
        
        HideModelSelector();
    }
}