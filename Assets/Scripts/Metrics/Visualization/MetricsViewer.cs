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
    public ReportRunner reportRunner;            // arraste o GO ReportRunner

    [Header("UI")]
    public GameObject panel;                     // painel inteiro (pode esconder/mostrar)
    public TMP_Dropdown dropdownModel;
    public TMP_Dropdown dropdownVariant;
    public TMP_Text summaryText;
    public Button buttonRefresh;
    public Button buttonOpenFolder;
    public Button buttonClose;                   // botão para fechar o painel

    [Header("Tabela")]
    public Transform tableContent;               // Content do ScrollView
    public GameObject rowPrefab;                 // Prefab com MetricsRowUI
    public int maxRows = 200;

    [Header("Layout")]
    public bool useReportCards = true;           // ← liga o modo relatório
    public GameObject cardPrefab;                // ← arraste o prefab ReportCard

    // ---- Multi-seleção de variantes (chips) ----
    [Header("Variants (multi-select)")]
    public bool multiSelectVariants = true;         // liga/desliga modo chips
    public Transform variantChipContainer;          // RowTop/VariantChips
    public Toggle variantChipPrefab;                // Toggle desativado (prefab)
    public Button buttonSelectAll;                  // opcional
    public Button buttonClearAll;                   // opcional

    [Header("Cards / comparação")]
    public bool latestPerVariant = true;            // 1 card por variante (run mais recente)

    List<MetricsEntry> _all = new();
    List<MetricsEntry> _filtered = new();

    // internos
    readonly System.Collections.Generic.Dictionary<string, Toggle> _variantToggles =
        new System.Collections.Generic.Dictionary<string, Toggle>(System.StringComparer.OrdinalIgnoreCase);

    void Awake()
    {
        if (buttonRefresh)    buttonRefresh.onClick.AddListener(Refresh);
        if (buttonOpenFolder) buttonOpenFolder.onClick.AddListener(OpenFolder);
        if (buttonClose)      buttonClose.onClick.AddListener(ClosePanel);
        if (dropdownModel)    dropdownModel.onValueChanged.AddListener(_ => ApplyFilters());
        if (dropdownVariant)  dropdownVariant.onValueChanged.AddListener(_ => ApplyFilters());
        if (buttonSelectAll) buttonSelectAll.onClick.AddListener(() => SetAllVariantChips(true));
        if (buttonClearAll)  buttonClearAll.onClick.AddListener(() => SetAllVariantChips(false));
    }

    void OnEnable() 
    { 
        Refresh(); 
    }

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
        _all = MetricsStore.LoadAllModels();
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
            ? new List<string>()
            : MetricsStore.Variants(_all, model);

        if (multiSelectVariants)
        {
            BuildVariantChips(variants);
        }
        else
        {
            // fallback: dropdown (se quiser manter ambos os modos)
            dropdownVariant?.ClearOptions();
            if (variants.Count == 0) variants.Add("(sem dados)");
            dropdownVariant?.AddOptions(variants);
            dropdownVariant?.SetValueWithoutNotify(0);
        }
    }

    void ApplyFilters()
    {
        if (_all == null) return;

        string model = dropdownModel != null && dropdownModel.options.Count > 0
            ? dropdownModel.options[dropdownModel.value].text : null;

        // ----- variantes selecionadas -----
        System.Collections.Generic.List<MetricsEntry> rows;

        if (multiSelectVariants && variantChipContainer != null)
        {
            var selected = GetSelectedVariants();

            if (selected.Count == 0)
            {
                // nada marcado → mostra nada (ou tudo, se preferir)
                rows = new System.Collections.Generic.List<MetricsEntry>();
            }
            else if (latestPerVariant)
            {
                // 1 card por variante: pega a execução mais recente de cada variante marcada
                rows = new System.Collections.Generic.List<MetricsEntry>();
                foreach (var v in selected)
                {
                    var e = _all.FirstOrDefault(x => x.model == model && x.variant == v);
                    if (e != null) rows.Add(e);
                }
            }
            else
            {
                // lista completa das variantes marcadas (ordenada por data desc)
                rows = _all.Where(e => e.model == model && selected.Contains(e.variant)).ToList();
            }
        }
        else
        {
            // modo antigo: dropdown de variante
            string variant = dropdownVariant != null && dropdownVariant.options.Count > 0
                ? dropdownVariant.options[dropdownVariant.value].text : null;

            rows = MetricsStore.Filter(_all, model, variant);
        }

        // Render
        if (useReportCards)
            RenderCards(rows);
        else
            RenderTable(rows); // se ainda quiser a tabela

        // Summary (ajuste a seu gosto)
        RenderSummary(rows, model, multiSelectVariants ? "(multi)" : (dropdownVariant?.options.Count > 0 ? dropdownVariant.options[dropdownVariant.value].text : null));
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

    void BuildVariantChips(System.Collections.Generic.List<string> variants)
    {
        // Limpa os antigos
        foreach (Transform c in variantChipContainer) Destroy(c.gameObject);
        _variantToggles.Clear();

        if (variantChipContainer == null || variantChipPrefab == null) return;

        // Cria um toggle por variante (default: ON)
        foreach (var v in variants)
        {
            var chip = Instantiate(variantChipPrefab, variantChipContainer);
            chip.gameObject.SetActive(true);
            var label = chip.GetComponentInChildren<TMPro.TMP_Text>();
            if (label) label.SetText(v);
            chip.isOn = true;
            chip.onValueChanged.AddListener(_ => ApplyFilters());
            _variantToggles[v] = chip;
        }
    }

    System.Collections.Generic.HashSet<string> GetSelectedVariants()
    {
        var set = new System.Collections.Generic.HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
        foreach (var kv in _variantToggles)
            if (kv.Value && kv.Value.isOn) set.Add(kv.Key);
        return set;
    }

    void SetAllVariantChips(bool on)
    {
        foreach (var t in _variantToggles.Values)
            if (t) t.isOn = on;
        ApplyFilters();
    }

    void ClosePanel()
    {
        UIInputLock.Unlock(this);
        if (panel != null)
            panel.SetActive(false);
    }
    
    // Métodos públicos para controle manual do bloqueio
    public void ShowPanel()
    {
        UIInputLock.Lock(this);
        if (panel != null)
            panel.SetActive(true);
    }
    
    public void HidePanel()
    {
        UIInputLock.Unlock(this);
        if (panel != null)
            panel.SetActive(false);
    }

    /// <summary>
    /// Pega o modelo selecionado no dropdown e solicita
    /// ao ReportRunner que gere um relatório para ele.
    /// </summary>
    public void TriggerReportGeneration()
    {
        if (reportRunner == null)
        {
            Debug.LogError("Referência do ReportRunner não está configurada no MetricsViewer!", this.gameObject);
            return;
        }

        if (dropdownModel == null || dropdownModel.options.Count == 0)
        {
            Debug.LogWarning("Dropdown de modelos não está disponível ou está vazio.", this.gameObject);
            return;
        }

        // Pega o nome do modelo selecionado no dropdown
        string selectedModel = dropdownModel.options[dropdownModel.value].text;

        if (string.IsNullOrEmpty(selectedModel) || selectedModel.Contains("(sem dados)"))
        {
            Debug.LogWarning("Modelo selecionado é inválido. Não é possível gerar relatório.", this.gameObject);
            return;
        }
        
        // Verificar se o modelo tem dados de benchmark
        string csvPath = MetricsPathProvider.GetCsvPath(selectedModel);
        bool hasData = MetricsPathProvider.HasBenchmarkData(selectedModel);
        
        Debug.Log($"[MetricsViewer] Verificando benchmark para '{selectedModel}':");
        Debug.Log($"[MetricsViewer] CSV Path: {csvPath}");
        Debug.Log($"[MetricsViewer] File exists: {System.IO.File.Exists(csvPath)}");
        Debug.Log($"[MetricsViewer] HasBenchmarkData: {hasData}");
        
        if (!hasData)
        {
            Debug.LogWarning($"[MetricsViewer] Nenhum benchmark encontrado para o modelo '{selectedModel}'. Execute os testes primeiro.", this.gameObject);
            return;
        }

        Debug.Log($"[MetricsViewer] Gerando relatório para modelo selecionado: {selectedModel}");
        
        // Informar localização onde o report será salvo
        string reportsDir = MetricsPathProvider.GetModelReportsDirectory(selectedModel);
        Debug.Log($"[MetricsViewer] Report será salvo em: {reportsDir}/<timestamp>/");
        
        // Chama o método no ReportRunner, passando o modelo selecionado
        reportRunner.RunReportForModel(selectedModel);
        
        // Feedback adicional sobre reports anteriores
        var previousReports = MetricsPathProvider.GetModelReportsList(selectedModel);
        if (previousReports.Length > 0)
        {
            Debug.Log($"[MetricsViewer] Este modelo tem {previousReports.Length} report(s) anterior(es).");
            Debug.Log($"[MetricsViewer] Report mais recente: {previousReports[0]}");
        }
    }
}
