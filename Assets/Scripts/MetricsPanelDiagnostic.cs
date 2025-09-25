using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Script para diagnosticar problemas com o toggle do painel de métricas
/// Adicione este script temporariamente a qualquer GameObject para verificar o status
/// </summary>
public class MetricsPanelDiagnostic : MonoBehaviour
{
    [Header("Executar diagnóstico")]
    [SerializeField] private bool runDiagnosticOnStart = true;
    
    void Start()
    {
        if (runDiagnosticOnStart)
        {
            Invoke(nameof(RunDiagnostic), 1f); // Espera 1 segundo para outros scripts inicializarem
        }
    }
    
    [ContextMenu("Executar Diagnóstico")]
    public void RunDiagnostic()
    {
        Debug.Log("=== DIAGNÓSTICO DO PAINEL DE MÉTRICAS ===");
        
        // 1. Procurar ButtonMetrics
        var buttonMetrics = GameObject.Find("ButtonMetrics");
        if (buttonMetrics != null)
        {
            Debug.Log("✅ ButtonMetrics encontrado");
            DiagnosticButton(buttonMetrics);
        }
        else
        {
            Debug.LogError("❌ ButtonMetrics não encontrado na scene!");
            
            // Procurar todos os botões que podem ser o de métricas
            var allButtons = FindObjectsOfType<Button>();
            Debug.Log($"Total de botões na scene: {allButtons.Length}");
            foreach (var btn in allButtons)
            {
                if (btn.name.ToLower().Contains("metric"))
                {
                    Debug.Log($"  Possível botão de métricas: {btn.name}");
                    DiagnosticButton(btn.gameObject);
                }
            }
        }
        
        // 2. Procurar MetricsPanel
        var metricsPanel = GameObject.Find("MetricsPanel");
        if (metricsPanel != null)
        {
            Debug.Log($"✅ MetricsPanel encontrado. Ativo: {metricsPanel.activeSelf}");
        }
        else
        {
            Debug.LogError("❌ MetricsPanel não encontrado!");
            
            // Procurar por MetricsViewer
            var metricsViewer = FindObjectOfType<MetricsViewer>();
            if (metricsViewer != null)
            {
                Debug.Log($"✅ MetricsViewer encontrado em: {metricsViewer.name}");
                if (metricsViewer.panel != null)
                {
                    Debug.Log($"  Painel configurado: {metricsViewer.panel.name}, Ativo: {metricsViewer.panel.activeSelf}");
                }
            }
        }
        
        Debug.Log("=== FIM DO DIAGNÓSTICO ===");
    }
    
    private void DiagnosticButton(GameObject buttonObj)
    {
        var button = buttonObj.GetComponent<Button>();
        if (button != null)
        {
            Debug.Log($"  Button component: ✅ Interactable: {button.interactable}");
            Debug.Log($"  OnClick listeners: {button.onClick.GetPersistentEventCount()}");
            
            for (int i = 0; i < button.onClick.GetPersistentEventCount(); i++)
            {
                var target = button.onClick.GetPersistentTarget(i);
                var methodName = button.onClick.GetPersistentMethodName(i);
                Debug.Log($"    Listener {i}: {target?.name}.{methodName}");
            }
        }
        
        var toggleActive = buttonObj.GetComponent<ToggleActive>();
        if (toggleActive != null)
        {
            Debug.Log("  ToggleActive component: ✅");
            if (toggleActive.target != null)
            {
                Debug.Log($"    Target: {toggleActive.target.name}, Ativo: {toggleActive.target.activeSelf}");
            }
            else
            {
                Debug.LogError("    Target não configurado!");
            }
        }
        
        var metricsToggle = buttonObj.GetComponent<MetricsToggle>();
        if (metricsToggle != null)
        {
            Debug.Log("  MetricsToggle component: ✅");
        }
    }
    
    [ContextMenu("Testar Toggle Manual")]
    public void TestToggleManual()
    {
        var metricsPanel = GameObject.Find("MetricsPanel");
        if (metricsPanel == null)
        {
            var metricsViewer = FindObjectOfType<MetricsViewer>();
            if (metricsViewer != null && metricsViewer.panel != null)
            {
                metricsPanel = metricsViewer.panel;
            }
        }
        
        if (metricsPanel != null)
        {
            bool newState = !metricsPanel.activeSelf;
            metricsPanel.SetActive(newState);
            Debug.Log($"[TESTE MANUAL] Painel alterado para: {newState}");
        }
        else
        {
            Debug.LogError("[TESTE MANUAL] MetricsPanel não encontrado!");
        }
    }
}