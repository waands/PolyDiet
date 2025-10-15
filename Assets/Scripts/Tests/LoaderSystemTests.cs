using System;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using PolyDiet.Core.ModelLoading.Validation;
using PolyDiet.Core.ModelLoading.Conversion;
using PolyDiet.Core.ModelLoading.Compression;
using PolyDiet.Core.ModelLoading.Loading;
using PolyDiet.Core.ModelLoading.Wizard;

namespace PolyDiet.Tests
{
    /// <summary>
    /// Testes do sistema de loader refatorado
    /// </summary>
    public class LoaderSystemTests : MonoBehaviour
    {
        [Header("Test Configuration")]
        public string testModelPath = "Assets/StreamingAssets/Models/suzanne/original/model.glb";
        public string testObjPath = "Assets/StreamingAssets/Models/suzanne/original/model.obj";
        public bool runValidationTests = true;
        public bool runConversionTests = true;
        public bool runCompressionTests = true;
        public bool runLoadingTests = true;
        public bool runWizardTests = true;
        
        [Header("Test Results")]
        public string testResults = "";
        
        [ContextMenu("Run All Tests")]
        public async void RunAllTests()
        {
            Debug.Log("=== INICIANDO TESTES DO SISTEMA DE LOADER ===");
            testResults = "=== TESTES DO SISTEMA DE LOADER ===\n\n";
            
            try
            {
                if (runValidationTests)
                {
                    await RunValidationTests();
                }
                
                if (runConversionTests)
                {
                    await RunConversionTests();
                }
                
                if (runCompressionTests)
                {
                    await RunCompressionTests();
                }
                
                if (runLoadingTests)
                {
                    await RunLoadingTests();
                }
                
                if (runWizardTests)
                {
                    await RunWizardTests();
                }
                
                Debug.Log("=== TODOS OS TESTES CONCLUÍDOS ===");
                testResults += "\n=== TODOS OS TESTES CONCLUÍDOS ===";
            }
            catch (Exception ex)
            {
                Debug.LogError($"Erro durante os testes: {ex.Message}");
                testResults += $"\n❌ ERRO GERAL: {ex.Message}";
            }
        }
        
        private async Task RunValidationTests()
        {
            Debug.Log("🔍 Testando Sistema de Validação...");
            testResults += "🔍 SISTEMA DE VALIDAÇÃO:\n";
            
            try
            {
                // Teste 1: Validação rápida de GLB
                if (File.Exists(testModelPath))
                {
                    var quickResult = GltfValidator.QuickValidate(testModelPath);
                    LogTestResult("QuickValidate GLB", quickResult.IsValid, quickResult.ErrorMessage);
                }
                else
                {
                    LogTestResult("QuickValidate GLB", false, "Arquivo de teste não encontrado");
                }
                
                // Teste 2: Validação completa de GLB
                if (File.Exists(testModelPath))
                {
                    var fullResult = GltfValidator.Validate(testModelPath);
                    LogTestResult("FullValidate GLB", fullResult.IsValid, fullResult.ErrorMessage);
                    
                    if (fullResult.IsValid && fullResult.FileInfo != null)
                    {
                        testResults += $"  📊 Vértices: {fullResult.FileInfo.EstimatedVertexCount}\n";
                        testResults += $"  📊 Faces: {fullResult.FileInfo.EstimatedTriangleCount}\n";
                        testResults += $"  📊 Tamanho: {fullResult.FileInfo.FileSizeBytes / 1024} KB\n";
                    }
                }
                
                // Teste 3: Validação de arquivo inexistente
                var notFoundResult = GltfValidator.QuickValidate("arquivo_inexistente.glb");
                LogTestResult("Validate Non-existent", !notFoundResult.IsValid, "Deveria falhar para arquivo inexistente");
                
            }
            catch (Exception ex)
            {
                LogTestResult("Validation Tests", false, $"Exceção: {ex.Message}");
            }
            
            testResults += "\n";
        }
        
        private async Task RunConversionTests()
        {
            Debug.Log("🔄 Testando Sistema de Conversão...");
            testResults += "🔄 SISTEMA DE CONVERSÃO:\n";
            
            try
            {
                var conversionManager = new ConversionManager();
                
                // Teste 1: Verificar estratégias disponíveis
                var strategies = await conversionManager.GetAvailableStrategiesAsync(".obj");
                LogTestResult("GetAvailableStrategies", strategies.Count > 0, $"Encontradas {strategies.Count} estratégias");
                
                if (strategies.Count > 0)
                {
                    testResults += $"  📋 Estratégias: {string.Join(", ", strategies)}\n";
                }
                
                // Teste 2: Tentar conversão se arquivo OBJ existir
                if (File.Exists(testObjPath))
                {
                    string tempOutput = Path.Combine(Application.temporaryCachePath, "test_conversion.glb");
                    var conversionResult = await conversionManager.ConvertAsync(testObjPath, tempOutput);
                    LogTestResult("Convert OBJ to GLB", conversionResult.Success, conversionResult.ErrorMessage);
                    
                    if (conversionResult.Success && File.Exists(tempOutput))
                    {
                        testResults += $"  📊 Entrada: {conversionResult.InputSizeBytes / 1024} KB\n";
                        testResults += $"  📊 Saída: {conversionResult.OutputSizeBytes / 1024} KB\n";
                        
                        // Limpa arquivo temporário
                        File.Delete(tempOutput);
                    }
                }
                else
                {
                    LogTestResult("Convert OBJ to GLB", false, "Arquivo OBJ de teste não encontrado");
                }
                
            }
            catch (Exception ex)
            {
                LogTestResult("Conversion Tests", false, $"Exceção: {ex.Message}");
            }
            
            testResults += "\n";
        }
        
