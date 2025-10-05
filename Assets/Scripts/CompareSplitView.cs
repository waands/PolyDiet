using UnityEngine;
using UnityEngine.UI;

public class CompareSplitView : MonoBehaviour
{
    [Header("Cameras (full-screen)")]
    public Camera camA;
    public Camera camB;
    public Camera viewCamera;        // opcional: câmera de display para modo 1×

    [Header("Camera Sync")]
    public Camera mainOrbitCamera;   // a driver (tem SimpleOrbitCamera)

    [Header("No Camera Message Fix")]
    public bool hideNoCameraMessage = true;

    [Header("UI")]
    public Slider splitSlider;
    public RawImage compositeImage;
    public RectTransform splitLine;

    // Optional labels for side info (if you have Text components in your UI)
    // they are text mesh pro ui
    public TMPro.TextMeshProUGUI labelLeft;
    public TMPro.TextMeshProUGUI labelRight;

    [Header("Edge")]
    [Range(0f, 0.05f)] public float feather = 0.02f;

    [Header("Input Lock")]
    public bool lockCameraWhileDragging = true;

    Material _mat; RenderTexture _rtA, _rtB;

    void Awake()
    {
        // 1) Força estado inicial: compare OFF
        SafeSetActive(compositeImage, false);
        SafeSetActive(splitLine, false);
        SafeSetActive(splitSlider, false); // ADICIONADO: inicia slider desativado
        SafeSetActive(labelLeft, false);   // Desativa label esquerdo
        SafeSetActive(labelRight, false);  // Desativa label direito
        if (camA) camA.enabled = false;
        if (camB) camB.enabled = false;
        if (viewCamera) viewCamera.enabled = true; // desenha o display no 1×

        // opcional, mas recomendado: RawImage não captura clique
        if (compositeImage) compositeImage.raycastTarget = false;

        SetupDragLock();
    }

    void Start()
    {
        // garante followers ligados mesmo se alguém deixou A/B ON no prefab
        BindFollower(camA, mainOrbitCamera);
        BindFollower(camB, mainOrbitCamera);

        // garante material
        EnsureMaterial();
        
        // Configuração inicial do slider
        if (splitSlider)
        {
            splitSlider.value = 0.5f; // valor central por padrão
            Debug.Log($"[CompareSplitView] Slider inicializado com valor: {splitSlider.value}");
        }
        
        UpdateSplitUI(); // posiciona a linha e o corte inicial
    }

    void OnDestroy()
    {
        if (_mat) Destroy(_mat);
        if (_rtA) _rtA.Release();
        if (_rtB) _rtB.Release();
    }

    // === API chamada pelo HUD/Loader ===
    public void SetCompareActive(bool on)
    {
        if (on)
        {
            EnsureMaterial();
            EnsureRTs();

            // liga overlay
            SafeSetActive(compositeImage, true);
            SafeSetActive(splitLine, true);
            SafeSetActive(splitSlider, true); // ADICIONADO: ativa o slider
            SafeSetActive(labelLeft, true);   // Ativa label esquerdo
            SafeSetActive(labelRight, true);  // Ativa label direito
            if (compositeImage) compositeImage.texture = _rtA; // qualquer; o shader usa _TexA/_TexB

            // roteia render para RTs
            if (camA) { camA.targetTexture = _rtA; camA.enabled = true; }
            if (camB) { camB.targetTexture = _rtB; camB.enabled = true; }

            // driver para de renderizar, mas continua lendo input
            if (mainOrbitCamera) mainOrbitCamera.enabled = false;
            if (viewCamera)      viewCamera.enabled      = hideNoCameraMessage ? true : false; // opcional

            // followers garantidos
            BindFollower(camA, mainOrbitCamera);
            BindFollower(camB, mainOrbitCamera);

            UpdateSplitUI();
        }
        else
        {
            // desliga overlay/RTs
            SafeSetActive(compositeImage, false);
            SafeSetActive(splitLine, false);
            SafeSetActive(splitSlider, false); // ADICIONADO: desativa o slider
            SafeSetActive(labelLeft, false);   // Desativa label esquerdo
            SafeSetActive(labelRight, false);  // Desativa label direito

            if (camA) { camA.targetTexture = null; camA.enabled = false; }
            if (camB) { camB.targetTexture = null; camB.enabled = false; }

            if (viewCamera)      viewCamera.enabled      = true;
            if (mainOrbitCamera) mainOrbitCamera.enabled = true;
        }
    }

    void EnsureMaterial()
    {
        if (_mat) return;
        // cria material do shader e aplica no RawImage
        _mat = new Material(Shader.Find("Hidden/CompareComposite"));
        if (compositeImage) compositeImage.material = _mat;
    }

    void EnsureRTs()
    {
        int w = Mathf.Max(1, Screen.width);
        int h = Mathf.Max(1, Screen.height);
        if (_rtA && (_rtA.width != w || _rtA.height != h)) { _rtA.Release(); _rtA = null; }
        if (_rtB && (_rtB.width != w || _rtB.height != h)) { _rtB.Release(); _rtB = null; }
        if (_rtA == null) _rtA = new RenderTexture(w, h, 24, RenderTextureFormat.Default);
        if (_rtB == null) _rtB = new RenderTexture(w, h, 24, RenderTextureFormat.Default);
        if (_mat)
        {
            _mat.SetTexture("_TexA", _rtA);
            _mat.SetTexture("_TexB", _rtB);
        }
    }

