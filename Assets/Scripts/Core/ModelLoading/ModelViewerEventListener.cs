using UnityEngine;
using PolyDiet.UI.Events;

namespace PolyDiet.Core.ModelLoading
{
    /// <summary>
    /// Event listener para ModelViewer
    /// Escuta eventos do GameEvents e gerencia carregamento de modelos
    /// </summary>
    public class ModelViewerEventListener : BaseEventListener
    {
        [Header("ModelViewer Reference")]
        [SerializeField] private ModelViewer _modelViewer;
        
        [Header("Event Settings")]
        [SerializeField] private bool _notifyOnModelLoad = true;
        [SerializeField] private bool _notifyOnModelUnload = true;
        [SerializeField] private bool _notifyOnModelsListUpdate = true;
        [SerializeField] private bool _notifyOnLoadError = true;
        
        protected override void Awake()
        {
            base.Awake();
            
            // Se não foi atribuído, tenta encontrar na mesma GameObject
            if (_modelViewer == null)
            {
                _modelViewer = GetComponent<ModelViewer>();
            }
        }
        
        protected override void RegisterEvents()
        {
            GameEvents.OnModelsListUpdated += HandleModelsListUpdated;
            GameEvents.OnModelLoadError += HandleModelLoadError;
            GameEvents.OnLongOperationChanged += HandleLongOperationChanged;
            
            LogDebug("Registered ModelViewer events");
        }
        
        protected override void UnregisterEvents()
        {
            GameEvents.OnModelsListUpdated -= HandleModelsListUpdated;
            GameEvents.OnModelLoadError -= HandleModelLoadError;
            GameEvents.OnLongOperationChanged -= HandleLongOperationChanged;
            
            LogDebug("Unregistered ModelViewer events");
        }
        
        protected override bool ValidateConfiguration()
        {
            if (_modelViewer == null)
            {
                LogError("Configuration", "ModelViewer reference is null");
                return false;
            }
            
            return true;
        }
        
        /// <summary>
        /// Manipula atualização da lista de modelos
        /// </summary>
        private void HandleModelsListUpdated(string[] modelNames)
        {
            if (_modelViewer == null)
            {
                LogWarning("HandleModelsListUpdated", "ModelViewer reference is null");
                return;
            }
            
            LogDebug("HandleModelsListUpdated", $"Models list updated: {modelNames.Length} models");
            
            // Aqui podemos adicionar lógica específica quando a lista de modelos é atualizada
            // Por exemplo, atualizar UI, validar seleções, etc.
        }
        
        /// <summary>
        /// Manipula erro de carregamento de modelo
        /// </summary>
        private void HandleModelLoadError(string modelName, string variant, string errorMessage)
        {
            if (_modelViewer == null)
            {
                LogWarning("HandleModelLoadError", "ModelViewer reference is null");
                return;
            }
            
            LogDebug("HandleModelLoadError", $"Model load error: {modelName} ({variant}) - {errorMessage}");
            
            // Aqui podemos adicionar lógica específica para lidar com erros de carregamento
            // Por exemplo, mostrar mensagem de erro na UI, tentar carregar variante alternativa, etc.
        }
        
        /// <summary>
        /// Manipula mudança de operações longas
        /// </summary>
        private void HandleLongOperationChanged(string operation, bool isStarted)
        {
            if (_modelViewer == null)
            {
                LogWarning("HandleLongOperationChanged", "ModelViewer reference is null");
                return;
            }
            
            LogDebug("HandleLongOperationChanged", $"Operation {(isStarted ? "started" : "completed")}: {operation}");
            
            // Aqui podemos adicionar lógica específica para operações longas
            // Por exemplo, mostrar indicador de progresso, desabilitar UI, etc.
        }
        
        /// <summary>
        /// Método público para notificar carregamento de modelo bem-sucedido
        /// </summary>
        public void NotifyModelLoaded(string modelName, string variant)
        {
            if (_notifyOnModelLoad)
            {
                GameEvents.ModelLoaded(modelName, variant);
                LogDebug("NotifyModelLoaded", $"Model loaded: {modelName} ({variant})");
            }
        }
        
        /// <summary>
        /// Método público para notificar descarregamento de modelo
        /// </summary>
        public void NotifyModelUnloaded()
        {
            if (_notifyOnModelUnload)
            {
                GameEvents.ModelUnloaded();
                LogDebug("NotifyModelUnloaded", "Model unloaded");
            }
        }
        
        /// <summary>
        /// Método público para notificar erro de carregamento
        /// </summary>
        public void NotifyModelLoadError(string modelName, string variant, string errorMessage)
        {
            if (_notifyOnLoadError)
            {
                GameEvents.ModelLoadError(modelName, variant, errorMessage);
                LogDebug("NotifyModelLoadError", $"Model load error: {modelName} ({variant}) - {errorMessage}");
            }
        }
        
        /// <summary>
        /// Método público para notificar atualização da lista de modelos
        /// </summary>
        public void NotifyModelsListUpdated(string[] modelNames)
        {
            if (_notifyOnModelsListUpdate)
            {
                GameEvents.ModelsListUpdated(modelNames);
                LogDebug("NotifyModelsListUpdated", $"Models list updated: {modelNames.Length} models");
            }
        }
        
        /// <summary>
        /// Método público para notificar início de operação longa
        /// </summary>
        public void NotifyLongOperationStarted(string operation)
        {
            GameEvents.LongOperationChanged(operation, true);
            LogDebug("NotifyLongOperationStarted", $"Operation started: {operation}");
        }
        
        /// <summary>
        /// Método público para notificar fim de operação longa
        /// </summary>
        public void NotifyLongOperationCompleted(string operation)
        {
            GameEvents.LongOperationChanged(operation, false);
            LogDebug("NotifyLongOperationCompleted", $"Operation completed: {operation}");
        }
        
        /// <summary>
        /// Método público para obter referência do ModelViewer
        /// </summary>
        public ModelViewer GetModelViewer()
        {
            return _modelViewer;
        }
        
        /// <summary>
        /// Método público para definir referência do ModelViewer
        /// </summary>
        public void SetModelViewer(ModelViewer modelViewer)
        {
            _modelViewer = modelViewer;
        }
        
        /// <summary>
        /// Método público para obter nome do modelo selecionado
        /// </summary>
        public string GetSelectedModelName()
        {
            if (_modelViewer != null)
            {
                return _modelViewer.GetSelectedModelNamePublic();
            }
            return null;
        }
        
        /// <summary>
        /// Método público para obter variantes disponíveis
        /// </summary>
        public string[] GetAvailableVariants()
        {
            if (_modelViewer != null)
            {
                return _modelViewer.GetAvailableVariantsPublic();
            }
            return new string[0];
        }
        
        /// <summary>
        /// Método público para verificar se um modelo existe
        /// </summary>
        public bool ModelExists(string modelName)
        {
            if (_modelViewer != null)
            {
                return _modelViewer.ModelExists(modelName);
            }
            return false;
        }
        
        /// <summary>
        /// Método público para verificar se um modelo tem variantes comprimidas
        /// </summary>
        public bool HasCompressedVariants(string modelName)
        {
            if (_modelViewer != null)
            {
                return _modelViewer.HasCompressedVariants(modelName);
            }
            return false;
        }
    }
}
