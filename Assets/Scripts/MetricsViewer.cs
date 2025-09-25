using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MetricsViewer : MonoBehaviour
{
    [Header("Refs")]
    public Metrics metrics;                      // arraste o GO Metrics

    [Header("UI")]
    public GameObject panel;                     // painel inteiro (pode esconder/mostrar)
    public TMP_Dropdown dropdownModel;
    public TMP_Dropdown dropdownVariant;
    public TMP_Text summaryText;
    public Button buttonRefresh;
    public Button buttonOpenFolder;

    [Header("Tabela")]
    public Transform tableContent;               // Content do ScrollView
    public GameObject rowPrefab;                 // Prefab com MetricsRowUI
    public int maxRows = 200;

    [Header("Layout")]
    public bool useReportCards = true;           // ← liga o modo relatório
    public GameObject cardPrefab;                // ← arraste o prefab ReportCard

    List<MetricsEntry> _all = new();
    List<MetricsEntry> _filtered = new();

    void Awake()
    {
        if (buttonRefresh)    buttonRefresh.onClick.AddListener(Refresh);
        if (buttonOpenFolder) buttonOpenFolder.onClick.AddListener(OpenFolder);
        if (dropdownModel)    dropdownModel.onValueChanged.AddListener(_ => ApplyFilters());
        if (dropdownVariant)  dropdownVariant.onValueChanged.AddListener(_ => ApplyFilters());
    }

    void OnEnable() => Refresh();

    void OpenFolder()
    {
        var dir = Path.GetDirectoryName(GetCsvPath());
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
            Application.OpenURL("file://" + dir.Replace("\\","/"));
        }
    }

    string GetCsvPath()
    {
        if (metrics != null) return metrics.GetCsvPathPublic();
        // fallback: mesmo local do persistent
        return Path.Combine(Application.persistentDataPath, "Benchmarks", "benchmarks.csv");
    }

    public void Refresh()
    {
        _all = MetricsStore.Load(GetCsvPath());
        PopulateFilters();
        ApplyFilters();
    }

    void PopulateFilters()
    {
        var models = MetricsStore.Models(_all);
        if (models.Count == 0) models.Add("(sem dados)");

        dropdownModel?.ClearOptions();
        dropdownModel?.AddOptions(models);
        dropdownModel?.SetValueWithoutNotify(0);

        PopulateVariants();
    }

    void PopulateVariants()
    {
        string model = dropdownModel != null && dropdownModel.options.Count > 0
            ? dropdownModel.options[dropdownModel.value].text : null;

        var variants = string.IsNullOrEmpty(model) || model == "(sem dados)"
            ? new List<string> { "(sem dados)" }
            : MetricsStore.Variants(_all, model);

        if (variants.Count == 0) variants.Add("(sem dados)");
        dropdownVariant?.ClearOptions();
        dropdownVariant?.AddOptions(variants);
        dropdownVariant?.SetValueWithoutNotify(0);
    }

    void ApplyFilters()
    {
        if (dropdownModel == null || dropdownVariant == null) return;
        string model   = dropdownModel.options.Count > 0 ? dropdownModel.options[dropdownModel.value].text : null;
        string variant = dropdownVariant.options.Count > 0 ? dropdownVariant.options[dropdownVariant.value].text : null;

        if (model == "(sem dados)") model = null;
        if (variant == "(sem dados)") variant = null;

        _filtered = MetricsStore.Filter(_all, model, variant).Take(maxRows).ToList();
        
        if (useReportCards) RenderCards(_filtered);
        else RenderTable(_filtered);
        
        RenderSummary(_filtered, model, variant);
    }

    void RenderCards(System.Collections.Generic.List<MetricsEntry> rows)
    {
        if (tableContent == null || cardPrefab == null) return;

        for (int i = tableContent.childCount - 1; i >= 0; i--)
            Destroy(tableContent.GetChild(i).gameObject);

        foreach (var e in rows)
        {
            var go = Instantiate(cardPrefab, tableContent);
            var ui = go.GetComponent<MetricsCardUI>();
            if (ui) ui.Set(e);
        }
    }

    void RenderTable(List<MetricsEntry> rows)
    {
        if (tableContent == null || rowPrefab == null) return;

        for (int i = tableContent.childCount - 1; i >= 0; i--)
            Destroy(tableContent.GetChild(i).gameObject);

        foreach (var e in rows)
        {
            var row = Instantiate(rowPrefab, tableContent);
            var ui = row.GetComponent<MetricsRowUI>();
            if (ui) ui.Set(e);
        }
    }

    void RenderSummary(List<MetricsEntry> rows, string model, string variant)
    {
        if (summaryText == null) return;
        if (rows.Count == 0) { summaryText.SetText("Sem dados."); return; }

        var (loadAvg, fpsAvg, fpsP01, memAvg, fileAvg) = MetricsStore.Summary(rows);
        var last = rows[0];

        summaryText.SetText(
            $"Modelo: {(model ?? "Todos")} · Variante: {(variant ?? "Todas")}  \n" +
            $"Último: {last.model}({last.variant})  load {last.load_ms:0.#} ms · FPS {last.fps_avg:0.#} · 1% {last.fps_1pc_low:0.#} · Mem {last.mem_mb:0.#} MB  \n" +
            $"Média: load {loadAvg:0.#} ms · FPS {fpsAvg:0.#} · 1% {fpsP01:0.#} · Mem {memAvg:0.#} MB · File {fileAvg:0.##} MB"
        );
    }
}
