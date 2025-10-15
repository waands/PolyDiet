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
    public float fpsWindowSeconds = MetricsConfig.DEFAULT_FPS_WINDOW_SECONDS;
    [Tooltip("Número de testes a executar por modelo/variante.")]
    public int numberOfTests = MetricsConfig.DEFAULT_NUMBER_OF_TESTS;

    [Header("Output")]
    [Tooltip("Salvar dentro de StreamingAssets/Models/{modelo}/benchmark/")]
    public bool saveInModelDirectory = true;
    [Tooltip("Nome do arquivo CSV dentro da pasta benchmark do modelo.")]
    public string csvFileName = MetricsConfig.CSV_FILENAME;

    // Estado corrente da medição
    string _modelName;
    string _variant;
    string _filePath;
    double _loadMs;
    double _fpsAvg;
    double _fpsMin;
    double _fpsMax;
    double _fpsMedian;
    double _fpsP01; // 1% low
    float _memMB;
    double _fileMB;
    bool _lastLoadOk;
    int _testNumber;
    
    // Run ID para agrupar execuções da mesma sessão
    string _runId;

    readonly Stopwatch _sw = new();
    readonly List<float> _frameDt = new(4096);

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        // Gera run_id único para esta sessão
        _runId = DateTimeOffset.Now.ToString("yyyyMMdd_HHmmss");

        // garante que está na raiz antes de DontDestroyOnLoad
        if (transform.parent != null) transform.SetParent(null);
        DontDestroyOnLoad(gameObject);
    }

    public string GetOutputDir()
    {
        if (saveInModelDirectory && !string.IsNullOrEmpty(_modelName))
        {
            return MetricsPathProvider.GetBenchmarkDirectory(_modelName);
        }
        else
        {
            return CrossPlatformHelper.CombinePaths(Application.persistentDataPath, MetricsConfig.BENCHMARKS_DIR_NAME);
        }
    }

    public string GetCsvPathPublic()
    {
        var dir = GetOutputDir();
        CrossPlatformHelper.EnsureDirectoryExists(dir);
        return CrossPlatformHelper.CombinePaths(dir, csvFileName);
    }


    // =============== API pública ===============

    public void BeginLoad(string modelName, string variant, string filePath, int testNumber = 1)
    {
        _modelName = modelName ?? "";
        _variant   = variant   ?? "";
        _filePath  = filePath  ?? "";
        _testNumber = testNumber;

        _loadMs = 0;
        _fpsAvg = 0;
        _fpsMin = 0;
        _fpsMax = 0;
        _fpsMedian = 0;
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
            if (dt > MetricsConfig.MIN_FRAME_DELTA_TIME && dt < MetricsConfig.MAX_FRAME_DELTA_TIME) 
                _frameDt.Add(dt);
            t += dt;
        }

        CalculateFpsStatistics();
    }

    public void WriteCsv()
    {
        var dir  = GetOutputDir();
        CrossPlatformHelper.EnsureDirectoryExists(dir);
        var path = CrossPlatformHelper.CombinePaths(dir, csvFileName);

        string ts       = DateTimeOffset.Now.ToString("yyyy-MM-ddTHH:mm:sszzz", CultureInfo.InvariantCulture);
        string platform = Application.platform.ToString();
        string unityVer = Application.unityVersion;
        string scene    = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;

        // Criar string com amostras de FPS (limitado para não tornar o CSV muito grande)
        string fpsSamplesStr = "";
        if (_frameDt.Count > 0)
        {
            var fpsSamples = _frameDt.Select(dt => 1f / dt).Take(MetricsConfig.MAX_FPS_SAMPLES_IN_CSV).ToArray();
            fpsSamplesStr = string.Join(";", fpsSamples.Select(f => f.ToString("0.##", CultureInfo.InvariantCulture)));
        }

        string header = "timestamp,run_id,test_number,platform,unity_version,scene,model,variant,file_mb,load_ms,mem_mb,fps_avg,fps_min,fps_max,fps_median,fps_1pc_low,fps_samples,fps_window_s,ok";
        string newline = string.Join(",",
            ts,
            Safe(_runId),
            _testNumber.ToString(),
            Safe(platform),
            Safe(unityVer),
            Safe(scene),
            Safe(_modelName),
            Safe(_variant),
            _fileMB.ToString("0.###", CultureInfo.InvariantCulture),
            _loadMs.ToString("0.###", CultureInfo.InvariantCulture),
            _memMB.ToString("0.###", CultureInfo.InvariantCulture),
            _fpsAvg.ToString("0.##", CultureInfo.InvariantCulture),
            _fpsMin.ToString("0.##", CultureInfo.InvariantCulture),
            _fpsMax.ToString("0.##", CultureInfo.InvariantCulture),
            _fpsMedian.ToString("0.##", CultureInfo.InvariantCulture),
            _fpsP01.ToString("0.##", CultureInfo.InvariantCulture),
            Safe(fpsSamplesStr),
            fpsWindowSeconds.ToString("0.##", CultureInfo.InvariantCulture),
            _lastLoadOk ? "true" : "false"
        );

        // Modo append: sempre adicionar nova linha
        bool writeHeader = !File.Exists(path);
        using var sw = new StreamWriter(path, append: true, System.Text.Encoding.UTF8);
        if (writeHeader) sw.WriteLine(header);
        sw.WriteLine(newline);
        UnityEngine.Debug.Log($"[Metrics] CSV salvo: {path}");
    }

    // =============== Utils ===============

    static float BytesToMB(long bytes) => (float)(bytes / MetricsConfig.BYTES_TO_MB_DIVISOR);

    static double SafeFileMB(string file)
    {
        try 
        { 
            var fi = new FileInfo(file); 
            if (fi.Exists) return fi.Length / MetricsConfig.BYTES_TO_MB_DIVISOR; 
        }
        catch (System.Exception ex) 
        { 
            UnityEngine.Debug.LogError($"[Metrics] Erro ao obter tamanho do arquivo {file}: {ex.Message}");
        }
        return 0;
    }

    static string Safe(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        // Escape correto de aspas em CSV: substituir " por ""
        return "\"" + s.Replace("\"", "\"\"") + "\"";
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
            if (dt > MetricsConfig.MIN_FRAME_DELTA_TIME && dt < MetricsConfig.MAX_FRAME_DELTA_TIME) 
                _frameDt.Add(dt);
            t += dt;

            onTick?.Invoke(Mathf.Max(0f, seconds - t));
        }

        CalculateFpsStatistics();
    }
    
    /// <summary>
    /// Calcula estatísticas de FPS a partir dos dados coletados em _frameDt
    /// </summary>
    private void CalculateFpsStatistics()
    {
        if (_frameDt.Count == 0) return;
        
        var fpsSamples = _frameDt.Select(dt => 1f / dt).ToArray();
        _fpsAvg = fpsSamples.Average();
        _fpsMin = fpsSamples.Min();
        _fpsMax = fpsSamples.Max();

        // Calcular mediana
        Array.Sort(fpsSamples);
        int n = fpsSamples.Length;
        if (n % 2 == 0)
            _fpsMedian = (fpsSamples[n / 2 - 1] + fpsSamples[n / 2]) / 2.0;
        else
            _fpsMedian = fpsSamples[n / 2];

        // 1% low
        int idx = Math.Max(0, (int)Math.Floor(n * 0.01) - 1);
        _fpsP01 = fpsSamples[Math.Clamp(idx, 0, n - 1)];
    }

}
