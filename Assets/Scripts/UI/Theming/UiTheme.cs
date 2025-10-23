// Assets/Scripts/UI/Theming/UiTheme.cs
using UnityEngine;
using TMPro;

[CreateAssetMenu(menuName="UI/Theme", fileName="DashboardTheme_Light")]
public class UiTheme : ScriptableObject {
    [Header("Base")]
    public Color bg = Hex("#FAFAFE");
    public Color text = Hex("#1F1F29");
    public Color muted = Hex("#6B7280");
    public Color card = Color.white;
    public Color shadow = new Color(0,0,0,0.12f);

    [Header("Brand / Variants")]
    public Color primary = Hex("#4CAF50");
    public Color original = Hex("#2196F3");
    public Color draco = Hex("#FF9800");
    public Color meshopt = Hex("#4CAF50");

    [Header("Badges")]
    public Color badgeExcellent = Hex("#28A745");
    public Color badgeGood      = Hex("#8BC34A");
    public Color badgeModerate  = Hex("#FFC107");
    public Color badgePoor      = Hex("#F44336");
    public Color badgeBaseline  = Hex("#6C757D");

    [Header("Buttons")]
    public Color btnPrimaryBg = Hex("#4CAF50");
    public Color btnPrimaryText = Color.white;

    [Header("Buttons - Secondary")]
    public Color btnSecondaryBg = Hex("#F3F4F6");
    public Color btnSecondaryText = Hex("#1F2937");
    public Color btnSecondaryBorder = Hex("#D1D5DB");

    [Header("Buttons - Tertiary")]
    public Color btnTertiaryBg = Color.white;
    public Color btnTertiaryText = Hex("#1F2937");
    public Color btnTertiaryBorder = Hex("#E5E7EB");

    [Header("Buttons - Ghost")]
    public Color btnGhostText = Hex("#4CAF50");
    public Color btnGhostHoverBg = Hex("#E8F5E9");

    [Header("Buttons - Outline")]
    public Color btnOutlineText = Hex("#4CAF50");
    public Color btnOutlineBorder = Hex("#4CAF50");
    public Color btnOutlineHoverBg = Hex("#E8F5E9");

    [Header("Buttons - Status Variants")]
    public Color btnSuccessBg = Hex("#22C55E");
    public Color btnSuccessText = Color.white;
    public Color btnDangerBg = Hex("#EF4444");
    public Color btnDangerText = Color.white;

    [Header("Chips")]
    public Color chipOnBg    = Hex("#E8F5E9");
    public Color chipOnText  = Hex("#2C3E50");
    public Color chipOffBg   = Hex("#F3F4F6");
    public Color chipOffText = Hex("#6B7280");
    public Color chipBorder  = Hex("#D1D5DB");

    [Header("Dropdowns (TMP)")]
    public Color dropdownBg = Color.white;
    public Color dropdownText = Hex("#1F1F29");
    public Color dropdownPlaceholder = Hex("#6B7280");
    public Color dropdownBorder = Hex("#D1D5DB");
    public Color dropdownArrow = Hex("#6B7280");
    public Color dropdownItemBg = Color.white;
    public Color dropdownItemText = Hex("#1F1F29");
    public Color dropdownItemHighlightBg = Hex("#E5E7EB");
    public Color dropdownItemHighlightText = Hex("#1F1F29");
    public Color dropdownDisabledText = new Color(0,0,0,0.3f);

    [Header("Viewport / Scroll")] 
    public Color viewportBg = Hex("#FFFFFF");
    public Color viewportBorder = Hex("#E5E7EB");
    public Color scrollbarTrack = Hex("#F3F4F6");
    public Color scrollbarThumb = Hex("#D1D5DB");
    public Color scrollbarThumbHover = Hex("#9CA3AF");

    [Header("Chips - Interactions")]
    public Color chipHoverBg = Hex("#EEF2FF");
    public Color chipPressedBg = Hex("#E5E7EB");
    public Color chipFocusBorder = Hex("#4CAF50");

    [Header("Shared")]
    public Sprite roundedSprite;   // 9-sliced arredondado
    public float cornerRadius = 12;

    [Header("Typography (TMP)")]
    public TMP_FontAsset fontRegular;
    public TMP_FontAsset fontMedium;
    public TMP_FontAsset fontBold;

    static Color Hex(string hex) { ColorUtility.TryParseHtmlString(hex, out var c); return c; }
}
