# 📝 Guia de Estilo de Código - PolyDiet Unity

## 🎯 Resumo Executivo

**Convenções Definidas:** 15 padrões principais  
**Template de Script:** 1 template completo  
**Padrões de Comentários:** 3 tipos documentados  
**Exemplos Práticos:** 20+ exemplos de código  

---

## 🏗️ Convenções de Nomenclatura

### Classes e Interfaces

```csharp
// ✅ CORRETO: PascalCase para classes
public class ModelViewer : MonoBehaviour
public class MetricsCollector : MonoBehaviour
public class CompareSplitView : MonoBehaviour

// ✅ CORRETO: PascalCase para interfaces com prefixo "I"
public interface IModelLoader
public interface IMetricsCollector
public interface ICompressor

// ❌ INCORRETO: camelCase ou snake_case
public class modelViewer : MonoBehaviour  // ❌
public class metrics_collector : MonoBehaviour  // ❌
```

### Métodos e Propriedades

```csharp
// ✅ CORRETO: PascalCase para métodos públicos
public void LoadModel(string modelName, string variant)
public async Task<bool> LoadOnlyAsync(string modelName, string variant)
public void NotifyModelLoaded(string modelName, string variant)

// ✅ CORRETO: PascalCase para propriedades
public string ModelName { get; set; }
public bool IsLoading { get; private set; }
public Transform SpawnParent { get; set; }

// ❌ INCORRETO: camelCase para métodos públicos
public void loadModel(string modelName, string variant)  // ❌
```

### Variáveis e Campos

```csharp
// ✅ CORRETO: camelCase para variáveis locais
string modelName = "Duck";
bool isLoading = false;
int testCount = 3;

// ✅ CORRETO: _camelCase para campos privados
private string _currentLoadedModel;
private bool _isLoading = false;
private readonly List<string> _selectedVariants = new();

// ✅ CORRETO: camelCase para parâmetros
public void SetModel(string modelName, string variant)
{
    _currentLoadedModel = modelName;
    // ...
}

// ❌ INCORRETO: PascalCase para variáveis locais
string ModelName = "Duck";  // ❌
bool IsLoading = false;     // ❌
```

### Constantes e Enums

```csharp
// ✅ CORRETO: PascalCase para constantes
public const string BASE_VARIANT = "original";
public const float DEFAULT_FPS_WINDOW_SECONDS = 5.0f;
public const int MAX_FPS_SAMPLES_IN_CSV = 50;

// ✅ CORRETO: PascalCase para enums
public enum Step { Import, AskCompress, Compressing, AskRun, Running, Done }
public enum CompressionType { Draco, Meshopt, None }

// ❌ INCORRETO: camelCase para constantes
public const string baseVariant = "original";  // ❌
```

### Namespaces

```csharp
// ✅ CORRETO: PascalCase para namespaces
namespace PolyDiet.Core.ModelLoading
namespace PolyDiet.Metrics.Data
namespace PolyDiet.UI.Controllers

// ❌ INCORRETO: camelCase ou snake_case
namespace polydiet.core.modelLoading  // ❌
namespace poly_diet.metrics.data     // ❌
```

---

## 📁 Estrutura de Arquivos

### Template de Script Padrão

