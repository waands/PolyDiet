using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using PolyDiet.Core.ModelLoading.Tools;
using PolyDiet.Core.ModelLoading.Validation;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PolyDiet.Core.ModelLoading.Conversion
{
    /// <summary>
    /// Estratégia de conversão usando gltf-transform
    /// Converte arquivos GLTF para GLB
    /// </summary>
    public class GltfTransformStrategy : IConversionStrategy
    {
        public string Name => "gltf-transform";
        
        private string _toolPath;
        
        public bool CanHandle(string sourceExtension)
        {
            return sourceExtension.ToLower() == ".gltf";
        }
        
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
        
        public async Task<ConversionResult> ConvertAsync(
            string sourcePath,
            string destinationPath,
            IProgress<float> progress = null,
            CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            
            try
            {
                // Validação prévia
                if (!File.Exists(sourcePath))
                {
                    return ConversionResult.Failed($"Arquivo de origem não encontrado: {sourcePath}", Name);
                }
                
                progress?.Report(0.1f);
                
                // Verifica se ferramenta está disponível
                if (!await IsAvailableAsync())
                {
                    var toolInfo = ToolDetector.DetectGltfTransform();
                    return ConversionResult.Failed(
                        $"gltf-transform não encontrado. Instale com: {toolInfo.InstallCommand}",
                        Name
                    );
                }
                
                progress?.Report(0.2f);
                
                // Valida arquivo GLTF
                var gltfValidation = GltfValidator.Validate(sourcePath);
                if (!gltfValidation.IsValid)
                {
                    return ConversionResult.Failed(
                        $"Arquivo GLTF inválido: {gltfValidation.ErrorMessage}",
                        Name
                    );
                }
                
                progress?.Report(0.3f);
                
                Debug.Log($"[{Name}] Converting {sourcePath} to {destinationPath}");
                
                // Cria diretório de destino
                string destDir = Path.GetDirectoryName(destinationPath);
                if (!Directory.Exists(destDir))
                {
                    Directory.CreateDirectory(destDir);
                }
                
                // Executa conversão (comando copy converte de GLTF para GLB)
                string arguments = $"copy \"{sourcePath}\" \"{destinationPath}\"";
                
                var processOptions = new ProcessOptions
                {
                    TimeoutMilliseconds = 180000, // 3 minutos
                    WorkingDirectory = Path.GetDirectoryName(sourcePath),
                    OutputProgress = new Progress<string>(line =>
                    {
                        Debug.Log($"[{Name}] {line}");
                        progress?.Report(0.6f);
                    })
                };
                
                var result = await ProcessRunner.RunAsync(_toolPath, arguments, processOptions, cancellationToken);
                
                progress?.Report(0.9f);
                
                stopwatch.Stop();
                
                // Verifica resultado
                if (!result.Success)
                {
                    return ConversionResult.Failed(
                        $"Conversão falhou: {result.StandardError}",
                        Name
                    );
                }
                
                // Valida arquivo de saída
                if (!File.Exists(destinationPath))
                {
                    return ConversionResult.Failed(
                        "Arquivo de destino não foi criado",
                        Name
                    );
                }
                
                var outputValidation = GltfValidator.QuickValidate(destinationPath);
                if (!outputValidation.IsValid)
                {
                    return ConversionResult.Failed(
                        $"Arquivo GLB gerado é inválido: {outputValidation.ErrorMessage}",
                        Name
                    );
                }
                
                progress?.Report(1.0f);
                
                // Cria resultado de sucesso
                var conversionResult = ConversionResult.Succeeded(destinationPath, Name, stopwatch.Elapsed);
                conversionResult.InputPath = sourcePath;
                conversionResult.InputSizeBytes = new FileInfo(sourcePath).Length;
                conversionResult.OutputSizeBytes = new FileInfo(destinationPath).Length;
                
                Debug.Log($"[{Name}] {conversionResult}");
                
                return conversionResult;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                Debug.LogError($"[{Name}] Exception during conversion: {ex.Message}");
                return ConversionResult.Failed($"Exceção: {ex.Message}", Name);
            }
        }
    }
}

