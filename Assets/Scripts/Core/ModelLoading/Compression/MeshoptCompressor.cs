using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using PolyDiet.Core.ModelLoading.Tools;
using PolyDiet.Core.ModelLoading.Validation;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PolyDiet.Core.ModelLoading.Compression
{
    /// <summary>
    /// Compressor Meshopt usando gltfpack
    /// </summary>
    public class MeshoptCompressor
    {
        private string _toolPath;
        
        public async Task<bool> IsAvailableAsync()
        {
            var toolInfo = ToolDetector.DetectGltfpack();
            if (toolInfo.IsInstalled)
            {
                _toolPath = toolInfo.Path;
                return true;
            }
            
            return false;
        }
        
        public async Task<CompressionResult> CompressAsync(
            string inputPath,
            string outputPath,
            CompressionOptions options = null,
            IProgress<float> progress = null,
            CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            
            try
            {
                // Validação prévia
                if (!File.Exists(inputPath))
                {
                    return CompressionResult.Failed($"Arquivo de entrada não encontrado: {inputPath}", CompressionType.Meshopt);
                }
                
                progress?.Report(0.1f);
                
                // Verifica se ferramenta está disponível
                if (!await IsAvailableAsync())
                {
                    var toolInfo = ToolDetector.DetectGltfpack();
                    return CompressionResult.Failed(
                        $"gltfpack não encontrado. Instale com: {toolInfo.InstallCommand}",
                        CompressionType.Meshopt
                    );
                }
                
                progress?.Report(0.2f);
                
                // Valida arquivo de entrada
                var validation = GltfValidator.QuickValidate(inputPath);
                if (!validation.IsValid)
                {
                    return CompressionResult.Failed(
                        $"Arquivo de entrada inválido: {validation.ErrorMessage}",
                        CompressionType.Meshopt
                    );
                }
                
                progress?.Report(0.3f);
                
                Debug.Log($"[MeshoptCompressor] Compressing {inputPath} to {outputPath}");
                
                // Cria diretório de destino
                string destDir = Path.GetDirectoryName(outputPath);
                if (!Directory.Exists(destDir))
                {
                    Directory.CreateDirectory(destDir);
                }
                
                // Verifica se arquivo de saída já existe
                if (File.Exists(outputPath) && !(options?.OverwriteExisting ?? true))
                {
                    return CompressionResult.Failed(
                        $"Arquivo de saída já existe: {outputPath}",
                        CompressionType.Meshopt
                    );
                }
                
                progress?.Report(0.4f);
                
                // Constrói argumentos baseado no nível de compressão
                string compressionArgs = GetCompressionArgs(options?.Level ?? CompressionLevel.Default);
                string arguments = $"-i \"{inputPath}\" -o \"{outputPath}\" {compressionArgs}";
                
                var processOptions = new ProcessOptions
                {
                    TimeoutMilliseconds = (options?.TimeoutSeconds ?? 300) * 1000,
                    WorkingDirectory = options?.WorkingDirectory ?? Path.GetDirectoryName(inputPath),
                    LogOutput = options?.LogOutput ?? true,
                    OutputProgress = new Progress<string>(line =>
                    {
                        Debug.Log($"[MeshoptCompressor] {line}");
                        
                        // Estima progresso baseado em output
                        if (line.Contains("Reading"))
                        {
                            progress?.Report(0.5f);
                        }
                        else if (line.Contains("Optimizing"))
                        {
                            progress?.Report(0.7f);
                        }
                        else if (line.Contains("Writing"))
                        {
                            progress?.Report(0.9f);
                        }
                    })
                };
                
                var result = await ProcessRunner.RunWithRetryAsync(
                    _toolPath,
                    arguments,
                    options?.MaxRetries ?? 2,
                    processOptions,
                    cancellationToken
                );
                
                progress?.Report(0.95f);
                
                stopwatch.Stop();
                
                // Verifica resultado
                if (!result.Success)
                {
                    return CompressionResult.Failed(
                        $"Compressão Meshopt falhou: {result.StandardError}",
                        CompressionType.Meshopt
                    );
                }
                
                // Valida arquivo de saída
                if (!File.Exists(outputPath))
                {
                    return CompressionResult.Failed(
                        "Arquivo de destino não foi criado",
                        CompressionType.Meshopt
                    );
                }
                
                var outputValidation = GltfValidator.QuickValidate(outputPath);
                if (!outputValidation.IsValid)
                {
                    return CompressionResult.Failed(
                        $"Arquivo GLB comprimido é inválido: {outputValidation.ErrorMessage}",
                        CompressionType.Meshopt
                    );
                }
                
                progress?.Report(1.0f);
                
                // Cria resultado de sucesso
                var compressionResult = CompressionResult.Succeeded(
                    CompressionType.Meshopt,
                    outputPath,
                    stopwatch.Elapsed
                );
                
                compressionResult.InputPath = inputPath;
                compressionResult.InputSizeBytes = new FileInfo(inputPath).Length;
                compressionResult.OutputSizeBytes = new FileInfo(outputPath).Length;
                
                Debug.Log($"[MeshoptCompressor] {compressionResult}");
                
                return compressionResult;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                Debug.LogError($"[MeshoptCompressor] Exception during compression: {ex.Message}");
                return CompressionResult.Failed($"Exceção: {ex.Message}", CompressionType.Meshopt);
            }
        }
        
        /// <summary>
        /// Constrói argumentos de compressão baseado no nível
        /// </summary>
        private string GetCompressionArgs(CompressionLevel level)
        {
            return level switch
            {
                CompressionLevel.Low => "-c", // compressão básica
                CompressionLevel.Default => "-cc", // compressão extra
                CompressionLevel.High => "-cc -si", // compressão extra + simplificação
                CompressionLevel.Maximum => "-cc -si -sa", // compressão máxima + simplificação + análise
                _ => "-cc"
            };
        }
    }
}

