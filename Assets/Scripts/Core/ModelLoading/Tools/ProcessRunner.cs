using System;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PolyDiet.Core.ModelLoading.Tools
{
    /// <summary>
    /// Resultado da execução de um processo
    /// </summary>
    public class ProcessResult
    {
        public int ExitCode { get; set; }
        public string StandardOutput { get; set; }
        public string StandardError { get; set; }
        public bool TimedOut { get; set; }
        public TimeSpan ExecutionTime { get; set; }
        
        public bool Success => ExitCode == 0 && !TimedOut;
        
        public override string ToString()
        {
            if (Success)
            {
                return $"Success (Exit: {ExitCode}, Time: {ExecutionTime.TotalSeconds:F2}s)";
            }
            else if (TimedOut)
            {
                return $"Timeout after {ExecutionTime.TotalSeconds:F2}s";
            }
            else
            {
                return $"Failed (Exit: {ExitCode}, Error: {StandardError})";
            }
        }
    }
    
    /// <summary>
    /// Opções para execução de processo
    /// </summary>
    public class ProcessOptions
    {
        public int TimeoutMilliseconds { get; set; } = 300000; // 5 minutos default
        public string WorkingDirectory { get; set; }
        public bool LogOutput { get; set; } = true;
        public bool LogErrors { get; set; } = true;
        public IProgress<string> OutputProgress { get; set; }
    }
    
    /// <summary>
    /// Wrapper para execução de processos com timeout e retry
    /// </summary>
    public static class ProcessRunner
    {
        /// <summary>
        /// Executa um processo com timeout
        /// </summary>
        public static async Task<ProcessResult> RunAsync(
            string fileName,
            string arguments,
            ProcessOptions options = null,
            CancellationToken cancellationToken = default)
        {
            options = options ?? new ProcessOptions();
            var stopwatch = Stopwatch.StartNew();
            
            try
            {
                var processInfo = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = arguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    WorkingDirectory = options.WorkingDirectory ?? Environment.CurrentDirectory
                };
                
                var outputBuilder = new StringBuilder();
                var errorBuilder = new StringBuilder();
                
                using (var process = new Process { StartInfo = processInfo })
                {
                    // Captura output em tempo real
                    process.OutputDataReceived += (sender, e) =>
                    {
                        if (!string.IsNullOrEmpty(e.Data))
                        {
                            outputBuilder.AppendLine(e.Data);
                            
                            if (options.LogOutput)
                            {
                                Debug.Log($"[ProcessRunner] {e.Data}");
                            }
                            
                            options.OutputProgress?.Report(e.Data);
                        }
                    };
                    
                    process.ErrorDataReceived += (sender, e) =>
                    {
                        if (!string.IsNullOrEmpty(e.Data))
                        {
                            errorBuilder.AppendLine(e.Data);
                            
                            if (options.LogErrors)
                            {
                                Debug.LogWarning($"[ProcessRunner] {e.Data}");
                            }
                        }
                    };
                    
                    // Inicia processo
                    if (!process.Start())
                    {
                        throw new InvalidOperationException($"Failed to start process: {fileName}");
                    }
                    
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();
                    
                    // Aguarda com timeout
                    bool completed = await Task.Run(() =>
                    {
                        return process.WaitForExit(options.TimeoutMilliseconds);
                    }, cancellationToken);
                    
                    stopwatch.Stop();
                    
                    // Se não completou no tempo, mata o processo
                    if (!completed || cancellationToken.IsCancellationRequested)
                    {
                        try
                        {
                            if (!process.HasExited)
                            {
                                process.Kill();
                                Debug.LogWarning($"[ProcessRunner] Process killed due to timeout or cancellation");
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.LogError($"[ProcessRunner] Failed to kill process: {ex.Message}");
                        }
                        
                        return new ProcessResult
                        {
                            ExitCode = -1,
                            TimedOut = true,
                            StandardOutput = outputBuilder.ToString(),
                            StandardError = errorBuilder.ToString(),
                            ExecutionTime = stopwatch.Elapsed
                        };
                    }
                    
                    // Aguarda um pouco mais para garantir que todo output foi capturado
                    await Task.Delay(100, cancellationToken);
                    
                    return new ProcessResult
                    {
                        ExitCode = process.ExitCode,
                        TimedOut = false,
                        StandardOutput = outputBuilder.ToString(),
                        StandardError = errorBuilder.ToString(),
                        ExecutionTime = stopwatch.Elapsed
                    };
                }
            }
            catch (OperationCanceledException)
            {
                stopwatch.Stop();
                Debug.LogWarning("[ProcessRunner] Operation cancelled");
                
                return new ProcessResult
                {
                    ExitCode = -1,
                    TimedOut = true,
                    StandardError = "Operation cancelled",
                    ExecutionTime = stopwatch.Elapsed
                };
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                Debug.LogError($"[ProcessRunner] Exception: {ex.Message}");
                
                return new ProcessResult
                {
                    ExitCode = -1,
                    StandardError = $"Exception: {ex.Message}\n{ex.StackTrace}",
                    ExecutionTime = stopwatch.Elapsed
                };
            }
        }
        
        /// <summary>
        /// Executa um processo com retry automático
        /// </summary>
        public static async Task<ProcessResult> RunWithRetryAsync(
            string fileName,
            string arguments,
            int maxRetries = 2,
            ProcessOptions options = null,
            CancellationToken cancellationToken = default)
        {
            ProcessResult lastResult = null;
            
            for (int attempt = 0; attempt <= maxRetries; attempt++)
            {
                if (attempt > 0)
                {
                    int delayMs = (int)Math.Pow(2, attempt) * 1000; // Exponential backoff
                    Debug.Log($"[ProcessRunner] Retry attempt {attempt}/{maxRetries} after {delayMs}ms delay");
                    await Task.Delay(delayMs, cancellationToken);
                }
                
                lastResult = await RunAsync(fileName, arguments, options, cancellationToken);
                
                if (lastResult.Success)
                {
                    return lastResult;
                }
                
                if (lastResult.TimedOut)
                {
                    Debug.LogWarning($"[ProcessRunner] Attempt {attempt + 1} timed out");
                }
                else
                {
                    Debug.LogWarning($"[ProcessRunner] Attempt {attempt + 1} failed with exit code {lastResult.ExitCode}");
                }
            }
            
            Debug.LogError($"[ProcessRunner] All {maxRetries + 1} attempts failed");
            return lastResult;
        }
        
        /// <summary>
        /// Verifica se um executável pode ser executado
        /// </summary>
        public static async Task<bool> CanExecuteAsync(string fileName)
        {
            try
            {
                var result = await RunAsync(
                    fileName,
                    "--version",
                    new ProcessOptions
                    {
                        TimeoutMilliseconds = 5000,
                        LogOutput = false,
                        LogErrors = false
                    }
                );
                
                return result.ExitCode == 0 || !result.TimedOut;
            }
            catch
            {
                return false;
            }
        }
    }
}

