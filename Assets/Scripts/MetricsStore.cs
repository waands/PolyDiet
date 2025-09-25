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
        if (string.IsNullOrEmpty(csvPath) || !File.Exists(csvPath)) return list;

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
            if (cols.Length < 12) continue;

            var e = new MetricsEntry();
            // timestamp
            if (!DateTime.TryParse(cols[0].Trim('"'), CultureInfo.InvariantCulture,
                    DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AdjustToUniversal, out e.timestamp))
                DateTime.TryParse(cols[0].Trim('"'), out e.timestamp);

            e.platform      = cols[1].Trim('"');
            e.unity_version = cols[2].Trim('"');
            e.scene         = cols[3].Trim('"');
            e.model         = cols[4].Trim('"');
            e.variant       = cols[5].Trim('"');
            e.file_mb       = Parse(cols[6]);
            e.load_ms       = Parse(cols[7]);
            e.mem_mb        = Parse(cols[8]);
            e.fps_avg       = Parse(cols[9]);
            e.fps_1pc_low   = Parse(cols[10]);
            e.ok            = cols[11].Equals("true", StringComparison.OrdinalIgnoreCase);
            list.Add(e);
        }
        list.Sort((a,b) => b.timestamp.CompareTo(a.timestamp)); // mais recentes primeiro
        return list;
    }

    public static List<string> Models(List<MetricsEntry> all) =>
        all.Select(e => e.model).Where(s => !string.IsNullOrEmpty(s)).Distinct().OrderBy(s => s).ToList();

    public static List<string> Variants(List<MetricsEntry> all, string model) =>
        all.Where(e => e.model == model).Select(e => e.variant)
            .Where(s => !string.IsNullOrEmpty(s)).Distinct()
            .OrderBy(v => v == "original" ? "0" : v).ToList();

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
