using System;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Conversor automático de modelos 3D para GLB/GLTF
/// Suporta múltiplos métodos de conversão
/// </summary>
public static class ModelConverter
{
    /// <summary>
    /// Converte um arquivo de modelo para GLB
    /// </summary>
    /// <param name="sourcePath">Caminho do arquivo de origem</param>
    /// <param name="destPath">Caminho de destino (GLB)</param>
    /// <returns>True se a conversão foi bem-sucedida</returns>
    public static async Task<bool> ConvertToGlbAsync(string sourcePath, string destPath)
    {
        if (!File.Exists(sourcePath))
        {
            Debug.LogError($"[ModelConverter] Arquivo não encontrado: {sourcePath}");
            return false;
        }

        string extension = Path.GetExtension(sourcePath).ToLower();
        Debug.Log($"[ModelConverter] Convertendo {extension.ToUpper()} para GLB...");
        Debug.Log($"[ModelConverter] Origem: {sourcePath}");
        Debug.Log($"[ModelConverter] Destino: {destPath}");

        // Cria diretório de destino se não existir
        string destDir = Path.GetDirectoryName(destPath);
        if (!Directory.Exists(destDir))
        {
            Directory.CreateDirectory(destDir);
            Debug.Log($"[ModelConverter] Diretório criado: {destDir}");
        }

        try
        {
            // Tenta diferentes métodos de conversão
            Debug.Log($"[ModelConverter] Tentando método 1: Assimp.NET...");
            if (await TryConvertWithAssimp(sourcePath, destPath))
            {
                Debug.Log($"[ModelConverter] ✅ Conversão bem-sucedida com Assimp.NET");
                return true;
            }

            Debug.Log($"[ModelConverter] Tentando método 2: FBX Exporter...");
            if (await TryConvertWithFbxExporter(sourcePath, destPath))
            {
                Debug.Log($"[ModelConverter] ✅ Conversão bem-sucedida com FBX Exporter");
                return true;
            }

            Debug.Log($"[ModelConverter] Tentando método 3: Ferramenta externa...");
            if (await TryConvertWithExternalTool(sourcePath, destPath))
            {
                Debug.Log($"[ModelConverter] ✅ Conversão bem-sucedida com ferramenta externa");
                return true;
            }

            // Se nenhum método funcionou, tenta conversão simples (cópia direta para GLB/GLTF)
            Debug.Log($"[ModelConverter] Tentando método 4: Conversão simples...");
            if (await TrySimpleConversion(sourcePath, destPath))
            {
                Debug.Log($"[ModelConverter] ✅ Conversão bem-sucedida com método simples");
                return true;
            }

            Debug.LogError($"[ModelConverter] ❌ Falha na conversão: nenhum método funcionou");
            Debug.LogError($"[ModelConverter] Arquivo de origem existe: {File.Exists(sourcePath)}");
            Debug.LogError($"[ModelConverter] Diretório de destino existe: {Directory.Exists(destDir)}");
            return false;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[ModelConverter] ❌ Erro na conversão: {ex.Message}");
            Debug.LogError($"[ModelConverter] Stack trace: {ex.StackTrace}");
            return false;
        }
    }

