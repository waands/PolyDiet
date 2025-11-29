// Assets/Scripts/UI/Theming/DropdownSkinTMP.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(TMP_Dropdown))]
public class DropdownSkinTMP : MonoBehaviour, IThemed {
    public Image bg;
    public Image border; // optional thin border image
    public TMP_Text label;
    public Image arrow;
    [Tooltip("Mantém a altura original do dropdown, útil para não afinar em layouts com layout groups.")]
    public bool preserveHeight = true;

    private TMP_Dropdown dd;
    private float baseHeight;

    void Awake(){
        dd = GetComponent<TMP_Dropdown>();
        var rt = GetComponent<RectTransform>();
        baseHeight = rt ? rt.rect.height : 0f;

        if (preserveHeight && rt)
        {
            var le = GetComponent<UnityEngine.UI.LayoutElement>();
            if (!le) le = gameObject.AddComponent<UnityEngine.UI.LayoutElement>();
            if (baseHeight > 0f)
            {
                le.minHeight = baseHeight;
                le.preferredHeight = baseHeight;
            }
        }
    }

    void OnDisable()
    {
        // Se o dropdown estiver aberto e o GO for desativado, fechar evita erros de ReleaseButton no IMGUI
        if (dd != null) dd.Hide();
    }

    public void Apply(UiTheme theme){
        if (bg){
            if (theme.roundedSprite){ bg.sprite = theme.roundedSprite; bg.type = Image.Type.Sliced; }
            bg.color = theme.dropdownBg;
        }
        if (border){ border.color = theme.dropdownBorder; }
        if (label){
            label.color = dd.interactable ? theme.dropdownText : theme.dropdownDisabledText;
            if (theme.fontRegular) label.font = theme.fontRegular;
        }
        if (arrow){ arrow.color = theme.dropdownArrow; }

        // Item template colors (when expanded)
        if (dd.template){
            var viewport = dd.template.GetComponentInChildren<ScrollRect>(true)?.viewport;
            if (viewport){
                var vpImg = viewport.GetComponent<Image>();
                if (vpImg) vpImg.color = theme.dropdownItemBg;
            }
            var item = dd.itemText; // reference assigned by TMP_Dropdown
            if (item){ item.color = theme.dropdownItemText; }
            var itemBg = dd.itemImage; // optional
            if (itemBg){ itemBg.color = theme.dropdownItemBg; }
        }
    }
}
