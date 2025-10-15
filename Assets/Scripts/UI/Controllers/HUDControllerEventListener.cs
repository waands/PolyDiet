using UnityEngine;
using PolyDiet.UI.Events;

namespace PolyDiet.UI.Controllers
{
    /// <summary>
    /// Event listener para HUDController
    /// Escuta eventos do GameEvents e coordena ações do HUD
    /// </summary>
    public class HUDControllerEventListener : BaseEventListener
    {
        [Header("HUDController Reference")]
        [SerializeField] private HUDController _hudController;
        
        [Header("Event Settings")]
        [SerializeField] private bool _handleCameraEvents = true;
        [SerializeField] private bool _handleModelEvents = true;
        [SerializeField] private bool _handleUIEvents = true;
        [SerializeField] private bool _handleMetricsEvents = true;
        
        protected void Awake()
        {
            // Se não foi atribuído, tenta encontrar na mesma GameObject
            if (_hudController == null)
            {
                _hudController = GetComponent<HUDController>();
            }
        }
        
        protected override void RegisterEvents()
        {
            if (_handleCameraEvents)
            {
                GameEvents.OnCameraResetRequested += HandleCameraResetRequested;
                GameEvents.OnCameraTargetChanged += HandleCameraTargetChanged;
                GameEvents.OnCameraModeChanged += HandleCameraModeChanged;
            }
            
            if (_handleModelEvents)
            {
                GameEvents.OnModelLoaded += HandleModelLoaded;
                GameEvents.OnModelUnloaded += HandleModelUnloaded;
                GameEvents.OnModelLoadError += HandleModelLoadError;
                GameEvents.OnModelsListUpdated += HandleModelsListUpdated;
            }
            
            if (_handleUIEvents)
            {
                GameEvents.OnCompareModeChanged += HandleCompareModeChanged;
                GameEvents.OnPanelVisibilityChanged += HandlePanelVisibilityChanged;
                GameEvents.OnVariantSelectionChanged += HandleVariantSelectionChanged;
            }
            
            if (_handleMetricsEvents)
            {
                GameEvents.OnMetricsCollectionStarted += HandleMetricsCollectionStarted;
                GameEvents.OnMetricsCollectionCompleted += HandleMetricsCollectionCompleted;
                GameEvents.OnReportRequested += HandleReportRequested;
            }
            
            LogDebug("Registered HUDController events");
        }
        
        protected override void UnregisterEvents()
        {
            if (_handleCameraEvents)
            {
                GameEvents.OnCameraResetRequested -= HandleCameraResetRequested;
                GameEvents.OnCameraTargetChanged -= HandleCameraTargetChanged;
                GameEvents.OnCameraModeChanged -= HandleCameraModeChanged;
            }
            
            if (_handleModelEvents)
            {
                GameEvents.OnModelLoaded -= HandleModelLoaded;
                GameEvents.OnModelUnloaded -= HandleModelUnloaded;
                GameEvents.OnModelLoadError -= HandleModelLoadError;
                GameEvents.OnModelsListUpdated -= HandleModelsListUpdated;
            }
            
            if (_handleUIEvents)
            {
                GameEvents.OnCompareModeChanged -= HandleCompareModeChanged;
                GameEvents.OnPanelVisibilityChanged -= HandlePanelVisibilityChanged;
                GameEvents.OnVariantSelectionChanged -= HandleVariantSelectionChanged;
            }
            
            if (_handleMetricsEvents)
            {
                GameEvents.OnMetricsCollectionStarted -= HandleMetricsCollectionStarted;
                GameEvents.OnMetricsCollectionCompleted -= HandleMetricsCollectionCompleted;
                GameEvents.OnReportRequested -= HandleReportRequested;
            }
            
            LogDebug("Unregistered HUDController events");
        }
        
        protected override bool ValidateConfiguration()
        {
            if (_hudController == null)
            {
                LogError("Configuration", "HUDController reference is null");
                return false;
            }
            
            return true;
        }
        
        #region Camera Event Handlers
        
