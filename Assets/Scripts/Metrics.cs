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
    public bool saveInsideProject = true;      // <- marque no Editor p/ salvar no projeto
    public string projectSubDir   = "Benchmarks"; // vira <raiz-do-projeto>/Benchmarks


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
            // raiz do projeto (irmã da pasta Assets)
            var projectRoot = System.IO.Directory.GetParent(Application.dataPath)!.FullName;
            return System.IO.Path.Combine(projectRoot, projectSubDir);
        }
        // fallback multiplataforma (recomendado para builds)
        return System.IO.Path.Combine(Application.persistentDataPath, "Benchmarks");
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
        System.IO.Directory.CreateDirectory(dir);
        var path = System.IO.Path.Combine(dir, csvFileName);

        bool writeHeader = !System.IO.File.Exists(path);

        // 'using' precisa ABRANGER a escrita TODA:
        using (var sw = new System.IO.StreamWriter(path, append: true))
        {
            if (writeHeader)
            {
                sw.WriteLine("timestamp,platform,unity_version,scene,model,variant,file_mb,load_ms,mem_mb,fps_avg,fps_1pc_low,ok");
            }

            string ts       = DateTimeOffset.Now.ToString("yyyy-MM-ddTHH:mm:sszzz", CultureInfo.InvariantCulture);
            string platform = Application.platform.ToString();
            string unityVer = Application.unityVersion;
            string scene    = SceneManager.GetActiveScene().name; // ou: UnityEngine.SceneManagement.SceneManager...

            sw.WriteLine(string.Join(",",
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
            ));
            // sw.Flush(); // opcional; o Dispose do 'using' já faz flush
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
}
