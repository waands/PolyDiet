using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEngine;
using TMPro; // se for usar statusLabel

public class ReportRunner : MonoBehaviour
{
    [Header("Events")]
    [Tooltip("Evento disparado quando um relatório é gerado com sucesso")]
    public UnityEngine.Events.UnityEvent<string> OnReportComplete;
    
    [Header("Paths")]
    [Tooltip("Use Python do sistema (ex.: 'python' no Linux, 'py' ou 'python.exe' no Windows). Deixe vazio para auto-escolha.")]
    public string pythonPath = ""; // se vazio, escolhemos automaticamente
    [Tooltip("Caminho para o script de relatório (deixe vazio para usar simple_report_generator.py)")]
    public string scriptPath = ""; // ex.: Application.dataPath + "/Scripts/Metrics/reports_tool/simple_report_generator.py"
    [Tooltip("Opcional: binário empacotado (PyInstaller). Se preenchido, ignora pythonPath/scriptPath.")]
    public string packagedExePath = ""; // ex.: Application.dataPath + "/../reports_tool/dist/metrics_report.exe"

    [Header("Inputs")]
    [Tooltip("Se vazio, usa persistentDataPath/Benchmarks/benchmarks.csv")]
    public string csvPathOverride = "";
    [Tooltip("Se vazio, usa persistentDataPath/Reports/<timestamp>")]
    public string outDirOverride = "";

    [Header("Options")]
    public int lastN = MetricsConfig.DEFAULT_LAST_N;
    public bool genHtml = true;
    public bool genPdf = true;
    public string pdfEngine = MetricsConfig.DEFAULT_PDF_ENGINE; // "chrome" ou "wkhtml"
    public string pdfEnginePath = "";   // opcional: path do chrome/wkhtmltopdf se precisar

    [Header("Open behavior")]
    public bool openInUnity = true;   // <— novo, abre o HTML via Unity ao final

    // você pode setar via Inspector, ou injetar pelo HUD/Wizard:
    [Header("Optional model override")]
    public string modelOverride = ""; // se vazio, tenta pegar do ModelViewer

    [Header("UI (opcional)")]
    public TMP_Text statusLabel; // arraste um TMP para ver status na tela (opcional)

    // Sistema de bloqueio para evitar múltiplas execuções simultâneas
    private bool _isGeneratingReport = false;
    private string _lastReportPath = "";

    string CsvPathDefault()
    {
        return MetricsPathProvider.GetFallbackCsvPath();
    }

    string ResolveModel()
    {
        if (!string.IsNullOrEmpty(modelOverride)) return modelOverride;
        // tenta pegar do ModelViewer da cena se existir
        var mv = FindObjectOfType<ModelViewer>();
        var m = mv != null ? mv.GetCurrentSelectedModel() : null;
        return string.IsNullOrEmpty(m) ? "all" : m;
    }

    string OutDirDefault(string model)
    {
        // Nova lógica: usa diretório unificado (sem timestamp)
        return MetricsPathProvider.GetModelReportUnifiedDirectory(model);
    }
    

    /// <summary>
    /// Inicia a geração de um relatório para um modelo específico,
    /// sobrescrevendo a lógica de auto-detecção.
    /// </summary>
    public void RunReportForModel(string modelName)
    {
        if (_isGeneratingReport)
        {
            Log("<color=orange>Um relatório já está sendo gerado. Por favor, aguarde.</color>");
            return; // Sai do método se já estiver ocupado
        }

        UnityEngine.Debug.Log($"[ReportRunner] Recebido pedido para gerar relatório específico para: {modelName}");
        this.modelOverride = modelName; // Define o override com o modelo recebido
        RunReport(); // Executa a lógica de relatório existente
    }

