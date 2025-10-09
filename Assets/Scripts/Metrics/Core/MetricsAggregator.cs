using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Tipos de métricas disponíveis
/// </summary>
public enum MetricKind { LoadMs, MemMB, FpsAvg, FpsP01 }

/// <summary>
/// Metadados sobre cada tipo de métrica
/// </summary>
public static class MetricMeta
{
    public static bool HigherIsBetter(MetricKind k) => k == MetricKind.FpsAvg || k == MetricKind.FpsP01;
    
    public static string Unit(MetricKind k) => k switch
    {
        MetricKind.LoadMs => "ms",
        MetricKind.MemMB => "MB",
        MetricKind.FpsAvg => "FPS",
        MetricKind.FpsP01 => "FPS",
        _ => ""
    };
    
    public static string Label(MetricKind k) => k switch
    {
        MetricKind.LoadMs => "Load Time",
        MetricKind.MemMB => "Memory",
        MetricKind.FpsAvg => "FPS Average",
        MetricKind.FpsP01 => "FPS 1% Low",
        _ => k.ToString()
    };
}

/// <summary>
/// Estatísticas agregadas para uma variante específica
/// </summary>
public class VariantStats
{
    public string variant;
    public double loadMs, memMB, fpsAvg, fpsP01;
    public int count;
    
    // Séries para timelines
    public List<(DateTime time, double value)> seriesLoad = new();
    public List<(DateTime time, double value)> seriesMem = new();
    public List<(DateTime time, double value)> seriesFps = new();
    public List<(DateTime time, double value)> seriesFpsLow = new();
}

/// <summary>
/// Estatísticas agregadas para um modelo específico
/// </summary>
public class ModelStats
{
    public string model;
    public Dictionary<string, VariantStats> byVariant = new();
}

/// <summary>
/// Agregador de métricas - processa dados brutos e calcula estatísticas
/// </summary>
public static class MetricsAggregator
{
    /// <summary>
    /// Constrói estatísticas agregadas a partir de entradas brutas
    /// </summary>
    public static List<ModelStats> Build(IEnumerable<MetricsEntry> rows)
    {
        var dict = new Dictionary<string, ModelStats>();
        
        foreach (var entry in rows.Where(e => e.ok)) // apenas runs bem-sucedidos
        {
            if (string.IsNullOrEmpty(entry.model)) continue;
            
            // Garante que existe ModelStats para este modelo
            if (!dict.TryGetValue(entry.model, out var modelStats))
            {
                modelStats = new ModelStats { model = entry.model };
                dict[entry.model] = modelStats;
            }
            
            // Garante que existe VariantStats para esta variante
            if (!modelStats.byVariant.TryGetValue(entry.variant, out var variantStats))
            {
                variantStats = new VariantStats { variant = entry.variant };
                modelStats.byVariant[entry.variant] = variantStats;
            }
            
            // Acumula valores para cálculo de média
            variantStats.loadMs += entry.load_ms;
            variantStats.memMB += entry.mem_mb;
            variantStats.fpsAvg += entry.fps_avg;
            variantStats.fpsP01 += entry.fps_1pc_low;
            variantStats.count++;
            
            // Adiciona pontos para séries temporais
            variantStats.seriesLoad.Add((entry.timestamp, entry.load_ms));
            variantStats.seriesMem.Add((entry.timestamp, entry.mem_mb));
            variantStats.seriesFps.Add((entry.timestamp, entry.fps_avg));
            variantStats.seriesFpsLow.Add((entry.timestamp, entry.fps_1pc_low));
        }
        
        // Calcula médias e ordena séries
        foreach (var modelStats in dict.Values)
        {
            foreach (var variantStats in modelStats.byVariant.Values)
            {
                if (variantStats.count > 0)
                {
                    variantStats.loadMs /= variantStats.count;
                    variantStats.memMB /= variantStats.count;
                    variantStats.fpsAvg /= variantStats.count;
                    variantStats.fpsP01 /= variantStats.count;
                }
                
                // Ordena séries por tempo (mais antigas primeiro)
                variantStats.seriesLoad.Sort((a, b) => a.time.CompareTo(b.time));
                variantStats.seriesMem.Sort((a, b) => a.time.CompareTo(b.time));
                variantStats.seriesFps.Sort((a, b) => a.time.CompareTo(b.time));
                variantStats.seriesFpsLow.Sort((a, b) => a.time.CompareTo(b.time));
            }
        }
        
        return dict.Values.ToList();
    }
    
    /// <summary>
    /// Calcula o ganho percentual de uma variante versus original
    /// Retorna valor positivo se for melhor, negativo se for pior
    /// </summary>
    public static double GainVsOriginal(VariantStats original, VariantStats variant, MetricKind kind)
    {
        if (original == null || variant == null) return 0;
        
        double origValue = GetValue(original, kind);
        double varValue = GetValue(variant, kind);
        
        if (Math.Abs(origValue) < 0.0001) return 0; // evita divisão por zero
        
        bool higherIsBetter = MetricMeta.HigherIsBetter(kind);
        
        if (higherIsBetter)
        {
            // FPS: maior é melhor → (variant - original) / original * 100
            return ((varValue - origValue) / origValue) * 100.0;
        }
        else
        {
            // Load/Mem: menor é melhor → (original - variant) / original * 100
            return ((origValue - varValue) / origValue) * 100.0;
        }
    }
    
    /// <summary>
    /// Extrai o valor correto de uma VariantStats baseado no tipo de métrica
    /// </summary>
    public static double GetValue(VariantStats stats, MetricKind kind)
    {
        return kind switch
        {
            MetricKind.LoadMs => stats.loadMs,
            MetricKind.MemMB => stats.memMB,
            MetricKind.FpsAvg => stats.fpsAvg,
            MetricKind.FpsP01 => stats.fpsP01,
            _ => 0
        };
    }
    
    /// <summary>
    /// Obtém a série temporal apropriada para um tipo de métrica
    /// </summary>
    public static List<(DateTime time, double value)> GetSeries(VariantStats stats, MetricKind kind)
    {
        return kind switch
        {
            MetricKind.LoadMs => stats.seriesLoad,
            MetricKind.MemMB => stats.seriesMem,
            MetricKind.FpsAvg => stats.seriesFps,
            MetricKind.FpsP01 => stats.seriesFpsLow,
            _ => new List<(DateTime, double)>()
        };
    }
}


