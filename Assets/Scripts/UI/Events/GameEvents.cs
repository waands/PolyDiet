using System;
using UnityEngine;

namespace PolyDiet.UI.Events
{
    /// <summary>
    /// Sistema central de eventos para comunicação entre componentes
    /// Desacopla componentes que anteriormente tinham dependências diretas
    /// </summary>
    public static class GameEvents
    {
        #region Model Events
        
        /// <summary>
        /// Disparado quando um modelo é carregado com sucesso
        /// </summary>
        /// <param name="modelName">Nome do modelo carregado</param>
        /// <param name="variant">Variante do modelo (original, draco, meshopt)</param>
        public static event Action<string, string> OnModelLoaded;
        
        /// <summary>
        /// Disparado quando um modelo é descarregado
        /// </summary>
        public static event Action OnModelUnloaded;
        
        /// <summary>
        /// Disparado quando há erro no carregamento de modelo
        /// </summary>
        /// <param name="modelName">Nome do modelo que falhou</param>
        /// <param name="variant">Variante que falhou</param>
        /// <param name="errorMessage">Mensagem de erro</param>
        public static event Action<string, string, string> OnModelLoadError;
        
        /// <summary>
        /// Disparado quando a lista de modelos disponíveis é atualizada
        /// </summary>
        /// <param name="modelNames">Lista de nomes de modelos disponíveis</param>
        public static event Action<string[]> OnModelsListUpdated;
        
        #endregion
        
        #region UI Events
        
        /// <summary>
        /// Disparado quando o modo de comparação é ativado/desativado
        /// </summary>
        /// <param name="isActive">True se modo compare ativo, false se desativado</param>
        public static event Action<bool> OnCompareModeChanged;
        
        /// <summary>
        /// Disparado quando painel de UI é aberto/fechado
        /// </summary>
        /// <param name="panelName">Nome do painel (ex: "Wizard", "Metrics", "Compare")</param>
        /// <param name="isOpen">True se aberto, false se fechado</param>
        public static event Action<string, bool> OnPanelVisibilityChanged;
        
        /// <summary>
        /// Disparado quando há mudança na seleção de variantes
        /// </summary>
        /// <param name="selectedVariants">Lista de variantes selecionadas</param>
        public static event Action<string[]> OnVariantSelectionChanged;
        
        #endregion
        
        #region Camera Events
        
        /// <summary>
        /// Disparado quando reset da câmera é solicitado
        /// </summary>
        public static event Action OnCameraResetRequested;
        
        /// <summary>
        /// Disparado quando target da câmera é alterado
        /// </summary>
        /// <param name="target">Novo target da câmera</param>
        public static event Action<Transform> OnCameraTargetChanged;
        
        /// <summary>
        /// Disparado quando modo de câmera muda (normal vs compare)
        /// </summary>
        /// <param name="mode">Modo da câmera ("Normal", "Compare")</param>
        public static event Action<string> OnCameraModeChanged;
        
        #endregion
        
        #region Metrics Events
        
        /// <summary>
        /// Disparado quando coleta de métricas é iniciada
        /// </summary>
        /// <param name="modelName">Nome do modelo sendo testado</param>
        /// <param name="variant">Variante sendo testada</param>
        public static event Action<string, string> OnMetricsCollectionStarted;
        
        /// <summary>
        /// Disparado quando coleta de métricas é finalizada
        /// </summary>
        /// <param name="modelName">Nome do modelo testado</param>
        /// <param name="variant">Variante testada</param>
        /// <param name="success">True se coleta bem-sucedida</param>
        public static event Action<string, string, bool> OnMetricsCollectionCompleted;
        
        /// <summary>
        /// Disparado quando relatório é solicitado
        /// </summary>
        /// <param name="modelName">Nome do modelo para relatório</param>
        public static event Action<string> OnReportRequested;
        
        #endregion
        
        #region System Events
        
        /// <summary>
        /// Disparado quando há erro crítico no sistema
        /// </summary>
        /// <param name="component">Nome do componente que gerou o erro</param>
        /// <param name="errorMessage">Mensagem de erro</param>
        /// <param name="exception">Exceção original (opcional)</param>
        public static event Action<string, string, Exception> OnSystemError;
        
        /// <summary>
        /// Disparado quando operação longa é iniciada/finalizada
        /// </summary>
        /// <param name="operation">Nome da operação</param>
        /// <param name="isStarted">True se iniciada, false se finalizada</param>
        public static event Action<string, bool> OnLongOperationChanged;
        
