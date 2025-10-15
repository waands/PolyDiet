using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Dashboard principal de métricas com visualização comparativa
/// </summary>
public class MetricsDashboard : MonoBehaviour
{
    [Header("Theme")]
    public DashboardTheme theme;
    
    [Header("Header Controls")]
    public TMP_Dropdown dropdownModel;
    public Toggle chipOriginal;
    public Toggle chipDraco;
    public Toggle chipMeshopt;
    public Button buttonRefresh;
    public Button buttonExportHtml;
    public Button buttonOpenFolder;
    public Button buttonClose; // Botão X para fechar o painel
    
    [Header("Metric Cards")]
    public TextMeshProUGUI cardLoadValue;
    public TextMeshProUGUI cardLoadGain;
    public TextMeshProUGUI cardMemValue;
    public TextMeshProUGUI cardMemGain;
    public TextMeshProUGUI cardFpsValue;
    public TextMeshProUGUI cardFpsGain;
    public TextMeshProUGUI cardFpsLowValue;
    public TextMeshProUGUI cardFpsLowGain;
    
    [Header("Charts")]
    public ChartBar chartLoad;
    public ChartBar chartMem;
    public ChartBar chartFps;
    public ChartTexture chartTimelineIndex;
    public ChartTexture chartTimelineTime;
    
    [Header("Table (opcional)")]
    public Transform tableContent;
    public GameObject rowPrefab;
    
    [Header("Export")]
    public MetricsHtmlExporter htmlExporter;
    
    // Dados em memória
    private List<ModelStats> _allStats;
    private ModelStats _currentModelStats;
    private HashSet<string> _selectedVariants = new HashSet<string>();
    
    void Awake()
    {
        // Conecta botões
        if (buttonRefresh) buttonRefresh.onClick.AddListener(RefreshAll);
        if (buttonExportHtml) buttonExportHtml.onClick.AddListener(() => StartCoroutine(ExportHtml()));
        if (buttonOpenFolder) buttonOpenFolder.onClick.AddListener(OpenFolder);
        if (buttonClose) buttonClose.onClick.AddListener(CloseDashboard);
        
        // Conecta chips
        if (chipOriginal) chipOriginal.onValueChanged.AddListener(on => OnVariantToggle("original", on));
        if (chipDraco) chipDraco.onValueChanged.AddListener(on => OnVariantToggle("draco", on));
        if (chipMeshopt) chipMeshopt.onValueChanged.AddListener(on => OnVariantToggle("meshopt", on));
        
        // Conecta dropdown
        if (dropdownModel) dropdownModel.onValueChanged.AddListener(_ => OnModelChanged());
    }
    
    void OnEnable()
    {
        RefreshAll();
    }
    
    /// <summary>
    /// Atualiza todos os dados e redesenha o dashboard
    /// </summary>
    public void RefreshAll()
    {
        Debug.Log("[Dashboard] Atualizando dashboard...");
        
        // Carrega dados de todos os modelos
        var entries = MetricsStore.LoadAllModels();
        
        if (entries.Count == 0)
        {
            Debug.LogWarning("[Dashboard] Nenhuma métrica encontrada nos CSVs");
            return;
        }
        
        // Agrega dados
        _allStats = MetricsAggregator.Build(entries);
        
        if (_allStats.Count == 0)
        {
            Debug.LogWarning("[Dashboard] Nenhum dado agregado disponível");
            return;
        }
        
        // Popula dropdown de modelos
        PopulateModelDropdown();
        
        // Desenha dados atuais
        OnModelChanged();
        
        Debug.Log($"[Dashboard] Dashboard atualizado: {_allStats.Count} modelos, {entries.Count} entradas");
    }
    
    /// <summary>
    /// Popula o dropdown com os modelos disponíveis
    /// </summary>
    private void PopulateModelDropdown()
    {
        if (dropdownModel == null) 
        {
            Debug.LogWarning("[Dashboard] dropdownModel é null!");
            return;
        }
        
        Debug.Log($"[Dashboard] Populando dropdown com {_allStats?.Count ?? 0} modelos");
        
        dropdownModel.ClearOptions();
        var modelNames = _allStats?.Select(s => s.model).ToList() ?? new List<string>();
        Debug.Log($"[Dashboard] Nomes dos modelos: {string.Join(", ", modelNames)}");
        
        dropdownModel.AddOptions(modelNames);
        
        if (modelNames.Count > 0)
        {
            dropdownModel.value = 0;
            dropdownModel.RefreshShownValue();
            Debug.Log($"[Dashboard] Dropdown populado com {modelNames.Count} opções");
        }
        else
        {
            Debug.LogWarning("[Dashboard] Nenhum modelo para popular no dropdown!");
        }
    }
    