        private async Task RunCompressionTests()
        {
            Debug.Log("🗜️ Testando Sistema de Compressão...");
            testResults += "🗜️ SISTEMA DE COMPRESSÃO:\n";
            
            try
            {
                var compressionManager = new CompressionManager();
                
                // Teste 1: Verificar compressores disponíveis
                var availableCompressions = await compressionManager.GetAvailableCompressionsAsync();
                LogTestResult("GetAvailableCompressions", availableCompressions.Length > 0, $"Encontrados {availableCompressions.Length} compressores");
                
                if (availableCompressions.Length > 0)
                {
                    testResults += $"  📋 Compressores: {string.Join(", ", availableCompressions)}\n";
                }
                
                // Teste 2: Tentar compressão se arquivo GLB existir
                if (File.Exists(testModelPath))
                {
                    string tempOutput = Path.Combine(Application.temporaryCachePath, "test_compression.glb");
                    
                    foreach (var compressionType in availableCompressions)
                    {
                        var compressionResult = await compressionManager.CompressAsync(
                            testModelPath, 
                            tempOutput, 
                            compressionType
                        );
                        
                        LogTestResult($"Compress {compressionType}", compressionResult.Success, compressionResult.ErrorMessage);
                        
                        if (compressionResult.Success)
                        {
                            testResults += $"  📊 {compressionType}: {compressionResult.CompressionPercentage:F1}% redução\n";
                            
                            // Limpa arquivo temporário
                            if (File.Exists(tempOutput))
                            {
                                File.Delete(tempOutput);
                            }
                        }
                    }
                }
                else
                {
                    LogTestResult("Compression Tests", false, "Arquivo GLB de teste não encontrado");
                }
                
            }
            catch (Exception ex)
            {
                LogTestResult("Compression Tests", false, $"Exceção: {ex.Message}");
            }
            
            testResults += "\n";
        }
        
        private async Task RunLoadingTests()
        {
            Debug.Log("📥 Testando Sistema de Carregamento...");
            testResults += "📥 SISTEMA DE CARREGAMENTO:\n";
            
            try
            {
                var modelLoader = new ModelLoader();
                
                // Teste 1: Carregamento com fallback
                var loadResult = await modelLoader.LoadModelWithFallbackAsync(
                    "suzanne", 
                    new[] { "draco", "meshopt", "original" }
                );
                
                LogTestResult("LoadModelWithFallback", loadResult.Success, loadResult.ErrorMessage);
                
                if (loadResult.Success)
                {
                    testResults += $"  📊 Variante usada: {loadResult.VariantUsed}\n";
                    testResults += $"  📊 Tempo de carregamento: {loadResult.LoadTimeSeconds:F2}s\n";
                    testResults += $"  📊 Tamanho do arquivo: {loadResult.FileSizeBytes / 1024} KB\n";
                    
                    // Limpa objeto carregado
                    if (loadResult.LoadedObject != null)
                    {
                        DestroyImmediate(loadResult.LoadedObject);
                    }
                }
                
                // Teste 2: Relatório do cache
                var cacheReport = modelLoader.GenerateCacheReport();
                testResults += $"  📋 Cache: {cacheReport}\n";
                
            }
            catch (Exception ex)
            {
                LogTestResult("Loading Tests", false, $"Exceção: {ex.Message}");
            }
            
            testResults += "\n";
        }
        
        private async Task RunWizardTests()
        {
            Debug.Log("🧙 Testando Sistema de Wizard...");
            testResults += "🧙 SISTEMA DE WIZARD:\n";
            
            try
            {
                // Teste 1: Validação de ferramentas
                var validationResult = await WizardValidator.ValidateToolsAsync();
                LogTestResult("ValidateTools", validationResult.IsValid, validationResult.GenerateReport());
                
                // Teste 2: Estado do wizard
                var wizardState = new WizardState();
                wizardState.ModelName = "TestModel";
                wizardState.SourcePath = testModelPath;
                wizardState.AddError("Erro de teste");
                wizardState.AddWarning("Warning de teste");
                
                LogTestResult("WizardState", true, "Estado criado com sucesso");
                
                // Teste 3: Relatório do estado
                var stateReport = wizardState.GenerateReport();
                testResults += $"  📋 Estado: {stateReport.Length} caracteres\n";
                
                // Teste 4: Persistência
                string tempStateFile = Path.Combine(Application.temporaryCachePath, "test_wizard_state.json");
                wizardState.SaveToFile(tempStateFile);
                
                if (File.Exists(tempStateFile))
                {
                    var loadedState = WizardState.LoadFromFile(tempStateFile);
                    LogTestResult("WizardState Persistence", loadedState.ModelName == "TestModel", "Estado salvo e carregado");
                    
                    // Limpa arquivo temporário
                    File.Delete(tempStateFile);
                }
                
            }
            catch (Exception ex)
            {
                LogTestResult("Wizard Tests", false, $"Exceção: {ex.Message}");
            }
            
            testResults += "\n";
        }
        
        private void LogTestResult(string testName, bool success, string details)
        {
            string status = success ? "✅" : "❌";
            Debug.Log($"{status} {testName}: {details}");
            testResults += $"  {status} {testName}: {details}\n";
        }
        
        [ContextMenu("Generate Test Report")]
        public void GenerateTestReport()
        {
            if (string.IsNullOrEmpty(testResults))
            {
                Debug.LogWarning("Execute os testes primeiro!");
                return;
            }
            
            string reportPath = Path.Combine(Application.dataPath, "TestResults.txt");
            File.WriteAllText(reportPath, testResults);
            Debug.Log($"Relatório salvo em: {reportPath}");
        }
    }
}
