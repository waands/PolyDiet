using UnityEngine;

/// <summary>
/// Configuração centralizada do PolyDiet
/// Crie um asset deste tipo via: Create > PolyDiet > Config
/// Configure todos os caminhos e ferramentas em um único lugar
/// </summary>
[CreateAssetMenu(fileName = "PolyDietConfig", menuName = "PolyDiet/Config", order = 1)]
public class PolyDietConfig : ScriptableObject
{
    [Header("Python")]
    [Tooltip("Caminho para o executável Python. Deixe vazio para auto-detecção (procura em reports_env/bin/python ou python3 do sistema)")]
    public string pythonPath = "";
    
    [Tooltip("Caminho para o script de relatório Python. Deixe vazio para usar o padrão (simple_report_generator.py)")]
    public string pythonScriptPath = "";
    
    [Tooltip("Caminho opcional para binário Python empacotado (PyInstaller). Se preenchido, ignora pythonPath e pythonScriptPath")]
    public string pythonPackagedExePath = "";

    [Header("Ferramentas Node.js")]
    [Tooltip("Caminho para gltf-transform. Deixe vazio para auto-detecção (procura no PATH ou NVM)")]
    public string gltfTransformPath = "";
    
    [Tooltip("Caminho para obj2gltf. Deixe vazio para auto-detecção (procura no PATH ou NVM)")]
    public string obj2gltfPath = "";

    [Header("Ferramentas do Sistema")]
    [Tooltip("Caminho para gltfpack. Deixe vazio para auto-detecção (procura em /usr/bin, /usr/local/bin ou PATH)")]
    public string gltfpackPath = "";

    [Header("Chromium/Chrome (para PDFs)")]
    [Tooltip("Caminho para Chromium/Chrome. Deixe vazio para auto-detecção (procura chromium, chromium-browser ou google-chrome)")]
    public string chromiumPath = "";

    [Header("Diretórios (Opcional - deixe vazio para usar padrões)")]
    [Tooltip("Caminho customizado para CSV de métricas. Deixe vazio para usar o padrão do sistema")]
    public string csvPathOverride = "";
    
    [Tooltip("Caminho customizado para diretório de saída de relatórios. Deixe vazio para usar o padrão do sistema")]
    public string reportsOutputPathOverride = "";

    [Header("URLs")]
    [Tooltip("URL do repositório GitHub (usado no botão de repositório)")]
    public string repositoryUrl = "https://github.com/waands/PolyDiet/";

    [Header("Configurações de Relatórios")]
    [Tooltip("Número de últimas execuções a incluir no relatório")]
    public int reportLastN = 20;
    
    [Tooltip("Gerar HTML nos relatórios")]
    public bool generateHtml = true;
    
    [Tooltip("Gerar PDF nos relatórios")]
    public bool generatePdf = true;
    
    [Tooltip("Engine para geração de PDF (chrome ou wkhtml)")]
    public string pdfEngine = "chrome";

    /// <summary>
    /// Obtém o caminho do Python, usando auto-detecção se necessário
    /// </summary>
    public string GetPythonPath()
    {
        if (!string.IsNullOrEmpty(pythonPath))
            return pythonPath;
        
        // Auto-detecção: tentar ambiente virtual primeiro
        string venvPath = System.IO.Path.Combine(Application.dataPath, "..", "reports_env", "bin", "python");
        if (System.IO.File.Exists(venvPath))
            return venvPath;
        
        // Fallback: python3 ou python do sistema
        return "python3";
    }

    /// <summary>
    /// Obtém o caminho do script Python, usando padrão se necessário
    /// </summary>
    public string GetPythonScriptPath()
    {
        if (!string.IsNullOrEmpty(pythonScriptPath))
            return pythonScriptPath;
        
        // Caminho padrão
        return System.IO.Path.Combine(Application.dataPath, "Scripts", "Metrics", "reports_tool", "simple_report_generator.py");
    }

    /// <summary>
    /// Obtém o caminho do gltf-transform, usando auto-detecção se necessário
    /// </summary>
    public string GetGltfTransformPath()
    {
        if (!string.IsNullOrEmpty(gltfTransformPath))
            return gltfTransformPath;
        
        // Auto-detecção será feita pelo código que usa esta config
        return "";
    }

    /// <summary>
    /// Obtém o caminho do obj2gltf, usando auto-detecção se necessário
    /// </summary>
    public string GetObj2GltfPath()
    {
        if (!string.IsNullOrEmpty(obj2gltfPath))
            return obj2gltfPath;
        
        // Auto-detecção será feita pelo código que usa esta config
        return "";
    }

    /// <summary>
    /// Obtém o caminho do gltfpack, usando auto-detecção se necessário
    /// </summary>
    public string GetGltfpackPath()
    {
        if (!string.IsNullOrEmpty(gltfpackPath))
            return gltfpackPath;
        
        // Auto-detecção será feita pelo código que usa esta config
        return "";
    }

    /// <summary>
    /// Obtém o caminho do Chromium, usando auto-detecção se necessário
    /// </summary>
    public string GetChromiumPath()
    {
        if (!string.IsNullOrEmpty(chromiumPath))
            return chromiumPath;
        
        // Auto-detecção será feita pelo código que usa esta config
        return "";
    }
}

