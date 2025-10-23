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

    private TMP_Dropdown dd;

    void Awake(){ dd = GetComponent<TMP_Dropdown>(); }

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
