using UnityEngine;

public class ToggleDebug : MonoBehaviour
{
    void Start()
    {
        // Procurar pelo ButtonMetrics
        var buttonMetrics = GameObject.Find("ButtonMetrics");
        if (buttonMetrics != null)
        {
            Debug.Log("✅ ButtonMetrics encontrado");
            
            var toggleActive = buttonMetrics.GetComponent<ToggleActive>();
            if (toggleActive != null)
            {
                Debug.Log("✅ ToggleActive component encontrado");
                if (toggleActive.target != null)
                {
                    Debug.Log($"✅ Target configurado: {toggleActive.target.name}");
                    Debug.Log($"Estado inicial do target: {toggleActive.target.activeSelf}");
                }
                else
                {
                    Debug.LogError("❌ Target não configurado no ToggleActive!");
                }
            }
            else
            {
                Debug.LogError("❌ ToggleActive component não encontrado!");
            }
            
            var button = buttonMetrics.GetComponent<UnityEngine.UI.Button>();
            if (button != null)
            {
                Debug.Log($"✅ Button component encontrado. Interactable: {button.interactable}");
                Debug.Log($"Número de listeners: {button.onClick.GetPersistentEventCount()}");
            }
        }
        else
        {
            Debug.LogError("❌ ButtonMetrics não encontrado na scene!");
        }
        
        // Procurar pelo MetricsPanel
        var metricsPanel = GameObject.Find("MetricsPanel");
        if (metricsPanel != null)
        {
            Debug.Log($"✅ MetricsPanel encontrado. Ativo: {metricsPanel.activeSelf}");
        }
        else
        {
            Debug.LogError("❌ MetricsPanel não encontrado!");
        }
    }
}