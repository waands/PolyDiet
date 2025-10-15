using UnityEngine;

public class SimpleOrbitCamera : MonoBehaviour
{
    [Header("Target / Focus")]
    public Transform target;                 // pivô (use o root do modelo instanciado)
    [Tooltip("Margem multiplicativa no AutoFit (1.1–1.6).")]
    public float fitMargin = 1.3f;

    [Header("Orbit")]
    public float yaw   = 30f;
    public float pitch = 20f;
    public Vector2 pitchLimits = new Vector2(-80f, 80f);
    public float orbitSpeed = 180f;          // graus/seg
    
    [Header("Distance")]
    public float distance = 4f;
    public float minDistance = 0.2f;
    public float maxDistance = 100f;
    public float zoomResponsiveness = 1.0f;  // 1 = direto, >1 mais responsivo
    public float zoomScale = 0.2f;           // fração da distância por notch
    
    [Header("Pan")]
    public bool  enablePan = true;
    public float panSpeed = 1.0f;            // 1 = natural; aumente se quiser mais rápido

    [Header("Stability / Safety")]
    public bool keepNearModel = true;        // "coleira" de pan pra não se perder
    [Range(1f, 10f)] public float panLeashRadiuses = 4f; // quantos "raios" do modelo
    public bool autoAdjustClipPlanes = true;

    [Header("UI Lock")]
    public bool respectUILock = true;        // usa UIInputLock.IsLocked
    public CanvasGroup[] lockPanels;         // painéis que bloqueiam input

    // estado interno
    Vector3 _focus;                  // ponto olhando
    Bounds  _lastBounds; bool _hasBounds;
    float   _distance;               // distância efetiva (suavizada)
    Camera  _cam;

    void Awake()
    {
        _cam = GetComponent<Camera>();
        _distance = Mathf.Clamp(distance, minDistance, maxDistance);
        if (target) FrameTarget(); else _focus = Vector3.zero;
    }

    void LateUpdate()
    {
        bool blocked = respectUILock && (IsUILocked() || IsAnyPanelBlocking());

        // ===== leitura de input (driver único) =====
        if (!blocked)
        {
            // Orbit (LMB)
            if (Input.GetMouseButton(0))
            {
                yaw   += Input.GetAxis("Mouse X") * orbitSpeed * Time.deltaTime;
                pitch -= Input.GetAxis("Mouse Y") * orbitSpeed * Time.deltaTime;
                pitch  = Mathf.Clamp(pitch, pitchLimits.x, pitchLimits.y);
            }

            // Pan (RMB)
            if (enablePan && Input.GetMouseButton(1))
            {
                // Baseado na escala de imagem: pan proporcional à distância/FOV
                float vFovRad = (_cam ? _cam.fieldOfView : 60f) * Mathf.Deg2Rad;
                float pixelsToWorld = Mathf.Tan(vFovRad * 0.5f) * _distance * 2f / Mathf.Max(1, Screen.height);
                float k = panSpeed * pixelsToWorld * 100f; // 100 ~ sensibilidade "boa"
                Vector3 right = transform.right;
                Vector3 up    = transform.up;
                _focus += (-Input.GetAxis("Mouse X") * right - Input.GetAxis("Mouse Y") * up) * k;
            }

            // Zoom (scroll), proporcional à distância
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 1e-4f)
            {
                float step = (_distance * zoomScale + 0.25f) * (-scroll) * zoomResponsiveness;
                _distance = Mathf.Clamp(_distance + step, minDistance, maxDistance);
            }
        }

        // ===== segurança contra "perder o alvo" =====
        if (keepNearModel && _hasBounds)
        {
            float leash = _lastBounds.extents.magnitude * panLeashRadiuses;
            Vector3 toFocus = _focus - _lastBounds.center;
            if (toFocus.sqrMagnitude > leash * leash)
                _focus = _lastBounds.center + toFocus.normalized * leash;

            // evita distâncias absurdas por acidente
            _distance = Mathf.Clamp(_distance, minDistance, Mathf.Max(minDistance * 2f, _lastBounds.extents.magnitude * 20f));
        }

        // ===== aplica pose =====
        Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 pos = _focus + rot * (Vector3.back * _distance);
        transform.SetPositionAndRotation(pos, rot);
    }

    // ===== API =====

    public void FrameTarget()
    {
        if (!target) return;

        // bounds do modelo (todos Renderers, ativos/inativos)
        var rs = target.GetComponentsInChildren<Renderer>(true);
        if (rs.Length == 0)
        {
            _focus = target.position;
            return;
        }
        Bounds b = rs[0].bounds;
        for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);

        _lastBounds = b; _hasBounds = true;
        _focus = b.center;

        // distância por bounding sphere + FOV mínimo (garante caber em W e H)
        float r = b.extents.magnitude;
        if (r < 1e-4f) r = 0.5f;
        float v = ((_cam ? _cam.fieldOfView : 60f) * Mathf.Deg2Rad) * 0.5f;
        float h = Mathf.Atan(Mathf.Tan(v) * (_cam ? _cam.aspect : 16f/9f));
        float halfMin = Mathf.Min(v, h);
        float fit = r / Mathf.Sin(Mathf.Max(0.1f, halfMin)); // evita sin(0)
        fit *= Mathf.Max(1.01f, fitMargin);
        _distance = distance = Mathf.Clamp(fit, minDistance, maxDistance);

        if (autoAdjustClipPlanes && _cam)
        {
            float near = Mathf.Max(0.01f, _distance - r * 1.2f);
            float far  = _distance + r * 4f;
            _cam.nearClipPlane = Mathf.Clamp(near, 0.01f, 5f);
            _cam.farClipPlane  = Mathf.Max(_cam.nearClipPlane + 10f, far);
        }
    }

    public void SetTarget(Transform t, bool autoFrame = true)
    {
        target = t;
        if (autoFrame) FrameTarget();
    }

    public void ResetAngles(float newYaw = 30f, float newPitch = 20f)
    {
        yaw = newYaw;
        pitch = Mathf.Clamp(newPitch, pitchLimits.x, pitchLimits.y);
    }

    // ===== helpers =====
    bool IsUILocked()
    {
        // se não existir sua classe, comente esta linha
        return UIInputLock.IsLocked;
    }
    bool IsAnyPanelBlocking()
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
