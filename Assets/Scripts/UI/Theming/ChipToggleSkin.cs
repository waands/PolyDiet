// Assets/Scripts/UI/Theming/ChipToggleSkin.cs
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using TMPro;

[RequireComponent(typeof(Toggle))]
public class ChipToggleSkin : MonoBehaviour, IThemed, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, ISelectHandler, IDeselectHandler {
    public Image bg;
    public TMP_Text label;
    [Tooltip("Optional: checkmark image shown when On. If not set, will try Toggle.graphic as Image.")]
    public Image checkmark;

    private Toggle t;
    private UnityAction<bool> cachedOnToggle;
    private Vector3 baseScale;

    private void Awake() {
    t = GetComponent<Toggle>();
    baseScale = transform.localScale;
        // Fallback: use Toggle.graphic if checkmark not assigned
        if (!checkmark && t && t.graphic is Image img)
            checkmark = img;
    }

    public void Apply(UiTheme theme) {
        if (bg) {
            if (theme.roundedSprite) { bg.sprite = theme.roundedSprite; bg.type = Image.Type.Sliced; }
            bg.color = t.isOn ? theme.chipOnBg : theme.chipOffBg;
        }
        if (label)  {
            label.color  = t.isOn ? theme.chipOnText : theme.chipOffText;
            if (theme.fontRegular) label.font = theme.fontRegular;
        }
        if (checkmark) {
            checkmark.enabled = t.isOn;
            // Checkmark color harmonized with text when on
            checkmark.color = t.isOn ? theme.chipOnText : new Color(theme.chipOnText.r, theme.chipOnText.g, theme.chipOnText.b, 0.0f);
        }

        // Make Toggle use ColorTint on its targetGraphic (bg) for visual state feedback
        if (t && t.targetGraphic == null && bg) t.targetGraphic = bg;
        if (t) t.transition = Selectable.Transition.ColorTint;
        if (t) {
            var colors = t.colors;
            colors.normalColor = t.isOn ? theme.chipOnBg : theme.chipOffBg;
            colors.highlightedColor = Color.Lerp(colors.normalColor, Color.white, 0.06f);
            colors.pressedColor = Color.Lerp(colors.normalColor, Color.black, 0.10f);
            colors.selectedColor = colors.normalColor;
            colors.disabledColor = new Color(0,0,0,0.2f);
            t.colors = colors;
        }

        // Re-apply when toggled (avoid stacking listeners)
        if (cachedOnToggle != null) t.onValueChanged.RemoveListener(cachedOnToggle);
        cachedOnToggle = _ => {
            if (bg) bg.color = t.isOn ? theme.chipOnBg : theme.chipOffBg;
            if (label) label.color = t.isOn ? theme.chipOnText : theme.chipOffText;
            if (checkmark) {
                checkmark.enabled = t.isOn;
                checkmark.color = t.isOn ? theme.chipOnText : new Color(theme.chipOnText.r, theme.chipOnText.g, theme.chipOnText.b, 0.0f);
            }
        };
        t.onValueChanged.AddListener(cachedOnToggle);
    }

    // Hover/press micro interactions
    public void OnPointerEnter(UnityEngine.EventSystems.PointerEventData e) {
        transform.localScale = baseScale * 1.02f;
        var themeField = FindObjectOfType<ThemeManager>()?.theme;
        if (bg && themeField && !t.isOn) bg.color = Color.Lerp(themeField.chipOffBg, themeField.chipHoverBg, 0.8f);
    }
    public void OnPointerExit(UnityEngine.EventSystems.PointerEventData e) {
        transform.localScale = baseScale;
        var themeField = FindObjectOfType<ThemeManager>()?.theme;
        if (bg && themeField && !t.isOn) bg.color = themeField.chipOffBg;
    }
    public void OnPointerDown(UnityEngine.EventSystems.PointerEventData e) {
        transform.localScale = baseScale * 0.98f;
        var themeField = FindObjectOfType<ThemeManager>()?.theme;
        if (bg && themeField) bg.color = t.isOn ? themeField.chipPressedBg : themeField.chipPressedBg;
    }
    public void OnPointerUp(UnityEngine.EventSystems.PointerEventData e) {
        transform.localScale = baseScale * 1.02f;
    }

    public void OnSelect(UnityEngine.EventSystems.BaseEventData e) {}
    public void OnDeselect(UnityEngine.EventSystems.BaseEventData e) {}
}
