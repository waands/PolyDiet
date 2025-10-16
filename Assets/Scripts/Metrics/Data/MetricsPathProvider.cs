using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// Provedor centralizado de caminhos para o sistema de métricas
/// </summary>
public static class MetricsPathProvider
{
    /// <summary>
    /// Obtém o diretório raiz de um modelo específico
    /// </summary>
    public static string GetModelDirectory(string modelName)
    {
        return CrossPlatformHelper.CombinePaths(Application.streamingAssetsPath, MetricsConfig.MODELS_DIR_NAME, modelName);
    }
    
    /// <summary>
    /// Obtém o diretório de benchmark de um modelo específico
    /// </summary>
    public static string GetBenchmarkDirectory(string modelName)
    {
        return CrossPlatformHelper.CombinePaths(GetModelDirectory(modelName), MetricsConfig.BENCHMARK_DIR_NAME);
    }
    
    /// <summary>
    /// Obtém o caminho completo do arquivo CSV para um modelo
    /// </summary>
    public static string GetCsvPath(string modelName)
    {
        return CrossPlatformHelper.CombinePaths(GetBenchmarkDirectory(modelName), MetricsConfig.CSV_FILENAME);
    }
    
    /// <summary>
    /// Obtém o caminho de fallback para o CSV antigo
    /// </summary>
    public static string GetFallbackCsvPath()
    {
        return CrossPlatformHelper.CombinePaths(Application.persistentDataPath, MetricsConfig.BENCHMARKS_DIR_NAME, MetricsConfig.LEGACY_CSV_FILENAME);
    }
    
    /// <summary>
    /// Obtém o diretório de saída dos relatórios (LEGACY - mantido para compatibilidade)
    /// </summary>
    [System.Obsolete("Use GetModelReportsDirectory(modelName) para salvar dentro da pasta do modelo")]
    public static string GetReportsDirectory()
    {
        return CrossPlatformHelper.CombinePaths(Application.persistentDataPath, MetricsConfig.REPORTS_DIR_NAME);
    }
    
    /// <summary>
    /// Obtém o diretório de reports de um modelo específico (dentro da pasta do modelo)
    /// </summary>
    public static string GetModelReportsDirectory(string modelName)
    {
        return CrossPlatformHelper.CombinePaths(GetModelDirectory(modelName), "reports");
    }
    
    /// <summary>
    /// Obtém o diretório de um report específico com timestamp
    /// </summary>
    public static string GetModelReportTimestampDirectory(string modelName, string timestamp = null)
    {
        if (string.IsNullOrEmpty(timestamp))
        {
            timestamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
        }
        return CrossPlatformHelper.CombinePaths(GetModelReportsDirectory(modelName), timestamp);
    }
    
    /// <summary>
    /// Obtém o diretório de report unificado para um modelo (sem timestamp)
    /// </summary>
    public static string GetModelReportUnifiedDirectory(string modelName)
    {
        return CrossPlatformHelper.CombinePaths(GetModelReportsDirectory(modelName), "latest");
    }
    
    /// <summary>
    /// Lista todos os reports de um modelo (ordenados por data, mais recente primeiro)
    /// </summary>
    public static string[] GetModelReportsList(string modelName)
    {
        var reportsDir = GetModelReportsDirectory(modelName);
        if (!Directory.Exists(reportsDir))
            return new string[0];
        
        var dirs = Directory.GetDirectories(reportsDir);
        System.Array.Sort(dirs);
        System.Array.Reverse(dirs); // Mais recente primeiro
        return dirs;
    }
    
    /// <summary>
    /// Obtém o caminho do report mais recente de um modelo (agora usa diretório unificado)
    /// </summary>
    public static string GetLatestModelReport(string modelName)
    {
        var unifiedDir = GetModelReportUnifiedDirectory(modelName);
        if (Directory.Exists(unifiedDir))
        {
            // Verificar se tem arquivos de report
            var htmlPath = CrossPlatformHelper.CombinePaths(unifiedDir, "report.html");
            var jsonPath = CrossPlatformHelper.CombinePaths(unifiedDir, "data.json");
            if (File.Exists(htmlPath) || File.Exists(jsonPath))
            {
                return unifiedDir;
            }
        }
        return null;
    }
    
    /// <summary>
    /// Obtém o diretório de modelos (StreamingAssets/Models)
    /// </summary>
    public static string GetModelsDirectory()
    {
        return CrossPlatformHelper.CombinePaths(Application.streamingAssetsPath, MetricsConfig.MODELS_DIR_NAME);
    }
    
    /// <summary>
    /// Verifica se um modelo tem dados de benchmark
    /// </summary>
    public static bool HasBenchmarkData(string modelName)
    {
        var csvPath = GetCsvPath(modelName);
        return File.Exists(csvPath);
    }
    
    /// <summary>
    /// Obtém todos os diretórios de modelos disponíveis
    /// </summary>
    public static string[] GetAllModelDirectories()
    {
        var modelsDir = GetModelsDirectory();
        if (!Directory.Exists(modelsDir))
            return new string[0];
            
        return Directory.GetDirectories(modelsDir);
    }
    
    /// <summary>
    /// Obtém todos os caminhos de CSV de modelos disponíveis
    /// </summary>
    public static string[] GetAllModelCsvPaths()
    {
        var csvPaths = new List<string>();
        var modelDirs = GetAllModelDirectories();
        
        foreach (var modelDir in modelDirs)
        {
            var modelName = Path.GetFileName(modelDir);
            var csvPath = GetCsvPath(modelName);
            
            if (File.Exists(csvPath))
            {
                csvPaths.Add(csvPath);
            }
            else
            {
                // Tenta formato legado
                var legacyPath = CrossPlatformHelper.CombinePaths(GetBenchmarkDirectory(modelName), MetricsConfig.LEGACY_CSV_FILENAME);
                if (File.Exists(legacyPath))
                {
                    csvPaths.Add(legacyPath);
                }
            }
        }
        
        return csvPaths.ToArray();
    }
    
    /// <summary>
    /// Obtém o caminho do CSV para um modelo específico
    /// </summary>
    public static string GetSingleModelCsvPath(string modelName)
    {
        var benchmarkDir = GetBenchmarkDirectory(modelName);
        
        // Tenta primeiro o nome padrão (benchmarks.csv)
        var path = CrossPlatformHelper.CombinePaths(benchmarkDir, "benchmarks.csv");
        if (File.Exists(path))
        {
            return path;
        }
        
        // Se não existir, tenta o nome alternativo (metrics.csv)
        path = CrossPlatformHelper.CombinePaths(benchmarkDir, "metrics.csv");
        if (File.Exists(path))
        {
            return path;
        }
        
        // Se nenhum dos dois existir, retorna string vazia
        return string.Empty;
    }
}
