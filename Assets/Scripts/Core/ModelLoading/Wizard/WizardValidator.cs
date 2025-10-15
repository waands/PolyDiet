using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace PolyDiet.Core.ModelLoading.Wizard
{
    /// <summary>
    /// Progresso do wizard
    /// </summary>
    [Serializable]
    public class WizardProgress
    {
        public WizardStep CurrentStep { get; set; }
        public float StepProgress { get; set; } // 0.0 a 1.0
        public string CurrentOperation { get; set; }
        public string Details { get; set; }
        public bool IsIndeterminate { get; set; }
        
        public override string ToString()
        {
            if (IsIndeterminate)
            {
                return $"{CurrentOperation}...";
            }
            else
            {
                return $"{CurrentOperation}: {(StepProgress * 100):F0}%";
            }
        }
    }
    
    /// <summary>
    /// Validação prévia para operações do wizard
    /// </summary>
    public static class WizardValidator
    {
        /// <summary>
        /// Valida se todas as ferramentas necessárias estão instaladas
        /// </summary>
        public static async Task<ValidationResult> ValidateToolsAsync()
        {
            var result = new ValidationResult();
            
            try
            {
                // Verifica ferramentas de conversão
                var conversionManager = new PolyDiet.Core.ModelLoading.Conversion.ConversionManager();
                var availableStrategies = await conversionManager.GetAvailableStrategiesAsync(".obj");
                
                if (availableStrategies.Count == 0)
                {
                    result.AddError("Nenhuma ferramenta de conversão disponível");
                }
                else
                {
                    result.AddInfo($"Ferramentas de conversão: {string.Join(", ", availableStrategies)}");
                }
                
                // Verifica ferramentas de compressão
                var compressionManager = new PolyDiet.Core.ModelLoading.Compression.CompressionManager();
                var availableCompressions = await compressionManager.GetAvailableCompressionsAsync();
                
                if (availableCompressions.Length == 0)
                {
                    result.AddWarning("Nenhuma ferramenta de compressão disponível");
                }
                else
                {
                    result.AddInfo($"Ferramentas de compressão: {string.Join(", ", availableCompressions)}");
                }
                
                // Verifica espaço em disco
                var diskSpace = GetAvailableDiskSpace();
                if (diskSpace < 100 * 1024 * 1024) // 100 MB
                {
                    result.AddWarning($"Pouco espaço em disco disponível: {diskSpace / (1024 * 1024)} MB");
                }
                else
                {
                    result.AddInfo($"Espaço em disco: {diskSpace / (1024 * 1024)} MB");
                }
                
                result.IsValid = result.Errors.Count == 0;
            }
            catch (Exception ex)
            {
                result.AddError($"Erro na validação: {ex.Message}");
                result.IsValid = false;
            }
            
            return result;
        }
        
        /// <summary>
        /// Valida arquivo de entrada
        /// </summary>
        public static PolyDiet.Core.ModelLoading.Validation.ValidationResult ValidateInputFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                return PolyDiet.Core.ModelLoading.Validation.ValidationResult.Failure(
                    "Caminho do arquivo não especificado",
                    PolyDiet.Core.ModelLoading.Validation.ValidationErrorType.FileNotFound
                );
            }
            
            if (!System.IO.File.Exists(filePath))
            {
                return PolyDiet.Core.ModelLoading.Validation.ValidationResult.Failure(
                    $"Arquivo não encontrado: {filePath}",
                    PolyDiet.Core.ModelLoading.Validation.ValidationErrorType.FileNotFound
                );
            }
            
            // Validação básica do arquivo
            var fileInfo = new System.IO.FileInfo(filePath);
            if (fileInfo.Length == 0)
            {
                return PolyDiet.Core.ModelLoading.Validation.ValidationResult.Failure(
                    "Arquivo vazio",
                    PolyDiet.Core.ModelLoading.Validation.ValidationErrorType.FileEmpty
                );
            }
            
            // Validação específica por extensão
            string extension = System.IO.Path.GetExtension(filePath).ToLower();
            if (extension == ".glb" || extension == ".gltf")
            {
                return PolyDiet.Core.ModelLoading.Validation.GltfValidator.QuickValidate(filePath);
            }
            else if (extension == ".obj")
            {
                // Validação básica de OBJ
                return ValidateObjFile(filePath);
            }
            else
            {
                return PolyDiet.Core.ModelLoading.Validation.ValidationResult.Failure(
                    $"Formato não suportado: {extension}",
                    PolyDiet.Core.ModelLoading.Validation.ValidationErrorType.InvalidExtension
                );
            }
        }
        
        /// <summary>
        /// Validação básica de arquivo OBJ
        /// </summary>
        private static PolyDiet.Core.ModelLoading.Validation.ValidationResult ValidateObjFile(string filePath)
        {
            try
            {
                var lines = System.IO.File.ReadAllLines(filePath);
                int vertexCount = 0;
                int faceCount = 0;
                
                foreach (var line in lines)
                {
                    var trimmedLine = line.Trim();
                    if (trimmedLine.StartsWith("v ")) vertexCount++;
                    else if (trimmedLine.StartsWith("f ")) faceCount++;
                }
                
                if (vertexCount == 0)
                {
                    return PolyDiet.Core.ModelLoading.Validation.ValidationResult.Failure(
                        "Nenhum vértice encontrado no arquivo OBJ",
                        PolyDiet.Core.ModelLoading.Validation.ValidationErrorType.MissingRequiredFields
                    );
                }
                
                if (faceCount == 0)
                {
                    return PolyDiet.Core.ModelLoading.Validation.ValidationResult.Failure(
                        "Nenhuma face encontrada no arquivo OBJ",
                        PolyDiet.Core.ModelLoading.Validation.ValidationErrorType.MissingRequiredFields
                    );
                }
                
                var fileInfo = new PolyDiet.Core.ModelLoading.Validation.GltfFileInfo
                {
                    FilePath = filePath,
                    FileSizeBytes = new System.IO.FileInfo(filePath).Length,
                    FileType = "OBJ",
                    EstimatedVertexCount = vertexCount,
                    EstimatedTriangleCount = faceCount
                };
                
                return PolyDiet.Core.ModelLoading.Validation.ValidationResult.Success(fileInfo);
            }
            catch (Exception ex)
            {
                return PolyDiet.Core.ModelLoading.Validation.ValidationResult.Failure(
                    $"Erro ao validar arquivo OBJ: {ex.Message}",
                    PolyDiet.Core.ModelLoading.Validation.ValidationErrorType.Unknown
                );
            }
        }
        
        /// <summary>
        /// Obtém espaço disponível em disco
        /// </summary>
        private static long GetAvailableDiskSpace()
        {
            try
            {
                string path = Application.streamingAssetsPath;
                var drive = new System.IO.DriveInfo(System.IO.Path.GetPathRoot(path));
                return drive.AvailableFreeSpace;
            }
            catch
            {
                return long.MaxValue; // Assume que há espaço suficiente
            }
        }
    }
    
    /// <summary>
    /// Resultado de validação do wizard
    /// </summary>
    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; }
        public List<string> Warnings { get; set; }
        public List<string> Infos { get; set; }
        
        public ValidationResult()
        {
            Errors = new List<string>();
            Warnings = new List<string>();
            Infos = new List<string>();
        }
        
        public void AddError(string error)
        {
            Errors.Add(error);
        }
        
        public void AddWarning(string warning)
        {
            Warnings.Add(warning);
        }
        
        public void AddInfo(string info)
        {
            Infos.Add(info);
        }
        
        public string GenerateReport()
        {
            var report = "=== Validação Prévia ===\n\n";
            
            if (Infos.Count > 0)
            {
                report += "Informações:\n";
                foreach (var info in Infos)
                {
                    report += $"  ℹ️ {info}\n";
                }
                report += "\n";
            }
            
            if (Warnings.Count > 0)
            {
                report += "Avisos:\n";
                foreach (var warning in Warnings)
                {
                    report += $"  ⚠️ {warning}\n";
                }
                report += "\n";
            }
            
            if (Errors.Count > 0)
            {
                report += "Erros:\n";
                foreach (var error in Errors)
                {
                    report += $"  ❌ {error}\n";
                }
                report += "\n";
            }
            
            report += $"Status: {(IsValid ? "✅ Pronto para continuar" : "❌ Corrija os erros antes de continuar")}\n";
            
            return report;
        }
    }
}

