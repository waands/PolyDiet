using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Script alternativo para controlar a visibilidade do painel de métricas
/// Pode ser usado em vez do ToggleActive se houver problemas
/// </summary>
public class MetricsToggle : MonoBehaviour
{
    [Header("Painel de Métricas")]
    public GameObject metricsPanel;
    
    [Header("Opções")]
    public bool startPanelHidden = true;
    public bool enableDebug = true;
    
    private Button button;
    
    void Start()
    {
        // Pega o componente Button deste GameObject
        button = GetComponent<Button>();
        if (button == null)
        {
            if (enableDebug) Debug.LogError("[MetricsToggle] Nenhum componente Button encontrado!", this);
            return;
        }
        
        // Se metricsPanel não foi configurado no Inspector, tenta encontrar automaticamente
        if (metricsPanel == null)
        {
            metricsPanel = GameObject.Find("MetricsPanel");
            if (metricsPanel == null)
            {
                // Tenta procurar por um GameObject com MetricsViewer component
                var metricsViewer = FindObjectOfType<MetricsViewer>();
                if (metricsViewer != null)
                {
                    metricsPanel = metricsViewer.panel;
                }
            }
        }
        
        if (metricsPanel == null)
        {
            if (enableDebug) Debug.LogError("[MetricsToggle] MetricsPanel não encontrado!", this);
            return;
        }
        
        // Configura estado inicial
        if (startPanelHidden)
        {
            metricsPanel.SetActive(false);
        }
        
        // Remove todos os listeners anteriores e adiciona o nosso
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(TogglePanel);
        
        if (enableDebug)
        {
            Debug.Log($"[MetricsToggle] Configurado com sucesso! Painel: {metricsPanel.name}, Estado inicial: {metricsPanel.activeSelf}", this);
        }
    }
    
    public void TogglePanel()
    {
        if (metricsPanel == null)
        {
            if (enableDebug) Debug.LogError("[MetricsToggle] MetricsPanel é null!", this);
            return;
        }
        
        bool newState = !metricsPanel.activeSelf;
        metricsPanel.SetActive(newState);
        
        if (enableDebug)
        {
            Debug.Log($"[MetricsToggle] Painel de métricas {(newState ? "MOSTRADO" : "OCULTADO")}", this);
        }
    }
    
    public void ShowPanel()
    {
        if (metricsPanel != null)
        {
            metricsPanel.SetActive(true);
            if (enableDebug) Debug.Log("[MetricsToggle] Painel de métricas MOSTRADO", this);
        }
    }
    
    public void HidePanel()
    {
        if (metricsPanel != null)
        {
            metricsPanel.SetActive(false);
            if (enableDebug) Debug.Log("[MetricsToggle] Painel de métricas OCULTADO", this);
        }
    }
}