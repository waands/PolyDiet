using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace PolyDiet.UI.Controllers
{
    /// <summary>
    /// Controlador para o painel de relatórios com previews PNG e botões de ação
    /// </summary>
    public class ReportsPanelController : MonoBehaviour
    {
        [Header("Referências")]
        [Tooltip("ReportRunner para gerar relatórios")]
        public ReportRunner reportRunner;
        
        [Header("Seleção de Modelo")]
        [Tooltip("Dropdown para seleção de modelo")]
        public TMP_Dropdown dropdownModel;
        [Tooltip("Botão para atualizar lista de modelos")]
        public Button buttonRefreshModels;
        
        [Header("Previews PNG")]
        [Tooltip("RawImage para preview de tempo de carregamento")]
        public RawImage previewLoad;
        [Tooltip("RawImage para preview de uso de memória")]
        public RawImage previewMem;
        [Tooltip("RawImage para preview de FPS")]
        public RawImage previewFps;
        
        [Header("Botões")]
        [Tooltip("Botão para gerar relatório")]
        public Button buttonGenerate;
        [Tooltip("Botão para abrir HTML")]
        public Button buttonOpenHtml;
        [Tooltip("Botão para abrir PDF")]
        public Button buttonOpenPdf;
        [Tooltip("Botão para abrir pasta")]
        public Button buttonOpenFolder;
        [Tooltip("Botão para visualizar imagem em tela cheia")]
        public Button buttonFullscreen;
        
        [Header("Status")]
        [Tooltip("Label de status")]
        public TextMeshProUGUI statusLabel;
        
        [Header("Labels dos Previews")]
        [Tooltip("Label do preview de carregamento")]
        public TextMeshProUGUI labelLoad;
        [Tooltip("Label do preview de memória")]
        public TextMeshProUGUI labelMem;
        [Tooltip("Label do preview de FPS")]
        public TextMeshProUGUI labelFps;
        
        // Estado interno
        private string _currentModel;
        private string _lastReportPath;
        private bool _isGeneratingReport;
        
        void Start()
        {
            SetupButtons();
            SetupDropdown();
            RefreshModelList();
            UpdateUI();
        }
        
        void OnEnable()
        {
            UpdateUI();
        }
        
        /// <summary>
        /// Configura os listeners dos botões
        /// </summary>
        private void SetupButtons()
        {
            if (buttonGenerate != null)
                buttonGenerate.onClick.AddListener(OnClickGenerate);
            
            if (buttonOpenHtml != null)
                buttonOpenHtml.onClick.AddListener(OnClickOpenHtml);
            
            if (buttonOpenPdf != null)
                buttonOpenPdf.onClick.AddListener(OnClickOpenPdf);
            
            if (buttonOpenFolder != null)
                buttonOpenFolder.onClick.AddListener(OnClickOpenFolder);
            
            if (buttonRefreshModels != null)
                buttonRefreshModels.onClick.AddListener(OnClickRefreshModels);
            
            if (buttonFullscreen != null)
                buttonFullscreen.onClick.AddListener(OnClickFullscreen);
        }
        
        /// <summary>
        /// Configura o dropdown de modelos
        /// </summary>
        private void SetupDropdown()
        {
            if (dropdownModel != null)
            {
                dropdownModel.onValueChanged.AddListener(OnModelSelectionChanged);
            }
        }
        
        /// <summary>
        /// Atualiza a lista de modelos no dropdown
        /// </summary>
        public void RefreshModelList()
        {
            if (dropdownModel == null) return;
            
            // Limpar opções existentes
            dropdownModel.ClearOptions();
            
            // Obter lista de modelos disponíveis
            var modelViewer = FindObjectOfType<ModelViewer>();
            if (modelViewer == null)
            {
                Debug.LogWarning("[ReportsPanel] ModelViewer não encontrado!");
                dropdownModel.AddOptions(new System.Collections.Generic.List<string> { "(sem modelos)" });
                return;
            }
            
            var modelNames = modelViewer.GetAllAvailableModels();
            if (modelNames == null || modelNames.Count == 0)
            {
                dropdownModel.AddOptions(new System.Collections.Generic.List<string> { "(sem modelos)" });
                return;
            }
            
            // Filtrar apenas modelos com dados de benchmark
            var modelsWithData = new System.Collections.Generic.List<string>();
            foreach (var modelName in modelNames)
            {
                if (MetricsPathProvider.HasBenchmarkData(modelName))
                {
                    modelsWithData.Add(modelName);
                }
            }
            
            if (modelsWithData.Count == 0)
            {
                dropdownModel.AddOptions(new System.Collections.Generic.List<string> { "(sem dados de benchmark)" });
            }
            else
            {
                dropdownModel.AddOptions(modelsWithData);
            }
            
            Debug.Log($"[ReportsPanel] Lista de modelos atualizada: {modelsWithData.Count} modelos com dados");
        }
        
        /// <summary>
        /// Callback para mudança de seleção no dropdown
        /// </summary>
        private void OnModelSelectionChanged(int index)
        {
            if (dropdownModel == null || dropdownModel.options.Count == 0) return;
            
            string selectedModel = dropdownModel.options[index].text;
            if (string.IsNullOrEmpty(selectedModel) || selectedModel.StartsWith("("))
            {
                _currentModel = null;
            }
            else
            {
                _currentModel = selectedModel;
            }
            
            Debug.Log($"[ReportsPanel] Modelo selecionado: {_currentModel}");
            UpdateUI();
        }
        
        /// <summary>
        /// Callback para botão de atualizar modelos
        /// </summary>
        public void OnClickRefreshModels()
        {
            Debug.Log("[ReportsPanel] Atualizando lista de modelos...");
            RefreshModelList();
            UpdateUI();
        }
        
        /// <summary>
        /// Callback para botão de tela cheia
        /// </summary>
        public void OnClickFullscreen()
        {
            if (string.IsNullOrEmpty(_lastReportPath))
            {
                SetStatus("Nenhum relatório disponível para visualizar");
                return;
            }
            
            string imagesDir = Path.Combine(_lastReportPath, "images");
            
            // Por enquanto, abrir a pasta de imagens
            // Em uma implementação mais avançada, poderia criar uma UI dedicada
            OpenUrl(imagesDir);
            Debug.Log($"[ReportsPanel] Abrindo visualizador de imagens: {imagesDir}");
        }
        
        /// <summary>
        /// Atualiza a UI baseada no estado atual
        /// </summary>
        private void UpdateUI()
        {
            Debug.Log($"[ReportsPanel] UpdateUI chamado - _currentModel: '{_currentModel}'");
            
            // O modelo atual é determinado pelo dropdown
            // _currentModel é atualizado em OnModelSelectionChanged()
            
            // Atualizar status
            if (string.IsNullOrEmpty(_currentModel))
            {
                Debug.Log("[ReportsPanel] Nenhum modelo selecionado");
                SetStatus("Selecione um modelo primeiro");
                SetButtonsEnabled(false);
                ClearPreviews();
                return;
            }
            
            // Verificar se tem dados de benchmark
            bool hasBenchmarkData = MetricsPathProvider.HasBenchmarkData(_currentModel);
            Debug.Log($"[ReportsPanel] Modelo '{_currentModel}' tem dados de benchmark: {hasBenchmarkData}");
            
            if (!hasBenchmarkData)
            {
                Debug.Log($"[ReportsPanel] Modelo '{_currentModel}' não possui dados de benchmark");
                SetStatus($"Modelo '{_currentModel}' não possui dados de benchmark. Execute os testes primeiro.");
                SetButtonsEnabled(false);
                ClearPreviews();
                return;
            }
            
            // Verificar se tem relatório recente
            var latestReport = MetricsPathProvider.GetLatestModelReport(_currentModel);
            Debug.Log($"[ReportsPanel] Último relatório para '{_currentModel}': '{latestReport}'");
            
            if (!string.IsNullOrEmpty(latestReport))
            {
                _lastReportPath = latestReport;
                SetStatus($"Modelo '{_currentModel}' - Relatório disponível");
                SetButtonsEnabled(true);
                LoadPreviews();
            }
            else
            {
                Debug.Log($"[ReportsPanel] Nenhum relatório encontrado para '{_currentModel}', habilitando botão gerar");
                SetStatus($"Modelo '{_currentModel}' - Pronto para gerar relatório");
                SetButtonsEnabled(true); // HABILITAR para gerar novo relatório
                ClearPreviews();
            }
        }
        
        /// <summary>
        /// Callback para botão Gerar Relatório
        /// </summary>
        public void OnClickGenerate()
        {
            Debug.Log($"[ReportsPanel] OnClickGenerate chamado - _isGeneratingReport: {_isGeneratingReport}, _currentModel: '{_currentModel}'");
            
            if (_isGeneratingReport)
            {
                Debug.Log("[ReportsPanel] Já está gerando relatório, ignorando clique");
                return;
            }
            
            if (string.IsNullOrEmpty(_currentModel))
            {
                Debug.Log("[ReportsPanel] Nenhum modelo selecionado, ignorando clique");
                SetStatus("Selecione um modelo primeiro");
                return;
            }
            
            _isGeneratingReport = true;
            SetStatus($"Gerando relatório para '{_currentModel}'...");
            SetButtonsEnabled(false);
            
            // Gerar relatório
            if (reportRunner != null)
            {
                Debug.Log($"[ReportsPanel] Chamando RunReportForModel para: {_currentModel}");
                reportRunner.RunReportForModel(_currentModel);
                
                // Aguardar conclusão (será chamado via callback)
                StartCoroutine(WaitForReportCompletion());
            }
            else
            {
                Debug.LogError("[ReportsPanel] ReportRunner não configurado!");
                SetStatus("Erro: ReportRunner não configurado");
                _isGeneratingReport = false;
                SetButtonsEnabled(true);
            }
        }
        
        /// <summary>
        /// Aguarda a conclusão da geração do relatório
        /// </summary>
        private IEnumerator WaitForReportCompletion()
        {
            float timeout = 60f; // 60 segundos timeout
            float elapsed = 0f;
            
            while (_isGeneratingReport && elapsed < timeout)
            {
                yield return new WaitForSeconds(0.5f);
                elapsed += 0.5f;
                
                // Verificar se o relatório foi gerado
                var latestReport = MetricsPathProvider.GetLatestModelReport(_currentModel);
                if (!string.IsNullOrEmpty(latestReport) && latestReport != _lastReportPath)
                {
                    _lastReportPath = latestReport;
                    OnReportGenerated();
                    yield break;
                }
            }
            
            if (_isGeneratingReport)
            {
                Debug.LogWarning("[ReportsPanel] Timeout na geração do relatório");
                SetStatus("Timeout na geração do relatório");
                _isGeneratingReport = false;
                SetButtonsEnabled(true);
            }
        }
        
        /// <summary>
        /// Callback chamado quando o relatório é gerado
        /// </summary>
        public void OnReportGenerated()
        {
            _isGeneratingReport = false;
            SetStatus($"Relatório gerado com sucesso para '{_currentModel}'");
            SetButtonsEnabled(true);
            LoadPreviews();
        }
        
        /// <summary>
        /// Callback para botão Abrir HTML
        /// </summary>
        public void OnClickOpenHtml()
        {
            if (string.IsNullOrEmpty(_lastReportPath))
                return;
            
            string htmlPath = Path.Combine(_lastReportPath, "report.html");
            if (File.Exists(htmlPath))
            {
                OpenUrl(htmlPath);
                Debug.Log($"[ReportsPanel] Abrindo HTML: {htmlPath}");
            }
            else
            {
                Debug.LogWarning($"[ReportsPanel] HTML não encontrado: {htmlPath}");
                SetStatus("HTML não encontrado");
            }
        }
        
        /// <summary>
        /// Callback para botão Abrir PDF
        /// </summary>
        public void OnClickOpenPdf()
        {
            if (string.IsNullOrEmpty(_lastReportPath))
                return;
            
            string pdfPath = Path.Combine(_lastReportPath, "report.pdf");
            if (File.Exists(pdfPath))
            {
                OpenUrl(pdfPath);
                Debug.Log($"[ReportsPanel] Abrindo PDF: {pdfPath}");
            }
            else
            {
                Debug.LogWarning($"[ReportsPanel] PDF não encontrado: {pdfPath}");
                SetStatus("PDF não encontrado");
            }
        }
        
        /// <summary>
        /// Callback para botão Abrir Pasta
        /// </summary>
        public void OnClickOpenFolder()
        {
            if (string.IsNullOrEmpty(_lastReportPath))
                return;
            
            OpenUrl(_lastReportPath);
            Debug.Log($"[ReportsPanel] Abrindo pasta: {_lastReportPath}");
        }
        
        /// <summary>
        /// Carrega os previews PNG nos RawImages
        /// </summary>
        private void LoadPreviews()
        {
            if (string.IsNullOrEmpty(_lastReportPath))
                return;
            
            string imagesDir = Path.Combine(_lastReportPath, "images");
            
            // Carregar preview de carregamento
            LoadPngPreview(previewLoad, Path.Combine(imagesDir, "bars_load.png"));
            
            // Carregar preview de memória
            LoadPngPreview(previewMem, Path.Combine(imagesDir, "bars_mem.png"));
            
            // Carregar preview de FPS
            LoadPngPreview(previewFps, Path.Combine(imagesDir, "bars_fps.png"));
        }
        
        /// <summary>
        /// Carrega um PNG específico em um RawImage
        /// </summary>
        private void LoadPngPreview(RawImage rawImage, string pngPath)
        {
            if (rawImage == null || !File.Exists(pngPath))
                return;
            
            try
            {
                byte[] pngData = File.ReadAllBytes(pngPath);
                Texture2D texture = new Texture2D(2, 2);
                texture.LoadImage(pngData);
                rawImage.texture = texture;
                
                Debug.Log($"[ReportsPanel] Preview carregado: {pngPath}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ReportsPanel] Erro ao carregar preview {pngPath}: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Limpa todos os previews
        /// </summary>
        private void ClearPreviews()
        {
            if (previewLoad != null) previewLoad.texture = null;
            if (previewMem != null) previewMem.texture = null;
            if (previewFps != null) previewFps.texture = null;
        }
        
        /// <summary>
        /// Abre uma URL/path no aplicativo padrão do sistema
        /// </summary>
        private void OpenUrl(string path)
        {
            try
            {
                Application.OpenURL(path);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ReportsPanel] Erro ao abrir {path}: {ex.Message}");
                SetStatus($"Erro ao abrir: {Path.GetFileName(path)}");
            }
        }
        
        /// <summary>
        /// Define o texto de status
        /// </summary>
        private void SetStatus(string message)
        {
            if (statusLabel != null)
                statusLabel.text = message;
        }
        
        /// <summary>
        /// Habilita/desabilita os botões
        /// </summary>
        private void SetButtonsEnabled(bool enabled)
        {
            Debug.Log($"[ReportsPanel] SetButtonsEnabled({enabled}) - _isGeneratingReport: {_isGeneratingReport}, _lastReportPath: '{_lastReportPath}'");
            
            if (buttonGenerate != null)
            {
                bool generateEnabled = enabled && !_isGeneratingReport;
                buttonGenerate.interactable = generateEnabled;
                Debug.Log($"[ReportsPanel] ButtonGenerate.interactable = {generateEnabled}");
            }
            
            if (buttonOpenHtml != null)
                buttonOpenHtml.interactable = enabled && !string.IsNullOrEmpty(_lastReportPath);
            
            if (buttonOpenPdf != null)
                buttonOpenPdf.interactable = enabled && !string.IsNullOrEmpty(_lastReportPath);
            
            if (buttonOpenFolder != null)
                buttonOpenFolder.interactable = enabled && !string.IsNullOrEmpty(_lastReportPath);
            
            if (buttonFullscreen != null)
                buttonFullscreen.interactable = enabled && !string.IsNullOrEmpty(_lastReportPath);
        }
        
        /// <summary>
        /// Método público para atualizar a UI (pode ser chamado externamente)
        /// </summary>
        public void RefreshUI()
        {
            RefreshModelList();
            UpdateUI();
        }
        
        /// <summary>
        /// Obtém o caminho do último relatório gerado
        /// </summary>
        public string GetLastReportPath()
        {
            return _lastReportPath;
        }
        
        /// <summary>
        /// Verifica se está gerando relatório
        /// </summary>
        public bool IsGeneratingReport()
        {
            return _isGeneratingReport;
        }
    }
}