        /// <summary>
        /// Manipula solicitação de reset da câmera
        /// </summary>
        private void HandleCameraResetRequested()
        {
            if (_hudController == null)
            {
                LogWarning("HandleCameraResetRequested", "HUDController reference is null");
                return;
            }
            
            LogDebug("HandleCameraResetRequested", "Camera reset requested");
            
            // Chama o método de reset da câmera do HUDController
            // Note: Este método precisa ser público no HUDController
            // _hudController.ResetCamera();
        }
        
        /// <summary>
        /// Manipula mudança de target da câmera
        /// </summary>
        private void HandleCameraTargetChanged(Transform newTarget)
        {
            if (_hudController == null)
            {
                LogWarning("HandleCameraTargetChanged", "HUDController reference is null");
                return;
            }
            
            LogDebug("HandleCameraTargetChanged", $"Camera target changed: {(newTarget != null ? newTarget.name : "null")}");
            
            // Aqui podemos adicionar lógica específica para mudança de target
        }
        
        /// <summary>
        /// Manipula mudança de modo da câmera
        /// </summary>
        private void HandleCameraModeChanged(string mode)
        {
            if (_hudController == null)
            {
                LogWarning("HandleCameraModeChanged", "HUDController reference is null");
                return;
            }
            
            LogDebug("HandleCameraModeChanged", $"Camera mode changed: {mode}");
            
            // Aqui podemos adicionar lógica específica para mudança de modo
        }
        
        #endregion
        
        #region Model Event Handlers
        
        /// <summary>
        /// Manipula carregamento de modelo
        /// </summary>
        private void HandleModelLoaded(string modelName, string variant)
        {
            if (_hudController == null)
            {
                LogWarning("HandleModelLoaded", "HUDController reference is null");
                return;
            }
            
            LogDebug("HandleModelLoaded", $"Model loaded: {modelName} ({variant})");
            
            // Notifica o HUDController sobre o carregamento
            _hudController.NotifyModelLoaded(modelName, variant);
        }
        
        /// <summary>
        /// Manipula descarregamento de modelo
        /// </summary>
        private void HandleModelUnloaded()
        {
            if (_hudController == null)
            {
                LogWarning("HandleModelUnloaded", "HUDController reference is null");
                return;
            }
            
            LogDebug("HandleModelUnloaded", "Model unloaded");
            
            // Aqui podemos adicionar lógica específica para descarregamento
        }
        
        /// <summary>
        /// Manipula erro de carregamento de modelo
        /// </summary>
        private void HandleModelLoadError(string modelName, string variant, string errorMessage)
        {
            if (_hudController == null)
            {
                LogWarning("HandleModelLoadError", "HUDController reference is null");
                return;
            }
            
            LogDebug("HandleModelLoadError", $"Model load error: {modelName} ({variant}) - {errorMessage}");
            
            // Aqui podemos adicionar lógica específica para erros de carregamento
        }
        
        /// <summary>
        /// Manipula atualização da lista de modelos
        /// </summary>
        private void HandleModelsListUpdated(string[] modelNames)
        {
            if (_hudController == null)
            {
                LogWarning("HandleModelsListUpdated", "HUDController reference is null");
                return;
            }
            
            LogDebug("HandleModelsListUpdated", $"Models list updated: {modelNames.Length} models");
            
            // Aqui podemos adicionar lógica específica para atualização da lista
        }
        
        #endregion
        
        #region UI Event Handlers
        
        /// <summary>
        /// Manipula mudança de modo de comparação
        /// </summary>
        private void HandleCompareModeChanged(bool isActive)
        {
            if (_hudController == null)
            {
                LogWarning("HandleCompareModeChanged", "HUDController reference is null");
                return;
            }
            
            LogDebug("HandleCompareModeChanged", $"Compare mode changed: {isActive}");
            
            // Aqui podemos adicionar lógica específica para mudança de modo de comparação
        }
        
