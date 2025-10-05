using UnityEngine;

public class SimpleOrbitCamera : MonoBehaviour
{
    [Header("Target")]
    public Transform target;     // deixe vazio para orbitar (0,0,0)
    
    [Header("Distance")]
    public float distance = 4f;
    public float minDistance = 0.5f;
    public float maxDistance = 20f;
    public float zoomSpeed = 4f;
    
    [Header("Rotation")]
    public float rotateSpeed = 300f;
    public Vector2 pitchClamp = new Vector2(-80f, 80f);
    
    [Header("Pan")]
    public float panSpeed = 8f;
    public bool enablePanning = true;
    
    [Header("Auto Positioning")]
    public bool autoFitToModel = true;
    public float autoFitMargin = 1.5f;
    
    [Header("Auto Fit (novo)")]
    public bool autoFitAnimated = true;
    public float autoFitDuration = 0.35f;
    public AnimationCurve autoFitEase = AnimationCurve.EaseInOut(0,0,1,1);
    public bool autoFitKeepAngles = true;  // mantém yaw/pitch atuais ao dar autofit?
    public float defaultYawOnFit = 30f;    // se não manter ângulos, usa esses
    public float defaultPitchOnFit = 20f;
    public bool adjustClipPlanes = true;   // ajusta near/far conforme o modelo

    [Header("UI Lock")]
    public bool lockWhenUIOpen = true;   // deixe ON
    public UnityEngine.CanvasGroup[] lockPanels; // opcional: arraste aqui os CanvasGroup dos painéis

    // Estado interno
    float _yaw;
    float _pitch = 15f;
    Vector3 _panOffset = Vector3.zero;
    
    // Valores padrão para reset
    float _defaultYaw;
    float _defaultPitch = 15f;
    float _defaultDistance;
    Vector3 _defaultPanOffset = Vector3.zero;

    void Start()
    {
        // Salva valores padrão para reset
        _defaultYaw = _yaw;
        _defaultPitch = _pitch;
        _defaultDistance = distance;
        
        // Posicionamento inicial automático
        if (autoFitToModel)
        {
            AutoFitToModel();
        }
    }

    void LateUpdate()
    {
        Vector3 pivot = (target ? target.position : Vector3.zero) + _panOffset;

        bool blocked = lockWhenUIOpen && (UIInputLock.IsLocked || IsAnyPanelBlocking());

        // Rotação (mouse esq), Pan (mouse dir), Zoom (scroll) — só quando NÃO bloqueado
        if (!blocked)
        {
            // Rotação orbital melhorada
            if (Input.GetMouseButton(0))
            {
                float mouseX = Input.GetAxis("Mouse X");
                float mouseY = Input.GetAxis("Mouse Y");
                
                // Rotação horizontal (yaw) - gira ao redor do objeto
                _yaw += mouseX * rotateSpeed * Time.deltaTime;
                
                // Rotação vertical (pitch) - move para cima/baixo
                _pitch -= mouseY * rotateSpeed * Time.deltaTime;
                _pitch = Mathf.Clamp(_pitch, pitchClamp.x, pitchClamp.y);
                
                // Log para debug (remova se não precisar)
                // Debug.Log($"Orbit: Yaw={_yaw:F1}°, Pitch={_pitch:F1}°, Distance={distance:F1}");
            }

            // Pan melhorado - move o centro de órbita
            if (enablePanning && Input.GetMouseButton(1))
            {
                // Usa a orientação atual da câmera para determinar direções
                Vector3 currentPos = CalculateSphericalPosition(pivot, _yaw, _pitch, distance);
                Vector3 panLookDirection = (pivot - currentPos).normalized;
                Vector3 right = Vector3.Cross(panLookDirection, Vector3.up).normalized;
                Vector3 up = Vector3.Cross(right, panLookDirection).normalized;
                
                // Pan relativo à vista atual
                Vector3 panDelta = (-Input.GetAxis("Mouse X") * right - Input.GetAxis("Mouse Y") * up) * panSpeed * Time.deltaTime;
                _panOffset += panDelta;
            }

            // Zoom
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.0001f)
            {
                distance = Mathf.Clamp(distance - scroll * zoomSpeed, minDistance, maxDistance);
            }
        }

        // Sempre aplica a pose atual (mesmo bloqueado)
        // Implementação melhorada de órbita esférica
        Vector3 sphericalPos = CalculateSphericalPosition(pivot, _yaw, _pitch, distance);
        
        // A câmera sempre olha para o pivot (centro do objeto)
        Vector3 lookDirection = (pivot - sphericalPos).normalized;
        Quaternion lookRotation = Quaternion.LookRotation(lookDirection);
        
