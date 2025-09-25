using TMPro;
using UnityEngine;

public class MetricsRowUI : MonoBehaviour
{
    public TMP_Text colTime, colScene, colModel, colVariant, colFileMB, colLoadMS, colMemMB, colFpsAvg, colFpsLow, colOK;

    public void Set(MetricsEntry e)
    {
        colTime?.SetText(e.timestamp.ToString("HH:mm:ss"));
        colScene?.SetText(e.scene);
        colModel?.SetText(e.model);
        colVariant?.SetText(e.variant);
        colFileMB?.SetText(e.file_mb.ToString("0.##"));
        colLoadMS?.SetText(e.load_ms.ToString("0.#"));
        colMemMB?.SetText(e.mem_mb.ToString("0.#"));
        colFpsAvg?.SetText(e.fps_avg.ToString("0.#"));
        colFpsLow?.SetText(e.fps_1pc_low.ToString("0.#"));
        colOK?.SetText(e.ok ? "OK" : "X");
        if (colOK != null) colOK.color = e.ok ? new Color(0.2f,0.7f,0.3f) : new Color(0.85f,0.25f,0.25f);
    }
}
