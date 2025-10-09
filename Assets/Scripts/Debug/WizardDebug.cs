using UnityEngine;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;

/// <summary>
/// Script de debug para identificar problemas no WizardController
/// </summary>
public class WizardDebug : MonoBehaviour
{
    [Header("Debug References")]
    public WizardController wizard;
    public ModelViewer viewer;
    public Metrics metrics;

    [Header("Test Settings")]
    public string testModelName = "Duck";
    public string[] testVariants = { "original", "draco", "meshopt" };

    void Start()
    {
        if (wizard == null) wizard = FindObjectOfType<WizardController>();
        if (viewer == null) viewer = FindObjectOfType<ModelViewer>();
        if (metrics == null) metrics = FindObjectOfType<Metrics>();
    }

    [ContextMenu("Debug Wizard State")]
    public void DebugWizardState()
    {
        Debug.Log("=== WIZARD DEBUG STATE ===");
        
        if (wizard == null)
        {
            Debug.LogError("❌ WizardController não encontrado!");
            return;
        }

        if (viewer == null)
        {
            Debug.LogError("❌ ModelViewer não encontrado!");
            return;
        }

        if (metrics == null)
        {
            Debug.LogError("❌ Metrics não encontrado!");
            return;
        }

        Debug.Log($"✅ WizardController: {wizard.name}");
        Debug.Log($"✅ ModelViewer: {viewer.name}");
        Debug.Log($"✅ Metrics: {metrics.name}");
        
        Debug.Log("=== END WIZARD DEBUG STATE ===");
    }

    [ContextMenu("Debug Model Availability")]
    public void DebugModelAvailability()
    {
        Debug.Log("=== MODEL AVAILABILITY DEBUG ===");
        
        if (viewer == null)
        {
            Debug.LogError("❌ ModelViewer não encontrado!");
            return;
        }

        // Rescan models
        viewer.RescanModels();
        
        // Get available models
        var models = viewer.GetAllAvailableModels();
        Debug.Log($"📁 Modelos disponíveis ({models.Count}):");
        foreach (var model in models)
        {
            Debug.Log($"  - {model}");
        }

        // Test specific model
        if (!string.IsNullOrEmpty(testModelName))
        {
            Debug.Log($"🔍 Testando modelo: {testModelName}");
            
            bool exists = viewer.ModelExists(testModelName);
            Debug.Log($"  Existe: {exists}");
            
            if (exists)
            {
                var variants = viewer.GetAvailableVariantsPublic(testModelName);
                Debug.Log($"  Variantes ({variants.Count}):");
                foreach (var variant in variants)
                {
                    Debug.Log($"    - {variant}");
                    
                    string path = viewer.ResolvePath(testModelName, variant);
                    bool fileExists = File.Exists(path);
                    Debug.Log($"      Caminho: {path}");
                    Debug.Log($"      Arquivo existe: {fileExists}");
                }
            }
        }
        
        Debug.Log("=== END MODEL AVAILABILITY DEBUG ===");
    }

    [ContextMenu("Test Load Model")]
    public async void TestLoadModel()
    {
        Debug.Log("=== TEST LOAD MODEL ===");
        
        if (viewer == null)
        {
            Debug.LogError("❌ ModelViewer não encontrado!");
            return;
        }

        if (string.IsNullOrEmpty(testModelName))
        {
            Debug.LogError("❌ testModelName está vazio!");
            return;
        }

        // Test each variant
        foreach (var variant in testVariants)
        {
            Debug.Log($"🔄 Testando carregamento: {testModelName} ({variant})");
            
            string path = viewer.ResolvePath(testModelName, variant);
            Debug.Log($"  Caminho: {path}");
            
            if (!File.Exists(path))
            {
                Debug.LogWarning($"⚠️ Arquivo não encontrado: {path}");
                continue;
            }

            try
            {
                bool success = await viewer.LoadOnlyAsync(testModelName, variant);
                Debug.Log($"  Resultado: {(success ? "✅ Sucesso" : "❌ Falha")}");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"  ❌ Erro: {ex.Message}");
            }
        }
        
