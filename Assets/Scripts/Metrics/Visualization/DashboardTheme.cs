using System;
using UnityEngine;

/// <summary>
/// Tema configurável para o Dashboard de Métricas
/// Todas as cores são variáveis, permitindo criar temas claros/escuros facilmente
/// </summary>
[CreateAssetMenu(menuName = "PolyDiet/Dashboard Theme", fileName = "DashboardTheme")]
public class DashboardTheme : ScriptableObject
{
    [Header("Cores Base")]
    [Tooltip("Cor de fundo principal")]
    public Color bg = new Color(0.98f, 0.98f, 1f, 1f);
    
    [Tooltip("Cor de texto principal")]
    public Color text = new Color(0.12f, 0.12f, 0.16f, 1f);
    
    [Tooltip("Cor de texto secundário/muted")]
    public Color muted = new Color(0.55f, 0.55f, 0.60f, 1f);
    
    [Header("Cores de Variantes")]
    [Tooltip("Cor para a variante Original")]
    public Color original = new Color(0.80f, 0.80f, 0.85f, 1f);
    
    [Tooltip("Cor para a variante Draco")]
    public Color draco = new Color(0.28f, 0.74f, 0.52f, 1f);
    
    [Tooltip("Cor para a variante Meshopt")]
    public Color meshopt = new Color(0.28f, 0.58f, 0.98f, 1f);
    
    [Header("Cores de Feedback")]
    [Tooltip("Cor para indicar melhoria/ganho positivo (▲)")]
    public Color good = new Color(0.23f, 0.75f, 0.32f, 1f);
    
    [Tooltip("Cor para indicar piora/ganho negativo (▼)")]
    public Color bad = new Color(0.88f, 0.32f, 0.32f, 1f);
    
    [Header("Cores para Gráficos")]
    [Tooltip("Cor de fundo dos gráficos")]
    public Color chartBg = new Color(0.95f, 0.95f, 0.97f, 1f);
    
    [Tooltip("Cor das linhas de grid")]
    public Color gridLine = new Color(0.85f, 0.85f, 0.87f, 1f);
    
    /// <summary>
    /// Retorna a cor apropriada para uma variante específica
    /// </summary>
    public Color GetVariantColor(string variant)
    {
        return variant?.ToLower() switch
        {
            "original" => original,
            "draco" => draco,
            "meshopt" => meshopt,
            _ => muted
        };
    }
    
    /// <summary>
    /// Retorna a cor apropriada baseado no ganho percentual
    /// </summary>
    public Color GetGainColor(double gainPercent)
    {
        return gainPercent >= 0 ? good : bad;
    }
    
    /// <summary>
    /// Retorna o símbolo de seta baseado no ganho
    /// </summary>
    public string GetGainArrow(double gainPercent)
    {
        if (Math.Abs(gainPercent) < 0.5) return ""; // desprezível
        return gainPercent >= 0 ? "▲" : "▼";
    }
    
    /// <summary>
    /// Formata o ganho percentual com sinal e símbolo
    /// </summary>
    public string FormatGain(double gainPercent)
    {
        string arrow = GetGainArrow(gainPercent);
        string sign = gainPercent >= 0 ? "+" : "";
        return $"{arrow}{sign}{gainPercent:F1}%";
    }
    
    /// <summary>
    /// Converte Color para string hexadecimal (para HTML/CSS)
    /// </summary>
    public static string ColorToHex(Color color)
    {
        int r = Mathf.RoundToInt(color.r * 255f);
        int g = Mathf.RoundToInt(color.g * 255f);
        int b = Mathf.RoundToInt(color.b * 255f);
        return $"#{r:X2}{g:X2}{b:X2}";
    }
}