```csharp
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace PolyDiet.Core.ModelLoading
{
    /// <summary>
    /// Responsável por [descrição clara da responsabilidade]
    /// </summary>
    public class ModelScanner : MonoBehaviour
    {
        #region Fields
        
        [Header("Configuration")]
        [SerializeField] private string _modelsPath = "Models";
        [SerializeField] private bool _autoScanOnStart = true;
        
        [Header("Debug")]
        [SerializeField] private bool _enableDebugLogs = true;
        
        // Campos privados
        private readonly List<string> _availableModels = new();
        private bool _isScanning = false;
        
        #endregion
        
        #region Properties
        
        /// <summary>
        /// Lista de modelos disponíveis (somente leitura)
        /// </summary>
        public IReadOnlyList<string> AvailableModels => _availableModels.AsReadOnly();
        
        /// <summary>
        /// Indica se está escaneando no momento
        /// </summary>
        public bool IsScanning => _isScanning;
        
        #endregion
        
        #region Unity Lifecycle
        
        void Awake()
        {
            // Inicialização que não depende de outros objetos
        }
        
        void Start()
        {
            if (_autoScanOnStart)
            {
                ScanModels();
            }
        }
        
        void OnDestroy()
        {
            // Limpeza de recursos
        }
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// Escaneia a pasta de modelos e atualiza a lista disponível
        /// </summary>
        public void ScanModels()
        {
            if (_isScanning)
            {
                LogDebug("Scan já em andamento, ignorando nova solicitação");
                return;
            }
            
            _isScanning = true;
            LogDebug("Iniciando scan de modelos");
            
            try
            {
                // Implementação do scan
                PerformScan();
            }
            catch (Exception ex)
            {
                LogError($"Erro durante scan: {ex.Message}");
            }
            finally
            {
                _isScanning = false;
            }
        }
        
        /// <summary>
        /// Escaneia modelos de forma assíncrona
        /// </summary>
        public async Task ScanModelsAsync()
        {
            // Implementação assíncrona
            await Task.Run(() => ScanModels());
        }
        
        #endregion
        
        #region Private Methods
        
        private void PerformScan()
        {
            // Implementação privada
        }
        
        private void LogDebug(string message)
        {
            if (_enableDebugLogs)
            {
                Debug.Log($"[ModelScanner] {message}");
            }
        }
        
        private void LogError(string message)
        {
            Debug.LogError($"[ModelScanner] {message}");
        }
        
        #endregion
        
        #region Events
        
        /// <summary>
        /// Disparado quando o scan é concluído
        /// </summary>
        public event System.Action OnScanCompleted;
        
        /// <summary>
        /// Disparado quando um novo modelo é encontrado
        /// </summary>
        public event System.Action<string> OnModelFound;
        
        #endregion
    }
}
```

### Organização de Regiões

```csharp
// ✅ CORRETO: Uso de regiões para organizar código
#region Fields
// Campos e propriedades
#endregion

#region Unity Lifecycle
// Awake, Start, Update, OnDestroy, etc.
#endregion

#region Public Methods
// Métodos públicos
#endregion

#region Private Methods
// Métodos privados
#endregion

#region Events
// Eventos e delegates
#endregion
```

---

## 💬 Padrões de Comentários

### 1. Comentários XML (Documentação)

```csharp
/// <summary>
/// Carrega um modelo 3D de forma assíncrona
/// </summary>
/// <param name="modelName">Nome do modelo a ser carregado</param>
/// <param name="variant">Variante do modelo (original, draco, meshopt)</param>
/// <returns>True se o carregamento foi bem-sucedido</returns>
/// <exception cref="ArgumentNullException">Quando modelName é null ou vazio</exception>
public async Task<bool> LoadModelAsync(string modelName, string variant)
{
    // Implementação
}
```

### 2. Comentários de "Porquê" (Decisões de Design)

```csharp
// Usamos HashSet para evitar duplicatas mesmo se o usuário clicar
// múltiplas vezes rapidamente no mesmo chip antes do toggle atualizar
readonly HashSet<string> _selectedVariants = new();

// Esperamos 100ms para garantir que o UnloadUnusedAssets finalizou
// antes de iniciar o próximo teste, evitando contaminação de memória
await Task.Delay(100);

// Usamos file:// protocol para compatibilidade com Unity em todas as plataformas
string url = "file://" + path.Replace("\\", "/");
```

### 3. Comentários de Seção (Organização)

```csharp
// ===== DESCOBERTA DINÂMICA =====
private readonly string[] _allKnownVariants = new[] { "original", "draco", "meshopt" };

// ===== CAMINHOS DOS CLIS =====
#if UNITY_EDITOR_LINUX || UNITY_STANDALONE_LINUX
private static string GetGltfTransformLinux() { /* ... */ }
#endif

// ===== MÉTODOS PÚBLICOS PARA WIZARD =====
public void RescanModels() => ScanModelsAndPopulateUI();
```

### ❌ Comentários a Evitar

```csharp
// ❌ INCORRETO: Comentários óbvios
// Limpa os antigos
foreach (Transform c in variantChipContainer) Destroy(c.gameObject);

// ❌ INCORRETO: Comentários desatualizados
// TODO: Implementar cache (implementado em 2024)

// ❌ INCORRETO: Comentários muito longos
// Este método faz muitas coisas: primeiro verifica se o modelo existe,
// depois carrega o arquivo, depois aplica a escala, depois configura
// a câmera, depois atualiza a UI, depois notifica outros componentes...
```

---

## 🔧 Convenções de Código

### Async/Await

