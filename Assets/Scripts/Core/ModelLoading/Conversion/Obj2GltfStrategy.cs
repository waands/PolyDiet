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
    /// Estratégia de conversão usando obj2gltf
    /// Converte arquivos OBJ para GLB
    /// </summary>
    public class Obj2GltfStrategy : IConversionStrategy
    {
        public string Name => "obj2gltf";
        
        private string _toolPath;
        
        public bool CanHandle(string sourceExtension)
        {
            return sourceExtension.ToLower() == ".obj";
        }
        
        public async Task<bool> IsAvailableAsync()
        {
            var toolInfo = ToolDetector.DetectObj2Gltf();
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
                    var toolInfo = ToolDetector.DetectObj2Gltf();
                    return ConversionResult.Failed(
                        $"obj2gltf não encontrado. Instale com: {toolInfo.InstallCommand}",
                        Name
                    );
                }
                
                progress?.Report(0.2f);
                
                // Valida arquivo OBJ
                var objValidation = ValidateObjFile(sourcePath);
                if (!objValidation.IsValid)
                {
                    return ConversionResult.Failed(
                        $"Arquivo OBJ inválido: {objValidation.ErrorMessage}",
                        Name
                    );
                }
                
                progress?.Report(0.3f);
                
                Debug.Log($"[{Name}] Converting {sourcePath} to {destinationPath}");
                Debug.Log($"[{Name}] OBJ info: {objValidation.VertexCount} vertices, {objValidation.FaceCount} faces");
                
                // Cria diretório de destino
                string destDir = Path.GetDirectoryName(destinationPath);
                if (!Directory.Exists(destDir))
                {
                    Directory.CreateDirectory(destDir);
                }
                
                // Executa conversão
                string arguments = $"-i \"{sourcePath}\" -o \"{destinationPath}\"";
                
                var processOptions = new ProcessOptions
                {
                    TimeoutMilliseconds = 300000, // 5 minutos
                    WorkingDirectory = Path.GetDirectoryName(sourcePath),
                    OutputProgress = new Progress<string>(line =>
                    {
                        Debug.Log($"[{Name}] {line}");
                        // Estima progresso baseado em output
                        if (line.Contains("Parsing"))
                        {
                            progress?.Report(0.4f);
                        }
                        else if (line.Contains("Converting"))
                        {
                            progress?.Report(0.6f);
                        }
                        else if (line.Contains("Writing"))
                        {
                            progress?.Report(0.8f);
                        }
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
                conversionResult.Metadata["VertexCount"] = objValidation.VertexCount.ToString();
                conversionResult.Metadata["FaceCount"] = objValidation.FaceCount.ToString();
                
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
        
        /// <summary>
        /// Validação básica de arquivo OBJ
        /// </summary>
        private ObjValidationResult ValidateObjFile(string objPath)
        {
            try
            {
                if (!File.Exists(objPath))
                {
                    return new ObjValidationResult
                    {
                        IsValid = false,
                        ErrorMessage = "Arquivo não encontrado"
                    };
                }
                
                var lines = File.ReadAllLines(objPath);
                int vertexCount = 0;
                int faceCount = 0;
                int normalCount = 0;
                int textureCount = 0;
                
                foreach (var line in lines)
                {
                    var trimmedLine = line.Trim();
                    if (string.IsNullOrEmpty(trimmedLine) || trimmedLine.StartsWith("#"))
                        continue;
                    
                    if (trimmedLine.StartsWith("v ")) vertexCount++;
                    else if (trimmedLine.StartsWith("vn ")) normalCount++;
                    else if (trimmedLine.StartsWith("vt ")) textureCount++;
                    else if (trimmedLine.StartsWith("f ")) faceCount++;
                }
                
                if (vertexCount == 0)
                {
                    return new ObjValidationResult
                    {
                        IsValid = false,
                        ErrorMessage = "Nenhum vértice encontrado"
                    };
                }
                
                if (faceCount == 0)
                {
                    return new ObjValidationResult
                    {
                        IsValid = false,
                        ErrorMessage = "Nenhuma face encontrada"
                    };
                }
                
                return new ObjValidationResult
                {
                    IsValid = true,
                    VertexCount = vertexCount,
                    FaceCount = faceCount,
                    NormalCount = normalCount,
                    TextureCount = textureCount
                };
            }
            catch (Exception ex)
            {
                return new ObjValidationResult
                {
                    IsValid = false,
                    ErrorMessage = $"Erro ao validar: {ex.Message}"
                };
            }
        }
        
        /// <summary>
        /// Resultado da validação de arquivo OBJ
        /// </summary>
        private class ObjValidationResult
        {
            public bool IsValid { get; set; }
            public string ErrorMessage { get; set; }
            public int VertexCount { get; set; }
            public int FaceCount { get; set; }
            public int NormalCount { get; set; }
            public int TextureCount { get; set; }
        }
    }
}