    /// <summary>
    /// Callback quando o modelo selecionado muda
    /// </summary>
    private void OnModelChanged()
    {
        string modelName = CurrentModelName();
        if (string.IsNullOrEmpty(modelName)) return;
        
        _currentModelStats = _allStats.FirstOrDefault(s => s.model == modelName);
        if (_currentModelStats == null) return;
        
        // Atualiza disponibilidade dos chips
        UpdateChipAvailability();
        
        // Atualiza seleção inicial (todas ligadas)
        if (_selectedVariants.Count == 0)
        {
            if (_currentModelStats.byVariant.ContainsKey("original")) _selectedVariants.Add("original");
            if (_currentModelStats.byVariant.ContainsKey("draco")) _selectedVariants.Add("draco");
            if (_currentModelStats.byVariant.ContainsKey("meshopt")) _selectedVariants.Add("meshopt");
            
            // Sincroniza com UI
            if (chipOriginal) chipOriginal.SetIsOnWithoutNotify(_selectedVariants.Contains("original"));
            if (chipDraco) chipDraco.SetIsOnWithoutNotify(_selectedVariants.Contains("draco"));
            if (chipMeshopt) chipMeshopt.SetIsOnWithoutNotify(_selectedVariants.Contains("meshopt"));
        }
        
        RedrawCurrent();
    }
    
    /// <summary>
    /// Atualiza disponibilidade dos chips baseado nas variantes existentes
    /// </summary>
    private void UpdateChipAvailability()
    {
        if (_currentModelStats == null) return;
        
        if (chipOriginal)
        {
            bool has = _currentModelStats.byVariant.ContainsKey("original");
            chipOriginal.interactable = has;
            if (!has) chipOriginal.isOn = false;
        }
        
        if (chipDraco)
        {
            bool has = _currentModelStats.byVariant.ContainsKey("draco");
            chipDraco.interactable = has;
            if (!has) chipDraco.isOn = false;
        }
        
        if (chipMeshopt)
        {
            bool has = _currentModelStats.byVariant.ContainsKey("meshopt");
            chipMeshopt.interactable = has;
            if (!has) chipMeshopt.isOn = false;
        }
    }
    
    /// <summary>
    /// Callback quando um chip de variante é toggled
    /// </summary>
    private void OnVariantToggle(string variant, bool isOn)
    {
        if (isOn)
            _selectedVariants.Add(variant);
        else
            _selectedVariants.Remove(variant);
        
        RedrawCurrent();
    }
    
    /// <summary>
    /// Redesenha todos os elementos visuais
    /// </summary>
    private void RedrawCurrent()
    {
        if (_currentModelStats == null || _selectedVariants.Count == 0) return;
        
        // Obtém stats para as variantes selecionadas
        var stats = new Dictionary<string, VariantStats>();
        foreach (var variant in _selectedVariants)
        {
            if (_currentModelStats.byVariant.TryGetValue(variant, out var variantStats))
                stats[variant] = variantStats;
        }
        
        if (stats.Count == 0) return;
        
        // Atualiza cards
        UpdateCards(stats);
        
        // Atualiza gráficos
        UpdateCharts(stats);
        
        // Atualiza timelines
        UpdateTimelines(stats);
        
        // Atualiza tabela (se houver)
        UpdateTable(stats);
    }
    
    /// <summary>
    /// Atualiza os cards de resumo
    /// </summary>
    private void UpdateCards(Dictionary<string, VariantStats> stats)
    {
        VariantStats original = stats.ContainsKey("original") ? stats["original"] : null;
        
        // Load Time
        UpdateCard(cardLoadValue, cardLoadGain, stats, original, MetricKind.LoadMs);
        
        // Memory
        UpdateCard(cardMemValue, cardMemGain, stats, original, MetricKind.MemMB);
        
        // FPS Average
        UpdateCard(cardFpsValue, cardFpsGain, stats, original, MetricKind.FpsAvg);
        
        // FPS 1% Low
        UpdateCard(cardFpsLowValue, cardFpsLowGain, stats, original, MetricKind.FpsP01);
    }
    