```csharp
// ✅ CORRETO: Métodos async sempre com sufixo Async
public async Task<bool> LoadModelAsync(string modelName, string variant)
public async Task CompressModelAsync(string input, string output)
public async Task GenerateReportAsync()

// ✅ CORRETO: Uso correto de async/await
public async Task<bool> LoadModelAsync(string modelName, string variant)
{
    try
    {
        // Operação assíncrona
        bool success = await gltfAsset.Load(url);
        return success;
    }
    catch (Exception ex)
    {
        LogError($"Erro ao carregar modelo: {ex.Message}");
        return false;
    }
}

// ❌ INCORRETO: Métodos async sem sufixo Async
public async Task<bool> LoadModel(string modelName, string variant)  // ❌
```

### Null Checks e Validação

```csharp
// ✅ CORRETO: Validação de entrada
public void SetModel(string modelName, string variant)
{
    if (string.IsNullOrEmpty(modelName))
        throw new ArgumentException("Model name cannot be null or empty", nameof(modelName));
    
    if (string.IsNullOrEmpty(variant))
        throw new ArgumentException("Variant cannot be null or empty", nameof(variant));
    
    // Implementação
}

// ✅ CORRETO: Null checks defensivos
public void UpdateUI()
{
    if (dropdownModel == null) return;
    if (dropdownVariant == null) return;
    
    // Implementação
}

// ✅ CORRETO: Null-conditional operators
var modelName = dropdownModel?.options?[dropdownModel.value]?.text;
if (string.IsNullOrEmpty(modelName)) return;
```

### Logging

```csharp
// ✅ CORRETO: Logs estruturados com contexto
Debug.Log($"[ModelViewer] Carregando modelo: {modelName} ({variant})");
Debug.LogError($"[ReportRunner] Falha ao executar Python: {ex.Message}");
Debug.LogWarning($"[HUDController] CompareLoader não configurado");

// ✅ CORRETO: Logs condicionais para debug
private void LogDebug(string message)
{
    if (_enableDebugLogs)
    {
        Debug.Log($"[{GetType().Name}] {message}");
    }
}

// ❌ INCORRETO: Logs sem contexto
Debug.Log("Carregando modelo");  // ❌
Debug.LogError("Erro");          // ❌
```

### Error Handling

```csharp
// ✅ CORRETO: Try-catch com contexto específico
public async Task<bool> LoadModelAsync(string modelName, string variant)
{
    try
    {
        // Operação principal
        bool success = await gltfAsset.Load(url);
        return success;
    }
    catch (FileNotFoundException ex)
    {
        LogError($"Arquivo não encontrado: {ex.FileName}");
        return false;
    }
    catch (UnauthorizedAccessException ex)
    {
        LogError($"Sem permissão para acessar arquivo: {ex.Message}");
        return false;
    }
    catch (Exception ex)
    {
        LogError($"Erro inesperado ao carregar modelo: {ex.Message}");
        return false;
    }
}

// ✅ CORRETO: Validação prévia
public void CompressModel(string input, string output)
{
    if (!File.Exists(input))
    {
        LogError($"Arquivo de entrada não encontrado: {input}");
        return;
    }
    
    if (!CrossPlatformHelper.EnsureDirectoryExists(Path.GetDirectoryName(output)))
    {
        LogError($"Não foi possível criar diretório de saída");
        return;
    }
    
    // Implementação
}
```

---

## 🎨 Convenções de UI

### Serialized Fields

```csharp
// ✅ CORRETO: Organização com Headers
[Header("References")]
public ModelViewer viewer;
public SimpleOrbitCamera orbitCamera;

[Header("UI Components")]
public Button buttonLoad;
public TMP_Dropdown dropdownModel;
public TMP_Text statusText;

[Header("Configuration")]
[SerializeField] private bool _autoLoadOnStart = true;
[SerializeField] private float _loadTimeout = 30f;

[Header("Debug")]
[SerializeField] private bool _enableDebugLogs = true;
```

### Event Handling

```csharp
// ✅ CORRETO: Event handlers com nomes descritivos
void Awake()
{
    if (buttonLoad != null)
        buttonLoad.onClick.AddListener(OnLoadButtonClicked);
    
    if (dropdownModel != null)
        dropdownModel.onValueChanged.AddListener(OnModelSelectionChanged);
}

private void OnLoadButtonClicked()
{
    // Implementação
}

private void OnModelSelectionChanged(int index)
{
    // Implementação
}

// ❌ INCORRETO: Event handlers genéricos
buttonLoad.onClick.AddListener(() => { /* ... */ });  // ❌
```

