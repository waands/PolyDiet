using UnityEngine;
using UnityEngine.UI;

public enum ViewMode { Split, SideBySide }

public class CompareSplitView : MonoBehaviour
{
    [Header("Cameras (full-screen)")]
    public Camera camA;
    public Camera camB;
    public Camera viewCamera;        // opcional: câmera de display para modo 1×

    [Header("Camera Sync")]
    public Camera mainOrbitCamera;   // a driver (tem SimpleOrbitCamera)
    public ModelViewer modelViewer;   // referência para acessar configuração de skybox

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
    [Range(0f, 0.05f)] public float feather = 0f; // Zero para divisão totalmente brusca (sem smoothstep)

    [Header("Input Lock")]
    public bool lockCameraWhileDragging = true;

    private const string SideBySideProp = "_SideBySide";
    Material _mat; RenderTexture _rtA, _rtB;
    private ViewMode _currentViewMode = ViewMode.Split;

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
            
            // Verifica configuração do RectTransform do slider
            VerifySliderConfiguration();
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
    public void SetCompareActive(bool on, ViewMode mode = ViewMode.Split)
    {
        _currentViewMode = mode;
        Debug.Log($"[CompareSplitView] SetCompareActive chamado - on={on}, mode={mode}");
        
        if (on)
        {
            EnsureMaterial();
            ApplyShaderMode();
            EnsureRTs();

            // liga overlay
            SafeSetActive(compositeImage, true);
            
            // Configura UI baseado no modo
            if (_currentViewMode == ViewMode.SideBySide)
            {
                // Modo Side by Side: oculta slider e linha divisória
                SafeSetActive(splitLine, false);
                SafeSetActive(splitSlider, false);
                Debug.Log("[CompareSplitView] ✅ Modo Side by Side - slider e linha divisória ocultos");
            }
            else
            {
                // Modo Split: mostra slider e linha divisória
                SafeSetActive(splitLine, true);
                SafeSetActive(splitSlider, true);
                Debug.Log("[CompareSplitView] ✅ Modo Split - slider e linha divisória visíveis");
            }
            
            SafeSetActive(labelLeft, true);   // Ativa label esquerdo
            SafeSetActive(labelRight, true);  // Ativa label direito
            if (compositeImage) compositeImage.texture = _rtA; // qualquer; o shader usa _TexA/_TexB

            // roteia render para RTs
            if (camA) { camA.targetTexture = _rtA; camA.enabled = true; }
            if (camB) { camB.targetTexture = _rtB; camB.enabled = true; }
            Debug.Log($"[CompareSplitView] Câmeras configuradas - camA: {(_rtA != null ? $"RT {_rtA.width}x{_rtA.height}" : "null")}, camB: {(_rtB != null ? $"RT {_rtB.width}x{_rtB.height}" : "null")}");

            // driver para de renderizar, mas continua lendo input
            if (mainOrbitCamera) mainOrbitCamera.enabled = false;
            if (viewCamera)      viewCamera.enabled      = hideNoCameraMessage ? true : false; // opcional

            // followers garantidos
            BindFollower(camA, mainOrbitCamera);
            BindFollower(camB, mainOrbitCamera);

            // Aplica configuração de skybox nas câmeras de comparação
            // (modelViewer deve ser configurado pelo HUDController no Start())
            UpdateCamerasSkybox();

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
    
    /// <summary>
    /// Define o modo de visualização (Split ou SideBySide)
    /// </summary>
    /// <param name="mode">Modo de visualização desejado</param>
    public void SetViewMode(ViewMode mode)
    {
        if (_currentViewMode == mode) return;
        
        _currentViewMode = mode;
        ApplyShaderMode();
        RecreateRTsForMode();
        
        // Se estiver em modo de comparação ativo, atualiza a UI
        if (compositeImage != null && compositeImage.gameObject.activeInHierarchy)
        {
            if (_currentViewMode == ViewMode.SideBySide)
            {
                SafeSetActive(splitLine, false);
                SafeSetActive(splitSlider, false);
            }
            else
            {
                SafeSetActive(splitLine, true);
                SafeSetActive(splitSlider, true);
            }
            
            UpdateSplitUI();
        }
    }
    
    /// <summary>
    /// Retorna o modo de visualização atual
    /// </summary>
    public ViewMode GetViewMode() => _currentViewMode;

    void EnsureMaterial()
    {
        if (_mat) return;
        // cria material do shader e aplica no RawImage
        _mat = new Material(Shader.Find("Hidden/CompareComposite"));
        if (compositeImage) compositeImage.material = _mat;
    }

    void RecreateRTsForMode()
    {
        // força recriação com resolução ajustada para cada modo, evitando distorção no side-by-side
        if (_rtA) { _rtA.Release(); _rtA = null; }
        if (_rtB) { _rtB.Release(); _rtB = null; }
        EnsureRTs();

        if (camA) camA.targetTexture = _rtA;
        if (camB) camB.targetTexture = _rtB;
    }

    void EnsureRTs()
    {
        int w = Mathf.Max(1, _currentViewMode == ViewMode.SideBySide ? Screen.width / 2 : Screen.width);
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

    void VerifySliderConfiguration()
    {
        if (!splitSlider) return;
        
        var rt = splitSlider.GetComponent<RectTransform>();
        if (rt)
        {
            Debug.Log($"[CompareSplitView] Slider RectTransform:");
            Debug.Log($"  - AnchorMin: {rt.anchorMin}");
            Debug.Log($"  - AnchorMax: {rt.anchorMax}");
            Debug.Log($"  - OffsetMin: {rt.offsetMin}");
            Debug.Log($"  - OffsetMax: {rt.offsetMax}");
            Debug.Log($"  - Rect: {rt.rect}");
            
            // Verifica se está em stretch (cobrindo tela toda)
            bool isStretchedHorizontally = Mathf.Approximately(rt.anchorMin.x, 0f) && Mathf.Approximately(rt.anchorMax.x, 1f);
            bool isStretchedVertically = Mathf.Approximately(rt.anchorMin.y, 0f) && Mathf.Approximately(rt.anchorMax.y, 1f);
            
            if (isStretchedHorizontally && isStretchedVertically)
            {
                Debug.LogWarning("[CompareSplitView] ⚠️ PROBLEMA: Slider está configurado para ocupar a tela toda!");
                Debug.LogWarning("[CompareSplitView] Solução: No Inspector, ajuste o RectTransform do Slider:");
                Debug.LogWarning("[CompareSplitView]   1. Selecione o GameObject do Slider na Hierarchy");
                Debug.LogWarning("[CompareSplitView]   2. No Inspector, configure Anchors para a posição/tamanho desejado");
                Debug.LogWarning("[CompareSplitView]   3. Exemplo: Bottom-Center com Width=200, Height=30");
                Debug.LogWarning("[CompareSplitView]   Ou use Anchors específicos (não stretch em ambos os eixos)");
            }
            else
            {
                Debug.Log("[CompareSplitView] ✓ Slider configurado corretamente (não está em full stretch)");
            }
            
            // Verifica raycast target
            var graphic = splitSlider.GetComponent<UnityEngine.UI.Graphic>();
            if (graphic && !graphic.raycastTarget)
            {
                Debug.LogWarning("[CompareSplitView] ⚠️ Slider tem raycastTarget=false, pode não responder a cliques!");
            }
        }
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
        if (!compositeImage) return;
        
        float splitValue;
        
        if (_currentViewMode == ViewMode.SideBySide)
        {
            // Modo Side by Side: fixa divisão no meio (0.5)
            splitValue = 0.5f;
            Debug.Log($"[CompareSplitView] Modo Side by Side ativo - fixando divisão em 0.5");
        }
        else
        {
            // Modo Split: usa valor do slider
            if (!splitSlider) return;
            splitValue = splitSlider.value;
        }
        
        // Calcula a posição real em coordenadas de tela
        RectTransform imageRect = compositeImage.rectTransform;
        
        if (imageRect)
        {
            // Para modo Side by Side, usa valor fixo 0.5
            // Para modo Split, calcula baseado no slider
            float normalizedX = splitValue;
            
            if (_currentViewMode == ViewMode.Split && splitSlider)
            {
                // Calcula posição baseada no slider (código original)
                RectTransform sliderRect = splitSlider.GetComponent<RectTransform>();
                
                if (sliderRect)
                {
                    // Converte posições para espaço de tela (screen space)
                    Canvas canvas = compositeImage.canvas;
                    Camera cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
                    
                    // Pega os cantos do slider em screen space
                    Vector3[] sliderCorners = new Vector3[4];
                    sliderRect.GetWorldCorners(sliderCorners);
                    
                    // Pega os cantos da imagem em screen space  
                    Vector3[] imageCorners = new Vector3[4];
                    imageRect.GetWorldCorners(imageCorners);
                    
                    // Calcula limites do slider e da imagem em coordenadas de tela
                    float sliderLeft = sliderCorners[0].x;
                    float sliderRight = sliderCorners[2].x;
                    float sliderWidth = sliderRight - sliderLeft;
                    
                    float imageLeft = imageCorners[0].x;
                    float imageRight = imageCorners[2].x;
                    float imageWidth = imageRight - imageLeft;
                    
                    // Posição absoluta do handle do slider
                    float handlePosX = sliderLeft + (sliderWidth * splitValue);
                    
                    // Converte para coordenada normalizada (0-1) no espaço da imagem
                    normalizedX = (handlePosX - imageLeft) / imageWidth;
                    normalizedX = Mathf.Clamp01(normalizedX);
                }
            }
            
            // Atualiza shader com a posição correta
            if (_mat)
            {
                ApplyShaderMode();
                _mat.SetFloat("_Split", normalizedX);
                _mat.SetFloat("_Feather", feather);
                Debug.Log($"[CompareSplitView] Shader atualizado - _Split={normalizedX:F3}, Modo={_currentViewMode}");
            }
            else
            {
                Debug.LogWarning("[CompareSplitView] Material não está inicializado!");
            }
            
            // Alinha linha divisória na mesma posição (apenas se estiver visível)
            if (splitLine && splitLine.gameObject.activeInHierarchy)
            {
                splitLine.SetParent(imageRect, false);
                splitLine.anchorMin = new Vector2(normalizedX, 0f);
                splitLine.anchorMax = new Vector2(normalizedX, 1f);
                splitLine.anchoredPosition = Vector2.zero;
                splitLine.sizeDelta = new Vector2(2f, 0f);
            }
        }
        else
        {
            // Fallback: usa valor direto
            if (_mat)
            {
                ApplyShaderMode();
                _mat.SetFloat("_Split", splitValue);
                _mat.SetFloat("_Feather", feather);
                Debug.Log($"[CompareSplitView] Fallback - _Split={splitValue:F3}, Modo={_currentViewMode}");
            }
            
            if (splitLine && splitLine.gameObject.activeInHierarchy)
            {
                splitLine.anchorMin = new Vector2(splitValue, 0f);
                splitLine.anchorMax = new Vector2(splitValue, 1f);
                splitLine.anchoredPosition = Vector2.zero;
            }
        }
    }

    void ApplyShaderMode()
    {
        if (!_mat) return;
        _mat.SetFloat(SideBySideProp, _currentViewMode == ViewMode.SideBySide ? 1f : 0f);
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

    /// <summary>
    /// Atualiza a configuração de skybox nas câmeras de comparação (camA e camB)
    /// baseado na configuração do ModelViewer
    /// </summary>
    public void UpdateCamerasSkybox()
    {
        if (modelViewer == null)
        {
            Debug.LogWarning("[CompareSplitView] modelViewer não está configurado - não é possível atualizar skybox");
            return;
        }

        bool currentUseSkybox = modelViewer.useSkybox;
        Color backgroundColor = modelViewer.solidBackgroundColor;

        Debug.Log($"[CompareSplitView] UpdateCamerasSkybox chamado - lendo useSkybox diretamente: {currentUseSkybox}");

        if (camA != null)
        {
            if (currentUseSkybox)
            {
                camA.clearFlags = CameraClearFlags.Skybox;
                Debug.Log($"[CompareSplitView] ✅ Câmera A ({camA.name}): Skybox habilitado");
            }
            else
            {
                camA.clearFlags = CameraClearFlags.SolidColor;
                camA.backgroundColor = backgroundColor;
                Debug.Log($"[CompareSplitView] ✅ Câmera A ({camA.name}): Cor sólida ({backgroundColor}) - useSkybox=false");
            }
            
            if (!currentUseSkybox && camA.clearFlags != CameraClearFlags.SolidColor)
            {
                Debug.LogError($"[CompareSplitView] ⚠️ ERRO: useSkybox=false mas camA não está com SolidColor! Forçando correção...");
                camA.clearFlags = CameraClearFlags.SolidColor;
                camA.backgroundColor = backgroundColor;
            }
        }
        else
        {
            Debug.LogWarning("[CompareSplitView] camA é null!");
        }

        if (camB != null)
        {
            if (currentUseSkybox)
            {
                camB.clearFlags = CameraClearFlags.Skybox;
                Debug.Log($"[CompareSplitView] ✅ Câmera B ({camB.name}): Skybox habilitado");
            }
            else
            {
                camB.clearFlags = CameraClearFlags.SolidColor;
                camB.backgroundColor = backgroundColor;
                Debug.Log($"[CompareSplitView] ✅ Câmera B ({camB.name}): Cor sólida ({backgroundColor}) - useSkybox=false");
            }
            
            if (!currentUseSkybox && camB.clearFlags != CameraClearFlags.SolidColor)
            {
                Debug.LogError($"[CompareSplitView] ⚠️ ERRO: useSkybox=false mas camB não está com SolidColor! Forçando correção...");
                camB.clearFlags = CameraClearFlags.SolidColor;
                camB.backgroundColor = backgroundColor;
            }
        }
        else
        {
            Debug.LogWarning("[CompareSplitView] camB é null!");
        }
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
