// Assets/Scripts/UI/Theming/UiTheme.cs
using UnityEngine;
using TMPro;

[CreateAssetMenu(menuName="UI/Theme", fileName="DashboardTheme_Light")]
public class UiTheme : ScriptableObject {
    [Header("Base")]
    public Color bg = Hex("#FAFAFE");
    public Color text = Hex("#0B1220");   // texto quase preto para máximo contraste
    public Color muted = Hex("#3B4558");  // cinza médio (legível em fundo claro)
    // Card translúcido (~60% opacidade) para manter leveza
    public Color card = Hex("#ffffff9a");
    public Color shadow = new Color(0, 0, 0, 0.12f); // sombra sutil para separar do fundo

    [Header("Brand / Variants")]
    public Color primary = Hex("#4AA376");
    public Color original = Hex("#2563EB");
    public Color draco = Hex("#F59E0B");
    public Color meshopt = Hex("#16A34A");

    [Header("Badges")]
    public Color badgeExcellent = Hex("#28A745");
    public Color badgeGood      = Hex("#8BC34A");
    public Color badgeModerate  = Hex("#FFC107");
    public Color badgePoor      = Hex("#F44336");
    public Color badgeBaseline  = Hex("#6C757D");

    [Header("Buttons")]
    public Color btnPrimaryBg = Hex("#4AA376");
    public Color btnPrimaryText = Color.white;

    [Header("Buttons - Secondary")]
    public Color btnSecondaryBg = Hex("#E5E7EF");
    public Color btnSecondaryText = Hex("#111827");
    public Color btnSecondaryBorder = Hex("#CBD2E0");

    [Header("Buttons - Tertiary")]
    public Color btnTertiaryBg = Hex("#F8FAFD");
    public Color btnTertiaryText = Hex("#0F172A");
    public Color btnTertiaryBorder = Hex("#E2E8F0");

    [Header("Buttons - Ghost")]
    public Color btnGhostText = Hex("#4AA376");
    public Color btnGhostHoverBg = Hex("#E8F6EF");

    [Header("Buttons - Outline")]
    public Color btnOutlineText = Hex("#4AA376");
    public Color btnOutlineBorder = Hex("#4AA376");
    public Color btnOutlineHoverBg = Hex("#E8F6EF");

    [Header("Buttons - Status Variants")]
    public Color btnSuccessBg = Hex("#16A34A");
    public Color btnSuccessText = Color.white;
    public Color btnDangerBg = Hex("#DC2626");
    public Color btnDangerText = Color.white;

    [Header("Chips")]
    public Color chipOnBg    = Hex("#E6F2FF");
    public Color chipOnText  = Hex("#0B1220");
    public Color chipOffBg   = Hex("#F7F9FC");
    public Color chipOffText = Hex("#3B4558");
    public Color chipBorder  = Hex("#D5DBE7");

    [Header("Dropdowns (TMP)")]
    public Color dropdownBg = Hex("#FFFFFF");
    public Color dropdownText = Hex("#0F172A");
    public Color dropdownPlaceholder = Hex("#6B7280");
    public Color dropdownBorder = Hex("#CBD2E0");
    public Color dropdownArrow = Hex("#4B5563");
    public Color dropdownItemBg = Hex("#FFFFFF");
    public Color dropdownItemText = Hex("#0F172A");
    public Color dropdownItemHighlightBg = Hex("#E6EDF9");
    public Color dropdownItemHighlightText = Hex("#0F172A");
    public Color dropdownDisabledText = new Color(0,0,0,0.35f);

    [Header("Viewport / Scroll")] 
    public Color viewportBg = Hex("#F8FAFD");
    public Color viewportBorder = Hex("#E2E8F0");
    public Color scrollbarTrack = Hex("#EEF2F7");
    public Color scrollbarThumb = Hex("#C9D3E1");
    public Color scrollbarThumbHover = Hex("#9CA3AF");

    [Header("Chips - Interactions")]
    public Color chipHoverBg = Hex("#E0ECFF");
    public Color chipPressedBg = Hex("#D7E3F5");
    public Color chipFocusBorder = Hex("#2563EB");

    [Header("Shared")]
    public Sprite roundedSprite;   // 9-sliced arredondado
    public float cornerRadius = 12;

    [Header("Typography (TMP)")]
    public TMP_FontAsset fontRegular;
    public TMP_FontAsset fontMedium;
    public TMP_FontAsset fontBold;

    static Color Hex(string hex) { ColorUtility.TryParseHtmlString(hex, out var c); return c; }
}
