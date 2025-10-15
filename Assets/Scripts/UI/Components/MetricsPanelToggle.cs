using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controla a visibilidade do painel de métricas
/// Vincula-se a um botão e gerencia o estado aberto/fechado
/// </summary>
public class MetricsPanelToggle : MonoBehaviour
{
    [Header("Painel")]
    [Tooltip("O painel de métricas que será mostrado/ocultado")]
    public GameObject metricsPanel;
    
    [Header("Opções")]
    [Tooltip("Trava a câmera quando o painel está aberto")]
    public bool lockCameraWhileOpen = true;
    
    [Tooltip("Estado inicial do painel")]
    public bool startClosed = true;
    
    [Header("Opcional: Ícones do Botão")]
    [Tooltip("Imagem do botão que muda baseado no estado")]
    public Image buttonIcon;
    public Sprite iconOpen;   // Ícone quando painel está fechado (ex: 📊)
    public Sprite iconClose;  // Ícone quando painel está aberto (ex: ✖️)
    
    private Button _button;
    private bool _isOpen;
    
    void Awake()
    {
        _button = GetComponent<Button>();
        if (_button != null)
        {
            _button.onClick.AddListener(Toggle);
        }
    }
    
    void Start()
    {
        // Define estado inicial
        if (metricsPanel != null)
        {
            _isOpen = !startClosed;
            SetPanelState(_isOpen, animate: false);
        }
    }
    
    /// <summary>
    /// Alterna entre aberto/fechado
    /// </summary>
    public void Toggle()
    {
        SetPanelState(!_isOpen);
    }
    
    /// <summary>
    /// Abre o painel
    /// </summary>
    public void Open()
    {
        SetPanelState(true);
    }
    
    /// <summary>
    /// Fecha o painel
    /// </summary>
    public void Close()
    {
        SetPanelState(false);
    }
    
    /// <summary>
    /// Define o estado do painel
    /// </summary>
    private void SetPanelState(bool open, bool animate = true)
    {
        if (metricsPanel == null)
        {
            Debug.LogWarning("[MetricsPanelToggle] Painel de métricas não configurado!");
            return;
        }
        
        _isOpen = open;
        
        // Mostra/oculta o painel
        metricsPanel.SetActive(open);
        
        // Trava/destrava câmera
        if (lockCameraWhileOpen)
        {
            if (open)
                UIInputLock.Lock(this);
            else
                UIInputLock.Unlock(this);
        }
        
        // Atualiza ícone do botão (se configurado)
        UpdateButtonIcon();
        
        // Atualiza métricas ao abrir
        if (open)
        {
            var dashboard = metricsPanel.GetComponent<MetricsDashboard>();
            if (dashboard != null)
            {
                dashboard.RefreshAll();
            }
            
            var viewer = metricsPanel.GetComponent<MetricsViewer>();
            if (viewer != null)
            {
                viewer.Refresh();
            }
        }
        
        Debug.Log($"[MetricsPanelToggle] Painel {(open ? "ABERTO" : "FECHADO")}");
    }
    
    /// <summary>
    /// Atualiza o ícone do botão baseado no estado
    /// </summary>
    private void UpdateButtonIcon()
    {
        if (buttonIcon != null && iconOpen != null && iconClose != null)
        {
            buttonIcon.sprite = _isOpen ? iconClose : iconOpen;
        }
    }
    
    void OnDestroy()
    {
        // Garante que destrava a câmera ao destruir
        if (lockCameraWhileOpen)
        {
            UIInputLock.Unlock(this);
        }
    }
}