---

## 📊 Convenções de Performance

### Object Pooling

```csharp
// ✅ CORRETO: Pool de objetos para UI dinâmica
public class VariantChipPool : MonoBehaviour
{
    [SerializeField] private Toggle _chipPrefab;
    [SerializeField] private Transform _container;
    
    private readonly Queue<Toggle> _pool = new();
    private readonly List<Toggle> _active = new();
    
    public Toggle GetChip()
    {
        if (_pool.Count > 0)
        {
            var chip = _pool.Dequeue();
            chip.gameObject.SetActive(true);
            _active.Add(chip);
            return chip;
        }
        
        var newChip = Instantiate(_chipPrefab, _container);
        _active.Add(newChip);
        return newChip;
    }
    
    public void ReturnChip(Toggle chip)
    {
        chip.gameObject.SetActive(false);
        _active.Remove(chip);
        _pool.Enqueue(chip);
    }
}
```

### Caching

```csharp
// ✅ CORRETO: Cache de resultados computacionais
public class ModelCache
{
    private readonly Dictionary<string, ModelData> _cache = new();
    private readonly int _maxCacheSize = 100;
    
    public ModelData GetModel(string modelName)
    {
        if (_cache.TryGetValue(modelName, out var cached))
        {
            return cached;
        }
        
        var modelData = LoadModelFromDisk(modelName);
        if (_cache.Count >= _maxCacheSize)
        {
            // Remove oldest entry
            var oldestKey = _cache.Keys.First();
            _cache.Remove(oldestKey);
        }
        
        _cache[modelName] = modelData;
        return modelData;
    }
}
```

---

## 🧪 Convenções de Testes

### Test Methods

```csharp
// ✅ CORRETO: Métodos de teste com nomes descritivos
[Test]
public void LoadModel_WithValidModel_ReturnsTrue()
{
    // Arrange
    var modelViewer = new ModelViewer();
    string modelName = "Duck";
    string variant = "original";
    
    // Act
    bool result = modelViewer.LoadModel(modelName, variant);
    
    // Assert
    Assert.IsTrue(result);
}

[Test]
public void LoadModel_WithInvalidModel_ThrowsArgumentException()
{
    // Arrange
    var modelViewer = new ModelViewer();
    
    // Act & Assert
    Assert.Throws<ArgumentException>(() => modelViewer.LoadModel("", "original"));
}
```

---

## 📋 Checklist de Qualidade

### Antes de Commitar

- [ ] Nomes seguem convenções (PascalCase para públicos, camelCase para privados)
- [ ] Métodos async têm sufixo `Async`
- [ ] Validação de entrada implementada
- [ ] Tratamento de erros adequado
- [ ] Logs estruturados com contexto
- [ ] Comentários XML em métodos públicos
- [ ] Comentários de "porquê" em decisões complexas
- [ ] Regiões organizando código
- [ ] Null checks defensivos
- [ ] Performance considerada (sem allocations desnecessárias)

### Code Review

- [ ] Código é legível sem comentários óbvios
- [ ] Responsabilidade única por classe/método
- [ ] Não há código duplicado
- [ ] Tratamento de edge cases
- [ ] Testes unitários se aplicável
- [ ] Documentação atualizada

---

## 🔄 Migration Guide

### Atualizando Código Existente

1. **Renomear Métodos Async**
   ```csharp
   // ANTES
   public async Task<bool> LoadModel(string name, string variant)
   
   // DEPOIS
   public async Task<bool> LoadModelAsync(string name, string variant)
   ```

2. **Adicionar Validação**
   ```csharp
   // ANTES
   public void SetModel(string modelName)
   {
       _currentModel = modelName;
   }
   
   // DEPOIS
   public void SetModel(string modelName)
   {
       if (string.IsNullOrEmpty(modelName))
           throw new ArgumentException("Model name cannot be null or empty", nameof(modelName));
       
       _currentModel = modelName;
   }
   ```

3. **Melhorar Logs**
   ```csharp
   // ANTES
   Debug.Log("Loading model");
   
   // DEPOIS
   Debug.Log($"[ModelViewer] Carregando modelo: {modelName} ({variant})");
   ```

---

**Data do Guia:** Janeiro 2025  
**Criado por:** AI Assistant  
**Próximo documento:** `EVENTS_SYSTEM_DESIGN.md`
