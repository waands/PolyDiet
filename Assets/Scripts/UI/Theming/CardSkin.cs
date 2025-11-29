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
    [Header("Options")]
    [Tooltip("Se verdadeiro, usa a opacidade definida no tema. Se falso, preserva a opacidade atual da Image.")]
    public bool forceThemeAlpha = true;

    public void Apply(UiTheme theme) {
        var img = GetComponent<Image>();
        // Ajusta cor do card; por padrão força a opacidade do tema para evitar cards translúcidos difíceis de ler
        var color = theme.card;
        if (!forceThemeAlpha)
        {
            color.a = img.color.a;
        }
        img.color = color;
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
