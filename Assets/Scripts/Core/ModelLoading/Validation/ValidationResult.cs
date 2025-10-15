using System;
using System.Collections.Generic;

namespace PolyDiet.Core.ModelLoading.Validation
{
    /// <summary>
    /// Resultado de uma validação de arquivo GLTF/GLB
    /// </summary>
    [Serializable]
    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public string ErrorMessage { get; set; }
        public List<string> Warnings { get; set; }
        public GltfFileInfo FileInfo { get; set; }
        
        // Detalhes adicionais do erro
        public ValidationErrorType ErrorType { get; set; }
        public bool CanBeRepaired { get; set; }
        public string RepairSuggestion { get; set; }
        
        public ValidationResult()
        {
            Warnings = new List<string>();
            FileInfo = new GltfFileInfo();
        }
        
        public static ValidationResult Success(GltfFileInfo fileInfo)
        {
            return new ValidationResult
            {
                IsValid = true,
                FileInfo = fileInfo
            };
        }
        
        public static ValidationResult Failure(string errorMessage, ValidationErrorType errorType = ValidationErrorType.Unknown)
        {
            return new ValidationResult
            {
                IsValid = false,
                ErrorMessage = errorMessage,
                ErrorType = errorType
            };
        }
        
        public override string ToString()
        {
            if (IsValid)
            {
                string warnings = Warnings.Count > 0 ? $" ({Warnings.Count} warnings)" : "";
                return $"Valid{warnings}: {FileInfo}";
            }
            else
            {
                return $"Invalid ({ErrorType}): {ErrorMessage}";
            }
        }
    }
    
    /// <summary>
    /// Tipos de erros de validação
    /// </summary>
    public enum ValidationErrorType
    {
        Unknown,
        FileNotFound,
        FileEmpty,
        InvalidExtension,
        InvalidMagicNumber,
        InvalidGlbHeader,
        InvalidJsonStructure,
        MissingRequiredFields,
        CorruptedChunks,
        InvalidBufferReferences
    }
}