        #endregion
        
        #region Event Helpers
        
        /// <summary>
        /// Helper para disparar evento de modelo carregado
        /// </summary>
        public static void ModelLoaded(string modelName, string variant)
        {
            OnModelLoaded?.Invoke(modelName, variant);
            Debug.Log($"[GameEvents] Model loaded: {modelName} ({variant})");
        }
        
        /// <summary>
        /// Helper para disparar evento de modelo descarregado
        /// </summary>
        public static void ModelUnloaded()
        {
            OnModelUnloaded?.Invoke();
            Debug.Log("[GameEvents] Model unloaded");
        }
        
        /// <summary>
        /// Helper para disparar evento de erro de carregamento
        /// </summary>
        public static void ModelLoadError(string modelName, string variant, string errorMessage)
        {
            OnModelLoadError?.Invoke(modelName, variant, errorMessage);
            Debug.LogError($"[GameEvents] Model load error: {modelName} ({variant}) - {errorMessage}");
        }
        
        /// <summary>
        /// Helper para disparar evento de lista de modelos atualizada
        /// </summary>
        public static void ModelsListUpdated(string[] modelNames)
        {
            OnModelsListUpdated?.Invoke(modelNames);
            Debug.Log($"[GameEvents] Models list updated: {modelNames.Length} models");
        }
        
        /// <summary>
        /// Helper para disparar evento de modo de comparação
        /// </summary>
        public static void CompareModeChanged(bool isActive)
        {
            OnCompareModeChanged?.Invoke(isActive);
            Debug.Log($"[GameEvents] Compare mode changed: {isActive}");
        }
        
        /// <summary>
        /// Helper para disparar evento de visibilidade de painel
        /// </summary>
        public static void PanelVisibilityChanged(string panelName, bool isOpen)
        {
            OnPanelVisibilityChanged?.Invoke(panelName, isOpen);
            Debug.Log($"[GameEvents] Panel visibility changed: {panelName} = {isOpen}");
        }
        
        /// <summary>
        /// Helper para disparar evento de seleção de variantes
        /// </summary>
        public static void VariantSelectionChanged(string[] selectedVariants)
        {
            OnVariantSelectionChanged?.Invoke(selectedVariants);
            Debug.Log($"[GameEvents] Variant selection changed: [{string.Join(", ", selectedVariants)}]");
        }
        
        /// <summary>
        /// Helper para disparar evento de reset de câmera
        /// </summary>
        public static void CameraResetRequested()
        {
            OnCameraResetRequested?.Invoke();
            Debug.Log("[GameEvents] Camera reset requested");
        }
        
        /// <summary>
        /// Helper para disparar evento de target de câmera alterado
        /// </summary>
        public static void CameraTargetChanged(Transform target)
        {
            OnCameraTargetChanged?.Invoke(target);
            Debug.Log($"[GameEvents] Camera target changed: {(target != null ? target.name : "null")}");
        }
        
        /// <summary>
        /// Helper para disparar evento de modo de câmera alterado
        /// </summary>
        public static void CameraModeChanged(string mode)
        {
            OnCameraModeChanged?.Invoke(mode);
            Debug.Log($"[GameEvents] Camera mode changed: {mode}");
        }
        
        /// <summary>
        /// Helper para disparar evento de início de coleta de métricas
        /// </summary>
        public static void MetricsCollectionStarted(string modelName, string variant)
        {
            OnMetricsCollectionStarted?.Invoke(modelName, variant);
            Debug.Log($"[GameEvents] Metrics collection started: {modelName} ({variant})");
        }
        
        /// <summary>
        /// Helper para disparar evento de fim de coleta de métricas
        /// </summary>
        public static void MetricsCollectionCompleted(string modelName, string variant, bool success)
        {
            OnMetricsCollectionCompleted?.Invoke(modelName, variant, success);
            Debug.Log($"[GameEvents] Metrics collection completed: {modelName} ({variant}) - Success: {success}");
        }
        
        /// <summary>
        /// Helper para disparar evento de solicitação de relatório
        /// </summary>
        public static void ReportRequested(string modelName)
        {
            OnReportRequested?.Invoke(modelName);
            Debug.Log($"[GameEvents] Report requested: {modelName}");
        }
        
