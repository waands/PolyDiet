using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEngine;
using TMPro; // se for usar statusLabel

public class ReportRunner : MonoBehaviour
{
    [Header("Paths")]
    [Tooltip("Use Python do sistema (ex.: 'python' no Linux, 'py' ou 'python.exe' no Windows). Deixe vazio para auto-escolha.")]
    public string pythonPath = ""; // se vazio, escolhemos automaticamente
    [Tooltip("Usar script avançado com análises complexas (recomendado)")]
    public bool useAdvancedScript = true;
    [Tooltip("Caminho para o metrics_report.py (LEGACY)")]
    public string scriptPath = ""; // ex.: Application.dataPath + "/../reports_tool/metrics_report.py"
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
        // Nova lógica: salva dentro da pasta do modelo com timestamp
        return MetricsPathProvider.GetModelReportTimestampDirectory(model);
    }
    
    /// <summary>
    /// Coleta informações de arquivos GLB do modelo
    /// </summary>
    string[] CollectFileInfo(string modelName)
    {
        var info = new List<string>();
        var modelDir = MetricsPathProvider.GetModelDirectory(modelName);
        
        foreach (var variant in new[] { "original", "draco", "meshopt" })
        {
            var glbPath = Path.Combine(modelDir, variant, "model.glb");
            if (File.Exists(glbPath))
            {
                var fileInfo = new FileInfo(glbPath);
                info.Add($"{variant}:{fileInfo.Length}:{glbPath}");
            }
        }
        
        return info.ToArray();
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

        // NOVO: Coletar informações de arquivos
        string[] fileInfo = CollectFileInfo(model);
        string fileInfoArgs = fileInfo.Length > 0 
            ? " " + string.Join(" ", fileInfo.Select(f => $"--file-info \"{f}\""))
            : "";
        
        Log($"[Report] Coletadas {fileInfo.Length} informações de arquivos");
        
        // Construir lista de variantes
        string variants = $"{MetricsConfig.BASE_VARIANT},{MetricsConfig.DRACO_VARIANT},{MetricsConfig.MESHOPT_VARIANT}";

        // Juntar todos os caminhos em uma única string, separados por espaço
        string allCsvPaths = string.Join(" ", csvPaths.Select(p => $"\"{p}\""));

        // Escolher script
        string actualScriptPath = scriptPath;
        if (useAdvancedScript)
        {
            // Usa script avançado por padrão
            string advancedScriptPath = Path.Combine(UApp.dataPath, "Scripts", "Metrics", "reports_tool", "advanced_metrics_report.py");
            if (File.Exists(advancedScriptPath))
            {
                actualScriptPath = advancedScriptPath;
                Log($"[Report] Usando script avançado: {actualScriptPath}");
            }
            else
            {
                Log($"<color=orange>[Report] Script avançado não encontrado em: {advancedScriptPath}. Usando script padrão.</color>");
            }
        }
        
        // Construir os argumentos para execução
        string args = $"\"{actualScriptPath}\" --out \"{outDir}\" --model {model} --variants {variants} --last-n {lastN} --csv-files {allCsvPaths}{fileInfoArgs}";
        
            if (genHtml) args += " --html";
            if (genPdf)  args += " --pdf";
            if (!string.IsNullOrEmpty(pdfEngine)) args += $" --pdf-engine {pdfEngine}";
            if (!string.IsNullOrEmpty(pdfEnginePath)) args += $" --pdf-engine-path \"{pdfEnginePath}\"";

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
        StartProcess(file, finalArgs, outDir);
    }

    string AutoPython()
    {
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
        if (!string.IsNullOrEmpty(pythonPath)) return pythonPath;
        return "py"; // tenta o launcher do Windows; se não, troque para "python"
#else
        if (!string.IsNullOrEmpty(pythonPath)) return pythonPath;
        return "python3";
#endif
    }

    void StartProcess(string file, string args, string outDir)
    {
        try
        {
            var psi = new System.Diagnostics.ProcessStartInfo(file, args)
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                WorkingDirectory = Path.GetDirectoryName(actualScriptPath) ?? System.Environment.CurrentDirectory,
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
}
