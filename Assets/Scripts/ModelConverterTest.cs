using UnityEngine;
using System.IO;
using System.Threading.Tasks;

/// <summary>
/// Script de teste para diagnosticar problemas de conversão
/// </summary>
public class ModelConverterTest : MonoBehaviour
{
    [Header("Teste de Conversão")]
    public string testFilePath = "";
    public string testOutputPath = "";
    
    [Header("Debug Info")]
    public bool showDebugInfo = true;

    void Start()
    {
        if (showDebugInfo)
        {
            ShowDebugInfo();
        }
    }

    void ShowDebugInfo()
    {
        Debug.Log("=== MODEL CONVERTER DEBUG INFO ===");
        
        // Verifica StreamingAssets
        string streamingAssetsPath = Application.streamingAssetsPath;
        Debug.Log($"StreamingAssets Path: {streamingAssetsPath}");
        Debug.Log($"StreamingAssets Exists: {Directory.Exists(streamingAssetsPath)}");
        
        // Verifica Tools directory
        string toolsDir = Path.Combine(streamingAssetsPath, "Tools");
        Debug.Log($"Tools Directory: {toolsDir}");
        Debug.Log($"Tools Directory Exists: {Directory.Exists(toolsDir)}");
        
        // Verifica gltf-pipeline.exe
        string gltfPipelinePath = Path.Combine(toolsDir, "gltf-pipeline.exe");
        Debug.Log($"gltf-pipeline.exe Path: {gltfPipelinePath}");
        Debug.Log($"gltf-pipeline.exe Exists: {File.Exists(gltfPipelinePath)}");
        
        // Verifica Models directory
        string modelsDir = Path.Combine(streamingAssetsPath, "Models");
        Debug.Log($"Models Directory: {modelsDir}");
        Debug.Log($"Models Directory Exists: {Directory.Exists(modelsDir)}");
        
        Debug.Log("=== END DEBUG INFO ===");
    }

    [ContextMenu("Test Conversion")]
    public async void TestConversion()
    {
        if (string.IsNullOrEmpty(testFilePath))
        {
            Debug.LogError("[ModelConverterTest] testFilePath está vazio!");
            return;
        }

        if (!File.Exists(testFilePath))
        {
            Debug.LogError($"[ModelConverterTest] Arquivo não encontrado: {testFilePath}");
            return;
        }

        if (string.IsNullOrEmpty(testOutputPath))
        {
            testOutputPath = Path.Combine(Application.streamingAssetsPath, "Models", "Test", "original", "model.glb");
        }

        Debug.Log($"[ModelConverterTest] Testando conversão...");
        Debug.Log($"[ModelConverterTest] Origem: {testFilePath}");
        Debug.Log($"[ModelConverterTest] Destino: {testOutputPath}");

        bool success = await ModelConverter.ConvertToGlbAsync(testFilePath, testOutputPath);
        
        if (success)
        {
            Debug.Log($"[ModelConverterTest] ✅ Conversão bem-sucedida!");
        }
        else
        {
            Debug.LogError($"[ModelConverterTest] ❌ Conversão falhou!");
        }
    }

    [ContextMenu("Create Test OBJ")]
    public void CreateTestObj()
    {
        string testObjPath = Path.Combine(Application.streamingAssetsPath, "test.obj");
        
        // Cria um arquivo OBJ simples para teste
        string objContent = @"# Test OBJ file
v 0.0 0.0 0.0
v 1.0 0.0 0.0
v 0.0 1.0 0.0
f 1 2 3
";

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(testObjPath));
            File.WriteAllText(testObjPath, objContent);
            Debug.Log($"[ModelConverterTest] ✅ Arquivo OBJ de teste criado: {testObjPath}");
            
            // Atualiza o campo de teste
            testFilePath = testObjPath;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[ModelConverterTest] ❌ Erro ao criar arquivo OBJ: {ex.Message}");
        }
    }

    [ContextMenu("Check File Info")]
    public void CheckFileInfo()
    {
        if (string.IsNullOrEmpty(testFilePath))
        {
            Debug.LogError("[ModelConverterTest] testFilePath está vazio!");
            return;
        }

        var info = ModelConverter.GetModelInfo(testFilePath);
        
        Debug.Log("=== FILE INFO ===");
        Debug.Log($"Is Valid: {info.IsValid}");
        Debug.Log($"File Name: {info.FileName}");
        Debug.Log($"Extension: {info.Extension}");
        Debug.Log($"Size Bytes: {info.SizeBytes}");
        Debug.Log($"Size MB: {info.SizeMB:F2}");
        Debug.Log($"Last Modified: {info.LastModified}");
        Debug.Log($"Can Convert: {info.CanConvert}");
        Debug.Log("=== END FILE INFO ===");
    }

    [ContextMenu("Setup Tools Directory")]
    public void SetupToolsDirectory()
    {
        string toolsDir = Path.Combine(Application.streamingAssetsPath, "Tools");
        
        try
        {
            Directory.CreateDirectory(toolsDir);
            Debug.Log($"[ModelConverterTest] ✅ Diretório Tools criado: {toolsDir}");
            
            // Cria um arquivo README com instruções
            string readmePath = Path.Combine(toolsDir, "README.txt");
            string readmeContent = @"INSTRUÇÕES PARA CONVERSÃO AUTOMÁTICA:

1. Baixe o gltf-pipeline:
   npm install -g gltf-pipeline

2. Crie um arquivo .bat que chama o Node.js:
   @echo off
   node ""C:\Users\%USERNAME%\AppData\Roaming\npm\node_modules\gltf-pipeline\bin\gltf-pipeline.js"" %*

3. Renomeie o arquivo .bat para gltf-pipeline.exe

4. Coloque o gltf-pipeline.exe neste diretório

5. Teste manualmente:
   gltf-pipeline.exe -i input.obj -o output.glb
";

            File.WriteAllText(readmePath, readmeContent);
            Debug.Log($"[ModelConverterTest] ✅ README criado: {readmePath}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[ModelConverterTest] ❌ Erro ao criar diretório Tools: {ex.Message}");
        }
    }
}
