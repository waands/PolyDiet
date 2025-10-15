using UnityEngine;
using PolyDiet.UI.Events;

namespace PolyDiet.Core.ModelLoading
{
    /// <summary>
    /// Event listener para CompareSplitView
    /// Escuta eventos do GameEvents e gerencia o modo de comparação
    /// </summary>
    public class CompareSplitViewEventListener : BaseEventListener
    {
        [Header("CompareSplitView Reference")]
        [SerializeField] private CompareSplitView _splitView;
        
        [Header("Event Settings")]
        [SerializeField] private bool _autoActivateOnCompareMode = true;
        [SerializeField] private bool _resetCamerasOnModeChange = true;
        [SerializeField] private bool _clearLabelsOnModeChange = true;
        
        protected void Awake()
        {
            // Se não foi atribuído, tenta encontrar na mesma GameObject
            if (_splitView == null)
            {
                _splitView = GetComponent<CompareSplitView>();
            }
        }
        
        protected override void RegisterEvents()
        {
            GameEvents.OnCompareModeChanged += HandleCompareModeChanged;
            GameEvents.OnModelLoaded += HandleModelLoaded;
            GameEvents.OnModelUnloaded += HandleModelUnloaded;
            GameEvents.OnCameraModeChanged += HandleCameraModeChanged;
            
            LogDebug("Registered CompareSplitView events");
        }
        
        protected override void UnregisterEvents()
        {
            GameEvents.OnCompareModeChanged -= HandleCompareModeChanged;
            GameEvents.OnModelLoaded -= HandleModelLoaded;
            GameEvents.OnModelUnloaded -= HandleModelUnloaded;
            GameEvents.OnCameraModeChanged -= HandleCameraModeChanged;
            
            LogDebug("Unregistered CompareSplitView events");
        }
        
        protected override bool ValidateConfiguration()
        {
            if (_splitView == null)
            {
                LogError("Configuration", "CompareSplitView reference is null");
                return false;
            }
            
            return true;
        }
        
        /// <summary>
        /// Manipula mudança de modo de comparação
        /// </summary>
        private void HandleCompareModeChanged(bool isActive)
        {
            if (_splitView == null)
            {
                LogError("HandleCompareModeChanged", "SplitView reference is null - cannot process compare mode change");
                return;
            }
            
            LogDebug("HandleCompareModeChanged", $"Processing compare mode change: {isActive}");
            
            try
            {
                if (_autoActivateOnCompareMode)
                {
                    _splitView.SetCompareActive(isActive);
                    LogDebug("HandleCompareModeChanged", $"SplitView compare mode set to: {isActive}");
                }
                
                if (isActive && _resetCamerasOnModeChange)
                {
                    _splitView.ResetAllComparisonCameras();
                    LogDebug("HandleCompareModeChanged", "Reset all comparison cameras");
                }
                
                if (!isActive && _clearLabelsOnModeChange)
                {
                    _splitView.ClearLabels();
                    LogDebug("HandleCompareModeChanged", "Cleared split view labels");
                }
            }
            catch (System.Exception e)
            {
                LogError("HandleCompareModeChanged", $"Failed to process compare mode change: {e.Message}");
            }
        }
        
        /// <summary>
        /// Manipula carregamento de modelo
        /// </summary>
        private void HandleModelLoaded(string modelName, string variant)
        {
            if (_splitView == null)
            {
                LogError("HandleModelLoaded", "SplitView reference is null - cannot process model load");
                return;
            }
            
            LogDebug("HandleModelLoaded", $"Processing model load: {modelName} ({variant})");
            
            // Se estivermos em modo de comparação, podemos atualizar as labels
            // ou fazer outras configurações específicas
            try
            {
                // Aqui podemos adicionar lógica específica para quando um modelo é carregado
                // Por exemplo, atualizar labels se estivermos em modo de comparação
                LogDebug("HandleModelLoaded", $"Model loaded successfully: {modelName} ({variant})");
            }
            catch (System.Exception e)
            {
                LogError("HandleModelLoaded", $"Failed to process model load: {e.Message}");
            }
        }
        
        /// <summary>
        /// Manipula descarregamento de modelo
        /// </summary>
        private void HandleModelUnloaded()
        {
            if (_splitView == null)
            {
                LogError("HandleModelUnloaded", "SplitView reference is null - cannot process model unload");
                return;
            }
            
            LogDebug("HandleModelUnloaded", "Processing model unload");
            
            try
            {
                // Limpa labels e força cleanup das câmeras
                _splitView.ClearLabels();
                _splitView.ForceCleanupCameras();
                LogDebug("HandleModelUnloaded", "Successfully cleared labels and cleaned up cameras");
            }
            catch (System.Exception e)
            {
                LogError("HandleModelUnloaded", $"Failed to process model unload: {e.Message}");
            }
        }
        
        /// <summary>
        /// Manipula mudança de modo da câmera
        /// </summary>
        private void HandleCameraModeChanged(string mode)
        {
            if (_splitView == null)
            {
                LogWarning("HandleCameraModeChanged", "SplitView reference is null");
                return;
            }
            
            LogDebug("HandleCameraModeChanged", $"Camera mode: {mode}");
            
            // Aqui podemos adicionar lógica específica baseada no modo da câmera
            switch (mode.ToLower())
            {
                case "compare":
                    // Configurações específicas para modo de comparação
                    if (_resetCamerasOnModeChange)
                    {
                        _splitView.ResetAllComparisonCameras();
                    }
                    break;
                    
                case "normal":
                    // Configurações específicas para modo normal
                    break;
                    
                default:
                    LogWarning("HandleCameraModeChanged", $"Unknown camera mode: {mode}");
                    break;
            }
        }
        
        /// <summary>
        /// Método público para ativar/desativar modo de comparação via código
        /// </summary>
        public void SetCompareMode(bool isActive)
        {
            if (_splitView != null)
            {
                _splitView.SetCompareActive(isActive);
            }
        }
        
        /// <summary>
        /// Método público para definir informações dos lados via código
        /// </summary>
        public void SetSideInfo(string leftInfo, string rightInfo)
        {
            if (_splitView != null)
            {
                _splitView.SetSideInfo(leftInfo, rightInfo);
            }
        }
        
        /// <summary>
        /// Método público para limpar labels via código
        /// </summary>
        public void ClearLabels()
        {
            if (_splitView != null)
            {
                _splitView.ClearLabels();
            }
        }
        
        /// <summary>
        /// Método público para resetar câmeras via código
        /// </summary>
        public void ResetCameras()
        {
            if (_splitView != null)
            {
                _splitView.ResetAllComparisonCameras();
            }
        }
        
        /// <summary>
        /// Método público para obter referência do SplitView
        /// </summary>
        public CompareSplitView GetSplitView()
        {
            return _splitView;
        }
        
        /// <summary>
        /// Método público para definir referência do SplitView
        /// </summary>
        public void SetSplitView(CompareSplitView splitView)
        {
            _splitView = splitView;
        }
        
        /// <summary>
        /// Método público para verificar se está em modo de comparação
        /// </summary>
        public bool IsCompareModeActive()
        {
            if (_splitView != null)
            {
                // Verifica se as câmeras de comparação estão ativas
                // Esta é uma implementação simples - pode ser melhorada
                return _splitView.camA != null && _splitView.camA.enabled;
            }
            return false;
        }
    }
}
