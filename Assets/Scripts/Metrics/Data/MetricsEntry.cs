using System;

[Serializable]
public class MetricsEntry
{
    public DateTime timestamp;
    public string run_id;
    public int test_number;
    public string platform, unity_version, scene, model, variant;
    public double file_mb, load_ms, mem_mb, fps_avg, fps_min, fps_max, fps_median, fps_1pc_low, fps_window_s;
    public string fps_samples; // Amostras de FPS como string separada por ';'
    public bool ok;
}
