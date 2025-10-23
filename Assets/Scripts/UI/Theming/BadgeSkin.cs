// Assets/Scripts/UI/Theming/BadgeSkin.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(Image))]
public class BadgeSkin : MonoBehaviour, IThemed {
    public enum Kind { Excellent, Good, Moderate, Poor, Baseline }
    public Kind kind;
    public TMP_Text label;

    public void Apply(UiTheme theme) {
        var img = GetComponent<Image>();
        if (theme.roundedSprite) { img.sprite = theme.roundedSprite; img.type = Image.Type.Sliced; }
        Color bg = theme.badgeBaseline;
        switch (kind) {
            case Kind.Excellent: bg = theme.badgeExcellent; break;
            case Kind.Good:      bg = theme.badgeGood;      break;
            case Kind.Moderate:  bg = theme.badgeModerate;  break;
            case Kind.Poor:      bg = theme.badgePoor;      break;
        }
        img.color = bg;
        if (label) {
            label.color = Color.white;
            if (theme.fontMedium) label.font = theme.fontMedium;
        }
    }
}
