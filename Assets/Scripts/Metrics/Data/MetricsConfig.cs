using UnityEngine;

/// <summary>
/// Configurações centralizadas do sistema de métricas
/// </summary>
public static class MetricsConfig
{
    // Configurações de variantes
    public const string BASE_VARIANT = "original";
    public const string DRACO_VARIANT = "draco";
    public const string MESHOPT_VARIANT = "meshopt";
    
    // Configurações de FPS
    public const float DEFAULT_FPS_WINDOW_SECONDS = 5.0f;
    public const int DEFAULT_NUMBER_OF_TESTS = 3;
    
    // Configurações de arquivos
    public const string CSV_FILENAME = "benchmarks.csv";
    public const string LEGACY_CSV_FILENAME = "benchmarks.csv";
    
    // Configurações de formato CSV
    public const int V1_COLUMN_COUNT = 12;  // Formato antigo básico
    public const int V2_COLUMN_COUNT = 14;  // Formato antigo com run_id
    public const int V3_COLUMN_COUNT = 19;  // Formato novo com test_number e novas métricas FPS
    
    // Configurações de relatórios
    public const int DEFAULT_LAST_N = 20;
    public const string DEFAULT_PDF_ENGINE = "chrome";
    
    // Configurações de diretórios
    public const string MODELS_DIR_NAME = "Models";
    public const string BENCHMARK_DIR_NAME = "benchmark";
    public const string REPORTS_DIR_NAME = "Reports";
    public const string BENCHMARKS_DIR_NAME = "Benchmarks";
    
    // Configurações de amostras FPS
    public const int MAX_FPS_SAMPLES_IN_CSV = 50;
    public const float MIN_FRAME_DELTA_TIME = 0.001f;
    public const float MAX_FRAME_DELTA_TIME = 1.0f;
    
    // Configurações de memória
    public const float BYTES_TO_MB_DIVISOR = 1024.0f * 1024.0f;
}

