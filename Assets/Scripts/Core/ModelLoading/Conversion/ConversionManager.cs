using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace PolyDiet.Core.ModelLoading.Conversion
{
    /// <summary>
    /// Gerenciador de conversão de modelos 3D
    /// Orquestra diferentes estratégias de conversão
    /// </summary>
    public class ConversionManager
    {
        private readonly List<IConversionStrategy> _strategies;
        
        public ConversionManager()
        {
            // Registra estratégias na ordem de prioridade
            _strategies = new List<IConversionStrategy>
            {
                new Obj2GltfStrategy(),
                new GltfTransformStrategy(),
                new SimpleConversionStrategy()
            };
        }
        
        /// <summary>
        /// Converte um arquivo de modelo para GLB
        /// Tenta diferentes estratégias automaticamente
        /// </summary>
        public async Task<ConversionResult> ConvertAsync(
            string sourcePath,
            string destinationPath,
            IProgress<float> progress = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Validação básica
                if (!File.Exists(sourcePath))
                {
                    return ConversionResult.Failed($"Arquivo de origem não encontrado: {sourcePath}");
                }
                
                string sourceExtension = Path.GetExtension(sourcePath).ToLower();
                
                Debug.Log($"[ConversionManager] Converting {sourceExtension} file to GLB");
                Debug.Log($"[ConversionManager] Source: {sourcePath}");
                Debug.Log($"[ConversionManager] Destination: {destinationPath}");
                
                // Encontra estratégias compatíveis
                var compatibleStrategies = _strategies.Where(s => s.CanHandle(sourceExtension)).ToList();
                
                if (compatibleStrategies.Count == 0)
                {
                    return ConversionResult.Failed(
                        $"Nenhuma estratégia disponível para converter {sourceExtension} para GLB"
                    );
                }
                
                Debug.Log($"[ConversionManager] Found {compatibleStrategies.Count} compatible strategies");
                
                // Tenta cada estratégia em ordem
                ConversionResult lastResult = null;
                
                foreach (var strategy in compatibleStrategies)
                {
                    Debug.Log($"[ConversionManager] Trying strategy: {strategy.Name}");
                    
                    // Verifica se estratégia está disponível
                    bool isAvailable = await strategy.IsAvailableAsync();
                    if (!isAvailable)
                    {
                        Debug.LogWarning($"[ConversionManager] Strategy {strategy.Name} is not available");
                        continue;
                    }
                    
                    // Tenta converter
                    try
                    {
                        lastResult = await strategy.ConvertAsync(
                            sourcePath,
                            destinationPath,
                            progress,
                            cancellationToken
                        );
                        
                        if (lastResult.Success)
                        {
                            Debug.Log($"[ConversionManager] ✅ Success with {strategy.Name}");
                            return lastResult;
                        }
                        else
                        {
                            Debug.LogWarning($"[ConversionManager] ❌ Strategy {strategy.Name} failed: {lastResult.ErrorMessage}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[ConversionManager] Exception with {strategy.Name}: {ex.Message}");
                        lastResult = ConversionResult.Failed($"Exception: {ex.Message}", strategy.Name);
                    }
                }
                
                // Se chegou aqui, todas as estratégias falharam
                string errorMsg = lastResult != null
                    ? $"Todas as estratégias falharam. Último erro: {lastResult.ErrorMessage}"
                    : "Nenhuma estratégia disponível";
                
                return ConversionResult.Failed(errorMsg);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ConversionManager] Fatal exception: {ex.Message}");
                return ConversionResult.Failed($"Erro fatal: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Converte usando uma estratégia específica
        /// </summary>
        public async Task<ConversionResult> ConvertWithStrategyAsync(
            string sourcePath,
            string destinationPath,
            string strategyName,
            IProgress<float> progress = null,
            CancellationToken cancellationToken = default)
        {
            var strategy = _strategies.FirstOrDefault(s => s.Name.Equals(strategyName, StringComparison.OrdinalIgnoreCase));
            
            if (strategy == null)
            {
                return ConversionResult.Failed($"Estratégia não encontrada: {strategyName}");
            }
            
            if (!await strategy.IsAvailableAsync())
            {
                return ConversionResult.Failed($"Estratégia {strategyName} não está disponível");
            }
            
            return await strategy.ConvertAsync(sourcePath, destinationPath, progress, cancellationToken);
        }
        
        /// <summary>
        /// Lista estratégias disponíveis para um formato
        /// </summary>
        public async Task<List<string>> GetAvailableStrategiesAsync(string sourceExtension)
        {
            var compatibleStrategies = _strategies.Where(s => s.CanHandle(sourceExtension));
            var availableStrategies = new List<string>();
            
            foreach (var strategy in compatibleStrategies)
            {
                if (await strategy.IsAvailableAsync())
                {
                    availableStrategies.Add(strategy.Name);
                }
            }
            
            return availableStrategies;
        }
        
        /// <summary>
        /// Verifica se um formato pode ser convertido
        /// </summary>
        public bool CanConvert(string sourceExtension)
        {
            return _strategies.Any(s => s.CanHandle(sourceExtension));
        }
        
        /// <summary>
        /// Gera relatório de estratégias disponíveis
        /// </summary>
        public async Task<string> GenerateStrategiesReportAsync()
        {
            var report = "=== Estratégias de Conversão ===\n\n";
            
            foreach (var strategy in _strategies)
            {
                bool isAvailable = await strategy.IsAvailableAsync();
                string status = isAvailable ? "✅ Disponível" : "❌ Indisponível";
                
                report += $"{strategy.Name}: {status}\n";
            }
            
            return report;
        }
    }
}

