using System;
using System.IO;
using UnityEngine;

/// <summary>
/// Validador de arquivos GLTF para diagnosticar problemas de carregamento
/// </summary>
public static class GLTFValidator
{
    /// <summary>
    /// Valida um arquivo GLTF/GLB e retorna informações sobre possíveis problemas
    /// </summary>
    public static ValidationResult ValidateFile(string filePath)
    {
        var result = new ValidationResult
        {
            FilePath = filePath,
            IsValid = false,
            ErrorMessage = ""
        };

        try
        {
            // Verifica se o arquivo existe
            if (!File.Exists(filePath))
            {
                result.ErrorMessage = "Arquivo não encontrado";
                return result;
            }

            // Verifica tamanho do arquivo
            var fileInfo = new FileInfo(filePath);
            result.FileSize = fileInfo.Length;
            
            if (result.FileSize == 0)
            {
                result.ErrorMessage = "Arquivo está vazio (0 bytes)";
                return result;
            }

            // Verifica extensão
            string extension = Path.GetExtension(filePath).ToLower();
            if (extension != ".gltf" && extension != ".glb")
            {
                result.ErrorMessage = $"Extensão inválida: {extension}. Esperado: .gltf ou .glb";
                return result;
            }

            // Para arquivos .gltf, verifica se o JSON é válido
            if (extension == ".gltf")
            {
                result = ValidateGLTFJson(filePath, result);
            }
            else if (extension == ".glb")
            {
                result = ValidateGLBBinary(filePath, result);
            }

            if (string.IsNullOrEmpty(result.ErrorMessage))
            {
                result.IsValid = true;
                result.ErrorMessage = "Arquivo válido";
            }
        }
        catch (Exception ex)
        {
            result.ErrorMessage = $"Erro durante validação: {ex.Message}";
        }

        return result;
    }

    private static ValidationResult ValidateGLTFJson(string filePath, ValidationResult result)
    {
        try
        {
            string jsonContent = File.ReadAllText(filePath);
            
            // Verifica se o arquivo não está vazio
            if (string.IsNullOrWhiteSpace(jsonContent))
            {
                result.ErrorMessage = "Arquivo GLTF está vazio";
                return result;
            }

            // Verifica se contém as propriedades básicas do GLTF
            if (!jsonContent.Contains("\"asset\""))
            {
                result.ErrorMessage = "Arquivo GLTF não contém propriedade 'asset' obrigatória";
                return result;
            }

            if (!jsonContent.Contains("\"version\""))
            {
                result.ErrorMessage = "Arquivo GLTF não contém propriedade 'version' obrigatória";
                return result;
            }

            // Tenta fazer parse do JSON (validação básica)
            try
            {
                var jsonObject = JsonUtility.FromJson<object>(jsonContent);
                result.JsonParseSuccess = true;
            }
            catch (Exception jsonEx)
            {
                result.ErrorMessage = $"JSON inválido: {jsonEx.Message}";
                return result;
            }
        }
        catch (Exception ex)
        {
            result.ErrorMessage = $"Erro ao ler arquivo GLTF: {ex.Message}";
        }

        return result;
    }

    private static ValidationResult ValidateGLBBinary(string filePath, ValidationResult result)
    {
        try
        {
            using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            using (var reader = new BinaryReader(fileStream))
            {
                // Verifica o magic number do GLB (0x46546C67 = "glTF")
                uint magic = reader.ReadUInt32();
                if (magic != 0x46546C67)
                {
                    result.ErrorMessage = "Arquivo GLB inválido: magic number incorreto";
                    return result;
                }

                // Verifica a versão
                uint version = reader.ReadUInt32();
                if (version != 2)
                {
                    result.ErrorMessage = $"Versão GLB não suportada: {version}. Esperado: 2";
                    return result;
                }

                // Verifica o tamanho total
                uint totalLength = reader.ReadUInt32();
                if (totalLength != result.FileSize)
                {
                    result.ErrorMessage = $"Tamanho do arquivo não confere com header GLB. Header: {totalLength}, Arquivo: {result.FileSize}";
                    return result;
                }

                result.GLBHeaderValid = true;
            }
        }
        catch (Exception ex)
        {
            result.ErrorMessage = $"Erro ao validar GLB: {ex.Message}";
        }

        return result;
    }

    /// <summary>
    /// Valida todos os arquivos GLTF/GLB em um diretório
    /// </summary>
    public static void ValidateDirectory(string directoryPath)
    {
        Debug.Log($"[GLTFValidator] Validando diretório: {directoryPath}");

        if (!Directory.Exists(directoryPath))
        {
            Debug.LogError($"[GLTFValidator] Diretório não encontrado: {directoryPath}");
            return;
        }

        string[] gltfFiles = Directory.GetFiles(directoryPath, "*.gltf", SearchOption.AllDirectories);
        string[] glbFiles = Directory.GetFiles(directoryPath, "*.glb", SearchOption.AllDirectories);

        Debug.Log($"[GLTFValidator] Encontrados {gltfFiles.Length} arquivos .gltf e {glbFiles.Length} arquivos .glb");

        int validCount = 0;
        int invalidCount = 0;

        foreach (string file in gltfFiles)
        {
            var result = ValidateFile(file);
            LogValidationResult(result);
            if (result.IsValid) validCount++; else invalidCount++;
        }

        foreach (string file in glbFiles)
        {
            var result = ValidateFile(file);
            LogValidationResult(result);
            if (result.IsValid) validCount++; else invalidCount++;
        }

        Debug.Log($"[GLTFValidator] Validação concluída: {validCount} válidos, {invalidCount} inválidos");
    }

    private static void LogValidationResult(ValidationResult result)
    {
        string status = result.IsValid ? "✅ VÁLIDO" : "❌ INVÁLIDO";
        Debug.Log($"[GLTFValidator] {status}: {Path.GetFileName(result.FilePath)} - {result.ErrorMessage}");
        
        if (!result.IsValid)
        {
            Debug.LogError($"[GLTFValidator] Detalhes do erro em {result.FilePath}:");
            Debug.LogError($"[GLTFValidator] - Tamanho: {result.FileSize} bytes");
            Debug.LogError($"[GLTFValidator] - Erro: {result.ErrorMessage}");
        }
    }
}

[System.Serializable]
public class ValidationResult
{
    public string FilePath;
    public bool IsValid;
    public string ErrorMessage;
    public long FileSize;
    public bool JsonParseSuccess;
    public bool GLBHeaderValid;
}

