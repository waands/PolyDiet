using UnityEngine;

public class ToggleActive : MonoBehaviour 
{
    [Header("Target")]
    public GameObject target;
    
    [Header("Debug")]
    public bool enableDebug = true;
    
    void Start()
    {
        if (enableDebug)
        {
            if (target == null)
            {
                Debug.LogError($"[ToggleActive] Target não configurado em {gameObject.name}!", this);
            }
            else
            {
                Debug.Log($"[ToggleActive] Configurado em {gameObject.name} -> Target: {target.name}", this);
            }
        }
    }
    
    public void Toggle()
    { 
        if (target == null)
        {
            if (enableDebug) Debug.LogError($"[ToggleActive] Target é null em {gameObject.name}!", this);
            return;
        }
        
        bool newState = !target.activeSelf;
        target.SetActive(newState);
        
        if (enableDebug)
        {
            Debug.Log($"[ToggleActive] {gameObject.name} -> {target.name} estado alterado para: {newState}", this);
        }
    }
    
    public void Show()
    { 
        if (target == null)
        {
            if (enableDebug) Debug.LogError($"[ToggleActive] Target é null em {gameObject.name}!", this);
            return;
        }
        
        target.SetActive(true);
        
        if (enableDebug)
        {
            Debug.Log($"[ToggleActive] {gameObject.name} -> {target.name} mostrado", this);
        }
    }
    
    public void Hide()
    { 
        if (target == null)
        {
            if (enableDebug) Debug.LogError($"[ToggleActive] Target é null em {gameObject.name}!", this);
            return;
        }
        
        target.SetActive(false);
        
        if (enableDebug)
        {
            Debug.Log($"[ToggleActive] {gameObject.name} -> {target.name} ocultado", this);
        }
    }
}
