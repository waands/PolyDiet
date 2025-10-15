using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEngine;

public static class MetricsStore
{
    public static List<MetricsEntry> Load(string csvPath)
    {
        var list = new List<MetricsEntry>();
        if (string.IsNullOrEmpty(csvPath) || !File.Exists(csvPath)) 
        {
            Debug.LogWarning($"[MetricsStore] Arquivo CSV não encontrado: {csvPath}");
            return list;
        }

        try
        {
            var lines = File.ReadAllLines(csvPath);
            bool hasHeader = false;

            foreach (var raw in lines)
            {
                var line = raw.Trim();
                if (string.IsNullOrEmpty(line)) continue;

                if (!hasHeader && line.StartsWith("timestamp,"))
                {
                    hasHeader = true;
                    continue;
                }

                var cols = SplitCsv(line);
                if (cols.Length < MetricsConfig.V1_COLUMN_COUNT) 
                {
                    Debug.LogWarning($"[MetricsStore] Linha ignorada - colunas insuficientes: {cols.Length}");
                    continue;
                }
                
                var entry = ParseCsvEntry(cols);
                if (entry != null)
                {
                    list.Add(entry);
                }
            }
            
            list.Sort((a,b) => b.timestamp.CompareTo(a.timestamp)); // mais recentes primeiro
            Debug.Log($"[MetricsStore] Carregadas {list.Count} entradas de {csvPath}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[MetricsStore] Erro ao carregar CSV {csvPath}: {ex.Message}");
        }
        
        return list;
    }

    /// <summary>
    /// Carrega dados de todos os CSVs de modelos na nova estrutura
    /// </summary>
    public static List<MetricsEntry> LoadAllModels()
    {
        var allEntries = new List<MetricsEntry>();
        var csvPaths = MetricsPathProvider.GetAllModelCsvPaths();
        
        Debug.Log($"[MetricsStore] Encontrados {csvPaths.Length} arquivos CSV de modelos");
        
        foreach (var csvPath in csvPaths)
        {
            var modelEntries = Load(csvPath);
            if (modelEntries.Count > 0)
            {
                allEntries.AddRange(modelEntries);
                var modelName = Path.GetFileName(Path.GetDirectoryName(Path.GetDirectoryName(csvPath)));
                Debug.Log($"[MetricsStore] ✓ Carregados {modelEntries.Count} entradas do modelo {modelName}");
            }
        }
        
        // Fallback: tentar carregar do CSV antigo se não houver dados novos
        if (allEntries.Count == 0)
        {
            Debug.Log("[MetricsStore] Nenhum dado novo encontrado, tentando fallback...");
            return LoadFallbackData();
        }
        
        Debug.Log($"[MetricsStore] Total: {allEntries.Count} entradas de {allEntries.GroupBy(e => e.model).Count()} modelos");
        return allEntries;
    }
    
    /// <summary>
    /// Carrega dados do CSV antigo como fallback
    /// </summary>
    private static List<MetricsEntry> LoadFallbackData()
    {
        var fallbackPath = MetricsPathProvider.GetFallbackCsvPath();
        if (File.Exists(fallbackPath))
        {
            var entries = Load(fallbackPath);
            Debug.Log($"[MetricsStore] Carregadas {entries.Count} entradas do CSV antigo");
            return entries;
        }
        
        Debug.Log("[MetricsStore] Nenhum dado de fallback encontrado");
        return new List<MetricsEntry>();
    }

    public static List<string> Models(List<MetricsEntry> all) =>
        all.Select(e => e.model).Where(s => !string.IsNullOrEmpty(s)).Distinct().OrderBy(s => s).ToList();

    public static List<string> Variants(List<MetricsEntry> all, string model) =>
        all.Where(e => e.model == model).Select(e => e.variant)
            .Where(s => !string.IsNullOrEmpty(s)).Distinct()
            .OrderBy(v => v == MetricsConfig.BASE_VARIANT ? "0" : v).ToList();

    public static List<MetricsEntry> Filter(List<MetricsEntry> all, string model, string variant) =>
        all.Where(e => (string.IsNullOrEmpty(model) || e.model == model) &&
                       (string.IsNullOrEmpty(variant) || e.variant == variant)).ToList();

    public static (double loadAvg, double fpsAvg, double fpsP01, double memAvg, double fileAvg) Summary(List<MetricsEntry> rows)
    {
        if (rows.Count == 0) return (0,0,0,0,0);
        return (rows.Average(r => r.load_ms),
                rows.Average(r => r.fps_avg),
                rows.Average(r => r.fps_1pc_low),
                rows.Average(r => r.mem_mb),
                rows.Average(r => r.file_mb));
    }

