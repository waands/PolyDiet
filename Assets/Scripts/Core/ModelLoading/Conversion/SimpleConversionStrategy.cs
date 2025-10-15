using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using PolyDiet.Core.ModelLoading.Validation;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PolyDiet.Core.ModelLoading.Conversion
{
    /// <summary>
    /// Estratégia simples de conversão
    /// Para arquivos GLB/GLTF que só precisam ser copiados
    /// </summary>
    public class SimpleConversionStrategy : IConversionStrategy
    {
        public string Name => "Simple Copy";
        
        public bool CanHandle(string sourceExtension)
        {
            string ext = sourceExtension.ToLower();
            return ext == ".glb" || ext == ".gltf";
        }
        
        public Task<bool> IsAvailableAsync()
        {
            // Sempre disponível
            return Task.FromResult(true);
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
                
                string sourceExt = Path.GetExtension(sourcePath).ToLower();
                string destExt = Path.GetExtension(destinationPath).ToLower();
                
                // Valida arquivo de entrada
                var validation = GltfValidator.Validate(sourcePath);
                if (!validation.IsValid)
                {
                    return ConversionResult.Failed(
                        $"Arquivo inválido: {validation.ErrorMessage}",
                        Name
                    );
                }
                
                progress?.Report(0.3f);
                
                Debug.Log($"[{Name}] Copying {sourcePath} to {destinationPath}");
                
                // Cria diretório de destino
                string destDir = Path.GetDirectoryName(destinationPath);
                if (!Directory.Exists(destDir))
                {
                    Directory.CreateDirectory(destDir);
                }
                
                progress?.Report(0.5f);
                
                // Se ambos são GLB, apenas copia
                if (sourceExt == ".glb" && destExt == ".glb")
                {
                    File.Copy(sourcePath, destinationPath, overwrite: true);
                }
                // Se origem é GLTF e destino é GLB, precisa converter
                // Mas como essa estratégia é "simples", apenas avisa que não pode fazer isso
                else if (sourceExt == ".gltf" && destExt == ".glb")
                {
                    return ConversionResult.Failed(
                        "Conversão GLTF → GLB requer gltf-transform. Use GltfTransformStrategy.",
                        Name
                    );
                }
                // Outros casos: copia diretamente
                else
                {
                    File.Copy(sourcePath, destinationPath, overwrite: true);
                }
                
                progress?.Report(0.9f);
                
                // Verifica se arquivo foi criado
                if (!File.Exists(destinationPath))
                {
                    return ConversionResult.Failed(
                        "Arquivo de destino não foi criado",
                        Name
                    );
                }
                
                progress?.Report(1.0f);
                
                stopwatch.Stop();
                
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