        /// <summary>
        /// Manipula mudança de visibilidade de painel
        /// </summary>
        private void HandlePanelVisibilityChanged(string panelName, bool isOpen)
        {
            if (_hudController == null)
            {
                LogWarning("HandlePanelVisibilityChanged", "HUDController reference is null");
                return;
            }
            
            LogDebug("HandlePanelVisibilityChanged", $"Panel visibility changed: {panelName} = {isOpen}");
            
            // Aqui podemos adicionar lógica específica para mudança de visibilidade
        }
        
        /// <summary>
        /// Manipula mudança de seleção de variantes
        /// </summary>
        private void HandleVariantSelectionChanged(string[] selectedVariants)
        {
            if (_hudController == null)
            {
                LogWarning("HandleVariantSelectionChanged", "HUDController reference is null");
                return;
            }
            
            LogDebug("HandleVariantSelectionChanged", $"Variant selection changed: [{string.Join(", ", selectedVariants)}]");
            
            // Aqui podemos adicionar lógica específica para mudança de seleção
        }
        
        #endregion
        
        #region Metrics Event Handlers
        
        /// <summary>
        /// Manipula início de coleta de métricas
        /// </summary>
        private void HandleMetricsCollectionStarted(string modelName, string variant)
        {
            if (_hudController == null)
            {
                LogWarning("HandleMetricsCollectionStarted", "HUDController reference is null");
                return;
            }
            
            LogDebug("HandleMetricsCollectionStarted", $"Metrics collection started: {modelName} ({variant})");
            
            // Aqui podemos adicionar lógica específica para início de coleta
        }
        
        /// <summary>
        /// Manipula fim de coleta de métricas
        /// </summary>
        private void HandleMetricsCollectionCompleted(string modelName, string variant, bool success)
        {
            if (_hudController == null)
            {
                LogWarning("HandleMetricsCollectionCompleted", "HUDController reference is null");
                return;
            }
            
            LogDebug("HandleMetricsCollectionCompleted", $"Metrics collection completed: {modelName} ({variant}) - Success: {success}");
            
            // Aqui podemos adicionar lógica específica para fim de coleta
        }
        
        /// <summary>
        /// Manipula solicitação de relatório
        /// </summary>
        private void HandleReportRequested(string modelName)
        {
            if (_hudController == null)
            {
                LogWarning("HandleReportRequested", "HUDController reference is null");
                return;
            }
            
            LogDebug("HandleReportRequested", $"Report requested: {modelName}");
            
            // Aqui podemos adicionar lógica específica para solicitação de relatório
        }
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// Método público para obter referência do HUDController
        /// </summary>
        public HUDController GetHUDController()
        {
            return _hudController;
        }
        
        /// <summary>
        /// Método público para definir referência do HUDController
        /// </summary>
        public void SetHUDController(HUDController hudController)
        {
            _hudController = hudController;
        }
        
        /// <summary>
        /// Método público para solicitar reset da câmera
        /// </summary>
        public void RequestCameraReset()
        {
            GameEvents.CameraResetRequested();
            LogDebug("RequestCameraReset", "Camera reset requested");
        }
        
        /// <summary>
        /// Método público para solicitar mudança de target da câmera
        /// </summary>
        public void RequestCameraTargetChange(Transform newTarget)
        {
            GameEvents.CameraTargetChanged(newTarget);
            LogDebug("RequestCameraTargetChange", $"Camera target change requested: {(newTarget != null ? newTarget.name : "null")}");
        }
        
        /// <summary>
        /// Método público para solicitar mudança de modo de comparação
        /// </summary>
        public void RequestCompareModeChange(bool isActive)
        {
            GameEvents.CompareModeChanged(isActive);
            LogDebug("RequestCompareModeChange", $"Compare mode change requested: {isActive}");
        }
        
        /// <summary>
        /// Método público para solicitar mudança de visibilidade de painel
        /// </summary>
        public void RequestPanelVisibilityChange(string panelName, bool isOpen)
        {
            GameEvents.PanelVisibilityChanged(panelName, isOpen);
            LogDebug("RequestPanelVisibilityChange", $"Panel visibility change requested: {panelName} = {isOpen}");
        }
        
        #endregion
    }
}