        /// <summary>
        /// Helper para disparar evento de erro do sistema
        /// </summary>
        public static void SystemError(string component, string errorMessage, Exception exception = null)
        {
            OnSystemError?.Invoke(component, errorMessage, exception);
            Debug.LogError($"[GameEvents] System error in {component}: {errorMessage}");
            if (exception != null)
            {
                Debug.LogException(exception);
            }
        }
        
        /// <summary>
        /// Helper para disparar evento de operação longa
        /// </summary>
        public static void LongOperationChanged(string operation, bool isStarted)
        {
            OnLongOperationChanged?.Invoke(operation, isStarted);
            Debug.Log($"[GameEvents] Long operation {(isStarted ? "started" : "completed")}: {operation}");
        }
        
        /// <summary>
        /// Helper para disparar evento de modelo carregado
        /// </summary>
        public static void NotifyModelLoaded(string modelName, string variant)
        {
            OnModelLoaded?.Invoke(modelName, variant);
            Debug.Log($"[GameEvents] Model loaded: {modelName} ({variant})");
        }
        
        /// <summary>
        /// Helper para disparar evento de erro no carregamento de modelo
        /// </summary>
        public static void NotifyModelLoadError(string modelName, string variant, string errorMessage)
        {
            OnModelLoadError?.Invoke(modelName, variant, errorMessage);
            Debug.LogError($"[GameEvents] Model load error: {modelName} ({variant}) - {errorMessage}");
        }
        
        /// <summary>
        /// Helper para disparar evento de modelo descarregado
        /// </summary>
        public static void NotifyModelUnloaded()
        {
            OnModelUnloaded?.Invoke();
            Debug.Log($"[GameEvents] Model unloaded");
        }
        
        /// <summary>
        /// Helper para disparar evento de lista de modelos atualizada
        /// </summary>
        public static void NotifyModelsListUpdated(string[] modelNames)
        {
            OnModelsListUpdated?.Invoke(modelNames);
            Debug.Log($"[GameEvents] Models list updated: {modelNames.Length} models");
        }
        
        #endregion
        
        #region Event Management
        
        /// <summary>
        /// Limpa todos os eventos (útil para testes e cleanup)
        /// </summary>
        public static void ClearAllEvents()
        {
            OnModelLoaded = null;
            OnModelUnloaded = null;
            OnModelLoadError = null;
            OnModelsListUpdated = null;
            OnCompareModeChanged = null;
            OnPanelVisibilityChanged = null;
            OnVariantSelectionChanged = null;
            OnCameraResetRequested = null;
            OnCameraTargetChanged = null;
            OnCameraModeChanged = null;
            OnMetricsCollectionStarted = null;
            OnMetricsCollectionCompleted = null;
            OnReportRequested = null;
            OnSystemError = null;
            OnLongOperationChanged = null;
            
            Debug.Log("[GameEvents] All events cleared");
        }
        
        /// <summary>
        /// Retorna o número de listeners ativos para debug
        /// </summary>
        public static int GetActiveListenersCount()
        {
            int count = 0;
            if (OnModelLoaded != null) count += OnModelLoaded.GetInvocationList().Length;
            if (OnModelUnloaded != null) count += OnModelUnloaded.GetInvocationList().Length;
            if (OnModelLoadError != null) count += OnModelLoadError.GetInvocationList().Length;
            if (OnModelsListUpdated != null) count += OnModelsListUpdated.GetInvocationList().Length;
            if (OnCompareModeChanged != null) count += OnCompareModeChanged.GetInvocationList().Length;
            if (OnPanelVisibilityChanged != null) count += OnPanelVisibilityChanged.GetInvocationList().Length;
            if (OnVariantSelectionChanged != null) count += OnVariantSelectionChanged.GetInvocationList().Length;
            if (OnCameraResetRequested != null) count += OnCameraResetRequested.GetInvocationList().Length;
            if (OnCameraTargetChanged != null) count += OnCameraTargetChanged.GetInvocationList().Length;
            if (OnCameraModeChanged != null) count += OnCameraModeChanged.GetInvocationList().Length;
            if (OnMetricsCollectionStarted != null) count += OnMetricsCollectionStarted.GetInvocationList().Length;
            if (OnMetricsCollectionCompleted != null) count += OnMetricsCollectionCompleted.GetInvocationList().Length;
            if (OnReportRequested != null) count += OnReportRequested.GetInvocationList().Length;
            if (OnSystemError != null) count += OnSystemError.GetInvocationList().Length;
            if (OnLongOperationChanged != null) count += OnLongOperationChanged.GetInvocationList().Length;
            
            return count;
        }
        
        #endregion
    }
}