    public void RunReport()
    {
        // Embora a verificação principal esteja em RunReportForModel,
        // adicionamos uma segurança extra aqui.
        if (_isGeneratingReport) return;

        _isGeneratingReport = true; // BLOQUEIA o sistema aqui

        string model = ResolveModel();
        
        // NOVO: validação - não aceitar "all"
        if (model.Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            Log("<color=orange>Reports globais não são mais suportados. Selecione um modelo específico.</color>");
            _isGeneratingReport = false;
            return;
        }
        
        string outDir = string.IsNullOrEmpty(outDirOverride) ? OutDirDefault(model) : outDirOverride;
        
        // Limpar diretório existente para garantir relatório unificado
        if (Directory.Exists(outDir))
        {
            Log($"[Report] Limpando diretório existente: {outDir}");
            Directory.Delete(outDir, true);
        }
        
        Directory.CreateDirectory(outDir);

        // Lógica de seleção de CSV para modelo específico
        string[] csvPaths;
        string specificCsvPath = MetricsPathProvider.GetSingleModelCsvPath(model);
        
        if (!string.IsNullOrEmpty(specificCsvPath) && File.Exists(specificCsvPath))
        {
            csvPaths = new string[] { specificCsvPath };
            Log($"[Report] Usando CSV para o modelo '{model}': {specificCsvPath}");
        }
        else
        {
            csvPaths = Array.Empty<string>();
            Log($"<color=orange>[Report] Nenhum arquivo CSV encontrado para o modelo '{model}'</color>");
        }

        // Fallback: se não há CSV, tenta usar CSV específico se fornecido
        if (csvPaths.Length == 0 && !string.IsNullOrEmpty(csvPathOverride))
        {
            if (File.Exists(csvPathOverride))
            {
                csvPaths = new string[] { csvPathOverride };
                Log($"[Report] Usando CSV específico (fallback): {csvPathOverride}");
            }
        }

        if (csvPaths.Length == 0)
        {
            Log($"<color=orange>Nenhum arquivo CSV encontrado para o modelo '{model}'. Execute os testes primeiro.</color>");
            _isGeneratingReport = false;
            return;
        }

        Log($"[Report] Usando {csvPaths.Length} arquivos CSV encontrados:");
        foreach (var path in csvPaths)
        {
            Log($"  - {path}");
        }

        // Juntar todos os caminhos em uma única string, separados por espaço
        string allCsvPaths = string.Join(" ", csvPaths.Select(p => $"\"{p}\""));

        // Escolher script (usa simple por padrão)
        string actualScriptPath = scriptPath;
        if (string.IsNullOrEmpty(actualScriptPath))
        {
            // Usa script simple por padrão
            actualScriptPath = Path.Combine(Application.dataPath, "Scripts", "Metrics", "reports_tool", "simple_report_generator.py");
        }
        
        if (!File.Exists(actualScriptPath))
        {
            Log($"<color=red>[Report] Script não encontrado em: {actualScriptPath}</color>");
            _isGeneratingReport = false;
            return;
        }
        
        Log($"[Report] Usando script: {actualScriptPath}");
        
        // Construir os argumentos para execução (novo formato simplificado)
        string args = $"\"{actualScriptPath}\" --out \"{outDir}\" --model {model} --csv-files {allCsvPaths}";
        
        if (genHtml) args += " --html";
        if (genPdf)  args += " --pdf";

        string file;
        string finalArgs;
        if (!string.IsNullOrEmpty(packagedExePath))
        {
            file = packagedExePath;
            finalArgs = args.Replace($"\"{actualScriptPath}\" ", "");
        }
        else
        {
            file = AutoPython();
            if (string.IsNullOrEmpty(file)) 
            { 
                Log("Python não encontrado."); 
                _isGeneratingReport = false;
                return; 
            }
            finalArgs = args;
        }

        Log($"[Report] Executando comando único:\n{file} {finalArgs}");
        StartProcess(file, finalArgs, outDir, actualScriptPath);
    }

    string AutoPython()
    {
        // Primeiro, tenta usar o ambiente virtual se existir
        string venvPath = Path.Combine(Application.dataPath, "..", "reports_env", "bin", "python");
        if (File.Exists(venvPath))
        {
            Log($"[Report] Usando ambiente virtual: {venvPath}");
            return venvPath;
        }
        
        // Fallback para Python do sistema
        if (!string.IsNullOrEmpty(pythonPath)) return pythonPath;
        
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
        return "py"; // tenta o launcher do Windows; se não, troque para "python"
#else
        return "python3";
#endif
    }

