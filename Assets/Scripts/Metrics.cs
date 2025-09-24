using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Profiling;
using System.Globalization;
using UnityEngine.SceneManagement;

public class Metrics : MonoBehaviour
{
    public static Metrics Instance { get; private set; }

    [Header("Config")]
    [Tooltip("Duração (segundos) da janela para medir FPS após o carregamento.")]
    public float fpsWindowSeconds = 5f;

    [Header("Output")]
    public bool saveInsideProject = true;
    public string projectSubDir   = "Benchmarks";
    public bool upsertBySceneModelVariant = true; // <- NOVO


    [Tooltip("Nome do arquivo CSV (será salvo em persistentDataPath/Benchmarks/).")]
    public string csvFileName = "benchmarks.csv";

    // Estado corrente da medição
    string _modelName;
    string _variant;
    string _filePath;
    double _loadMs;
    double _fpsAvg;
    double _fpsP01; // 1% low
    float _memMB;
    double _fileMB;
    bool _lastLoadOk;

    readonly Stopwatch _sw = new();
    readonly List<float> _frameDt = new(4096);

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private string GetOutputDir()
    {
        if (saveInsideProject)
        {
            var projectRoot = Directory.GetParent(Application.dataPath)!.FullName;
            return Path.Combine(projectRoot, projectSubDir);
        }
        return Path.Combine(Application.persistentDataPath, "Benchmarks");
    }


    // =============== API pública ===============

    public void BeginLoad(string modelName, string variant, string filePath)
    {
        _modelName = modelName ?? "";
        _variant   = variant   ?? "";
        _filePath  = filePath  ?? "";

        _loadMs = 0;
        _fpsAvg = 0;
        _fpsP01 = 0;
        _memMB  = 0;
        _fileMB = SafeFileMB(_filePath);
        _lastLoadOk = false;

        _sw.Reset();
        _sw.Start();
    }

    public Task EndLoad(bool ok)
    {
        _sw.Stop();
        _lastLoadOk = ok;
        _loadMs = _sw.Elapsed.TotalMilliseconds;

        // memória total alocada logo após o load
        _memMB = BytesToMB(Profiler.GetTotalAllocatedMemoryLong());
        return Task.CompletedTask;
    }

    public async Task MeasureFpsWindow(float seconds)
    {
        _frameDt.Clear();
        if (!_lastLoadOk || seconds <= 0.05f) return;

        float t = 0f;
        // coleta por 'seconds' (usar unscaledDeltaTime para não ser afetado por timescale)
        while (t < seconds)
        {
            await Task.Yield();
            float dt = Time.unscaledDeltaTime;
            if (dt > 0f && dt < 1f) _frameDt.Add(dt);
            t += dt;
        }

        if (_frameDt.Count > 0)
        {
            // FPS médio = frames / tempo; equivalente a média de (1/dt)
            var fpsSamples = _frameDt.Select(dt => 1f / dt).ToArray();
            _fpsAvg = fpsSamples.Average();

            Array.Sort(fpsSamples);
            int n = fpsSamples.Length;
            int idx = Math.Max(0, (int)Math.Floor(n * 0.01) - 1); // 1% low ~ percentil 1
            _fpsP01 = fpsSamples[Math.Clamp(idx, 0, n - 1)];
        }
    }

    public void WriteCsv()
    {
        var dir  = GetOutputDir();
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, csvFileName);

        string ts       = DateTimeOffset.Now.ToString("yyyy-MM-ddTHH:mm:sszzz", CultureInfo.InvariantCulture);
        string platform = Application.platform.ToString();
        string unityVer = Application.unityVersion;
        string scene    = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;

        string header = "timestamp,platform,unity_version,scene,model,variant,file_mb,load_ms,mem_mb,fps_avg,fps_1pc_low,ok";
        string newline = string.Join(",",
            ts,
            Safe(platform),
            Safe(unityVer),
            Safe(scene),
            Safe(_modelName),
            Safe(_variant),
            _fileMB.ToString("0.###", CultureInfo.InvariantCulture),
            _loadMs.ToString("0.###", CultureInfo.InvariantCulture),
            _memMB.ToString("0.###", CultureInfo.InvariantCulture),
            _fpsAvg.ToString("0.##", CultureInfo.InvariantCulture),
            _fpsP01.ToString("0.##", CultureInfo.InvariantCulture),
            _lastLoadOk ? "true" : "false"
        );

        if (!upsertBySceneModelVariant)
        {
            bool writeHeader = !File.Exists(path);
            using var sw = new StreamWriter(path, append: true);
            if (writeHeader) sw.WriteLine(header);
            sw.WriteLine(newline);
            UnityEngine.Debug.Log($"[Metrics] CSV salvo: {path}");
            return;
        }

        // === UPSERT: substitui linhas com mesmo (scene, model, variant) ===
        var pattern = "," + Safe(scene) + "," + Safe(_modelName) + "," + Safe(_variant) + ",";
        string[] lines = File.Exists(path) ? File.ReadAllLines(path) : Array.Empty<string>();

        using (var sw = new StreamWriter(path, append: false))
        {
            if (lines.Length == 0 || (lines.Length > 0 && lines[0] != header))
                sw.WriteLine(header);

            bool replaced = false;
            for (int i = 0; i < lines.Length; i++)
            {
                if (i == 0 && lines[i] == header) continue; // pula cabeçalho antigo, já escrevemos
                var line = lines[i];
                if (!replaced && line.Contains(pattern, StringComparison.Ordinal))
                {
                    sw.WriteLine(newline);
                    replaced = true;
                }
                else
                {
                    sw.WriteLine(line);
                }
            }
            if (!replaced) sw.WriteLine(newline);
        }
        UnityEngine.Debug.Log($"[Metrics] CSV salvo: {path}");
    }

    // =============== Utils ===============

    static float BytesToMB(long bytes) => (float)(bytes / (1024.0 * 1024.0));

    static double SafeFileMB(string file)
    {
        try { var fi = new FileInfo(file); if (fi.Exists) return fi.Length / (1024.0 * 1024.0); }
        catch { /* ignore */ }
        return 0;
    }

    static string Safe(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        // sem vírgulas; CSV usa vírgula como separador
        return "\"" + s.Replace("\"", "''") + "\"";
    }

    public async Task MeasureFpsWindowWithCallback(float seconds, Action<float> onTick)
    {
        _frameDt.Clear();
        if (!_lastLoadOk || seconds <= 0.05f) return;

        float t = 0f;
        while (t < seconds)
        {
            await Task.Yield();
            float dt = Time.unscaledDeltaTime;
            if (dt > 0f && dt < 1f) _frameDt.Add(dt);
            t += dt;

            onTick?.Invoke(Mathf.Max(0f, seconds - t));
        }

        if (_frameDt.Count > 0)
        {
            var fps = _frameDt.Select(d => 1f / d).ToArray();
            _fpsAvg = fps.Average();

            Array.Sort(fps);
            int n = fps.Length;
            int idx = Mathf.Clamp(Mathf.FloorToInt(n * 0.01f) - 1, 0, n - 1); // 1% low
            _fpsP01 = fps[idx];
        }
    }

}
