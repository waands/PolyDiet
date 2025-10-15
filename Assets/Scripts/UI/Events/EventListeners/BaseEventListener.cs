using UnityEngine;

namespace PolyDiet.UI.Events
{
    /// <summary>
    /// Classe base para componentes que escutam eventos do GameEvents
    /// Fornece funcionalidades comuns como registro/desregistro automático
    /// </summary>
    public abstract class BaseEventListener : MonoBehaviour
    {
        [Header("Event Listener Settings")]
        [SerializeField] protected bool _enableDebugLogs = false;
        [SerializeField] protected bool _autoRegisterOnEnable = true;
        [SerializeField] protected bool _autoUnregisterOnDisable = true;
        
        protected virtual void OnEnable()
        {
            if (_autoRegisterOnEnable)
            {
                RegisterEvents();
            }
        }
        
        protected virtual void OnDisable()
        {
            if (_autoUnregisterOnDisable)
            {
                UnregisterEvents();
            }
        }
        
        protected virtual void OnDestroy()
        {
            // Garantir que eventos sejam desregistrados mesmo se OnDisable não foi chamado
            UnregisterEvents();
        }
        
        /// <summary>
        /// Registra os eventos que este listener escuta
        /// Deve ser implementado pelas classes filhas
        /// </summary>
        protected abstract void RegisterEvents();
        
        /// <summary>
        /// Desregistra os eventos que este listener escuta
        /// Deve ser implementado pelas classes filhas
        /// </summary>
        protected abstract void UnregisterEvents();
        
        /// <summary>
        /// Helper para debug logs condicionais
        /// </summary>
        protected void LogDebug(string message)
        {
            if (_enableDebugLogs)
            {
                Debug.Log($"[{GetType().Name}] {message}");
            }
        }
        
        /// <summary>
        /// Helper para debug logs condicionais com contexto
        /// </summary>
        protected void LogDebug(string context, string message)
        {
            if (_enableDebugLogs)
            {
                Debug.Log($"[{GetType().Name}] {context}: {message}");
            }
        }
        
        /// <summary>
        /// Helper para debug errors com contexto
        /// </summary>
        protected void LogError(string context, string message)
        {
            Debug.LogError($"[{GetType().Name}] {context}: {message}");
        }
        
        /// <summary>
        /// Helper para debug warnings com contexto
        /// </summary>
        protected void LogWarning(string context, string message)
        {
            Debug.LogWarning($"[{GetType().Name}] {context}: {message}");
        }
        
        /// <summary>
        /// Valida se o componente está configurado corretamente
        /// Pode ser sobrescrito pelas classes filhas para validações específicas
        /// </summary>
        protected virtual bool ValidateConfiguration()
        {
            return true;
        }
        
        /// <summary>
        /// Executa validação e registra eventos se válido
        /// </summary>
        protected void SafeRegisterEvents()
        {
            if (ValidateConfiguration())
            {
                RegisterEvents();
                LogDebug("Events registered successfully");
            }
            else
            {
                LogError("Configuration", "Invalid configuration, events not registered");
            }
        }
        
        /// <summary>
        /// Executa desregistro de eventos de forma segura
        /// </summary>
        protected void SafeUnregisterEvents()
        {
            try
            {
                UnregisterEvents();
                LogDebug("Events unregistered successfully");
            }
            catch (System.Exception e)
            {
                LogError("Unregister", $"Error unregistering events: {e.Message}");
            }
        }
    }
}
