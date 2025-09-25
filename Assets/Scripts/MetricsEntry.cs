using System;

[Serializable]
public class MetricsEntry
{
    public DateTime timestamp;
    public string platform, unity_version, scene, model, variant;
    public double file_mb, load_ms, mem_mb, fps_avg, fps_1pc_low;
    public bool ok;
}
