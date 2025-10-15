using UnityEngine;
public class ToggleActive : MonoBehaviour {
    public GameObject target;
    public bool lockCameraWhileActive = false; // DESABILITADO por padrão para evitar conflitos
    
    public void Toggle(){ if (target) target.SetActive(!target.activeSelf); }
    public void Show(){ if (target) target.SetActive(true); }
    public void Hide(){ if (target) target.SetActive(false); }
    
    void OnEnable()  
    { 
        if (lockCameraWhileActive && target && target.activeSelf) 
            UIInputLock.Lock(this); 
    }
    void OnDisable() 
    { 
        if (lockCameraWhileActive) 
            UIInputLock.Unlock(this); 
    }
}