    /// <summary>
    /// Parse uma entrada CSV baseado no número de colunas
    /// </summary>
    private static MetricsEntry ParseCsvEntry(string[] cols)
    {
        try
        {
            if (cols.Length >= MetricsConfig.V3_COLUMN_COUNT)
            {
                return ParseV3Entry(cols);
            }
            else if (cols.Length >= MetricsConfig.V2_COLUMN_COUNT)
            {
                return ParseV2Entry(cols);
            }
            else if (cols.Length >= MetricsConfig.V1_COLUMN_COUNT)
            {
                return ParseV1Entry(cols);
            }
            else
            {
                Debug.LogWarning($"[MetricsStore] Formato de CSV não suportado: {cols.Length} colunas");
                return null;
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[MetricsStore] Erro ao fazer parse da entrada: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Parse formato V3 (novo) com test_number e novas métricas FPS
    /// </summary>
    private static MetricsEntry ParseV3Entry(string[] cols)
    {
        var e = new MetricsEntry();
        
        // timestamp
        if (!DateTime.TryParse(cols[0].Trim('"'), CultureInfo.InvariantCulture,
                DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AdjustToUniversal, out e.timestamp))
            DateTime.TryParse(cols[0].Trim('"'), out e.timestamp);

        e.run_id = cols[1].Trim('"');
        e.test_number = int.TryParse(cols[2].Trim('"'), out int testNum) ? testNum : 1;
        e.platform = cols[3].Trim('"');
        e.unity_version = cols[4].Trim('"');
        e.scene = cols[5].Trim('"');
        e.model = cols[6].Trim('"');
        e.variant = cols[7].Trim('"');
        e.file_mb = Parse(cols[8]);
        e.load_ms = Parse(cols[9]);
        e.mem_mb = Parse(cols[10]);
        e.fps_avg = Parse(cols[11]);
        e.fps_min = Parse(cols[12]);
        e.fps_max = Parse(cols[13]);
        e.fps_median = Parse(cols[14]);
        e.fps_1pc_low = Parse(cols[15]);
        e.fps_samples = cols[16].Trim('"');
        e.fps_window_s = Parse(cols[17]);
        e.ok = cols[18].Equals("true", StringComparison.OrdinalIgnoreCase);
        
        return e;
    }

    /// <summary>
    /// Parse formato V2 (antigo com run_id)
    /// </summary>
    private static MetricsEntry ParseV2Entry(string[] cols)
    {
        var e = new MetricsEntry();
        
        // timestamp
        if (!DateTime.TryParse(cols[0].Trim('"'), CultureInfo.InvariantCulture,
                DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AdjustToUniversal, out e.timestamp))
            DateTime.TryParse(cols[0].Trim('"'), out e.timestamp);

        e.run_id = cols[1].Trim('"');
        e.test_number = 1; // CSV antigo não tem test_number
        e.platform = cols[2].Trim('"');
        e.unity_version = cols[3].Trim('"');
        e.scene = cols[4].Trim('"');
        e.model = cols[5].Trim('"');
        e.variant = cols[6].Trim('"');
        e.file_mb = Parse(cols[7]);
        e.load_ms = Parse(cols[8]);
        e.mem_mb = Parse(cols[9]);
        e.fps_avg = Parse(cols[10]);
        e.fps_min = 0;
        e.fps_max = 0;
        e.fps_median = 0;
        e.fps_1pc_low = Parse(cols[11]);
        e.fps_samples = "";
        e.fps_window_s = cols.Length >= 13 ? Parse(cols[12]) : MetricsConfig.DEFAULT_FPS_WINDOW_SECONDS;
        e.ok = cols[12].Equals("true", StringComparison.OrdinalIgnoreCase);
        
        return e;
    }

    /// <summary>
    /// Parse formato V1 (antigo básico)
    /// </summary>
    private static MetricsEntry ParseV1Entry(string[] cols)
    {
        var e = new MetricsEntry();
        
        // timestamp
        if (!DateTime.TryParse(cols[0].Trim('"'), CultureInfo.InvariantCulture,
                DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AdjustToUniversal, out e.timestamp))
            DateTime.TryParse(cols[0].Trim('"'), out e.timestamp);

        e.run_id = "";
        e.test_number = 1;
        e.platform = cols[1].Trim('"');
        e.unity_version = cols[2].Trim('"');
        e.scene = cols[3].Trim('"');
        e.model = cols[4].Trim('"');
        e.variant = cols[5].Trim('"');
        e.file_mb = Parse(cols[6]);
        e.load_ms = Parse(cols[7]);
        e.mem_mb = Parse(cols[8]);
        e.fps_avg = Parse(cols[9]);
        e.fps_min = 0;
        e.fps_max = 0;
        e.fps_median = 0;
        e.fps_1pc_low = Parse(cols[10]);
        e.fps_samples = "";
        e.fps_window_s = MetricsConfig.DEFAULT_FPS_WINDOW_SECONDS;
        e.ok = cols[11].Equals("true", StringComparison.OrdinalIgnoreCase);
        
        return e;
    }

    static double Parse(string s) { double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var v); return v; }

    static string[] SplitCsv(string line)
    {
        var cols = new List<string>();
        bool inQ = false; int start = 0;
        for (int i = 0; i < line.Length; i++)
        {
            var c = line[i];
            if (c == '"') inQ = !inQ;
            else if (c == ',' && !inQ) { cols.Add(line.Substring(start, i - start)); start = i + 1; }
        }
        cols.Add(line.Substring(start));
        return cols.ToArray();
    }
}
