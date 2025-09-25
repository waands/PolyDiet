using UnityEngine;
public class ToggleActive : MonoBehaviour {
    public GameObject target;
    public void Toggle(){ if (target) target.SetActive(!target.activeSelf); }
    public void Show(){ if (target) target.SetActive(true); }
    public void Hide(){ if (target) target.SetActive(false); }
}