    void StartProcess(string file, string args, string outDir, string scriptPath)
    {
        try
        {
            var psi = new System.Diagnostics.ProcessStartInfo(file, args)
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                WorkingDirectory = Path.GetDirectoryName(scriptPath) ?? System.Environment.CurrentDirectory,
            };
            // força UTF-8
            psi.EnvironmentVariables["PYTHONIOENCODING"] = "utf-8";

            var p = new System.Diagnostics.Process { StartInfo = psi, EnableRaisingEvents = true };
            p.OutputDataReceived += (_, e) => { if (!string.IsNullOrEmpty(e.Data)) Log(e.Data); };
            p.ErrorDataReceived  += (_, e) => { if (!string.IsNullOrEmpty(e.Data)) Log("<color=#E05252>ERROR: " + e.Data + "</color>"); };
            p.Exited += (_, __) =>
            {
                Log($"[Report] Finalizado. Code={p.ExitCode}");
                
                // Captura stderr completo após o processo terminar
                string stderr = p.StandardError.ReadToEnd();
                if (!string.IsNullOrEmpty(stderr))
                {
                    Log("<color=#E05252>STDERR COMPLETO:</color>");
                    Log("<color=#E05252>" + stderr + "</color>");
                }
                
                if (openInUnity)
                {
                    var html = System.IO.Path.Combine(outDir, "report.html");
                    if (System.IO.File.Exists(html)) Application.OpenURL("file://" + html);
                }

                _isGeneratingReport = false; // DESBLOQUEIA ao finalizar
                
                // Invocar callback se o relatório foi gerado com sucesso
                if (p.ExitCode == 0)
                {
                    _lastReportPath = outDir;
                    OnReportComplete?.Invoke(outDir);
                    Log($"[Report] Callback invocado para: {outDir}");
                }
                
                p.Dispose();
            };
            if (!p.Start()) 
            { 
                Log("Falha ao iniciar processo."); 
                _isGeneratingReport = false; // DESBLOQUEIA em caso de falha ao iniciar
                return; 
            }
            p.BeginOutputReadLine(); p.BeginErrorReadLine();
        }
        catch (System.Exception ex) 
        { 
            Log(ex.ToString()); 
            _isGeneratingReport = false; // DESBLOQUEIA em caso de exceção
        }
    }

    void Log(string msg)
    {
        UnityEngine.Debug.Log(msg);
        if (statusLabel) statusLabel.SetText(msg);
    }
    
    // =====================================================================
    // MÉTODOS PÚBLICOS PARA INTEGRAÇÃO COM UI
    // =====================================================================
    
    /// <summary>
    /// Obtém o caminho do último relatório gerado
    /// </summary>
    public string GetLastReportPath()
    {
        return _lastReportPath;
    }
    
    /// <summary>
    /// Verifica se está gerando relatório
    /// </summary>
    public bool IsGeneratingReport()
    {
        return _isGeneratingReport;
    }
    
    /// <summary>
    /// Abre o HTML do último relatório
    /// </summary>
    public void OpenLastHtml()
    {
        if (string.IsNullOrEmpty(_lastReportPath))
            return;
        
        string htmlPath = Path.Combine(_lastReportPath, "report.html");
        if (File.Exists(htmlPath))
        {
            Application.OpenURL(htmlPath);
            Log($"[ReportRunner] Abrindo HTML: {htmlPath}");
        }
        else
        {
            Log($"<color=orange>[ReportRunner] HTML não encontrado: {htmlPath}</color>");
        }
    }
    
    /// <summary>
    /// Abre o PDF do último relatório
    /// </summary>
    public void OpenLastPdf()
    {
        if (string.IsNullOrEmpty(_lastReportPath))
            return;
        
        string pdfPath = Path.Combine(_lastReportPath, "report.pdf");
        if (File.Exists(pdfPath))
        {
            Application.OpenURL(pdfPath);
            Log($"[ReportRunner] Abrindo PDF: {pdfPath}");
        }
        else
        {
            Log($"<color=orange>[ReportRunner] PDF não encontrado: {pdfPath}</color>");
        }
    }
    
    /// <summary>
    /// Abre a pasta do último relatório
    /// </summary>
    public void OpenLastFolder()
    {
        if (string.IsNullOrEmpty(_lastReportPath))
            return;
        
        Application.OpenURL(_lastReportPath);
        Log($"[ReportRunner] Abrindo pasta: {_lastReportPath}");
    }
    
    /// <summary>
    /// Coleta informações dos arquivos GLB (tamanho e caminho) para passar ao advanced report
    /// Formato: --file-info variant:sizeBytes:path
    /// </summary>
    private string CollectFileInfoArgs(string modelName)
    {
        string modelsRoot = Path.Combine(Application.streamingAssetsPath, "Models", modelName);
        
        if (!Directory.Exists(modelsRoot))
        {
            Log($"[Report] Pasta de modelos não encontrada: {modelsRoot}");
            return "";
        }
        
        string[] variants = { "original", "draco", "meshopt" };
        List<string> fileInfos = new List<string>();
        
        foreach (string variant in variants)
        {
            string variantDir = Path.Combine(modelsRoot, variant);
            if (!Directory.Exists(variantDir))
                continue;
            
            // Procurar por model.glb ou qualquer .glb
            string glbPath = Path.Combine(variantDir, "model.glb");
            
            if (!File.Exists(glbPath))
            {
                // Tentar encontrar qualquer .glb
                var glbFiles = Directory.GetFiles(variantDir, "*.glb");
                if (glbFiles.Length > 0)
                {
                    glbPath = glbFiles[0];
                }
                else
                {
                    continue; // Nenhum arquivo .glb encontrado nesta variante
                }
            }
            
            try
            {
                FileInfo fileInfo = new FileInfo(glbPath);
                long sizeBytes = fileInfo.Length;
                
                // Formato: variant:sizeBytes:path
                string fileInfoArg = $"{variant}:{sizeBytes}:{glbPath}";
                fileInfos.Add(fileInfoArg);
                
                Log($"[Report] File info: {variant} = {sizeBytes} bytes ({fileInfo.Length / (1024.0 * 1024.0):F2} MB)");
            }
            catch (Exception ex)
            {
                Log($"<color=orange>[Report] Erro ao obter info do arquivo {glbPath}: {ex.Message}</color>");
            }
        }
        
        if (fileInfos.Count == 0)
            return "";
        
        // Construir argumentos --file-info
        string result = "";
        foreach (string info in fileInfos)
        {
            result += $" --file-info \"{info}\"";
        }
        
        return result;
    }
}
