using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MetricsCardUI : MonoBehaviour
{
    [Header("Header")]
    public TMP_Text title;
    public TMP_Text badgeOK;      // mostra "OK"/"X"
    public Image    badgeBg;      // opcional: pill colorido

    [Header("Valores")]
    public TMP_Text valFileMB, valLoadMS, valMemMB, valFpsAvg, valFpsLow, valPlatform, valUnity;

    public void Set(MetricsEntry e)
    {
        if (title) title.SetText($"{e.timestamp:HH:mm:ss} — {e.scene}/{e.model} ({e.variant})");

        if (badgeOK) badgeOK.SetText(e.ok ? "OK" : "X");
        if (badgeBg)
        {
            var col = e.ok ? new Color(0.18f,0.75f,0.44f) : new Color(0.84f,0.29f,0.29f);
            badgeBg.color = col;
        }

        if (valFileMB) valFileMB.SetText(e.file_mb.ToString("0.##"));
        if (valLoadMS) valLoadMS.SetText(e.load_ms.ToString("0.#"));
        if (valMemMB)  valMemMB.SetText(e.mem_mb.ToString("0.#"));
        if (valFpsAvg) valFpsAvg.SetText(e.fps_avg.ToString("0.#"));
        if (valFpsLow) valFpsLow.SetText(e.fps_1pc_low.ToString("0.#"));

        if (valPlatform) valPlatform.SetText(e.platform);
        if (valUnity)    valUnity.SetText(e.unity_version);
    }
}
