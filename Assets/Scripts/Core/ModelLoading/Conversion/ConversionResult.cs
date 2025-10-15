using System;
using System.Collections.Generic;

namespace PolyDiet.Core.ModelLoading.Conversion
{
    /// <summary>
    /// Resultado de uma conversão de modelo
    /// </summary>
    [Serializable]
    public class ConversionResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public string InputPath { get; set; }
        public string OutputPath { get; set; }
        public string StrategyUsed { get; set; }
        public TimeSpan ConversionTime { get; set; }
        public long InputSizeBytes { get; set; }
        public long OutputSizeBytes { get; set; }
        public List<string> Warnings { get; set; }
        public Dictionary<string, string> Metadata { get; set; }
        
        public ConversionResult()
        {
            Warnings = new List<string>();
            Metadata = new Dictionary<string, string>();
        }
        
        public static ConversionResult Failed(string errorMessage, string strategyUsed = null)
        {
            return new ConversionResult
            {
                Success = false,
                ErrorMessage = errorMessage,
                StrategyUsed = strategyUsed
            };
        }
        
        public static ConversionResult Succeeded(string outputPath, string strategyUsed, TimeSpan conversionTime)
        {
            return new ConversionResult
            {
                Success = true,
                OutputPath = outputPath,
                StrategyUsed = strategyUsed,
                ConversionTime = conversionTime
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
        
        public override string ToString()
        {
            if (Success)
            {
                return $"Success using {StrategyUsed} in {ConversionTime.TotalSeconds:F2}s (ratio: {CompressionRatio:F2})";
            }
            else
            {
                return $"Failed: {ErrorMessage}";
            }
        }
    }
}

