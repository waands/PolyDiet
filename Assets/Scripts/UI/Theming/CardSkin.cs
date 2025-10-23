// Assets/Scripts/UI/Theming/CardSkin.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(Image))]
public class CardSkin : MonoBehaviour, IThemed {
    [Header("References")]
    public TMP_Text title;     // .title
    public TMP_Text value;     // .value or .value-large
    public TMP_Text subtitle;  // .subtitle

    public void Apply(UiTheme theme) {
        var img = GetComponent<Image>();
        img.color = theme.card;
        if (theme.roundedSprite) {
            img.sprite = theme.roundedSprite;
            img.type = Image.Type.Sliced;
        }

        var shadow = GetComponent<Shadow>();
        if (!shadow) shadow = gameObject.AddComponent<Shadow>();
        shadow.effectColor = theme.shadow;
        shadow.effectDistance = new Vector2(0, 6);

        if (title)   title.color    = theme.muted;
        if (value)   value.color    = theme.text;
        if (subtitle)subtitle.color = theme.muted;

        // Typography
        if (title && theme.fontMedium) title.font = theme.fontMedium;
        if (value && theme.fontBold) value.font = theme.fontBold;
        if (subtitle && theme.fontRegular) subtitle.font = theme.fontRegular;
    }
}
