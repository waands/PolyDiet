using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace PolyDiet.Core.ModelLoading.Validation
{
    /// <summary>
    /// Validador robusto de arquivos GLTF/GLB
    /// </summary>
    public static class GltfValidator
    {
        private const uint GLB_MAGIC = 0x46546C67; // "glTF" em little-endian
        private const uint GLB_VERSION_2 = 2;
        private const uint JSON_CHUNK_TYPE = 0x4E4F534A; // "JSON" em little-endian
        private const uint BIN_CHUNK_TYPE = 0x004E4942; // "BIN\0" em little-endian
        
        /// <summary>
        /// Validação completa de arquivo GLTF/GLB
        /// </summary>
        public static ValidationResult Validate(string filePath)
        {
            try
            {
                // Validações básicas
                if (!File.Exists(filePath))
                {
                    return ValidationResult.Failure(
                        $"Arquivo não encontrado: {filePath}",
                        ValidationErrorType.FileNotFound
                    );
                }
                
                FileInfo fileInfo = new FileInfo(filePath);
                if (fileInfo.Length == 0)
                {
                    return ValidationResult.Failure(
                        "Arquivo vazio",
                        ValidationErrorType.FileEmpty
                    );
                }
                
                string extension = Path.GetExtension(filePath).ToLower();
                if (extension != ".glb" && extension != ".gltf")
                {
                    return ValidationResult.Failure(
                        $"Extensão inválida: {extension}. Esperado .glb ou .gltf",
                        ValidationErrorType.InvalidExtension
                    );
                }
                
                // Validação específica por tipo
                if (extension == ".glb")
                {
                    return ValidateGlb(filePath, fileInfo);
                }
                else
                {
                    return ValidateGltf(filePath, fileInfo);
                }
            }
            catch (Exception ex)
            {
                return ValidationResult.Failure(
                    $"Erro durante validação: {ex.Message}",
                    ValidationErrorType.Unknown
                );
            }
        }
        
        /// <summary>
        /// Validação rápida (apenas magic number e tamanho)
        /// </summary>
        public static ValidationResult QuickValidate(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    return ValidationResult.Failure(
                        "Arquivo não encontrado",
                        ValidationErrorType.FileNotFound
                    );
                }
                
                FileInfo fileInfo = new FileInfo(filePath);
                if (fileInfo.Length == 0)
                {
                    return ValidationResult.Failure(
                        "Arquivo vazio",
                        ValidationErrorType.FileEmpty
                    );
                }
                
                string extension = Path.GetExtension(filePath).ToLower();
                
                // Para GLB, verifica magic number
                if (extension == ".glb")
                {
                    using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                    using (var reader = new BinaryReader(fs))
                    {
                        if (fs.Length < 12)
                        {
                            return ValidationResult.Failure(
                                "Arquivo GLB muito pequeno (< 12 bytes)",
                                ValidationErrorType.InvalidGlbHeader
                            );
                        }
                        
                        uint magic = reader.ReadUInt32();
                        if (magic != GLB_MAGIC)
                        {
                            return ValidationResult.Failure(
                                $"Magic number inválido: 0x{magic:X8} (esperado: 0x{GLB_MAGIC:X8})",
                                ValidationErrorType.InvalidMagicNumber
                            );
                        }
                    }
                }
                
                return ValidationResult.Success(new GltfFileInfo
                {
                    FilePath = filePath,
                    FileSizeBytes = fileInfo.Length,
                    FileType = extension == ".glb" ? "GLB" : "GLTF"
                });
            }
            catch (Exception ex)
            {
                return ValidationResult.Failure(
                    $"Erro durante validação rápida: {ex.Message}",
                    ValidationErrorType.Unknown
                );
            }
        }
        
        /// <summary>
        /// Verifica se um arquivo pode ser reparado
        /// </summary>
        public static bool CanBeRepaired(string filePath)
        {
            var result = QuickValidate(filePath);
            
            // Arquivos que não existem ou estão vazios não podem ser reparados
            if (result.ErrorType == ValidationErrorType.FileNotFound ||
                result.ErrorType == ValidationErrorType.FileEmpty)
            {
                return false;
            }
            
            // Magic number incorreto pode indicar corrupção irreparável
            if (result.ErrorType == ValidationErrorType.InvalidMagicNumber)
            {
                return false;
            }
            
            // Outros erros podem potencialmente ser reparados
            return true;
        }
        
        /// <summary>
        /// Validação específica para arquivos GLB
        /// </summary>
        private static ValidationResult ValidateGlb(string filePath, FileInfo fileInfo)
        {
            try
            {
                using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                using (var reader = new BinaryReader(fs))
                {
                    // Verifica tamanho mínimo
                    if (fs.Length < 12)
                    {
                        return ValidationResult.Failure(
                            "Arquivo GLB muito pequeno (< 12 bytes)",
                            ValidationErrorType.InvalidGlbHeader
                        );
                    }
                    
                    // Lê header GLB (12 bytes)
                    uint magic = reader.ReadUInt32();
                    uint version = reader.ReadUInt32();
                    uint length = reader.ReadUInt32();
                    
                    // Valida magic number
                    if (magic != GLB_MAGIC)
                    {
                        var result = ValidationResult.Failure(
                            $"Magic number inválido: 0x{magic:X8} (esperado: 0x{GLB_MAGIC:X8})",
                            ValidationErrorType.InvalidMagicNumber
                        );
                        result.RepairSuggestion = "Arquivo pode estar corrompido. Tente reconverter do formato original.";
                        return result;
                    }
                    
                    // Valida versão
                    if (version != GLB_VERSION_2)
                    {
                        var result = ValidationResult.Failure(
                            $"Versão GLB não suportada: {version} (esperado: 2)",
                            ValidationErrorType.InvalidGlbHeader
                        );
                        result.Warnings.Add($"Versão {version} pode não ser totalmente compatível");
                        return result;
                    }
                    
                    // Valida tamanho declarado vs real
                    if (length != fs.Length)
                    {
                        var result = ValidationResult.Failure(
                            $"Tamanho declarado ({length}) não corresponde ao tamanho real ({fs.Length})",
                            ValidationErrorType.InvalidGlbHeader
                        );
                        result.CanBeRepaired = true;
                        result.RepairSuggestion = "Arquivo pode estar truncado ou corrompido no final.";
                        return result;
                    }
                    
                    // Valida JSON chunk (primeiro chunk obrigatório)
                    if (fs.Position + 8 > fs.Length)
                    {
                        return ValidationResult.Failure(
                            "Arquivo GLB não contém chunk JSON",
                            ValidationErrorType.CorruptedChunks
                        );
                    }
                    
                    uint jsonChunkLength = reader.ReadUInt32();
                    uint jsonChunkType = reader.ReadUInt32();
                    
                    if (jsonChunkType != JSON_CHUNK_TYPE)
                    {
                        return ValidationResult.Failure(
                            $"Primeiro chunk não é JSON: 0x{jsonChunkType:X8}",
                            ValidationErrorType.CorruptedChunks
                        );
                    }
                    
                    // Tenta ler e parsear JSON
                    if (fs.Position + jsonChunkLength > fs.Length)
                    {
                        return ValidationResult.Failure(
                            "JSON chunk excede tamanho do arquivo",
                            ValidationErrorType.CorruptedChunks
                        );
                    }
                    
                    byte[] jsonBytes = reader.ReadBytes((int)jsonChunkLength);
                    string jsonString = Encoding.UTF8.GetString(jsonBytes);
                    
                    // Validação básica do JSON
                    var jsonValidation = ValidateGltfJson(jsonString);
                    if (!jsonValidation.IsValid)
                    {
                        jsonValidation.ErrorType = ValidationErrorType.InvalidJsonStructure;
                        return jsonValidation;
                    }
                    
                    // Cria informações do arquivo
                    var gltfFileInfo = new GltfFileInfo
                    {
                        FilePath = filePath,
                        FileSizeBytes = fileInfo.Length,
                        FileType = "GLB",
                        GlbVersion = version,
                        GlbLength = length
                    };
                    
                    // Tenta extrair informações do JSON
                    ExtractInfoFromJson(jsonString, gltfFileInfo);
                    
                    var successResult = ValidationResult.Success(gltfFileInfo);
                    
                    // Adiciona warnings se aplicável
                    if (fs.Position < fs.Length)
                    {
                        successResult.Warnings.Add($"Arquivo contém dados após o JSON chunk ({fs.Length - fs.Position} bytes)");
                    }
                    
                    return successResult;
                }
            }
            catch (Exception ex)
            {
                return ValidationResult.Failure(
                    $"Erro ao validar GLB: {ex.Message}",
                    ValidationErrorType.Unknown
                );
            }
        }
        
        /// <summary>
        /// Validação específica para arquivos GLTF (JSON puro)
        /// </summary>
        private static ValidationResult ValidateGltf(string filePath, FileInfo fileInfo)
        {
            try
            {
                string jsonString = File.ReadAllText(filePath);
                
                // Validação do JSON
                var jsonValidation = ValidateGltfJson(jsonString);
                if (!jsonValidation.IsValid)
                {
                    return jsonValidation;
                }
                
                // Cria informações do arquivo
                var gltfFileInfo = new GltfFileInfo
                {
                    FilePath = filePath,
                    FileSizeBytes = fileInfo.Length,
                    FileType = "GLTF"
                };
                
                // Tenta extrair informações do JSON
                ExtractInfoFromJson(jsonString, gltfFileInfo);
                
                var result = ValidationResult.Success(gltfFileInfo);
                
                // Verifica se há referências a arquivos .bin externos
                if (jsonString.Contains(".bin"))
                {
                    result.Warnings.Add("Arquivo GLTF contém referências a arquivos .bin externos");
                }
                
                return result;
            }
            catch (Exception ex)
            {
                return ValidationResult.Failure(
                    $"Erro ao validar GLTF: {ex.Message}",
                    ValidationErrorType.Unknown
                );
            }
        }
        
        /// <summary>
        /// Validação básica da estrutura JSON do GLTF
        /// </summary>
        private static ValidationResult ValidateGltfJson(string jsonString)
        {
            try
            {
                // Validação básica: deve começar com { e terminar com }
                jsonString = jsonString.Trim();
                if (!jsonString.StartsWith("{") || !jsonString.EndsWith("}"))
                {
                    return ValidationResult.Failure(
                        "JSON inválido: não é um objeto JSON",
                        ValidationErrorType.InvalidJsonStructure
                    );
                }
                
                // Tenta fazer parse básico com JsonUtility do Unity
                // Nota: JsonUtility é limitado, mas é suficiente para validação básica
                if (!jsonString.Contains("\"asset\""))
                {
                    return ValidationResult.Failure(
                        "JSON GLTF inválido: campo 'asset' obrigatório não encontrado",
                        ValidationErrorType.MissingRequiredFields
                    );
                }
                
                return ValidationResult.Success(new GltfFileInfo());
            }
            catch (Exception ex)
            {
                return ValidationResult.Failure(
                    $"JSON inválido: {ex.Message}",
                    ValidationErrorType.InvalidJsonStructure
                );
            }
        }
        
        /// <summary>
        /// Extrai informações do JSON do GLTF
        /// </summary>
        private static void ExtractInfoFromJson(string jsonString, GltfFileInfo fileInfo)
        {
            try
            {
                // Contagem simples de arrays (não é parsing completo, mas é rápido)
                fileInfo.MeshCount = CountJsonArrayItems(jsonString, "\"meshes\"");
                fileInfo.NodeCount = CountJsonArrayItems(jsonString, "\"nodes\"");
                fileInfo.MaterialCount = CountJsonArrayItems(jsonString, "\"materials\"");
                fileInfo.TextureCount = CountJsonArrayItems(jsonString, "\"textures\"");
                fileInfo.AnimationCount = CountJsonArrayItems(jsonString, "\"animations\"");
            }
            catch
            {
                // Se falhar, não é crítico - apenas não teremos essas informações
                Debug.LogWarning("[GltfValidator] Não foi possível extrair informações detalhadas do JSON");
            }
        }
        
        /// <summary>
        /// Conta items em um array JSON de forma simples (sem parser completo)
        /// </summary>
        private static int CountJsonArrayItems(string json, string arrayName)
        {
            int arrayStart = json.IndexOf(arrayName);
            if (arrayStart == -1) return 0;
            
            arrayStart = json.IndexOf('[', arrayStart);
            if (arrayStart == -1) return 0;
            
            int arrayEnd = json.IndexOf(']', arrayStart);
            if (arrayEnd == -1) return 0;
            
            string arrayContent = json.Substring(arrayStart, arrayEnd - arrayStart);
            
            // Conta objetos {} no array
            int count = 0;
            int depth = 0;
            bool inObject = false;
            
            for (int i = 0; i < arrayContent.Length; i++)
            {
                if (arrayContent[i] == '{')
                {
                    if (depth == 0) inObject = true;
                    depth++;
                }
                else if (arrayContent[i] == '}')
                {
                    depth--;
                    if (depth == 0 && inObject)
                    {
                        count++;
                        inObject = false;
                    }
                }
            }
            
            return count;
        }
    }
}

