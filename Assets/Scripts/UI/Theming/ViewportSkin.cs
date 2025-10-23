// Assets/Scripts/UI/Theming/ViewportSkin.cs
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(ScrollRect))]
public class ViewportSkin : MonoBehaviour, IThemed {
    public Image viewportBg; // usually the Image on the Viewport GO
    public Image border;     // optional thin border image
    public Scrollbar horizontalScrollbar;
    public Scrollbar verticalScrollbar;

    private ScrollRect sr;

    void Awake(){ sr = GetComponent<ScrollRect>(); }

    public void Apply(UiTheme theme){
        if (viewportBg){
            if (theme.roundedSprite){ viewportBg.sprite = theme.roundedSprite; viewportBg.type = Image.Type.Sliced; }
            viewportBg.color = theme.viewportBg;
        }
        if (border){ border.color = theme.viewportBorder; }
        if (horizontalScrollbar) ApplyScrollbar(horizontalScrollbar, theme);
        if (verticalScrollbar) ApplyScrollbar(verticalScrollbar, theme);
    }

    private static void ApplyScrollbar(Scrollbar sb, UiTheme theme){
        if (!sb) return;
        // Track
        var trackImg = sb.GetComponent<Image>();
        if (trackImg) trackImg.color = theme.scrollbarTrack;
        // Thumb
        if (sb.handleRect){
            var thumbImg = sb.handleRect.GetComponent<Image>();
            if (thumbImg) thumbImg.color = theme.scrollbarThumb;
        }
    }
}
