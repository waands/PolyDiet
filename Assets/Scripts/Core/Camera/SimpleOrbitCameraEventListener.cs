using UnityEngine;
using PolyDiet.UI.Events;

namespace PolyDiet.Core.Camera
{
    /// <summary>
    /// Event listener para SimpleOrbitCamera
    /// Escuta eventos do GameEvents e aplica mudanças na câmera
    /// </summary>
    public class SimpleOrbitCameraEventListener : BaseEventListener
    {
        [Header("Camera Reference")]
        [SerializeField] private SimpleOrbitCamera _camera;
        
        [Header("Event Settings")]
        [SerializeField] private bool _autoFrameOnTargetChange = true;
        [SerializeField] private bool _resetAnglesOnReset = true;
        [SerializeField] private float _defaultYaw = 30f;
        [SerializeField] private float _defaultPitch = 20f;
        
        protected override void Awake()
        {
            // Se não foi atribuído, tenta encontrar na mesma GameObject
            if (_camera == null)
            {
                _camera = GetComponent<SimpleOrbitCamera>();
            }
        }
        
        protected override void RegisterEvents()
        {
            GameEvents.OnCameraTargetChanged += HandleCameraTargetChanged;
            GameEvents.OnCameraResetRequested += HandleCameraResetRequested;
            GameEvents.OnCameraModeChanged += HandleCameraModeChanged;
            
            LogDebug("Registered camera events");
        }
        
        protected override void UnregisterEvents()
        {
            GameEvents.OnCameraTargetChanged -= HandleCameraTargetChanged;
            GameEvents.OnCameraResetRequested -= HandleCameraResetRequested;
            GameEvents.OnCameraModeChanged -= HandleCameraModeChanged;
            
            LogDebug("Unregistered camera events");
        }
        
        protected override bool ValidateConfiguration()
        {
            if (_camera == null)
            {
                LogError("Configuration", "SimpleOrbitCamera reference is null");
                return false;
            }
            
            return true;
        }
        
        /// <summary>
        /// Manipula mudança de target da câmera
        /// </summary>
        private void HandleCameraTargetChanged(Transform newTarget)
        {
            if (_camera == null)
            {
                LogWarning("HandleCameraTargetChanged", "Camera reference is null");
                return;
            }
            
            LogDebug("HandleCameraTargetChanged", $"New target: {(newTarget != null ? newTarget.name : "null")}");
            
            _camera.SetTarget(newTarget, _autoFrameOnTargetChange);
        }
        
        /// <summary>
        /// Manipula solicitação de reset da câmera
        /// </summary>
        private void HandleCameraResetRequested()
        {
            if (_camera == null)
            {
                LogWarning("HandleCameraResetRequested", "Camera reference is null");
                return;
            }
            
            LogDebug("HandleCameraResetRequested", "Resetting camera");
            
            if (_resetAnglesOnReset)
            {
                _camera.ResetAngles(_defaultYaw, _defaultPitch);
            }
            
            // Frame o target atual se existir
            if (_camera.target != null)
            {
                _camera.FrameTarget();
            }
        }
        
        /// <summary>
        /// Manipula mudança de modo da câmera (Normal vs Compare)
        /// </summary>
        private void HandleCameraModeChanged(string mode)
        {
            if (_camera == null)
            {
                LogWarning("HandleCameraModeChanged", "Camera reference is null");
                return;
            }
            
            LogDebug("HandleCameraModeChanged", $"New mode: {mode}");
            
            // Aqui podemos adicionar lógica específica para diferentes modos
            // Por exemplo, ajustar configurações baseado no modo
            switch (mode.ToLower())
            {
                case "normal":
                    // Configurações para modo normal
                    break;
                    
                case "compare":
                    // Configurações para modo de comparação
                    // Por exemplo, ajustar FOV ou outras propriedades
                    break;
                    
                default:
                    LogWarning("HandleCameraModeChanged", $"Unknown camera mode: {mode}");
                    break;
            }
        }
        
        /// <summary>
        /// Método público para definir o target da câmera via código
        /// </summary>
        public void SetCameraTarget(Transform target)
        {
            if (_camera != null)
            {
                _camera.SetTarget(target, _autoFrameOnTargetChange);
            }
        }
        
        /// <summary>
        /// Método público para resetar a câmera via código
        /// </summary>
        public void ResetCamera()
        {
            if (_camera != null)
            {
                if (_resetAnglesOnReset)
                {
                    _camera.ResetAngles(_defaultYaw, _defaultPitch);
                }
                
                if (_camera.target != null)
                {
                    _camera.FrameTarget();
                }
            }
        }
        
        /// <summary>
        /// Método público para obter referência da câmera
        /// </summary>
        public SimpleOrbitCamera GetCamera()
        {
            return _camera;
        }
        
        /// <summary>
        /// Método público para definir referência da câmera
        /// </summary>
        public void SetCamera(SimpleOrbitCamera camera)
        {
            _camera = camera;
        }
    }
}
