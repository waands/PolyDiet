// Assets/Scripts/UI/Theming/ThemeManager.cs
using UnityEngine;

public interface IThemed { void Apply(UiTheme theme); }

public class ThemeManager : MonoBehaviour {
    [Tooltip("Theme asset to apply across the scene")]
    public UiTheme theme;

    private void Awake() {
        if (theme == null) {
            Debug.LogWarning("ThemeManager: nenhum UiTheme");
            return;
        }

        // Apply to all active and inactive objects in the scene
        var behaviours = FindObjectsOfType<MonoBehaviour>(true);
        foreach (var b in behaviours) {
            if (b is IThemed sk) {
                sk.Apply(theme);
            }
        }

        // Optional: set global background color
        var cam = Camera.main;
        if (cam) cam.backgroundColor = theme.bg;
    }
}
