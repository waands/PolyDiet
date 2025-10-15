using System;
using System.Collections.Generic;

namespace PolyDiet.Core.ModelLoading.Compression
{
    /// <summary>
    /// Tipos de compressão disponíveis
    /// </summary>
    public enum CompressionType
    {
        Draco,
        Meshopt
    }
    
    /// <summary>
    /// Níveis de compressão
    /// </summary>
    public enum CompressionLevel
    {
        Low,
        Default,
        High,
        Maximum
    }
    
    /// <summary>
    /// Opções para compressão
    /// </summary>
    public class CompressionOptions
    {
        public int TimeoutSeconds { get; set; } = 300; // 5 minutos
        public int MaxRetries { get; set; } = 2;
        public bool OverwriteExisting { get; set; } = true;
        public CompressionLevel Level { get; set; } = CompressionLevel.Default;
        public string WorkingDirectory { get; set; }
        public bool LogOutput { get; set; } = true;
    }
    
    /// <summary>
    /// Resultado de uma operação de compressão
    /// </summary>
    [Serializable]
    public class CompressionResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public CompressionType Type { get; set; }
        public string InputPath { get; set; }
        public string OutputPath { get; set; }
        public TimeSpan CompressionTime { get; set; }
        public long InputSizeBytes { get; set; }
        public long OutputSizeBytes { get; set; }
        public List<string> Warnings { get; set; }
        public Dictionary<string, string> Metadata { get; set; }
        
        public CompressionResult()
        {
            Warnings = new List<string>();
            Metadata = new Dictionary<string, string>();
        }
        
        public static CompressionResult Failed(string errorMessage, CompressionType type)
        {
            return new CompressionResult
            {
                Success = false,
                ErrorMessage = errorMessage,
                Type = type
            };
        }
        
        public static CompressionResult Succeeded(
            CompressionType type,
            string outputPath,
            TimeSpan compressionTime)
        {
            return new CompressionResult
            {
                Success = true,
                Type = type,
                OutputPath = outputPath,
                CompressionTime = compressionTime
            };
        }
        
        public float CompressionRatio
        {
            get
            {
                if (InputSizeBytes == 0) return 1.0f;
                return (float)OutputSizeBytes / InputSizeBytes;
            }
        }
        
        public float CompressionPercentage
        {
            get
            {
                return (1.0f - CompressionRatio) * 100.0f;
            }
        }
        
        public override string ToString()
        {
            if (Success)
            {
                return $"{Type} Success: {InputSizeBytes} → {OutputSizeBytes} bytes ({CompressionPercentage:F1}% reduction) in {CompressionTime.TotalSeconds:F2}s";
            }
            else
            {
                return $"{Type} Failed: {ErrorMessage}";
            }
        }
    }
}