    /// <summary>
    /// Método 1: Conversão usando Assimp.NET
    /// Requer: Package Assimp.NET
    /// </summary>
    static async Task<bool> TryConvertWithAssimp(string sourcePath, string destPath)
    {
        try
        {
            // TODO: Implementar usando Assimp.NET
            // Exemplo de implementação:
            /*
            using (var importer = new Assimp.AssimpContext())
            {
                var scene = importer.ImportFile(sourcePath, Assimp.PostProcessSteps.Triangulate | Assimp.PostProcessSteps.FlipUVs);
                
                // Converter para GLTF
                using (var exporter = new Assimp.AssimpContext())
                {
                    exporter.ExportFile(scene, destPath, "gltf2");
                }
            }
            */
            
            Debug.Log("[ModelConverter] ⚠️ Assimp.NET não implementado ainda");
            await Task.Yield();
            return false;
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[ModelConverter] ⚠️ Assimp.NET falhou: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Método 2: Conversão usando FBX Exporter + glTFast
    /// Requer: Packages FBX Exporter e glTFast
    /// </summary>
    static async Task<bool> TryConvertWithFbxExporter(string sourcePath, string destPath)
    {
        try
        {
            // TODO: Implementar usando FBX Exporter + glTFast
            // Exemplo de implementação:
            /*
            // 1. Importar usando FBX Exporter
            var fbxImporter = AssetImporter.GetAtPath(sourcePath) as FbxImporter;
            if (fbxImporter != null)
            {
                // 2. Exportar para GLTF usando glTFast
                var gltfExporter = new GltfExporter();
                await gltfExporter.ExportAsync(destPath);
            }
            */
            
            Debug.Log("[ModelConverter] ⚠️ FBX Exporter não implementado ainda");
            await Task.Yield();
            return false;
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[ModelConverter] ⚠️ FBX Exporter falhou: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Método 3: Conversão usando ferramenta externa
    /// Requer: gltf-pipeline.exe em StreamingAssets/Tools/
    /// </summary>
    static async Task<bool> TryConvertWithExternalTool(string sourcePath, string destPath)
    {
        try
        {
            string toolsDir = Path.Combine(Application.streamingAssetsPath, "Tools");
            string gltfPipelinePath = Path.Combine(toolsDir, "gltf-pipeline.exe");
            
            Debug.Log($"[ModelConverter] Procurando ferramenta em: {gltfPipelinePath}");
            
            if (!File.Exists(gltfPipelinePath))
            {
                Debug.LogWarning($"[ModelConverter] ⚠️ gltf-pipeline.exe não encontrado em: {toolsDir}");
                Debug.LogWarning($"[ModelConverter] ⚠️ Crie o diretório Tools e coloque gltf-pipeline.exe lá");
                return false;
            }

            Debug.Log($"[ModelConverter] Executando: {gltfPipelinePath} -i \"{sourcePath}\" -o \"{destPath}\"");

            // Comando: gltf-pipeline -i input.obj -o output.glb
            var processInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = gltfPipelinePath,
                Arguments = $"-i \"{sourcePath}\" -o \"{destPath}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                WorkingDirectory = toolsDir
            };

            using (var process = System.Diagnostics.Process.Start(processInfo))
            {
                if (process != null)
                {
                    // WaitForExitAsync não está disponível em versões antigas do .NET
                    // Usamos Task.Run para tornar assíncrono
                    await Task.Run(() => process.WaitForExit());
                    bool success = process.ExitCode == 0 && File.Exists(destPath);
                    
                    if (success)
                    {
                        Debug.Log($"[ModelConverter] ✅ Conversão externa bem-sucedida: {Path.GetFileName(sourcePath)} → {Path.GetFileName(destPath)}");
                    }
                    else
                    {
                        string error = process.StandardError.ReadToEnd();
                        string output = process.StandardOutput.ReadToEnd();
                        Debug.LogError($"[ModelConverter] ❌ Erro na conversão externa (ExitCode: {process.ExitCode})");
                        Debug.LogError($"[ModelConverter] ❌ Erro: {error}");
                        Debug.LogError($"[ModelConverter] ❌ Output: {output}");
                    }
                    
                    return success;
                }
            }
            
            return false;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[ModelConverter] ❌ Erro na conversão externa: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Método 4: Conversão simples (cópia direta para GLB/GLTF)
    /// Para arquivos que já são GLB/GLTF ou podem ser copiados diretamente
    /// </summary>
    static async Task<bool> TrySimpleConversion(string sourcePath, string destPath)
    {
        try
        {
            string extension = Path.GetExtension(sourcePath).ToLower();
            
            // Se já é GLB/GLTF, apenas copia
            if (extension == ".glb" || extension == ".gltf")
            {
                Debug.Log($"[ModelConverter] Arquivo já é {extension.ToUpper()}, copiando diretamente...");
                File.Copy(sourcePath, destPath, overwrite: true);
                await Task.Yield();
                return true;
            }

            // Para OBJ/FBX, tenta uma conversão básica usando Unity
            if (extension == ".obj" || extension == ".fbx")
            {
                Debug.Log($"[ModelConverter] Tentando conversão básica para {extension.ToUpper()}...");
                
                // TODO: Implementar conversão básica usando Unity Asset Pipeline
                // Por enquanto, apenas retorna false
                Debug.Log($"[ModelConverter] ⚠️ Conversão básica não implementada ainda");
                await Task.Yield();
                return false;
            }

            Debug.Log($"[ModelConverter] ⚠️ Formato {extension.ToUpper()} não suportado para conversão simples");
            await Task.Yield();
            return false;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[ModelConverter] ❌ Erro na conversão simples: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Verifica se um formato é suportado para conversão
    /// </summary>
    public static bool IsSupportedFormat(string filePath)
    {
        string extension = Path.GetExtension(filePath).ToLower();
        return extension == ".obj" || extension == ".fbx" || extension == ".dae" || extension == ".3ds";
    }

    /// <summary>
    /// Obtém informações sobre o arquivo de modelo
    /// </summary>
    public static ModelInfo GetModelInfo(string filePath)
    {
        if (!File.Exists(filePath))
            return new ModelInfo { IsValid = false };

        var fileInfo = new FileInfo(filePath);
        return new ModelInfo
        {
            IsValid = true,
            FileName = Path.GetFileName(filePath),
            Extension = Path.GetExtension(filePath).ToLower(),
            SizeBytes = fileInfo.Length,
            SizeMB = fileInfo.Length / (1024.0 * 1024.0),
            LastModified = fileInfo.LastWriteTime,
            CanConvert = IsSupportedFormat(filePath)
        };
    }
}

/// <summary>
/// Informações sobre um arquivo de modelo
/// </summary>
public struct ModelInfo
{
    public bool IsValid;
    public string FileName;
    public string Extension;
    public long SizeBytes;
    public double SizeMB;
    public DateTime LastModified;
    public bool CanConvert;
}
