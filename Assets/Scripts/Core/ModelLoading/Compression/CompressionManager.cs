
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace PolyDiet.Core.ModelLoading.Compression
{
    /// <summary>
    /// Gerenciador de compressão de modelos GLB
    /// Orquestra diferentes tipos de compressão (Draco, Meshopt)
    /// </summary>
    public class CompressionManager
    {
        private readonly DracoCompressor _dracoCompressor;
        private readonly MeshoptCompressor _meshoptCompressor;
        
        public CompressionManager()
        {
            _dracoCompressor = new DracoCompressor();
            _meshoptCompressor = new MeshoptCompressor();
        }
        
        /// <summary>
        /// Comprime um arquivo GLB usando o tipo especificado
        /// </summary>
        public async Task<CompressionResult> CompressAsync(
            string inputPath,
            string outputPath,
            CompressionType type,
            CompressionOptions options = null,
            IProgress<float> progress = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Validação básica
                if (!File.Exists(inputPath))
                {
                    return CompressionResult.Failed($"Arquivo de entrada não encontrado: {inputPath}", type);
                }
                
                Debug.Log($"[CompressionManager] Compressing {type}: {inputPath} → {outputPath}");
                
                // Delega para o compressor específico
                return type switch
                {
                    CompressionType.Draco => await _dracoCompressor.CompressAsync(
                        inputPath, outputPath, options, progress, cancellationToken),
                    
                    CompressionType.Meshopt => await _meshoptCompressor.CompressAsync(
                        inputPath, outputPath, options, progress, cancellationToken),
                    
                    _ => CompressionResult.Failed($"Tipo de compressão não suportado: {type}", type)
                };
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CompressionManager] Exception during compression: {ex.Message}");
                return CompressionResult.Failed($"Erro fatal: {ex.Message}", type);
            }
        }
        
        /// <summary>
        /// Comprime um arquivo para múltiplas variantes
        /// </summary>
        public async Task<CompressionResult[]> CompressMultipleAsync(
            string inputPath,
            string outputDirectory,
            CompressionType[] types,
            CompressionOptions options = null,
            IProgress<float> progress = null,
            CancellationToken cancellationToken = default)
        {
            var results = new List<CompressionResult>();
            int totalTypes = types.Length;
            
            for (int i = 0; i < types.Length; i++)
            {
                var type = types[i];
                string outputPath = Path.Combine(outputDirectory, $"{type.ToString().ToLower()}.glb");
                
                // Progresso para este tipo específico
                var typeProgress = new Progress<float>(value =>
                {
                    float overallProgress = (i + value) / totalTypes;
                    progress?.Report(overallProgress);
                });
                
                var result = await CompressAsync(inputPath, outputPath, type, options, typeProgress, cancellationToken);
                results.Add(result);
                
                // Se falhou e não é o último, continua com os outros
                if (!result.Success)
                {
                    Debug.LogWarning($"[CompressionManager] Compression {type} failed, continuing with others");
                }
            }
            
            return results.ToArray();
        }
        
        /// <summary>
        /// Comprime automaticamente para todas as variantes disponíveis
        /// </summary>
        public async Task<CompressionResult[]> CompressAllAsync(
            string inputPath,
            string outputDirectory,
            CompressionOptions options = null,
            IProgress<float> progress = null,
            CancellationToken cancellationToken = default)
        {
            var availableTypes = new List<CompressionType>();
            
            // Verifica quais compressores estão disponíveis
            if (await _dracoCompressor.IsAvailableAsync())
            {
                availableTypes.Add(CompressionType.Draco);
            }
            
            if (await _meshoptCompressor.IsAvailableAsync())
            {
                availableTypes.Add(CompressionType.Meshopt);
            }
            
            if (availableTypes.Count == 0)
            {
                Debug.LogError("[CompressionManager] Nenhum compressor disponível");
                return new[] { CompressionResult.Failed("Nenhum compressor disponível", CompressionType.Draco) };
            }
            
            Debug.Log($"[CompressionManager] Available compressors: {string.Join(", ", availableTypes)}");
            
            return await CompressMultipleAsync(inputPath, outputDirectory, availableTypes.ToArray(), options, progress, cancellationToken);
        }
        
        /// <summary>
        /// Verifica se um tipo de compressão está disponível
        /// </summary>
        public async Task<bool> IsCompressionAvailableAsync(CompressionType type)
        {
            return type switch
            {
                CompressionType.Draco => await _dracoCompressor.IsAvailableAsync(),
                CompressionType.Meshopt => await _meshoptCompressor.IsAvailableAsync(),
                _ => false
            };
        }
        
        /// <summary>
        /// Lista todos os tipos de compressão disponíveis
        /// </summary>
        public async Task<CompressionType[]> GetAvailableCompressionsAsync()
        {
            var availableTypes = new List<CompressionType>();
            
            if (await _dracoCompressor.IsAvailableAsync())
            {
                availableTypes.Add(CompressionType.Draco);
            }
            
            if (await _meshoptCompressor.IsAvailableAsync())
            {
                availableTypes.Add(CompressionType.Meshopt);
            }
            
            return availableTypes.ToArray();
        }
        
        /// <summary>
        /// Gera relatório de compressores disponíveis
        /// </summary>
        public async Task<string> GenerateCompressorsReportAsync()
        {
            var report = "=== Compressores Disponíveis ===\n\n";
            
            var dracoAvailable = await _dracoCompressor.IsAvailableAsync();
            var meshoptAvailable = await _meshoptCompressor.IsAvailableAsync();
            
            report += $"Draco: {(dracoAvailable ? "✅ Disponível" : "❌ Indisponível")}\n";
            report += $"Meshopt: {(meshoptAvailable ? "✅ Disponível" : "❌ Indisponível")}\n";
            
            if (!dracoAvailable && !meshoptAvailable)
            {
                report += "\n⚠️ Nenhum compressor disponível!\n";
                report += "Instale as ferramentas necessárias:\n";
                report += "- Draco: npm install -g @gltf-transform/cli\n";
                report += "- Meshopt: npm install -g gltfpack\n";
            }
            
            return report;
        }
        
        /// <summary>
        /// Compara resultados de diferentes tipos de compressão
        /// </summary>
        public string CompareCompressionResults(CompressionResult[] results)
        {
            if (results == null || results.Length == 0)
            {
                return "Nenhum resultado para comparar";
            }
            
            var report = "=== Comparação de Compressão ===\n\n";
            
            foreach (var result in results.Where(r => r.Success))
            {
                report += $"{result.Type}:\n";
                report += $"  Tamanho: {result.InputSizeBytes:N0} → {result.OutputSizeBytes:N0} bytes\n";
                report += $"  Redução: {result.CompressionPercentage:F1}%\n";
                report += $"  Tempo: {result.CompressionTime.TotalSeconds:F2}s\n\n";
            }
            
            var failedResults = results.Where(r => !r.Success).ToArray();
            if (failedResults.Length > 0)
            {
                report += "Falhas:\n";
                foreach (var result in failedResults)
                {
                    report += $"  {result.Type}: {result.ErrorMessage}\n";
                }
            }
            
            return report;
        }
    }
}

