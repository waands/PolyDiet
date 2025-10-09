using System;

[Serializable]
public class MetricsEntry
{
    public DateTime timestamp;
    public string run_id;
    public string platform, unity_version, scene, model, variant;
    public double file_mb, load_ms, mem_mb, fps_avg, fps_1pc_low, fps_window_s;
    public bool ok;
}