        Debug.Log("=== END TEST LOAD MODEL ===");
    }

    [ContextMenu("Debug Metrics")]
    public void DebugMetrics()
    {
        Debug.Log("=== METRICS DEBUG ===");
        
        if (metrics == null)
        {
            Debug.LogError("❌ Metrics não encontrado!");
            return;
        }

        Debug.Log($"✅ Metrics Instance: {metrics.name}");
        Debug.Log($"✅ Metrics Singleton: {Metrics.Instance != null}");
        
        if (Metrics.Instance != null)
        {
            Debug.Log($"  CSV Path: {Metrics.Instance.GetCsvPathPublic()}");
            Debug.Log($"  CSV Exists: {File.Exists(Metrics.Instance.GetCsvPathPublic())}");
        }
        
        Debug.Log("=== END METRICS DEBUG ===");
    }

    [ContextMenu("Simulate Test Run")]
    public async void SimulateTestRun()
    {
        Debug.Log("=== SIMULATE TEST RUN ===");
        
        if (viewer == null || metrics == null)
        {
            Debug.LogError("❌ Componentes necessários não encontrados!");
            return;
        }

        if (string.IsNullOrEmpty(testModelName))
        {
            Debug.LogError("❌ testModelName está vazio!");
            return;
        }

        // Simulate the test run process
        string[] runOrder = { "original", "meshopt", "draco" };
        
        foreach (var variant in runOrder)
        {
            Debug.Log($"🔄 Simulando teste: {testModelName} ({variant})");
            
            var variants = viewer.GetAvailableVariantsPublic(testModelName);
            if (!variants.Contains(variant))
            {
                Debug.Log($"  ⚠️ Variante {variant} não disponível, pulando...");
                continue;
            }

            string path = viewer.ResolvePath(testModelName, variant);
            Debug.Log($"  Caminho: {path}");

            if (!File.Exists(path))
            {
                Debug.LogError($"  ❌ Arquivo não encontrado: {path}");
                continue;
            }

            // Begin metrics
            Debug.Log("  📊 Iniciando métricas...");
            Metrics.Instance?.BeginLoad(testModelName, variant, path);

            // Load model
            Debug.Log("  🔄 Carregando modelo...");
            bool ok = await viewer.LoadOnlyAsync(testModelName, variant);
            Debug.Log($"  Resultado do carregamento: {(ok ? "✅ Sucesso" : "❌ Falha")}");

            // End load metrics
            if (Metrics.Instance != null)
            {
                Debug.Log("  📊 Finalizando métricas de carregamento...");
                await Metrics.Instance.EndLoad(ok);
            }

            if (!ok)
            {
                Debug.LogError($"  ❌ Falha ao carregar {testModelName} ({variant}), pulando...");
                continue;
            }

            // Measure FPS
            if (Metrics.Instance != null)
            {
                Debug.Log("  📊 Medindo FPS...");
                float secs = Metrics.Instance.fpsWindowSeconds;
                await Metrics.Instance.MeasureFpsWindowWithCallback(secs, (remaining) =>
                {
                    Debug.Log($"    Medindo {remaining:0.0}s...");
                });
                
                Debug.Log("  💾 Salvando CSV...");
                Metrics.Instance.WriteCsv();
            }

            // Clear between runs
            Debug.Log("  🧹 Limpando cache...");
            await ClearBetweenRunsAsync();
        }

        Debug.Log("✅ Simulação de teste concluída!");
        Debug.Log("=== END SIMULATE TEST RUN ===");
    }

    static async Task ClearBetweenRunsAsync()
    {
        var op = Resources.UnloadUnusedAssets();
        while (!op.isDone) await Task.Yield();
        System.GC.Collect();
        await Task.Delay(100);
    }

    [ContextMenu("Check All Files")]
    public void CheckAllFiles()
    {
        Debug.Log("=== CHECK ALL FILES ===");
        
        string modelsDir = Path.Combine(Application.streamingAssetsPath, "Models");
        Debug.Log($"📁 Models Directory: {modelsDir}");
        Debug.Log($"📁 Exists: {Directory.Exists(modelsDir)}");
        
        if (Directory.Exists(modelsDir))
        {
            var modelDirs = Directory.GetDirectories(modelsDir);
            Debug.Log($"📁 Model Directories ({modelDirs.Length}):");
            
            foreach (var modelDir in modelDirs)
            {
                string modelName = Path.GetFileName(modelDir);
                Debug.Log($"  📁 {modelName}:");
                
                var variantDirs = Directory.GetDirectories(modelDir);
                foreach (var variantDir in variantDirs)
                {
                    string variantName = Path.GetFileName(variantDir);
                    Debug.Log($"    📁 {variantName}:");
                    
                    var files = Directory.GetFiles(variantDir);
                    foreach (var file in files)
                    {
                        string fileName = Path.GetFileName(file);
                        long fileSize = new FileInfo(file).Length;
                        Debug.Log($"      📄 {fileName} ({fileSize} bytes)");
                    }
                }
            }
        }
        
        Debug.Log("=== END CHECK ALL FILES ===");
    }
}
