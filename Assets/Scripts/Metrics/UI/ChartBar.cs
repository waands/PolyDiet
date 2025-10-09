using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Gráfico de barras simples para comparação de variantes
/// Cria barras dinamicamente usando UI Images
/// </summary>
public class ChartBar : MonoBehaviour
{
    [Header("Refs")]
    public Transform barsContainer;
    public GameObject barPrefab; // prefab com Image + 2 TextMeshProUGUI (label, value)
    
    [Header("Config")]
    public float maxBarHeight = 200f;
    public float barWidth = 60f;
    public float barSpacing = 20f;
    
    private List<GameObject> _activeBars = new();
    
    /// <summary>
    /// Desenha o gráfico com os dados fornecidos
    /// </summary>
    /// <param name="data">Dicionário: variante → valor</param>
    /// <param name="theme">Tema para colorir as barras</param>
    /// <param name="unit">Unidade para exibir (ms, MB, FPS)</param>
    /// <param name="higherIsBetter">Se true, normaliza com maior valor = 100%</param>
    public void Draw(Dictionary<string, double> data, DashboardTheme theme, string unit, bool higherIsBetter)
    {
        Clear();
        
        if (data == null || data.Count == 0) return;
        
        // Encontra o valor máximo para normalização
        double maxValue = double.MinValue;
        double minValue = double.MaxValue;
        
        foreach (var value in data.Values)
        {
            if (value > maxValue) maxValue = value;
            if (value < minValue) minValue = value;
        }
        
        if (Math.Abs(maxValue) < 0.0001) maxValue = 1.0; // evita divisão por zero
        
        // Cria barras para cada variante
        int index = 0;
        var sortedVariants = new List<string> { "original", "draco", "meshopt" };
        
        foreach (var variant in sortedVariants)
        {
            if (!data.ContainsKey(variant)) continue;
            
            double value = data[variant];
            
            // Calcula altura da barra (normalizada)
            float normalizedHeight;
            if (higherIsBetter)
            {
                // Para FPS: maior = melhor = barra maior
                normalizedHeight = maxValue > 0 ? (float)(value / maxValue) : 0f;
            }
            else
            {
                // Para Load/Mem: menor = melhor = barra maior (invertido)
                // Menor valor tem altura máxima
                if (maxValue > minValue)
                    normalizedHeight = 1f - (float)((value - minValue) / (maxValue - minValue));
                else
                    normalizedHeight = 1f;
            }
            
            float barHeight = maxBarHeight * Mathf.Clamp01(normalizedHeight);
            
            // Cria barra
            GameObject bar = CreateBar(variant, value, barHeight, theme, unit);
            
            // Posiciona barra
            RectTransform rt = bar.GetComponent<RectTransform>();
            if (rt != null)
            {
                float xPos = index * (barWidth + barSpacing);
                rt.anchoredPosition = new Vector2(xPos, 0);
            }
            
            _activeBars.Add(bar);
            index++;
        }
    }
    
    /// <summary>
    /// Cria uma barra individual
    /// </summary>
    private GameObject CreateBar(string variant, double value, float height, DashboardTheme theme, string unit)
    {
        GameObject bar;
        
        if (barPrefab != null)
        {
            bar = Instantiate(barPrefab, barsContainer);
        }
        else
        {
            // Cria barra simples se não houver prefab
            bar = new GameObject($"Bar_{variant}");
            bar.transform.SetParent(barsContainer, false);
            
            var rt = bar.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(barWidth, height);
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(0, 0);
            rt.pivot = new Vector2(0.5f, 0);
            
            var image = bar.AddComponent<Image>();
            image.color = theme.GetVariantColor(variant);
            
            // Label (nome da variante)
            var labelObj = new GameObject("Label");
            labelObj.transform.SetParent(bar.transform, false);
            var labelRt = labelObj.AddComponent<RectTransform>();
            labelRt.anchorMin = new Vector2(0, 0);
            labelRt.anchorMax = new Vector2(1, 0);
            labelRt.pivot = new Vector2(0.5f, 1);
            labelRt.anchoredPosition = new Vector2(0, -5);
            labelRt.sizeDelta = new Vector2(0, 20);
            
            var labelText = labelObj.AddComponent<TextMeshProUGUI>();
            labelText.text = variant;
            labelText.fontSize = 12;
            labelText.color = theme.text;
            labelText.alignment = TextAlignmentOptions.Center;
            
            // Value (valor numérico)
            var valueObj = new GameObject("Value");
            valueObj.transform.SetParent(bar.transform, false);
            var valueRt = valueObj.AddComponent<RectTransform>();
            valueRt.anchorMin = new Vector2(0, 1);
            valueRt.anchorMax = new Vector2(1, 1);
            valueRt.pivot = new Vector2(0.5f, 0);
            valueRt.anchoredPosition = new Vector2(0, 5);
            valueRt.sizeDelta = new Vector2(0, 20);
            
            var valueText = valueObj.AddComponent<TextMeshProUGUI>();
            valueText.text = $"{value:F1}{unit}";
            valueText.fontSize = 11;
            valueText.color = theme.text;
            valueText.alignment = TextAlignmentOptions.Center;
        }
        
        // Ajusta altura da barra
        var barRt = bar.GetComponent<RectTransform>();
        if (barRt != null)
        {
            var size = barRt.sizeDelta;
            size.y = height;
            barRt.sizeDelta = size;
        }
        
        // Atualiza cor e textos se houver prefab
        var img = bar.GetComponent<Image>();
        if (img != null)
            img.color = theme.GetVariantColor(variant);
        
        var texts = bar.GetComponentsInChildren<TextMeshProUGUI>();
        if (texts.Length >= 2)
        {
            texts[0].text = variant; // label
            texts[1].text = $"{value:F1}{unit}"; // value
        }
        
        return bar;
    }
    
    /// <summary>
    /// Limpa todas as barras
    /// </summary>
    public void Clear()
    {
        foreach (var bar in _activeBars)
        {
            if (bar != null)
                Destroy(bar);
        }
        _activeBars.Clear();
    }
}

