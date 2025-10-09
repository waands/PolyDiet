using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Utilitário para capturar screenshots de elementos UI (RectTransform)
/// Usa ReadPixels para capturar região específica da tela
/// </summary>
public static class UICapture
{
    /// <summary>
    /// Captura um RectTransform como PNG
    /// </summary>
    /// <param name="rectTransform">Elemento UI a capturar</param>
    /// <param name="scale">Escala de captura (1.0 = tamanho normal, 1.5 = 150%)</param>
    /// <param name="onReady">Callback com os bytes do PNG</param>
    public static IEnumerator CaptureRectToPng(RectTransform rectTransform, float scale, Action<byte[]> onReady)
    {
        if (rectTransform == null)
        {
            Debug.LogError("[UICapture] RectTransform is null!");
            onReady?.Invoke(null);
            yield break;
        }
        
        // Aguarda final do frame para garantir que tudo foi renderizado
        yield return new WaitForEndOfFrame();
        
        // Calcula bounds em coordenadas de tela
        Vector3[] corners = new Vector3[4];
        rectTransform.GetWorldCorners(corners);
        
        // Converte para coordenadas de tela
        Camera cam = GetUICamera(rectTransform);
        if (cam == null) cam = Camera.main;
        
        Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
        Vector2 max = new Vector2(float.MinValue, float.MinValue);
        
        for (int i = 0; i < 4; i++)
        {
            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(cam, corners[i]);
            min.x = Mathf.Min(min.x, screenPoint.x);
            min.y = Mathf.Min(min.y, screenPoint.y);
            max.x = Mathf.Max(max.x, screenPoint.x);
            max.y = Mathf.Max(max.y, screenPoint.y);
        }
        
        // Garante que está dentro dos limites da tela
        int x = Mathf.Clamp((int)min.x, 0, Screen.width);
        int y = Mathf.Clamp((int)min.y, 0, Screen.height);
        int w = Mathf.Clamp((int)(max.x - min.x), 1, Screen.width - x);
        int h = Mathf.Clamp((int)(max.y - min.y), 1, Screen.height - y);
        
        if (w <= 0 || h <= 0)
        {
            Debug.LogError($"[UICapture] Invalid capture dimensions: {w}x{h}");
            onReady?.Invoke(null);
            yield break;
        }
        
        // Captura pixels
        Texture2D screenshot = new Texture2D(w, h, TextureFormat.RGB24, false);
        screenshot.ReadPixels(new Rect(x, y, w, h), 0, 0, false);
        screenshot.Apply();
        
        // Aplica escala se necessário
        if (Math.Abs(scale - 1.0f) > 0.01f)
        {
            int newW = Mathf.RoundToInt(w * scale);
            int newH = Mathf.RoundToInt(h * scale);
            screenshot = ScaleTexture(screenshot, newW, newH);
        }
        
        // Converte para PNG
        byte[] pngData = screenshot.EncodeToPNG();
        UnityEngine.Object.Destroy(screenshot);
        
        onReady?.Invoke(pngData);
    }
    
    /// <summary>
    /// Encontra a câmera associada ao Canvas
    /// </summary>
    private static Camera GetUICamera(RectTransform rt)
    {
        Canvas canvas = rt.GetComponentInParent<Canvas>();
        if (canvas == null) return null;
        
        if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            return null; // usa Screen.width/height diretamente
        
        return canvas.worldCamera;
    }
    
    /// <summary>
    /// Redimensiona uma textura usando interpolação bilinear
    /// </summary>
    private static Texture2D ScaleTexture(Texture2D source, int newWidth, int newHeight)
    {
        Texture2D result = new Texture2D(newWidth, newHeight, TextureFormat.RGB24, false);
        
        float xRatio = (float)source.width / newWidth;
        float yRatio = (float)source.height / newHeight;
        
        for (int y = 0; y < newHeight; y++)
        {
            for (int x = 0; x < newWidth; x++)
            {
                float srcX = x * xRatio;
                float srcY = y * yRatio;
                
                Color color = source.GetPixelBilinear(srcX / source.width, srcY / source.height);
                result.SetPixel(x, y, color);
            }
        }
        
        result.Apply();
        UnityEngine.Object.Destroy(source);
        
        return result;
    }
    
    /// <summary>
    /// Converte bytes PNG para string base64 (para embedding em HTML)
    /// </summary>
    public static string PngToBase64(byte[] pngData)
    {
        if (pngData == null || pngData.Length == 0)
            return "";
        
        return Convert.ToBase64String(pngData);
    }
    
    /// <summary>
    /// Cria uma data URI para imagem PNG
    /// </summary>
    public static string PngToDataUri(byte[] pngData)
    {
        if (pngData == null || pngData.Length == 0)
            return "";
        
        string base64 = PngToBase64(pngData);
        return $"data:image/png;base64,{base64}";
    }
}


