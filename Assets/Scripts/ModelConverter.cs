using System;
using System.IO;
using System.Linq;
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
    /// Usa obj2gltf e gltf-transform para converter OBJ para GLB
    /// </summary>
    static async Task<bool> TryConvertWithExternalTool(string sourcePath, string destPath)
    {
        try
        {
            string extension = Path.GetExtension(sourcePath).ToLower();
            
            // Para arquivos .obj, usa obj2gltf
            if (extension == ".obj")
            {
                Debug.Log($"[ModelConverter] Iniciando conversão OBJ → GLB usando obj2gltf");
                return await ConvertObjToGlbWithObj2Gltf(sourcePath, destPath);
            }
            
            // Para arquivos .gltf, converte para .glb usando gltf-transform
            if (extension == ".gltf")
            {
                Debug.Log($"[ModelConverter] Convertendo GLTF → GLB usando gltf-transform");
                return await ConvertGltfToGlbWithGltfTransform(sourcePath, destPath);
            }
            
            Debug.LogWarning($"[ModelConverter] ⚠️ Formato {extension} não suportado pela ferramenta externa");
            return false;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[ModelConverter] ❌ Erro na conversão externa: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Converte OBJ diretamente para GLB usando obj2gltf
    /// </summary>
    static async Task<bool> ConvertObjToGlbWithObj2Gltf(string objPath, string glbPath)
    {
        try
        {
            // Verifica se obj2gltf está disponível
            string obj2gltf = DetectObj2GltfPath();
            if (string.IsNullOrEmpty(obj2gltf))
            {
                Debug.LogError("[ModelConverter] obj2gltf não encontrado. Instale com: npm install -g obj2gltf");
                return false;
            }
            
            Debug.Log($"[ModelConverter] Usando obj2gltf: {obj2gltf}");
            Debug.Log($"[ModelConverter] Convertendo: {objPath} → {glbPath}");

            // obj2gltf pode converter diretamente para GLB usando --binary
            var processInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = obj2gltf,
                Arguments = $"--binary -i \"{objPath}\" -o \"{glbPath}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                WorkingDirectory = Path.GetDirectoryName(objPath)
            };

            using (var process = System.Diagnostics.Process.Start(processInfo))
            {
                if (process != null)
                {
                    await Task.Run(() => process.WaitForExit(60000)); // timeout de 60s
                    
                    string stdout = process.StandardOutput.ReadToEnd();
                    string stderr = process.StandardError.ReadToEnd();
                    
                    bool success = process.ExitCode == 0 && File.Exists(glbPath);
                    
                    if (success)
                    {
                        var fileInfo = new FileInfo(glbPath);
                        Debug.Log($"[ModelConverter] ✅ obj2gltf conversão bem-sucedida!");
                        Debug.Log($"[ModelConverter] Arquivo GLB criado: {glbPath} ({fileInfo.Length / 1024.0:F1} KB)");
                    }
                    else
                    {
                        Debug.LogError($"[ModelConverter] ❌ obj2gltf falhou (ExitCode: {process.ExitCode})");
                        if (!string.IsNullOrEmpty(stderr))
                            Debug.LogError($"[ModelConverter] stderr: {stderr}");
                        if (!string.IsNullOrEmpty(stdout))
                            Debug.LogError($"[ModelConverter] stdout: {stdout}");
                    }
                    
                    return success;
                }
            }
            
            return false;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[ModelConverter] ❌ obj2gltf erro: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Converte GLTF para GLB usando gltf-transform
    /// </summary>
    static async Task<bool> ConvertGltfToGlbWithGltfTransform(string gltfPath, string glbPath)
    {
        try
        {
            string gltfTransform = DetectGltfTransformPath();
            if (string.IsNullOrEmpty(gltfTransform))
            {
                Debug.LogError("[ModelConverter] gltf-transform não encontrado");
                return false;
            }
            
            Debug.Log($"[ModelConverter] Usando gltf-transform: {gltfTransform}");
            Debug.Log($"[ModelConverter] Convertendo: {gltfPath} → {glbPath}");

            var processInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = gltfTransform,
                Arguments = $"copy \"{gltfPath}\" \"{glbPath}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                WorkingDirectory = Path.GetDirectoryName(gltfPath)
            };

            using (var process = System.Diagnostics.Process.Start(processInfo))
            {
                if (process != null)
                {
                    await Task.Run(() => process.WaitForExit());
                    
                    string stdout = process.StandardOutput.ReadToEnd();
                    string stderr = process.StandardError.ReadToEnd();
                    
                    bool success = process.ExitCode == 0 && File.Exists(glbPath);
                    
                    if (success)
                    {
                        Debug.Log($"[ModelConverter] ✅ gltf-transform conversão GLTF→GLB bem-sucedida");
                    }
                    else
                    {
                        Debug.LogWarning($"[ModelConverter] ⚠️ gltf-transform falhou (ExitCode: {process.ExitCode})");
                        if (!string.IsNullOrEmpty(stderr))
                            Debug.LogWarning($"[ModelConverter] stderr: {stderr}");
                        if (!string.IsNullOrEmpty(stdout))
                            Debug.LogWarning($"[ModelConverter] stdout: {stdout}");
                    }
                    
                    return success;
                }
            }
            
            return false;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[ModelConverter] ❌ gltf-transform erro: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Detecta o caminho do obj2gltf
    /// </summary>
    static string DetectObj2GltfPath()
    {
        // Usa a mesma lógica do gltf-transform para encontrar obj2gltf
        var nvmPath = Environment.GetEnvironmentVariable("HOME");
        if (!string.IsNullOrEmpty(nvmPath))
        {
            // Tenta o caminho específico da versão atual do Node.js
            var nvmObj2Gltf = Path.Combine(nvmPath, ".nvm/versions/node/v22.19.0/bin/obj2gltf");
            if (File.Exists(nvmObj2Gltf)) return nvmObj2Gltf;
            
            // Procura em todas as versões do Node.js
            var nodeVersionsDir = Path.Combine(nvmPath, ".nvm/versions/node");
            if (Directory.Exists(nodeVersionsDir))
            {
                var versions = Directory.GetDirectories(nodeVersionsDir)
                    .OrderByDescending(d => d)
                    .ToArray();
                
                foreach (var versionDir in versions)
                {
                    var obj2GltfPath = Path.Combine(versionDir, "bin/obj2gltf");
                    if (File.Exists(obj2GltfPath)) return obj2GltfPath;
                }
            }
        }
        
        // Tenta caminhos comuns do sistema
        string[] possiblePaths = {
            "/usr/bin/obj2gltf",
            "/usr/local/bin/obj2gltf"
        };
        
        foreach (string path in possiblePaths)
        {
            if (File.Exists(path))
            {
                return path;
            }
        }
        
        // Fallback para PATH
        if (CrossPlatformHelper.ExecutableExists("obj2gltf"))
        {
            return "obj2gltf";
        }
        
        return null;
    }

    /// <summary>
    /// Detecta o caminho do gltf-transform
    /// </summary>
    static string DetectGltfTransformPath()
    {
        // Usa a mesma lógica do ModelViewer
        var nvmPath = Environment.GetEnvironmentVariable("HOME");
        if (!string.IsNullOrEmpty(nvmPath))
        {
            var nvmGltfTransform = Path.Combine(nvmPath, ".nvm/versions/node/v22.19.0/bin/gltf-transform");
            if (File.Exists(nvmGltfTransform)) return nvmGltfTransform;
            
            var nodeVersionsDir = Path.Combine(nvmPath, ".nvm/versions/node");
            if (Directory.Exists(nodeVersionsDir))
            {
                var versions = Directory.GetDirectories(nodeVersionsDir)
                    .OrderByDescending(d => d)
                    .ToArray();
                
                foreach (var versionDir in versions)
                {
                    var gltfTransformPath = Path.Combine(versionDir, "bin/gltf-transform");
                    if (File.Exists(gltfTransformPath)) return gltfTransformPath;
                }
            }
        }
        
        // Fallback para PATH
        if (CrossPlatformHelper.ExecutableExists("gltf-transform"))
        {
            return "gltf-transform";
        }
        
        return null;
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
