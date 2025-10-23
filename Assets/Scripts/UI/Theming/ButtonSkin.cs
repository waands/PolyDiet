// Assets/Scripts/UI/Theming/ButtonSkin.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Button))]
[RequireComponent(typeof(Image))]
public class ButtonSkin : MonoBehaviour, IThemed, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler {
    public enum Style { Primary, Secondary, Tertiary, Ghost, Outline, Success, Danger }
    [Header("Style")]
    public Style style = Style.Primary;
    public TMP_Text label;

    private Image img; 
    private Button btn; 
    private Vector3 baseScale;

    private void Awake() {
        img = GetComponent<Image>();
        btn = GetComponent<Button>();
        baseScale = transform.localScale;
        // Ensure the button uses this Image as targetGraphic to receive color transitions
        if (btn && btn.targetGraphic != img) btn.targetGraphic = img;
    }

    private void OnEnable() {
        // If a ThemeManager exists, prefer applying themed colors; otherwise fallback to computed tints
        var theme = FindObjectOfType<ThemeManager>()?.theme;
        if (theme) {
            Apply(theme);
        } else {
            ApplyFallback();
        }
    }

    public void Apply(UiTheme theme) {
        if (!img) img = GetComponent<Image>();
        if (!btn) btn = GetComponent<Button>();

        if (theme.roundedSprite) {
            img.sprite = theme.roundedSprite;
            img.type = Image.Type.Sliced;
        }

    var cb = btn.colors;
        switch (style) {
            case Style.Primary:
                img.color = theme.btnPrimaryBg;
                if (label) label.color = theme.btnPrimaryText;
                cb.normalColor = theme.btnPrimaryBg;
                cb.highlightedColor = Color.Lerp(theme.btnPrimaryBg, Color.white, 0.06f);
                cb.pressedColor = Color.Lerp(theme.btnPrimaryBg, Color.black, 0.12f);
                break;
            case Style.Secondary:
                img.color = theme.btnSecondaryBg;
                if (label) label.color = theme.btnSecondaryText;
                cb.normalColor = theme.btnSecondaryBg;
                cb.highlightedColor = Color.Lerp(theme.btnSecondaryBg, Color.white, 0.08f);
                cb.pressedColor = Color.Lerp(theme.btnSecondaryBg, Color.black, 0.12f);
                break;
            case Style.Tertiary:
                img.color = theme.btnTertiaryBg;
                if (label) label.color = theme.btnTertiaryText;
                cb.normalColor = theme.btnTertiaryBg;
                cb.highlightedColor = Color.Lerp(theme.btnTertiaryBg, Color.black, 0.04f);
                cb.pressedColor = Color.Lerp(theme.btnTertiaryBg, Color.black, 0.10f);
                break;
            case Style.Ghost:
                img.color = Color.clear;
                if (label) label.color = theme.btnGhostText;
                cb.normalColor = new Color(theme.btnGhostHoverBg.r, theme.btnGhostHoverBg.g, theme.btnGhostHoverBg.b, 0f);
                cb.highlightedColor = theme.btnGhostHoverBg;
                cb.pressedColor = Color.Lerp(theme.btnGhostHoverBg, Color.black, 0.08f);
                break;
            case Style.Outline:
                img.color = Color.clear;
                if (label) label.color = theme.btnOutlineText;
                cb.normalColor = new Color(theme.btnOutlineHoverBg.r, theme.btnOutlineHoverBg.g, theme.btnOutlineHoverBg.b, 0f);
                cb.highlightedColor = theme.btnOutlineHoverBg;
                cb.pressedColor = Color.Lerp(theme.btnOutlineHoverBg, Color.black, 0.08f);
                break;
            case Style.Success:
                img.color = theme.btnSuccessBg;
                if (label) label.color = theme.btnSuccessText;
                cb.normalColor = theme.btnSuccessBg;
                cb.highlightedColor = Color.Lerp(theme.btnSuccessBg, Color.white, 0.06f);
                cb.pressedColor = Color.Lerp(theme.btnSuccessBg, Color.black, 0.12f);
                break;
            case Style.Danger:
                img.color = theme.btnDangerBg;
                if (label) label.color = theme.btnDangerText;
                cb.normalColor = theme.btnDangerBg;
                cb.highlightedColor = Color.Lerp(theme.btnDangerBg, Color.white, 0.06f);
                cb.pressedColor = Color.Lerp(theme.btnDangerBg, Color.black, 0.12f);
                break;
        }
        cb.selectedColor = cb.normalColor;
        cb.disabledColor = new Color(0,0,0,0.25f);
        btn.colors = cb;
        btn.transition = Selectable.Transition.ColorTint;

        // Typography
        if (label) {
            if (theme.fontMedium) label.font = theme.fontMedium;
            else if (theme.fontRegular) label.font = theme.fontRegular;
        }
    }

    // Fallback when no ThemeManager/theme is present: derive highlight/pressed from current background
    private void ApplyFallback() {
        if (!img || !btn) return;
        var bg = img.color;
        var cb = btn.colors;
        cb.normalColor = bg;
        cb.highlightedColor = Color.Lerp(bg, Color.white, 0.06f);
        cb.pressedColor = Color.Lerp(bg, Color.black, 0.12f);
        cb.selectedColor = cb.normalColor;
        cb.disabledColor = new Color(0,0,0,0.25f);
        btn.colors = cb;
        btn.transition = Selectable.Transition.ColorTint;
    }

    // micro-hover/press animation via scale
    public void OnPointerEnter(PointerEventData e) => transform.localScale = baseScale * 1.02f;
    public void OnPointerExit (PointerEventData e) => transform.localScale = baseScale;
    public void OnPointerDown (PointerEventData e) => transform.localScale = baseScale * 0.98f;
    public void OnPointerUp   (PointerEventData e) => transform.localScale = baseScale * 1.02f;
}