    /// <summary>
    /// Atualiza um card individual
    /// </summary>
    private void UpdateCard(TextMeshProUGUI valueText, TextMeshProUGUI gainText, 
                            Dictionary<string, VariantStats> stats, VariantStats original, MetricKind kind)
    {
        if (valueText == null) return;
        
        // Calcula valor médio das variantes selecionadas
        double avgValue = 0;
        foreach (var stat in stats.Values)
        {
            avgValue += MetricsAggregator.GetValue(stat, kind);
        }
        avgValue /= stats.Count;
        
        // Exibe valor com unidade
        string unit = MetricMeta.Unit(kind);
        valueText.text = $"{avgValue:F1} {unit}";
        
        // Calcula e exibe ganho vs original (se houver)
        if (gainText != null && original != null && stats.Count == 1 && !stats.ContainsKey("original"))
        {
            var variant = stats.Values.First();
            double gain = MetricsAggregator.GainVsOriginal(original, variant, kind);
            
            if (theme != null)
            {
                gainText.text = theme.FormatGain(gain);
                gainText.color = theme.GetGainColor(gain);
            }
            else
            {
                string sign = gain >= 0 ? "+" : "";
                gainText.text = $"{sign}{gain:F1}%";
                gainText.color = gain >= 0 ? Color.green : Color.red;
            }
        }
        else if (gainText != null)
        {
            gainText.text = ""; // sem comparação
        }
    }
    
    /// <summary>
    /// Atualiza os gráficos de barras
    /// </summary>
    private void UpdateCharts(Dictionary<string, VariantStats> stats)
    {
        // Load Time
        if (chartLoad != null)
        {
            var data = stats.ToDictionary(kv => kv.Key, kv => kv.Value.loadMs);
            chartLoad.Draw(data, theme, "ms", false); // menor é melhor
        }
        
        // Memory
        if (chartMem != null)
        {
            var data = stats.ToDictionary(kv => kv.Key, kv => kv.Value.memMB);
            chartMem.Draw(data, theme, "MB", false); // menor é melhor
        }
        
        // FPS
        if (chartFps != null)
        {
            var data = stats.ToDictionary(kv => kv.Key, kv => kv.Value.fpsAvg);
            chartFps.Draw(data, theme, "FPS", true); // maior é melhor
        }
    }
    
    /// <summary>
    /// Atualiza as timelines
    /// </summary>
    private void UpdateTimelines(Dictionary<string, VariantStats> stats)
    {
        // Timeline por índice (ordem de execução)
        if (chartTimelineIndex != null)
        {
            var series = stats.ToDictionary(
                kv => kv.Key, 
                kv => MetricsAggregator.GetSeries(kv.Value, MetricKind.FpsAvg)
            );
            chartTimelineIndex.DrawIndexTimeline(series, theme, "FPS");
        }
        
        // Timeline por data
        if (chartTimelineTime != null)
        {
            var series = stats.ToDictionary(
                kv => kv.Key,
                kv => MetricsAggregator.GetSeries(kv.Value, MetricKind.FpsAvg)
            );
            chartTimelineTime.DrawTimeTimeline(series, theme, "FPS");
        }
    }
    
    /// <summary>
    /// Atualiza a tabela detalhada (opcional)
    /// </summary>
    private void UpdateTable(Dictionary<string, VariantStats> stats)
    {
        if (tableContent == null) return;
        
        // Limpa tabela existente
        foreach (Transform child in tableContent)
        {
            Destroy(child.gameObject);
        }
        
        // TODO: Criar rows da tabela se necessário
    }
    
    /// <summary>
    /// Exporta o dashboard atual para HTML
    /// </summary>
    private System.Collections.IEnumerator ExportHtml()
    {
        if (htmlExporter == null)
        {
            Debug.LogError("[Dashboard] HtmlExporter não configurado!");
            yield break;
        }
        
        string modelName = CurrentModelName();
        yield return htmlExporter.ExportHtml(modelName, path =>
        {
            if (!string.IsNullOrEmpty(path))
                Debug.Log($"[Dashboard] HTML exportado com sucesso: {path}");
            else
                Debug.LogError("[Dashboard] Falha ao exportar HTML");
        });
    }
    
    /// <summary>
    /// Abre a pasta de relatórios no explorador de arquivos
    /// </summary>
    private void OpenFolder()
    {
        string dir = System.IO.Path.Combine(Application.persistentDataPath, "Reports");
        System.IO.Directory.CreateDirectory(dir);
        Application.OpenURL("file://" + dir);
        Debug.Log($"[Dashboard] Abrindo pasta: {dir}");
    }
    
    /// <summary>
    /// Retorna o nome do modelo atualmente selecionado
    /// </summary>
    public string CurrentModelName()
    {
        if (dropdownModel == null || dropdownModel.options.Count == 0)
            return "";
        
        return dropdownModel.options[dropdownModel.value].text;
    }
    
    /// <summary>
    /// Fecha o dashboard (desativa o GameObject pai)
    /// </summary>
    public void CloseDashboard()
    {
        // Destrava a câmera
        UIInputLock.Unlock(this);
        
        // Fecha o painel (desativa o GameObject)
        gameObject.SetActive(false);
        
        Debug.Log("[Dashboard] Painel fechado");
    }
}

