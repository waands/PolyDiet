using System;
using System.Collections;
using System.IO;
using System.Text;
using UnityEngine;

/// <summary>
/// Exportador de Dashboard para HTML
/// Captura seções do painel como PNG e gera um arquivo HTML standalone
/// </summary>
public class MetricsHtmlExporter : MonoBehaviour
{
    [Header("Theme")]
    public DashboardTheme theme;
    
    [Header("Sections to Capture")]
    public RectTransform headerSection;
    public RectTransform cardsSection;
    public RectTransform barsSection;
    public RectTransform timelineIndexSection;
    public RectTransform timelineTimeSection;
    public RectTransform tableSection;
    
    [Header("Settings")]
    public float captureScale = 1.5f;
    public bool openAfterExport = true;
    
    private struct CapturedSection
    {
        public string name;
        public string dataUri;
    }
    
    /// <summary>
    /// Exporta o dashboard atual para HTML
    /// </summary>
    /// <param name="modelName">Nome do modelo sendo visualizado</param>
    /// <param name="onDone">Callback com o caminho do arquivo gerado</param>
    public IEnumerator ExportHtml(string modelName, Action<string> onDone)
    {
        Debug.Log($"[HtmlExporter] Iniciando exportação para modelo: {modelName}");
        
        var sections = new CapturedSection[]
        {
            new CapturedSection { name = "header" },
            new CapturedSection { name = "cards" },
            new CapturedSection { name = "bars" },
            new CapturedSection { name = "timeline-index" },
            new CapturedSection { name = "timeline-time" },
            new CapturedSection { name = "table" }
        };
        
        var transforms = new RectTransform[]
        {
            headerSection,
            cardsSection,
            barsSection,
            timelineIndexSection,
            timelineTimeSection,
            tableSection
        };
        
        // Captura cada seção
        for (int i = 0; i < sections.Length; i++)
        {
            if (transforms[i] == null || !transforms[i].gameObject.activeInHierarchy)
            {
                Debug.LogWarning($"[HtmlExporter] Seção {sections[i].name} não está disponível, pulando...");
                sections[i].dataUri = "";
                continue;
            }
            
            Debug.Log($"[HtmlExporter] Capturando seção: {sections[i].name}");
            
            byte[] pngData = null;
            yield return UICapture.CaptureRectToPng(transforms[i], captureScale, data => pngData = data);
            
            if (pngData != null && pngData.Length > 0)
            {
                sections[i].dataUri = UICapture.PngToDataUri(pngData);
                Debug.Log($"[HtmlExporter] Seção {sections[i].name} capturada: {pngData.Length / 1024}KB");
            }
            else
            {
                Debug.LogWarning($"[HtmlExporter] Falha ao capturar seção: {sections[i].name}");
                sections[i].dataUri = "";
            }
        }
        
        // Gera HTML
        string html = GenerateHtml(modelName, sections);
        
        // Salva arquivo
        string outputDir = Path.Combine(Application.persistentDataPath, "Reports");
        Directory.CreateDirectory(outputDir);
        
        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string fileName = $"{SanitizeFileName(modelName)}_{timestamp}.html";
        string filePath = Path.Combine(outputDir, fileName);
        
        try
        {
            File.WriteAllText(filePath, html, Encoding.UTF8);
            Debug.Log($"[HtmlExporter] ✅ HTML exportado: {filePath}");
            Debug.Log($"[HtmlExporter] Tamanho do arquivo: {new FileInfo(filePath).Length / 1024}KB");
            
            // Abre no navegador
            if (openAfterExport)
            {
                Application.OpenURL("file://" + filePath);
                Debug.Log($"[HtmlExporter] Abrindo no navegador...");
            }
            
            onDone?.Invoke(filePath);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[HtmlExporter] ❌ Erro ao salvar HTML: {ex.Message}");
            onDone?.Invoke(null);
        }
    }
    
