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
    /// Carregador de modelos com fallback e recovery automático
    /// </summary>
    public class ModelLoader
    {
        private readonly Dictionary<string, GameObject> _modelCache;
        
        public ModelLoader()
        {
            _modelCache = new Dictionary<string, GameObject>();
        }
        
        /// <summary>
        /// Carrega um modelo específico
        /// </summary>
        public async Task<LoadResult> LoadModelAsync(
            string modelName,
            string variant,
            LoadOptions options = null,
            IProgress<float> progress = null,
            CancellationToken cancellationToken = default)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            try
            {
                options = options ?? new LoadOptions();
                
                Debug.Log($"[ModelLoader] Loading {modelName} ({variant})");
                
                // Resolve caminho do arquivo
                string filePath = ResolveModelPath(modelName, variant);
                if (string.IsNullOrEmpty(filePath))
                {
                    return LoadResult.Failed($"Arquivo não encontrado para {modelName} ({variant})", modelName, variant);
                }
                
                progress?.Report(0.1f);
                
                // Validação prévia (se habilitada)
                if (options.ValidateBeforeLoad)
                {
                    var validation = GltfValidator.Validate(filePath);
                    if (!validation.IsValid)
                    {
                        return LoadResult.Failed(
                            $"Arquivo inválido: {validation.ErrorMessage}",
                            modelName,
                            variant
                        );
                    }
                    
                    progress?.Report(0.2f);
                }
                
                // Verifica cache
                string cacheKey = $"{modelName}_{variant}";
                if (_modelCache.TryGetValue(cacheKey, out GameObject cachedModel))
                {
                    Debug.Log($"[ModelLoader] Using cached model: {cacheKey}");
                    progress?.Report(1.0f);
                    
                    var result = LoadResult.Succeeded(cachedModel, modelName, variant, filePath, 0.0f);
                    result.Metadata["FromCache"] = "true";
                    return result;
                }
                
                progress?.Report(0.3f);
                
                // Carrega modelo
                var loadResult = await LoadModelFromFileAsync(filePath, modelName, variant, options, progress, cancellationToken);
                
                stopwatch.Stop();
                loadResult.LoadTimeSeconds = (float)stopwatch.Elapsed.TotalSeconds;
                loadResult.FileSizeBytes = new FileInfo(filePath).Length;
                
                // Adiciona ao cache se bem-sucedido
                if (loadResult.Success && loadResult.LoadedObject != null)
                {
                    _modelCache[cacheKey] = loadResult.LoadedObject;
                }
                
                return loadResult;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                Debug.LogError($"[ModelLoader] Exception during load: {ex.Message}");
                return LoadResult.Failed($"Exceção: {ex.Message}", modelName, variant);
            }
        }
        
        /// <summary>
        /// Carrega modelo com fallback automático para outras variantes
        /// </summary>
        public async Task<LoadResult> LoadModelWithFallbackAsync(
            string modelName,
            string[] variantPriority = null,
            LoadOptions options = null,
            IProgress<float> progress = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                options = options ?? new LoadOptions();
                variantPriority = variantPriority ?? options.FallbackVariants;
                
                Debug.Log($"[ModelLoader] Loading {modelName} with fallback: {string.Join(", ", variantPriority)}");
                
                LoadResult lastResult = null;
                int totalVariants = variantPriority.Length;
                
                for (int i = 0; i < variantPriority.Length; i++)
                {
                    string variant = variantPriority[i];
                    
                    // Progresso para esta variante
                    var variantProgress = new Progress<float>(value =>
                    {
                        float overallProgress = (i + value) / totalVariants;
                        progress?.Report(overallProgress);
                    });
                    
                    Debug.Log($"[ModelLoader] Trying variant {i + 1}/{totalVariants}: {variant}");
                    
                    try
                    {
                        lastResult = await LoadModelAsync(modelName, variant, options, variantProgress, cancellationToken);
                        
                        if (lastResult.Success)
                        {
                            Debug.Log($"[ModelLoader] ✅ Success with variant: {variant}");
                            lastResult.Metadata["FallbackUsed"] = i > 0 ? "true" : "false";
                            lastResult.Metadata["FallbackOrder"] = i.ToString();
                            return lastResult;
                        }
                        else
                        {
                            Debug.LogWarning($"[ModelLoader] ❌ Variant {variant} failed: {lastResult.ErrorMessage}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[ModelLoader] Exception with variant {variant}: {ex.Message}");
                        lastResult = LoadResult.Failed($"Exceção: {ex.Message}", modelName, variant);
                    }
                }
                
                // Se chegou aqui, todas as variantes falharam
                string errorMsg = lastResult != null
                    ? $"Todas as variantes falharam. Último erro: {lastResult.ErrorMessage}"
                    : "Nenhuma variante disponível";
                
                Debug.LogError($"[ModelLoader] All variants failed for {modelName}");
                return LoadResult.Failed(errorMsg, modelName);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ModelLoader] Fatal exception in fallback load: {ex.Message}");
                return LoadResult.Failed($"Erro fatal: {ex.Message}", modelName);
            }
        }
        
        /// <summary>
        /// Carrega modelo do arquivo usando GLTFast
        /// </summary>
        private async Task<LoadResult> LoadModelFromFileAsync(
            string filePath,
            string modelName,
            string variant,
            LoadOptions options,
            IProgress<float> progress,
            CancellationToken cancellationToken)
        {
            try
            {
                progress?.Report(0.4f);
                
                // Cria container para o modelo
                GameObject container = new GameObject($"GLTF_{modelName}_{variant}");
                if (options.SpawnParent != null)
                {
                    container.transform.SetParent(options.SpawnParent, false);
                }
                
                progress?.Report(0.5f);
                
                // Adiciona componente GLTFast
                var gltf = container.AddComponent<GltfAsset>();
                gltf.LoadOnStartup = false;
                
                progress?.Report(0.6f);
                
                // Constrói URL para Unity
                string url = "file://" + filePath.Replace("\\", "/");
                Debug.Log($"[ModelLoader] Loading URL: {url}");
                
                progress?.Report(0.7f);
                
                // Carrega modelo
                bool success = false;
                try
                {
                    success = await gltf.Load(url);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[ModelLoader] GLTFast load failed: {ex.Message}");
                    success = false;
                }
                
                progress?.Report(0.8f);
                
                if (!success)
                {
                    // Limpa container se falhou
                    if (container != null)
                    {
                        UnityEngine.Object.DestroyImmediate(container);
                    }
                    
                    return LoadResult.Failed("Falha no carregamento GLTFast", modelName, variant);
                }
                
                progress?.Report(0.9f);
                
                // Normaliza escala se habilitado
                if (options.NormalizeScale)
                {
                    NormalizeModelScale(container);
                }
                
                progress?.Report(1.0f);
                
                // Notifica sucesso via eventos
                GameEvents.NotifyModelLoaded(modelName, variant);
                
                Debug.Log($"[ModelLoader] ✅ Model loaded successfully: {modelName} ({variant})");
                
                return LoadResult.Succeeded(container, modelName, variant, filePath, 0.0f);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ModelLoader] Exception in LoadModelFromFileAsync: {ex.Message}");
                return LoadResult.Failed($"Exceção no carregamento: {ex.Message}", modelName, variant);
            }
        }
        
        /// <summary>
        /// Resolve caminho do arquivo do modelo
        /// </summary>
        private string ResolveModelPath(string modelName, string variant)
        {
            try
            {
                // Procura por arquivos GLB/GLTF na estrutura padrão
                string[] possibleExtensions = { ".glb", ".gltf" };
                
                foreach (string ext in possibleExtensions)
                {
                    string fileName = $"model{ext}";
                    string path = Path.Combine(
                        Application.streamingAssetsPath,
                        "Models",
                        modelName,
                        variant,
                        fileName
                    );
                    
                    if (File.Exists(path))
                    {
                        return path;
                    }
                }
                
                return null;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ModelLoader] Exception resolving path: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// Normaliza escala do modelo carregado
        /// </summary>
        private void NormalizeModelScale(GameObject modelContainer)
        {
            try
            {
                // Encontra todos os renderers no modelo
                var renderers = modelContainer.GetComponentsInChildren<Renderer>();
                if (renderers.Length == 0) return;
                
                // Calcula bounds combinados
                Bounds combinedBounds = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++)
                {
                    combinedBounds.Encapsulate(renderers[i].bounds);
                }
                
                // Calcula escala para normalizar para tamanho unitário
                float maxDimension = Mathf.Max(combinedBounds.size.x, combinedBounds.size.y, combinedBounds.size.z);
                if (maxDimension > 0.001f) // Evita divisão por zero
                {
                    float scaleFactor = 1.0f / maxDimension;
                    modelContainer.transform.localScale = Vector3.one * scaleFactor;
                    
                    Debug.Log($"[ModelLoader] Normalized scale: {scaleFactor:F3} (max dimension: {maxDimension:F3})");
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ModelLoader] Failed to normalize scale: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Limpa cache de modelos
        /// </summary>
        public void ClearCache()
        {
            foreach (var kvp in _modelCache)
            {
                if (kvp.Value != null)
                {
                    UnityEngine.Object.DestroyImmediate(kvp.Value);
                }
            }
            _modelCache.Clear();
            Debug.Log("[ModelLoader] Cache cleared");
        }
        
        /// <summary>
        /// Remove modelo específico do cache
        /// </summary>
        public void RemoveFromCache(string modelName, string variant)
        {
            string cacheKey = $"{modelName}_{variant}";
            if (_modelCache.TryGetValue(cacheKey, out GameObject cachedModel))
            {
                if (cachedModel != null)
                {
                    UnityEngine.Object.DestroyImmediate(cachedModel);
                }
                _modelCache.Remove(cacheKey);
                Debug.Log($"[ModelLoader] Removed from cache: {cacheKey}");
            }
        }
        
        /// <summary>
        /// Gera relatório do cache
        /// </summary>
        public string GenerateCacheReport()
        {
            var report = "=== Model Cache Report ===\n\n";
            report += $"Total cached models: {_modelCache.Count}\n\n";
            
            foreach (var kvp in _modelCache)
            {
                string status = kvp.Value != null ? "✅ Active" : "❌ Destroyed";
                report += $"{kvp.Key}: {status}\n";
            }
            
            return report;
        }
    }
}

