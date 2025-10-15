using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Gráfico de linha usando Texture2D para desenhar
/// Ideal para timelines e séries temporais
/// </summary>
[RequireComponent(typeof(RawImage))]
public class ChartTexture : MonoBehaviour
{
    [Header("Config")]
    public int width = 512;
    public int height = 256;
    public int margin = 40;
    
    [Header("Grid")]
    public bool showGrid = true;
    public int gridLines = 5;
    
    private RawImage _image;
    private Texture2D _texture;
    private DashboardTheme _theme;
    
    void Awake()
    {
        _image = GetComponent<RawImage>();
        CreateTexture();
    }
    
    void CreateTexture()
    {
        if (_texture != null)
            Destroy(_texture);
        
        _texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        _texture.filterMode = FilterMode.Bilinear;
        _texture.wrapMode = TextureWrapMode.Clamp;
        
        if (_image != null)
            _image.texture = _texture;
    }
    
    /// <summary>
    /// Desenha timeline por índice (ordem de execução)
    /// </summary>
    public void DrawIndexTimeline(Dictionary<string, List<(DateTime time, double value)>> seriesByVariant, 
                                   DashboardTheme theme, string unit)
    {
        _theme = theme;
        ClearTexture();
        
        if (seriesByVariant == null || seriesByVariant.Count == 0)
        {
            ApplyTexture();
            return;
        }
        
        // Encontra range de valores
        double minVal = double.MaxValue;
        double maxVal = double.MinValue;
        
        foreach (var series in seriesByVariant.Values)
        {
            foreach (var point in series)
            {
                if (point.value < minVal) minVal = point.value;
                if (point.value > maxVal) maxVal = point.value;
            }
        }
        
        if (Math.Abs(maxVal - minVal) < 0.0001) { maxVal = minVal + 1; }
        
        // Desenha grid
        if (showGrid)
            DrawGrid(minVal, maxVal, unit);
        
        // Desenha linha para cada variante
        var sortedVariants = new List<string> { "original", "draco", "meshopt" };
        foreach (var variant in sortedVariants)
        {
            if (!seriesByVariant.TryGetValue(variant, out var series) || series.Count == 0)
                continue;
            
            Color color = theme.GetVariantColor(variant);
            DrawLineSeries(series, minVal, maxVal, color);
        }
        
        ApplyTexture();
    }
    
    /// <summary>
    /// Desenha timeline por data (eixo temporal real)
    /// </summary>
    public void DrawTimeTimeline(Dictionary<string, List<(DateTime time, double value)>> seriesByVariant,
                                 DashboardTheme theme, string unit)
    {
        // Por simplicidade, usa mesmo algoritmo que índice
        // Em produção, poderia mapear DateTime para eixo X proporcional
        DrawIndexTimeline(seriesByVariant, theme, unit);
    }
    
    private void DrawLineSeries(List<(DateTime time, double value)> series, double minVal, double maxVal, Color color)
    {
        if (series.Count < 2) return;
        
        int chartWidth = width - 2 * margin;
        int chartHeight = height - 2 * margin;
        
        for (int i = 0; i < series.Count - 1; i++)
        {
            // Normaliza posições
            float x1 = margin + (i / (float)(series.Count - 1)) * chartWidth;
            float y1 = margin + (float)((series[i].value - minVal) / (maxVal - minVal)) * chartHeight;
            
            float x2 = margin + ((i + 1) / (float)(series.Count - 1)) * chartWidth;
            float y2 = margin + (float)((series[i + 1].value - minVal) / (maxVal - minVal)) * chartHeight;
            
            DrawLine((int)x1, (int)y1, (int)x2, (int)y2, color);
            
            // Desenha ponto
            DrawCircle((int)x1, (int)y1, 3, color);
        }
        
        // Último ponto
        if (series.Count > 0)
        {
            var last = series[series.Count - 1];
            float x = margin + chartWidth;
            float y = margin + (float)((last.value - minVal) / (maxVal - minVal)) * chartHeight;
            DrawCircle((int)x, (int)y, 3, color);
        }
    }
    
    private void DrawGrid(double minVal, double maxVal, string unit)
    {
        if (_theme == null) return;
        
        int chartWidth = width - 2 * margin;
        int chartHeight = height - 2 * margin;
        
        Color gridColor = _theme.gridLine;
        
        // Linhas horizontais
        for (int i = 0; i <= gridLines; i++)
        {
            int y = margin + (i * chartHeight / gridLines);
            DrawLine(margin, y, margin + chartWidth, y, gridColor);
        }
    }
    
    private void DrawLine(int x0, int y0, int x1, int y1, Color color)
    {
        // Algoritmo de Bresenham
        int dx = Mathf.Abs(x1 - x0);
        int dy = Mathf.Abs(y1 - y0);
        int sx = x0 < x1 ? 1 : -1;
        int sy = y0 < y1 ? 1 : -1;
        int err = dx - dy;
        
        while (true)
        {
            SetPixelSafe(x0, y0, color);
            
            if (x0 == x1 && y0 == y1) break;
            
            int e2 = 2 * err;
            if (e2 > -dy)
            {
                err -= dy;
                x0 += sx;
            }
            if (e2 < dx)
            {
                err += dx;
                y0 += sy;
            }
        }
    }
    
    private void DrawCircle(int cx, int cy, int radius, Color color)
    {
        for (int y = -radius; y <= radius; y++)
        {
            for (int x = -radius; x <= radius; x++)
            {
                if (x * x + y * y <= radius * radius)
                    SetPixelSafe(cx + x, cy + y, color);
            }
        }
    }
    
    private void SetPixelSafe(int x, int y, Color color)
    {
        if (x >= 0 && x < width && y >= 0 && y < height)
            _texture.SetPixel(x, y, color);
    }
    
    private void ClearTexture()
    {
        if (_texture == null) CreateTexture();
        
        Color bgColor = _theme != null ? _theme.chartBg : Color.white;
        
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                _texture.SetPixel(x, y, bgColor);
            }
        }
    }
    
    private void ApplyTexture()
    {
        if (_texture != null)
            _texture.Apply();
    }
    
    void OnDestroy()
    {
        if (_texture != null)
            Destroy(_texture);
    }
}