    /// <summary>
    /// Gera o HTML completo com CSS variables e imagens inline
    /// </summary>
    private string GenerateHtml(string modelName, CapturedSection[] sections)
    {
        var sb = new StringBuilder();
        
        // Header
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang=\"pt-BR\">");
        sb.AppendLine("<head>");
        sb.AppendLine("    <meta charset=\"UTF-8\">");
        sb.AppendLine("    <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
        sb.AppendLine($"    <title>Dashboard - {modelName}</title>");
        
        // CSS com variables do tema
        sb.AppendLine("    <style>");
        sb.AppendLine("        :root {");
        
        if (theme != null)
        {
            sb.AppendLine($"            --color-bg: {DashboardTheme.ColorToHex(theme.bg)};");
            sb.AppendLine($"            --color-text: {DashboardTheme.ColorToHex(theme.text)};");
            sb.AppendLine($"            --color-muted: {DashboardTheme.ColorToHex(theme.muted)};");
            sb.AppendLine($"            --color-original: {DashboardTheme.ColorToHex(theme.original)};");
            sb.AppendLine($"            --color-draco: {DashboardTheme.ColorToHex(theme.draco)};");
            sb.AppendLine($"            --color-meshopt: {DashboardTheme.ColorToHex(theme.meshopt)};");
            sb.AppendLine($"            --color-good: {DashboardTheme.ColorToHex(theme.good)};");
            sb.AppendLine($"            --color-bad: {DashboardTheme.ColorToHex(theme.bad)};");
        }
        else
        {
            // Valores default
            sb.AppendLine("            --color-bg: #fafafe;");
            sb.AppendLine("            --color-text: #1f1f29;");
            sb.AppendLine("            --color-muted: #8c8c99;");
            sb.AppendLine("            --color-original: #ccccda;");
            sb.AppendLine("            --color-draco: #47bd85;");
            sb.AppendLine("            --color-meshopt: #4794fa;");
            sb.AppendLine("            --color-good: #3bc051;");
            sb.AppendLine("            --color-bad: #e05252;");
        }
        
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        * { margin: 0; padding: 0; box-sizing: border-box; }");
        sb.AppendLine("        body {");
        sb.AppendLine("            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif;");
        sb.AppendLine("            background: var(--color-bg);");
        sb.AppendLine("            color: var(--color-text);");
        sb.AppendLine("            line-height: 1.6;");
        sb.AppendLine("            padding: 2rem;");
        sb.AppendLine("        }");
        sb.AppendLine("        .container { max-width: 1400px; margin: 0 auto; }");
        sb.AppendLine("        h1 { font-size: 2rem; margin-bottom: 0.5rem; }");
        sb.AppendLine("        .subtitle { color: var(--color-muted); margin-bottom: 2rem; }");
        sb.AppendLine("        .section {");
        sb.AppendLine("            background: white;");
        sb.AppendLine("            border-radius: 8px;");
        sb.AppendLine("            padding: 1.5rem;");
        sb.AppendLine("            margin-bottom: 1.5rem;");
        sb.AppendLine("            box-shadow: 0 2px 8px rgba(0,0,0,0.08);");
        sb.AppendLine("        }");
        sb.AppendLine("        .section h2 { font-size: 1.25rem; margin-bottom: 1rem; }");
        sb.AppendLine("        .section img { width: 100%; height: auto; border-radius: 4px; }");
        sb.AppendLine("        .footer {");
        sb.AppendLine("            text-align: center;");
        sb.AppendLine("            color: var(--color-muted);");
        sb.AppendLine("            font-size: 0.875rem;");
        sb.AppendLine("            margin-top: 3rem;");
        sb.AppendLine("            padding-top: 2rem;");
        sb.AppendLine("            border-top: 1px solid #e0e0e5;");
        sb.AppendLine("        }");
        sb.AppendLine("        @media print {");
        sb.AppendLine("            body { padding: 1rem; }");
        sb.AppendLine("            .section { break-inside: avoid; }");
        sb.AppendLine("        }");
        sb.AppendLine("    </style>");
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");
        sb.AppendLine("    <div class=\"container\">");
        
        // Title
        sb.AppendLine($"        <h1>Dashboard de Métricas - {modelName}</h1>");
        sb.AppendLine($"        <p class=\"subtitle\">Gerado em: {DateTime.Now:dd/MM/yyyy HH:mm:ss}</p>");
        
        // Sections
        string[] sectionTitles = new[]
        {
            "Informações Gerais",
            "Resumo de Métricas",
            "Comparação por Barras",
            "Timeline por Execução",
            "Timeline Temporal",
            "Tabela Detalhada"
        };
        
        for (int i = 0; i < sections.Length; i++)
        {
            if (!string.IsNullOrEmpty(sections[i].dataUri))
            {
                sb.AppendLine();
                sb.AppendLine("        <div class=\"section\">");
                sb.AppendLine($"            <h2>{sectionTitles[i]}</h2>");
                sb.AppendLine($"            <img src=\"{sections[i].dataUri}\" alt=\"{sections[i].name}\" />");
                sb.AppendLine("        </div>");
            }
        }
        
        // Footer
        sb.AppendLine();
        sb.AppendLine("        <div class=\"footer\">");
        sb.AppendLine("            <p>PolyDiet - Sistema de Análise de Compressão de Modelos 3D</p>");
        sb.AppendLine($"            <p>Exportado em {DateTime.Now:dd/MM/yyyy} às {DateTime.Now:HH:mm:ss}</p>");
        sb.AppendLine("        </div>");
        
        sb.AppendLine("    </div>");
        sb.AppendLine("</body>");
        sb.AppendLine("</html>");
        
        return sb.ToString();
    }
    
    /// <summary>
    /// Remove caracteres inválidos do nome do arquivo
    /// </summary>
    private string SanitizeFileName(string fileName)
    {
        if (string.IsNullOrEmpty(fileName))
            return "dashboard";
        
        char[] invalids = Path.GetInvalidFileNameChars();
        string sanitized = fileName;
        
        foreach (char c in invalids)
        {
            sanitized = sanitized.Replace(c, '_');
        }
        
        return sanitized;
    }
}