    void BindFollower(Camera cam, Camera driver)
    {
        if (!cam || !driver) return;
        var f = cam.GetComponent<CameraPoseFollower>();
        if (!f) f = cam.gameObject.AddComponent<CameraPoseFollower>();
        f.source   = driver.transform;
        f.sourceCam= driver;
    }

    void SetupDragLock()
    {
        if (!lockCameraWhileDragging || !splitSlider) return;
        var et = splitSlider.GetComponent<UnityEngine.EventSystems.EventTrigger>();
        if (!et) et = splitSlider.gameObject.AddComponent<UnityEngine.EventSystems.EventTrigger>();
        et.triggers ??= new System.Collections.Generic.List<UnityEngine.EventSystems.EventTrigger.Entry>();
        AddTrig(et, UnityEngine.EventSystems.EventTriggerType.PointerDown, _ => UIInputLock.Lock(this));
        AddTrig(et, UnityEngine.EventSystems.EventTriggerType.BeginDrag,  _ => UIInputLock.Lock(this));
        AddTrig(et, UnityEngine.EventSystems.EventTriggerType.PointerUp,  _ => UIInputLock.Unlock(this));
        AddTrig(et, UnityEngine.EventSystems.EventTriggerType.EndDrag,    _ => UIInputLock.Unlock(this));

        // também atualiza o corte ao mover o slider
        splitSlider.onValueChanged.AddListener(_ => UpdateSplitUI());
    }
    static void AddTrig(UnityEngine.EventSystems.EventTrigger et,
        UnityEngine.EventSystems.EventTriggerType type,
        UnityEngine.Events.UnityAction<UnityEngine.EventSystems.BaseEventData> cb)
    {
        var e = new UnityEngine.EventSystems.EventTrigger.Entry { eventID = type };
        e.callback.AddListener(cb);
        et.triggers.Add(e);
    }

    void UpdateSplitUI()
    {
        float sliderValue = splitSlider ? splitSlider.value : 0.5f;
        Debug.Log($"[CompareSplitView] UpdateSplitUI: slider={sliderValue}, material={_mat != null}, splitLine={splitLine != null}");
        
        if (_mat && splitSlider)
        {
            _mat.SetFloat("_Split", sliderValue);
            _mat.SetFloat("_Feather", feather);
            Debug.Log($"[CompareSplitView] Material updated: Split={sliderValue}, Feather={feather}");
        }
        else
        {
            Debug.LogWarning($"[CompareSplitView] Cannot update material: _mat={_mat != null}, splitSlider={splitSlider != null}");
        }
        
        if (splitLine && compositeImage)
        {
            // alinha a linha visual ao mesmo percentual do corte
            splitLine.anchorMin = new Vector2(sliderValue, 0f);
            splitLine.anchorMax = new Vector2(sliderValue, 1f);
            splitLine.anchoredPosition = Vector2.zero;
            Debug.Log($"[CompareSplitView] Split line positioned at {sliderValue}");
        }
        else
        {
            Debug.LogWarning($"[CompareSplitView] Cannot update split line: splitLine={splitLine != null}, compositeImage={compositeImage != null}");
        }
    }

    static void SafeSetActive(Graphic g, bool on) { if (g) g.gameObject.SetActive(on); }
    static void SafeSetActive(Transform t, bool on) { if (t) t.gameObject.SetActive(on); }
    static void SafeSetActive(Slider s, bool on) { if (s) s.gameObject.SetActive(on); } // ADICIONADO: para Slider
    static void SafeSetActive(TMPro.TextMeshProUGUI txt, bool on) { if (txt) txt.gameObject.SetActive(on); } // ADICIONADO: para TextMeshPro

    // Métodos de compatibilidade para o HUDController
    public void SetSideInfo(string left, string right)
    {
        // Esta versão simplificada não tem labels visuais, mas podemos mostrar no console para debug
        Debug.Log($"[CompareSplitView] Comparação ativa: {left} × {right}");
        
        // Opcional: Se houver labels na UI, você pode descomentar e configurar aqui
        // Por exemplo, se você tiver Text components na UI:
        if (labelLeft) labelLeft.text = left;
        if (labelRight) labelRight.text = right;
    }

    public void ClearLabels()
    {
        // Limpa e desativa os labels
        if (labelLeft) 
        {
            labelLeft.text = "";
            labelLeft.gameObject.SetActive(false);
        }
        if (labelRight) 
        {
            labelRight.text = "";
            labelRight.gameObject.SetActive(false);
        }
    }

    public void ForceCleanupCameras()
    {
        // Limpa estados das câmeras
        if (camA) { camA.targetTexture = null; camA.enabled = false; }
        if (camB) { camB.targetTexture = null; camB.enabled = false; }
        if (mainOrbitCamera) mainOrbitCamera.enabled = true;
        if (viewCamera) viewCamera.enabled = true;
    }

    public void ResetAllComparisonCameras()
    {
        // Reset da câmera principal (SimpleOrbitCamera)
        if (mainOrbitCamera != null)
        {
            var orbitScript = mainOrbitCamera.GetComponent<SimpleOrbitCamera>();
            if (orbitScript != null)
            {
                orbitScript.ResetAngles(30f, 20f); // Reseta ângulos para padrão
                orbitScript.FrameTarget(); // Enquadra o modelo
            }
        }
    }
}