        transform.SetPositionAndRotation(sphericalPos, lookRotation);
    }
    
    // Calcula posição esférica ao redor do pivot
    Vector3 CalculateSphericalPosition(Vector3 center, float yaw, float pitch, float radius)
    {
        // Converte ângulos para radianos
        float yawRad = yaw * Mathf.Deg2Rad;
        float pitchRad = pitch * Mathf.Deg2Rad;
        
        // Coordenadas esféricas para cartesianas
        float x = radius * Mathf.Cos(pitchRad) * Mathf.Sin(yawRad);
        float y = radius * Mathf.Sin(pitchRad);
        float z = radius * Mathf.Cos(pitchRad) * Mathf.Cos(yawRad);
        
        return center + new Vector3(x, y, z);
    }

    // Posicionamento automático baseado no modelo carregado
    public void AutoFitToModel()
    {
        if (target == null) return;

        // 1) Bounds do modelo
        Bounds b = CalculateModelBounds();
        if (b.size.sqrMagnitude < 1e-6f) return;

        // 2) Novo pivot no centro do modelo (ajusta panOffset)
        Vector3 worldPivot = target ? target.position : Vector3.zero;
        Vector3 newPanOffset = b.center - worldPivot;

        // 3) Distância ideal pelo FOV (cabe na vertical E horizontal)
        Camera cam = GetComponent<Camera>() != null ? GetComponent<Camera>() : Camera.main;
        if (cam == null) return;

        // vertical/horizontal half-FOV (em rad)
        float vFov = cam.fieldOfView * Mathf.Deg2Rad;
        float hFov = 2f * Mathf.Atan(Mathf.Tan(vFov * 0.5f) * cam.aspect);

        Vector3 ext = b.extents; // metade do tamanho em cada eixo
        float radius = ext.magnitude;

        // Distância necessária para caber verticalmente/horizontalmente
        // CORREÇÃO: Usar a maior dimensão do modelo para garantir que cabe completamente
        float maxModelDimension = Mathf.Max(ext.x, ext.y, ext.z);
        
        // Distância baseada na maior dimensão e no FOV mais restritivo
        float distV = maxModelDimension / Mathf.Tan(vFov * 0.5f);
        float distH = maxModelDimension / Mathf.Tan(hFov * 0.5f);
        float fitDistance = Mathf.Max(distV, distH);

        // CORREÇÃO: Aplicar margem ANTES de clampar para evitar distâncias muito pequenas
        fitDistance *= autoFitMargin;
        fitDistance = Mathf.Clamp(fitDistance, minDistance, maxDistance);

        // Debug info (pode remover depois)
        //Debug.Log($"[AutoFit] Model bounds: {b.size}, Max dimension: {maxModelDimension}, " +
        //         $"DistV: {distV:F2}, DistH: {distH:F2}, Final distance: {fitDistance:F2}");

        // 4) Ângulos
        float newYaw   = autoFitKeepAngles ? _yaw   : defaultYawOnFit;
        float newPitch = autoFitKeepAngles ? _pitch : Mathf.Clamp(defaultPitchOnFit, pitchClamp.x, pitchClamp.y);

        // 5) Ajuste de clip planes (opcional)
        if (adjustClipPlanes && cam != null)
        {
            float near = Mathf.Max(0.01f, fitDistance - radius * 1.2f);
            float far  = fitDistance + radius * 4f;
            // Evite near muito alto/baixo
            cam.nearClipPlane = Mathf.Clamp(near, 0.01f, 5f);
            cam.farClipPlane  = Mathf.Max(cam.nearClipPlane + 10f, far);
        }

        // 6) Aplicar (com animação ou direto)
        if (autoFitAnimated)
            StopAllCoroutines();

        if (autoFitAnimated && autoFitDuration > 0.01f)
            StartCoroutine(CoLerpFit(newPanOffset, fitDistance, newYaw, newPitch, autoFitDuration));
        else
        {
            _panOffset = newPanOffset;
            distance   = fitDistance;
            _yaw       = newYaw;
            _pitch     = newPitch;
        }
    }

    // Coroutine de animação suave para AutoFit
    System.Collections.IEnumerator CoLerpFit(
        Vector3 panTarget, float distTarget, float yawTarget, float pitchTarget, float dur)
    {
        Vector3 pan0 = _panOffset;
        float   d0   = distance;
        float   y0   = _yaw;
        float   p0   = _pitch;

        float t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            float k = autoFitEase != null ? autoFitEase.Evaluate(Mathf.Clamp01(t / dur)) : (t / dur);

            _panOffset = Vector3.Lerp(pan0, panTarget, k);
            distance   = Mathf.Lerp(d0,   distTarget, k);
            _yaw       = Mathf.LerpAngle(y0, yawTarget, k);
            _pitch     = Mathf.Lerp(p0, pitchTarget, k);

            yield return null;
        }

        _panOffset = panTarget;
        distance   = distTarget;
        _yaw       = yawTarget;
        _pitch     = pitchTarget;
    }


    // Calcula os bounds do modelo (incluindo todos os filhos)
    Bounds CalculateModelBounds()
    {
        if (target == null) return new Bounds(Vector3.zero, Vector3.zero);

        // Inclui inativos (true) e abrange todos os Renderers
        var renderers = target.GetComponentsInChildren<Renderer>(true);
        if (renderers == null || renderers.Length == 0)
            return new Bounds(target.position, Vector3.zero);

        Bounds b = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            b.Encapsulate(renderers[i].bounds);
        return b;
    }

    // Reset da câmera para posição padrão
    public void ResetCamera()
    {
        // Sempre aplica AutoFit para garantir posicionamento ideal
        if (autoFitToModel && target != null)
        {
            AutoFitToModel();
        }
        else
        {
            // Fallback para valores padrão se AutoFit não estiver disponível
            _yaw = _defaultYaw;
            _pitch = _defaultPitch;
            distance = _defaultDistance;
            _panOffset = _defaultPanOffset;
        }
    }

    // Define um novo target e ajusta automaticamente
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        if (autoFitToModel)
        {
            AutoFitToModel();
        }
    }

    // Se preferir também bloquear por "painéis ativos" específicos (CanvasGroup)
    public bool IsAnyPanelBlocking()
    {
        if (lockPanels == null) return false;
        for (int i = 0; i < lockPanels.Length; i++)
        {
            var cg = lockPanels[i];
            if (cg && cg.gameObject.activeInHierarchy && cg.interactable && cg.blocksRaycasts)
                return true;
        }
        return false;
    }
}
