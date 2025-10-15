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
    /// Compressor Draco usando gltf-transform
    /// </summary>
    public class DracoCompressor
    {
        private string _toolPath;
        
        public async Task<bool> IsAvailableAsync()
        {
            var toolInfo = ToolDetector.DetectGltfTransform();
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
                    return CompressionResult.Failed($"Arquivo de entrada não encontrado: {inputPath}", CompressionType.Draco);
                }
                
                progress?.Report(0.1f);
                
                // Verifica se ferramenta está disponível
                if (!await IsAvailableAsync())
                {
                    var toolInfo = ToolDetector.DetectGltfTransform();
                    return CompressionResult.Failed(
                        $"gltf-transform não encontrado. Instale com: {toolInfo.InstallCommand}",
                        CompressionType.Draco
                    );
                }
                
                progress?.Report(0.2f);
                
                // Valida arquivo de entrada
                var validation = GltfValidator.QuickValidate(inputPath);
                if (!validation.IsValid)
                {
                    return CompressionResult.Failed(
                        $"Arquivo de entrada inválido: {validation.ErrorMessage}",
                        CompressionType.Draco
                    );
                }
                
                progress?.Report(0.3f);
                
                Debug.Log($"[DracoCompressor] Compressing {inputPath} to {outputPath}");
                
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
                        CompressionType.Draco
                    );
                }
                
                progress?.Report(0.4f);
                
                // Constrói argumentos baseado no nível de compressão
                string compressionArgs = GetCompressionArgs(options?.Level ?? CompressionLevel.Default);
                string arguments = $"optimize \"{inputPath}\" \"{outputPath}\" --compress draco {compressionArgs}";
                
                var processOptions = new ProcessOptions
                {
                    TimeoutMilliseconds = (options?.TimeoutSeconds ?? 300) * 1000,
                    WorkingDirectory = options?.WorkingDirectory ?? Path.GetDirectoryName(inputPath),
                    LogOutput = options?.LogOutput ?? true,
                    OutputProgress = new Progress<string>(line =>
                    {
                        Debug.Log($"[DracoCompressor] {line}");
                        
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
                        $"Compressão Draco falhou: {result.StandardError}",
                        CompressionType.Draco
                    );
                }
                
                // Valida arquivo de saída
                if (!File.Exists(outputPath))
                {
                    return CompressionResult.Failed(
                        "Arquivo de destino não foi criado",
                        CompressionType.Draco
                    );
                }
                
                var outputValidation = GltfValidator.QuickValidate(outputPath);
                if (!outputValidation.IsValid)
                {
                    return CompressionResult.Failed(
                        $"Arquivo GLB comprimido é inválido: {outputValidation.ErrorMessage}",
                        CompressionType.Draco
                    );
                }
                
                progress?.Report(1.0f);
                
                // Cria resultado de sucesso
                var compressionResult = CompressionResult.Succeeded(
                    CompressionType.Draco,
                    outputPath,
                    stopwatch.Elapsed
                );
                
                compressionResult.InputPath = inputPath;
                compressionResult.InputSizeBytes = new FileInfo(inputPath).Length;
                compressionResult.OutputSizeBytes = new FileInfo(outputPath).Length;
                
                Debug.Log($"[DracoCompressor] {compressionResult}");
                
                return compressionResult;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                Debug.LogError($"[DracoCompressor] Exception during compression: {ex.Message}");
                return CompressionResult.Failed($"Exceção: {ex.Message}", CompressionType.Draco);
            }
        }
        
        /// <summary>
        /// Constrói argumentos de compressão baseado no nível
        /// </summary>
        private string GetCompressionArgs(CompressionLevel level)
        {
            return level switch
            {
                CompressionLevel.Low => "--draco-compression-level 1",
                CompressionLevel.Default => "--draco-compression-level 5",
                CompressionLevel.High => "--draco-compression-level 8",
                CompressionLevel.Maximum => "--draco-compression-level 10",
                _ => "--draco-compression-level 5"
            };
        }
    }
}

