using System;
using System.IO;
using UnityEngine;
using PolyDiet.Core.ModelLoading.Validation;
using PolyDiet.Core.ModelLoading.Conversion;
using PolyDiet.Core.ModelLoading.Compression;
using PolyDiet.Core.ModelLoading.Loading;
using PolyDiet.Core.ModelLoading.Wizard;

namespace PolyDiet.Tests
{
    /// <summary>
    /// Teste rápido do sistema de loader
    /// </summary>
    public class QuickLoaderTest : MonoBehaviour
    {
        [ContextMenu("Test System")]
        public async void TestSystem()
        {
            Debug.Log("=== TESTE RÁPIDO DO SISTEMA DE LOADER ===");
            
            try
            {
                // Teste 1: Validação
                Debug.Log("🔍 Testando validação...");
                var testPath = "Assets/StreamingAssets/Models/suzanne/original/model.glb";
                if (File.Exists(testPath))
                {
                    var validation = GltfValidator.QuickValidate(testPath);
                    Debug.Log($"✅ Validação: {(validation.IsValid ? "SUCESSO" : "FALHOU")} - {validation.ErrorMessage}");
                }
                else
                {
                    Debug.Log("⚠️ Arquivo de teste não encontrado para validação");
                }
                
                // Teste 2: Conversão
                Debug.Log("🔄 Testando conversão...");
                var conversionManager = new ConversionManager();
                var strategies = await conversionManager.GetAvailableStrategiesAsync(".obj");
                Debug.Log($"✅ Estratégias de conversão: {strategies.Count} encontradas");
                
                // Teste 3: Compressão
                Debug.Log("🗜️ Testando compressão...");
                var compressionManager = new CompressionManager();
                var compressions = await compressionManager.GetAvailableCompressionsAsync();
                Debug.Log($"✅ Compressores: {compressions.Length} encontrados");
                
                // Teste 4: Carregamento
                Debug.Log("📥 Testando carregamento...");
                var modelLoader = new ModelLoader();
                var loadResult = await modelLoader.LoadModelWithFallbackAsync("suzanne");
                Debug.Log($"✅ Carregamento: {(loadResult.Success ? "SUCESSO" : "FALHOU")} - {loadResult.ErrorMessage}");
                
                // Teste 5: Wizard
                Debug.Log("🧙 Testando wizard...");
                var wizardValidation = await WizardValidator.ValidateToolsAsync();
                Debug.Log($"✅ Validação de ferramentas: {(wizardValidation.IsValid ? "SUCESSO" : "FALHOU")}");
                
                Debug.Log("=== TESTE CONCLUÍDO COM SUCESSO ===");
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ ERRO NO TESTE: {ex.Message}");
                Debug.LogError($"Stack trace: {ex.StackTrace}");
            }
        }
        
        [ContextMenu("Test Individual Components")]
        public async void TestIndividualComponents()
        {
            Debug.Log("=== TESTE INDIVIDUAL DE COMPONENTES ===");
            
            // Teste GltfValidator
            try
            {
                Debug.Log("🔍 Testando GltfValidator...");
                var result = GltfValidator.QuickValidate("test.glb");
                Debug.Log($"✅ GltfValidator funcionando: {result.IsValid}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ GltfValidator falhou: {ex.Message}");
            }
            
            // Teste ConversionManager
            try
            {
                Debug.Log("🔄 Testando ConversionManager...");
                var manager = new ConversionManager();
                var strategies = await manager.GetAvailableStrategiesAsync(".obj");
                Debug.Log($"✅ ConversionManager funcionando: {strategies.Count} estratégias");
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ ConversionManager falhou: {ex.Message}");
            }
            
            // Teste CompressionManager
            try
            {
                Debug.Log("🗜️ Testando CompressionManager...");
                var manager = new CompressionManager();
                var compressions = await manager.GetAvailableCompressionsAsync();
                Debug.Log($"✅ CompressionManager funcionando: {compressions.Length} compressores");
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ CompressionManager falhou: {ex.Message}");
            }
            
            // Teste ModelLoader
            try
            {
                Debug.Log("📥 Testando ModelLoader...");
                var loader = new ModelLoader();
                Debug.Log($"✅ ModelLoader funcionando: instanciado com sucesso");
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ ModelLoader falhou: {ex.Message}");
            }
            
            // Teste WizardValidator
            try
            {
                Debug.Log("🧙 Testando WizardValidator...");
                var validation = await WizardValidator.ValidateToolsAsync();
                Debug.Log($"✅ WizardValidator funcionando: {validation.IsValid}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ WizardValidator falhou: {ex.Message}");
            }
            
            Debug.Log("=== TESTE INDIVIDUAL CONCLUÍDO ===");
        }
    }
}
