using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using PolyDiet.Core.ModelLoading.Validation;
using PolyDiet.UI.Events;
using UnityEngine;
using GLTFast;
using Debug = UnityEngine.Debug;

namespace PolyDiet.Core.ModelLoading.Loading
{
    /// <summary>
    /// Opções para carregamento de modelo
    /// </summary>
    public class LoadOptions
    {
        public bool ValidateBeforeLoad { get; set; } = true;
        public bool NormalizeScale { get; set; } = true;
        public Transform SpawnParent { get; set; }
        public bool EnableFallback { get; set; } = true;
        public string[] FallbackVariants { get; set; } = new[] { "draco", "meshopt", "original" };
        public int MaxRetries { get; set; } = 2;
        public float TimeoutSeconds { get; set; } = 60.0f;
    }
    
    /// <summary>
    /// Resultado de uma operação de carregamento
    /// </summary>
    [Serializable]
    public class LoadResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public GameObject LoadedObject { get; set; }
        public string ModelName { get; set; }
        public string VariantUsed { get; set; }
        public string FilePath { get; set; }
        public float LoadTimeSeconds { get; set; }
        public long FileSizeBytes { get; set; }
        public List<string> Warnings { get; set; }
        public Dictionary<string, string> Metadata { get; set; }
        
        public LoadResult()
        {
            Warnings = new List<string>();
            Metadata = new Dictionary<string, string>();
        }
        
        public static LoadResult Failed(string errorMessage, string modelName = null, string variant = null)
        {
            return new LoadResult
            {
                Success = false,
                ErrorMessage = errorMessage,
                ModelName = modelName,
                VariantUsed = variant
            };
        }
        
        public static LoadResult Succeeded(
            GameObject loadedObject,
            string modelName,
            string variant,
            string filePath,
            float loadTimeSeconds)
        {
            return new LoadResult
            {
                Success = true,
                LoadedObject = loadedObject,
                ModelName = modelName,
                VariantUsed = variant,
                FilePath = filePath,
                LoadTimeSeconds = loadTimeSeconds
            };
        }
        
        public override string ToString()
        {
            if (Success)
            {
                return $"Load Success: {ModelName} ({VariantUsed}) in {LoadTimeSeconds:F2}s";
            }
            else
            {
                return $"Load Failed: {ErrorMessage}";
            }
        }
    }
}

