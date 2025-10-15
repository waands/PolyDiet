using GLTFast;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Diagnostics = System.Diagnostics;
using PolyDiet.UI.Events;
using UDebug = UnityEngine.Debug;
using UI = UnityEngine.UI;
using UApp = UnityEngine.Application;

public class ModelViewer : MonoBehaviour
{
    [Header("UI")]
    public TMP_Dropdown dropdownModel;
    public TMP_Dropdown dropdownVariant;
    public UI.Button buttonLoad;
    public TMP_Text textStatus;

    public UI.Button buttonCompressDraco;
    public UI.Button buttonCompressMeshopt;

    [Header("Scene")]
    public Transform spawnParent;
    
    [Header("Refs")]
    public HUDController hudController; // referência para notificar sobre carregamentos
    public SimpleOrbitCamera orbitCamera; // referência para ajustar câmera automaticamente

    // ===== Descoberta dinâmica =====
    private readonly string[] _allKnownVariants = new[] { "original", "draco", "meshopt" };
    private readonly List<string> _modelNames = new();
    private readonly Dictionary<string, Dictionary<string, string>> _fileByModelVariant = new();

    // ===== Caminhos dos CLIs =====
#if UNITY_EDITOR_LINUX || UNITY_STANDALONE_LINUX
    // Função para detectar o caminho do gltf-transform
    private static string GetGltfTransformLinux()
    {
        // Primeiro, tentar o caminho específico (Node.js via NVM)
        var nvmPath = Environment.GetEnvironmentVariable("HOME");
        if (!string.IsNullOrEmpty(nvmPath))
        {
            var nvmGltfTransform = Path.Combine(nvmPath, ".nvm/versions/node/v22.19.0/bin/gltf-transform");
            if (File.Exists(nvmGltfTransform)) return nvmGltfTransform;
            
            // Tentar outras versões do Node.js
            var nodeVersionsDir = Path.Combine(nvmPath, ".nvm/versions/node");
            if (Directory.Exists(nodeVersionsDir))
            {
                var versions = Directory.GetDirectories(nodeVersionsDir)
                    .OrderByDescending(d => d) // Versões mais recentes primeiro
                    .ToArray();
                
                foreach (var versionDir in versions)
                {
                    var gltfTransformPath = Path.Combine(versionDir, "bin/gltf-transform");
                    if (File.Exists(gltfTransformPath)) return gltfTransformPath;
                }
            }
        }
        
        // Tentar no PATH do sistema
        return "gltf-transform"; // Fallback para o PATH
    }
    
    // Função para detectar o caminho do gltfpack
    private static string GetGltfpackLinux()
    {
        // Primeiro tentar o caminho comum
        if (File.Exists("/usr/bin/gltfpack")) return "/usr/bin/gltfpack";
        if (File.Exists("/usr/local/bin/gltfpack")) return "/usr/local/bin/gltfpack";
        
        // Fallback para o PATH
        return "gltfpack";
    }
#elif UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
    private const string GLTF_TRANSFORM_WIN_CMD = @"%APPDATA%\npm\gltf-transform.cmd"; // chamado via cmd.exe /c
#else
    private const string GLTF_TRANSFORM_FALLBACK = "gltf-transform";
    private const string GLTFPACK_FALLBACK = "gltfpack";
#endif

    private GameObject _currentContainer;

    private void SetStatus(string msg) => textStatus?.SetText(msg);

    void Awake()
    {
        ScanModelsAndPopulateUI();

        buttonLoad.onClick.AddListener(() => _ = OnClickLoadAsync());
        if (buttonCompressDraco != null)
            buttonCompressDraco.onClick.AddListener(() => _ = OnClickCompressDracoAsync());
        if (buttonCompressMeshopt != null)
            buttonCompressMeshopt.onClick.AddListener(() => _ = OnClickCompressMeshoptAsync());

        // quando trocar o modelo, recalcula as variantes disponíveis
        dropdownModel.onValueChanged.AddListener(_ => PopulateVariantsForCurrentModel());

        SetStatus("Pronto.");
    }

    // ======== SCAN DINÂMICO ========

    void ScanModelsAndPopulateUI()
    {
        _modelNames.Clear();
        _fileByModelVariant.Clear();

        string modelsRoot = Path.Combine(UApp.streamingAssetsPath, "Models");
        if (!Directory.Exists(modelsRoot))
        {
            UDebug.LogWarning($"Pasta não encontrada: {modelsRoot}");
        }
        else
        {
            foreach (var dir in Directory.GetDirectories(modelsRoot))
            {
                var modelName = Path.GetFileName(dir);
                var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                foreach (var variant in _allKnownVariants)
                {
                    var vdir = Path.Combine(dir, variant);
                    if (!Directory.Exists(vdir)) continue;

                    // preferir "model.glb"; senão, pegar o primeiro *.glb
                    string f = Path.Combine(vdir, "model.glb");
                    if (!File.Exists(f))
                    {
                        var glbs = Directory.GetFiles(vdir, "*.glb");
                        if (glbs.Length > 0)
                        {
                            string candidate = glbs.FirstOrDefault(g => Path.GetFileName(g).Equals("model.glb", StringComparison.OrdinalIgnoreCase))
                                             ?? glbs.FirstOrDefault(g => Path.GetFileName(g).Contains("model", StringComparison.OrdinalIgnoreCase))
                                             ?? glbs[0];
                            f = candidate;
                        }
                        else
                        {
                            continue; // sem .glb nessa variante
                        }
                    }

                    map[variant] = Path.GetFileName(f);
                }

                if (map.Count > 0)
                {
                    _modelNames.Add(modelName);
                    _fileByModelVariant[modelName] = map;
                }
            }
        }

        // Popular dropdown de modelos
        dropdownModel.ClearOptions();
        if (_modelNames.Count == 0)
        {
            dropdownModel.AddOptions(new List<string> { "(sem modelos)" });
            dropdownModel.interactable = false;
            dropdownVariant.ClearOptions();
            dropdownVariant.AddOptions(new List<string> { "-" });
            dropdownVariant.interactable = false;
        }
        else
        {
            dropdownModel.AddOptions(_modelNames);
            dropdownModel.interactable = true;
            PopulateVariantsForCurrentModel();
        }
    }

    void PopulateVariantsForCurrentModel()
    {
        var modelName = GetSelectedModelName();
        var variants = GetAvailableVariants(modelName);

        dropdownVariant.ClearOptions();
        dropdownVariant.AddOptions(variants);
        dropdownVariant.interactable = variants.Count > 0;
        if (variants.Count > 0) dropdownVariant.value = 0;
        dropdownVariant.RefreshShownValue();
    }

    string GetSelectedModelName()
    {
        if (_modelNames.Count == 0) return null;
        int idx = Mathf.Clamp(dropdownModel.value, 0, _modelNames.Count - 1);
        return _modelNames[idx];
    }

    List<string> GetAvailableVariants(string modelName)
    {
        var list = new List<string>();
        if (string.IsNullOrEmpty(modelName) || !_fileByModelVariant.TryGetValue(modelName, out var map) || map.Count == 0)
            return list;

        foreach (var v in _allKnownVariants)
            if (map.ContainsKey(v)) list.Add(v);

        foreach (var kv in map.Keys)
            if (!list.Contains(kv)) list.Add(kv);

        return list;
    }

    public string GetFileNameFor(string modelName, string variant)
    {
        if (string.IsNullOrEmpty(modelName)) return null;
        if (_fileByModelVariant.TryGetValue(modelName, out var map))
        {
            if (map.TryGetValue(variant, out var fn)) return fn;
        }
        return null;
    }

    private void ClearSpawn()
    {
        if (_currentContainer != null)
        {
            Destroy(_currentContainer);
            _currentContainer = null;
        }

        if (spawnParent != null)
        {
            for (int i = spawnParent.childCount - 1; i >= 0; i--)
                Destroy(spawnParent.GetChild(i).gameObject);
        }
    }

    // Duplica a escala de modelos muito pequenos
    private void NormalizeModelScale(GameObject container)
    {
        if (container == null) return;

        // Calcula o bounding box do modelo
        var renderers = container.GetComponentsInChildren<Renderer>(true);
        if (renderers == null || renderers.Length == 0) return;

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);

        // Tamanho atual do modelo (maior dimensão)
        float currentSize = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
        
        // Se menor que 0.5 unidades, duplica a escala (2x)
        const float minSize = 0.5f;
        
        if (currentSize < 1e-6f) return; // Evita divisão por zero

        if (currentSize < minSize)
        {
            // Simplesmente duplica a escala
            float scaleFactor = 3.0f;
            container.transform.localScale = Vector3.one * scaleFactor;
            UDebug.Log($"[AutoScale] Modelo pequeno detectado (tamanho: {currentSize:F4}). " +
                      $"Duplicando escala (2x) → novo tamanho: {currentSize * scaleFactor:F4}");
        }
    }

    // ======== RUNTIME LOAD ========

    private async Task OnClickLoadAsync()
    {
        buttonLoad.interactable = false;
        try
        {
            SetStatus("Carregando...");
            ClearSpawn();

            var modelName = GetSelectedModelName();
            var variants = GetAvailableVariants(modelName);
            if (string.IsNullOrEmpty(modelName) || variants.Count == 0)
            {
                SetStatus("Nenhum modelo/variante disponível.");
                return;
            }

            string variant = variants[Mathf.Clamp(dropdownVariant.value, 0, variants.Count - 1)];
            string fileName = GetFileNameFor(modelName, variant) ?? "model.glb";

            string root = UApp.streamingAssetsPath;
            string path = Path.Combine(root, "Models", modelName, variant, fileName);
            path = CrossPlatformHelper.NormalizePath(path);
            
            if (!File.Exists(path))
            {
                SetStatus($"Arquivo não encontrado: {variant}/{fileName}.");
                return;
            }

            // Garantir que a URL use barras Unix para compatibilidade
            string url = "file://" + CrossPlatformHelper.ToUnixPath(path);

            // Iniciar medição de métricas
            if (Metrics.Instance != null)
            {
                Metrics.Instance.BeginLoad(modelName, variant, path, 1); // Teste único para carregamento manual
            }

            _currentContainer = new GameObject($"GLTF_{modelName}_{variant}");
            _currentContainer.transform.SetParent(spawnParent, false);

            var gltf = _currentContainer.AddComponent<GltfAsset>();
            gltf.LoadOnStartup = false;

            bool ok = false;
            try { ok = await gltf.Load(url); }
            catch (System.SystemException ex) { UDebug.LogError(ex); ok = false; }

            // Finalizar medição de carregamento
            if (Metrics.Instance != null)
            {
                await Metrics.Instance.EndLoad(ok);
            }

            if (!ok)
            {
                SetStatus($"Falha ao carregar: {modelName}/{variant}");
                ClearSpawn();
                return;
            }

            // Normaliza a escala do modelo se necessário
            NormalizeModelScale(_currentContainer);

            // Medir FPS após carregamento bem-sucedido
            if (Metrics.Instance != null && ok)
            {
                SetStatus("Medindo performance...");
                await Metrics.Instance.MeasureFpsWindow(Metrics.Instance.fpsWindowSeconds);
                
                // Verificar se já existe uma entrada similar e perguntar se quer sobrescrever
                if (ShouldAskToOverwrite(modelName, variant))
                {
                    SetStatus($"Modelo {modelName} ({variant}) já foi testado. Sobrescrever? (Y/N)");
                    // Por enquanto, vamos sempre sobrescrever. Em uma implementação completa,
                    // você poderia mostrar um diálogo de confirmação aqui.
                }
                
                Metrics.Instance.WriteCsv();
                SetStatus($"Carregado e medido: {modelName} ({variant})");
            }
            else
            {
                SetStatus($"{modelName} ({variant})");
            }

            // Notifica o HUDController sobre o modelo carregado e ajusta câmera
            if (ok)
            {
                if (hudController != null)
                {
                    hudController.NotifyModelLoaded(modelName, variant);
                }
                
                // Define o target da câmera para o modelo carregado com auto-fit
                if (orbitCamera != null && _currentContainer != null)
                {
                    orbitCamera.SetTarget(_currentContainer.transform, autoFrame: true);
                }
            }

            buttonLoad.interactable = true;
        }
        finally
        {
            buttonLoad.interactable = true;
        }
    }

    private (string inputOriginal, string outDraco, string outMeshopt) GetCurrentPaths()
    {
        var modelName = GetSelectedModelName();
        string root = UApp.streamingAssetsPath;

        string baseOriginal = GetFileNameFor(modelName, "original") ?? "model.glb";
        string baseDraco = GetFileNameFor(modelName, "draco") ?? baseOriginal;
        string baseMesh = GetFileNameFor(modelName, "meshopt") ?? baseOriginal;

        string original = CrossPlatformHelper.CombinePaths(root, "Models", modelName, "original", baseOriginal);
        string outDraco = CrossPlatformHelper.CombinePaths(root, "Models", modelName, "draco", baseDraco);
        string outMesh = CrossPlatformHelper.CombinePaths(root, "Models", modelName, "meshopt", baseMesh);
        
        return (original, outDraco, outMesh);
    }

    // ======== EXEC CLI ========

    // helper de aspas
    private static string Q(string s) => $"\"{s}\"";

    // Resolve caminho absoluto e WorkingDirectory (pasta do executável, se absoluto)
    private static (string absExe, string workingDir) ResolveExe(string exe)
    {
        // Se for relativo dentro do projeto (ex.: Assets/StreamingAssets/Tools/gltfpack.exe),
        // promova para absoluto a partir de Application.dataPath quando começar com "Assets".
        if (!Path.IsPathRooted(exe))
        {
            if (exe.StartsWith("Assets", StringComparison.OrdinalIgnoreCase))
            {
                // Assets/... -> <Projeto>/Assets/...
                string projRoot = Directory.GetParent(UApp.dataPath)!.FullName;
                string abs = Path.Combine(projRoot, exe.Replace('/', Path.DirectorySeparatorChar));
                return (abs, Path.GetDirectoryName(abs) ?? Directory.GetCurrentDirectory());
            }
            // Se for um nome simples (no PATH), devolve sem WorkingDirectory específico
            return (exe, Directory.GetCurrentDirectory());
        }
        else
        {
            return (exe, Path.GetDirectoryName(exe) ?? Directory.GetCurrentDirectory());
        }
    }

    private Task<int> RunProcessAsync(string file, string args)
    {
        var tcs = new TaskCompletionSource<int>();

        var (absExe, wd) = ResolveExe(file);

        var psi = new Diagnostics.ProcessStartInfo(absExe, args)
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            WorkingDirectory = wd
        };

        var p = new Diagnostics.Process { StartInfo = psi, EnableRaisingEvents = true };
        p.Exited += (s, e) =>
        {
            try
            {
                int code = p.ExitCode;
                string so = p.StandardOutput.ReadToEnd();
                string se = p.StandardError.ReadToEnd();
                
                // Logs mais detalhados
                if (code == 0)
                {
                    if (!string.IsNullOrEmpty(so))
                        UDebug.Log($"[Process] Output: {so}");
                    UDebug.Log($"[Process] ✅ Processo concluído com sucesso (exit code: {code})");
                }
                else
                {
                    UDebug.LogError($"[Process] ❌ Processo falhou (exit code: {code})");
                    if (!string.IsNullOrEmpty(so))
                        UDebug.LogError($"[Process] Output: {so}");
                    if (!string.IsNullOrEmpty(se))
                        UDebug.LogError($"[Process] Error: {se}");
                }
                
                tcs.TrySetResult(code);
            }
            catch (Exception ex) { UDebug.LogError($"[Process] ❌ Erro no callback: {ex.Message}"); tcs.TrySetResult(-1); }
            finally { p.Dispose(); }
        };

        try
        {
            UDebug.Log($"[Exec] {absExe} {args}\n[WD] {wd}");
            p.Start();
        }
        catch (Exception ex) { UDebug.LogError(ex); tcs.TrySetResult(-1); }

        return tcs.Task;
    }

    // Caminho absoluto do gltfpack dentro do projeto (Windows)
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
    private static string GetGltfpackExeWin()
    {
        // queremos Assets/StreamingAssets/Tools/gltfpack.exe -> absoluto
        string projRoot = Directory.GetParent(UApp.dataPath)!.FullName;
        return Path.Combine(projRoot, "Assets", "StreamingAssets", "Tools", "gltfpack.exe");
    }
#endif

    private async Task<bool> CompressMeshoptAsync(string input, string output)
    {
        try
        {
            // Verifica se arquivo de entrada existe
            if (!File.Exists(input))
            {
                UDebug.LogError($"[CompressMeshopt] Arquivo de entrada não encontrado: {input}");
                return false;
            }

            if (!CrossPlatformHelper.EnsureDirectoryExists(Path.GetDirectoryName(output)!))
            {
                UDebug.LogError($"[CompressMeshopt] Não foi possível criar diretório de saída");
                return false;
            }

#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            string exe = GetGltfpackExeWin(); // absoluto
            if (!File.Exists(exe))
            {
                UDebug.LogError($"[CompressMeshopt] gltfpack.exe não encontrado: {exe}");
                UDebug.LogError($"[CompressMeshopt] Coloque em Assets/StreamingAssets/Tools/");
                return false;
            }
            string args = $"-i {Q(Path.GetFullPath(input))} -o {Q(Path.GetFullPath(output))} -cc";
#elif UNITY_EDITOR_LINUX || UNITY_STANDALONE_LINUX
            string exe = GetGltfpackLinux();      // dinâmico
            string args = $"-i {Q(input)} -o {Q(output)} -cc";
#else
            string exe = GLTFPACK_FALLBACK;          // fallback
            string args = $"-i {Q(input)} -o {Q(output)} -cc";
#endif

            UDebug.Log($"[CompressMeshopt] Comprimindo: {input} → {output}");
            UDebug.Log($"[CompressMeshopt] Executando: {exe} {args}");
            
            // Verificar se o executável existe antes de tentar executar
            if (!CrossPlatformHelper.ExecutableExists(exe) && !Path.IsPathRooted(exe))
            {
                UDebug.LogError($"[CompressMeshopt] Executável não encontrado no PATH: {exe}");
                UDebug.LogError($"[CompressMeshopt] Instale o gltfpack ou ajuste o caminho");
                return false;
            }

            int code = await RunProcessAsync(exe, args);
            bool success = code == 0 && File.Exists(output);
            
            if (success)
            {
                long inputSize = new FileInfo(input).Length;
                long outputSize = new FileInfo(output).Length;
                float compressionRatio = (float)outputSize / inputSize;
                UDebug.Log($"[CompressMeshopt] ✅ Sucesso! Tamanho: {inputSize} → {outputSize} bytes (ratio: {compressionRatio:F2})");
            }
            else
            {
                UDebug.LogError($"[CompressMeshopt] ❌ Falha! Exit code: {code}, Arquivo existe: {File.Exists(output)}");
            }
            
            return success;
        }
        catch (Exception ex)
        {
            UDebug.LogError($"[CompressMeshopt] ❌ Erro: {ex.Message}");
            return false;
        }
    }


    private static string WinPath(string p) => p.Replace('/', '\\');

    private async Task<bool> CompressDracoAsync(string input, string output)
    {
        try
        {
            // Verifica se arquivo de entrada existe
            if (!File.Exists(input))
            {
                UDebug.LogError($"[CompressDraco] Arquivo de entrada não encontrado: {input}");
                return false;
            }

            if (!CrossPlatformHelper.EnsureDirectoryExists(Path.GetDirectoryName(output)!))
            {
                UDebug.LogError($"[CompressDraco] Não foi possível criar diretório de saída");
                return false;
            }

#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            // 1) Caminho absoluto do .cmd
            string cmdPath = Environment.ExpandEnvironmentVariables(@"%APPDATA%\npm\gltf-transform.cmd");
            if (!File.Exists(cmdPath))
            {
                UDebug.LogError($"[CompressDraco] gltf-transform.cmd não encontrado: {cmdPath}");
                UDebug.LogError($"[CompressDraco] Instale com: npm i -g @gltf-transform/cli");
                return false;
            }

            // 2) Normaliza caminhos para BACKSLASH e usa aspas
            string inPath  = WinPath(Path.GetFullPath(input));
            string outPath = WinPath(Path.GetFullPath(output));

            UDebug.Log($"[CompressDraco] Comprimindo: {inPath} → {outPath}");

            // 3) QUOTING CORRETO: /c ""C:\...\gltf-transform.cmd" optimize "in" "out" --compress draco"
            string file = Environment.GetEnvironmentVariable("ComSpec") ?? "cmd.exe";
            string args = $"/c \"\"{cmdPath}\" optimize {Q(inPath)} {Q(outPath)} --compress draco\"";
#elif UNITY_EDITOR_LINUX || UNITY_STANDALONE_LINUX
            UDebug.Log($"[CompressDraco] Comprimindo: {input} → {output}");
            
            string file = GetGltfTransformLinux(); // dinâmico
            string args = $"optimize {Q(input)} {Q(output)} --compress draco";
            
            // Verificar se o executável existe
            if (!CrossPlatformHelper.ExecutableExists(file) && !Path.IsPathRooted(file))
            {
                UDebug.LogError($"[CompressDraco] gltf-transform não encontrado: {file}");
                UDebug.LogError($"[CompressDraco] Instale com: npm i -g @gltf-transform/cli");
                return false;
            }
#else
            string file = GLTF_TRANSFORM_FALLBACK;
            string args = $"optimize {Q(input)} {Q(output)} --compress draco";
#endif

            int code = await RunProcessAsync(file, args);
            bool success = code == 0 && File.Exists(output);
            
            if (success)
            {
                long inputSize = new FileInfo(input).Length;
                long outputSize = new FileInfo(output).Length;
                float compressionRatio = (float)outputSize / inputSize;
                UDebug.Log($"[CompressDraco] ✅ Sucesso! Tamanho: {inputSize} → {outputSize} bytes (ratio: {compressionRatio:F2})");
            }
            else
            {
                UDebug.LogError($"[CompressDraco] ❌ Falha! Exit code: {code}, Arquivo existe: {File.Exists(output)}");
            }
            
            return success;
        }
        catch (Exception ex)
        {
            UDebug.LogError($"[CompressDraco] ❌ Erro: {ex.Message}");
            return false;
        }
    }


    // ======== BOTÕES ========

    private async Task OnClickCompressMeshoptAsync()
    {
        if (buttonCompressMeshopt) buttonCompressMeshopt.interactable = false;
        SetStatus("Compactando com Meshopt...");

        var (original, _, outMesh) = GetCurrentPaths();
        if (!File.Exists(original))
        {
            SetStatus("Não achei o original. Coloque em original/model.glb (ou qualquer .glb).");
            if (buttonCompressMeshopt) buttonCompressMeshopt.interactable = true;
            return;
        }

        bool ok = await CompressMeshoptAsync(original, outMesh);
        RegisterVariantFile(GetSelectedModelName(), "meshopt", Path.GetFileName(outMesh));
        PopulateVariantsForCurrentModel();

        SetStatus(ok ? "Meshopt: OK" : "Meshopt: falhou");
        if (buttonCompressMeshopt) buttonCompressMeshopt.interactable = true;
    }

    private async Task OnClickCompressDracoAsync()
    {
        if (buttonCompressDraco) buttonCompressDraco.interactable = false;
        SetStatus("Compactando com Draco...");

        var (original, outDraco, _) = GetCurrentPaths();
        if (!File.Exists(original))
        {
            SetStatus("Não achei o original. Coloque em original/model.glb (ou qualquer .glb).");
            if (buttonCompressDraco) buttonCompressDraco.interactable = true;
            return;
        }

        bool ok = await CompressDracoAsync(original, outDraco);
        RegisterVariantFile(GetSelectedModelName(), "draco", Path.GetFileName(outDraco));
        PopulateVariantsForCurrentModel();

        SetStatus(ok ? "Draco: OK" : "Draco: falhou");
        if (buttonCompressDraco) buttonCompressDraco.interactable = true;
    }

    void RegisterVariantFile(string modelName, string variant, string fileName)
    {
        if (string.IsNullOrEmpty(modelName) || string.IsNullOrEmpty(variant) || string.IsNullOrEmpty(fileName)) return;
        if (!_fileByModelVariant.TryGetValue(modelName, out var map))
        {
            map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            _fileByModelVariant[modelName] = map;
        }
        map[variant] = fileName;
    }

    private bool ShouldAskToOverwrite(string modelName, string variant)
    {
        if (Metrics.Instance == null) return false;

        try
        {
            var csvPath = GetCsvPath();
            if (!File.Exists(csvPath)) return false;

            var lines = File.ReadAllLines(csvPath);
            if (lines.Length <= 1) return false; // apenas header ou arquivo vazio

            // Verificar se já existe uma entrada com o mesmo modelo e variante
            for (int i = 1; i < lines.Length; i++) // pular header
            {
                var columns = ParseCsvLine(lines[i]);
                if (columns.Length >= 5) // timestamp,platform,unity_version,scene,model,variant...
                {
                    var existingModel = columns[4].Trim('"');
                    var existingVariant = columns[5].Trim('"');
                    
                    if (string.Equals(existingModel, modelName, StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(existingVariant, variant, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            UDebug.LogWarning($"Erro ao verificar CSV existente: {ex.Message}");
        }

        return false;
    }

    private string GetCsvPath()
    {
        // Usar o novo método da classe Metrics
        return Metrics.Instance.GetCsvPathPublic();
    }

    private string[] ParseCsvLine(string line)
    {
        var result = new List<string>();
        bool inQuotes = false;
        string currentField = "";
        
        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            
            if (c == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (c == ',' && !inQuotes)
            {
                result.Add(currentField);
                currentField = "";
            }
            else
            {
                currentField += c;
            }
        }
        
        result.Add(currentField); // adicionar último campo
        return result.ToArray();
    }

    // ======== MÉTODO PARA COMPARAÇÃO ========

    public async System.Threading.Tasks.Task<GameObject> LoadIntoAsync(string modelName, string variant, Transform parent, int layer)
    {
        string fileName = GetFileNameFor(modelName, variant) ?? "model.glb";
        string path = System.IO.Path.Combine(UApp.streamingAssetsPath, "Models", modelName, variant, fileName);
        
        if (!System.IO.File.Exists(path)) return null;

        // instancia container
        var go = new GameObject($"GLTF_{modelName}_{variant}");
        go.transform.SetParent(parent, false);
        go.layer = layer;

        var asset = go.AddComponent<GLTFast.GltfAsset>();
        asset.LoadOnStartup = false;
        string url = "file://" + path.Replace("\\", "/");

        bool ok = false;
        try { ok = await asset.Load(url); } catch { ok = false; }
        if (!ok) { Destroy(go); return null; }

        // IMPORTANTE: Aplicar layer DEPOIS do carregamento, quando todos os filhos existem
        SetLayerRecursively(go, layer);
        UDebug.Log($"[LoadIntoAsync] Layer {layer} aplicada recursivamente a {go.name} e todos os filhos");

        // Normaliza a escala do modelo se necessário
        NormalizeModelScale(go);

        return go;
    }

    static void SetLayerRecursively(GameObject root, int layer)
    {
        if (root == null) return;
        
        root.layer = layer;
        var t = root.transform;
        
        // Debug: mostra quantos filhos tem
        int childCount = t.childCount;
        if (childCount > 0)
        {
            UDebug.Log($"[SetLayerRecursively] {root.name}: definindo layer {layer} para {childCount} filhos");
        }
        
        for (int i = 0; i < childCount; i++)
        {
            SetLayerRecursively(t.GetChild(i).gameObject, layer);
        }
    }

    // ======== MÉTODOS PÚBLICOS PARA WIZARD ========

    // Permite outro script pedir um re-scan
    public void RescanModels() => ScanModelsAndPopulateUI();

    // Limpa modelos carregados (útil antes de entrar no modo Compare)
    public void ClearLoadedModels()
    {
        ClearSpawn();
        UDebug.Log("[ModelViewer] Modelos limpos (chamado externamente)");
    }

    // Expor nome e variantes do modelo atual/qualquer
    public string GetSelectedModelNamePublic() => GetSelectedModelName();
    public System.Collections.Generic.List<string> GetAvailableVariantsPublic(string modelName) => GetAvailableVariants(modelName);

    // Carrega sem mexer no estado da UI (usado pelo wizard)
    public async Task<bool> LoadAsync(string modelName, string variant)
    {
        // Reuso da lógica do OnClickLoadAsync, mas sem mexer em dropdowns/botão
        string fileName = GetFileNameFor(modelName, variant) ?? "model.glb";
        string root = UApp.streamingAssetsPath;
        string path = System.IO.Path.Combine(root, "Models", modelName, variant, fileName);
        if (!System.IO.File.Exists(path))
            return false;

        // begin metrics
        Metrics.Instance?.BeginLoad(modelName, variant, path);

        // limpar anterior
        ClearSpawn();

        _currentContainer = new GameObject($"GLTF_{modelName}_{variant}");
        _currentContainer.transform.SetParent(spawnParent, false);

        var gltf = _currentContainer.AddComponent<GltfAsset>();
        gltf.LoadOnStartup = false;

        string url = "file://" + path.Replace("\\", "/");
        bool ok = false;
        try { ok = await gltf.Load(url); }
        catch (System.SystemException ex) { UDebug.LogError(ex); ok = false; }

        if (Metrics.Instance != null) await Metrics.Instance.EndLoad(ok);

        if (!ok)
        {
            ClearSpawn();
            return false;
        }

        // Normaliza a escala do modelo se necessário
        NormalizeModelScale(_currentContainer);

        // mede FPS + upsert CSV
        if (Metrics.Instance != null)
        {
            await Metrics.Instance.MeasureFpsWindow(Metrics.Instance.fpsWindowSeconds);
            Metrics.Instance.WriteCsv(); // (com upsert, ver patch abaixo)
        }
        return true;
    }

    // Compressão "dos dois" para um modelo (usado pelo wizard)
    public async Task<bool> CompressBothAsyncFor(string modelName)
    {
        string root = UApp.streamingAssetsPath;
        string baseOriginal = GetFileNameFor(modelName, "original") ?? "model.glb";
        string original = System.IO.Path.Combine(root, "Models", modelName, "original", baseOriginal);
        string outDraco = System.IO.Path.Combine(root, "Models", modelName, "draco",    baseOriginal);
        string outMesh  = System.IO.Path.Combine(root, "Models", modelName, "meshopt",  baseOriginal);
        System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(outDraco));
        System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(outMesh));

        bool ok1 = await CompressDracoAsync(original, outDraco);
        bool ok2 = await CompressMeshoptAsync(original, outMesh);

        // registra para aparecer nas variantes sem reabrir a cena
        if (ok1) RegisterVariantFile(modelName, "draco", System.IO.Path.GetFileName(outDraco));
        if (ok2) RegisterVariantFile(modelName, "meshopt", System.IO.Path.GetFileName(outMesh));
        return ok1 && ok2;
    }

    // Verifica se um modelo já existe
    public bool ModelExists(string modelName)
    {
        if (string.IsNullOrEmpty(modelName)) return false;
        return _fileByModelVariant.ContainsKey(modelName);
    }

    // Verifica se um modelo já tem variantes comprimidas
    public bool HasCompressedVariants(string modelName)
    {
        if (string.IsNullOrEmpty(modelName) || !_fileByModelVariant.TryGetValue(modelName, out var map))
            return false;
        
        return map.ContainsKey("draco") || map.ContainsKey("meshopt");
    }

    // Retorna lista de todos os modelos disponíveis
    public System.Collections.Generic.List<string> GetAllAvailableModels()
    {
        return new System.Collections.Generic.List<string>(_modelNames);
    }

    // Retorna o modelo atualmente selecionado no dropdown
    public string GetCurrentSelectedModel()
    {
        return GetSelectedModelName();
    }

    // Retorna a variante atualmente selecionada no dropdown
    public string GetCurrentSelectedVariant()
    {
        var modelName = GetSelectedModelName();
        if (string.IsNullOrEmpty(modelName)) return null;
        
        var variants = GetAvailableVariants(modelName);
        if (variants.Count == 0) return null;
        
        int idx = Mathf.Clamp(dropdownVariant.value, 0, variants.Count - 1);
        return variants[idx];
    }

    // Define o modelo selecionado programaticamente
    public void SetSelectedModel(string modelName)
    {
        if (string.IsNullOrEmpty(modelName)) return;
        
        int index = _modelNames.IndexOf(modelName);
        if (index >= 0)
        {
            dropdownModel.value = index;
            PopulateVariantsForCurrentModel();
        }
    }

    // Define a variante selecionada programaticamente
    public void SetSelectedVariant(string variantName)
    {
        if (string.IsNullOrEmpty(variantName)) return;
        
        var modelName = GetSelectedModelName();
        if (string.IsNullOrEmpty(modelName)) return;
        
        var variants = GetAvailableVariants(modelName);
        int index = variants.IndexOf(variantName);
        if (index >= 0)
        {
            dropdownVariant.value = index;
            dropdownVariant.RefreshShownValue();
        }
    }

    public string ResolvePath(string modelName, string variant)
    {
        string fileName = GetFileNameFor(modelName, variant) ?? "model.glb";
        return System.IO.Path.Combine(UApp.streamingAssetsPath, "Models", modelName, variant, fileName);
    }

    // Carrega o modelo sem rodar métricas (o Wizard cuida das métricas)
    /// <summary>
    /// Valida se um arquivo GLB/GLTF é válido antes do carregamento
    /// </summary>
    /// <param name="filePath">Caminho do arquivo</param>
    /// <returns>True se válido, false caso contrário</returns>
    private bool ValidateGltfFile(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                UDebug.LogError($"[ValidateGltfFile] Arquivo não encontrado: {filePath}");
                return false;
            }

            // Verificar tamanho do arquivo
            FileInfo fileInfo = new FileInfo(filePath);
            if (fileInfo.Length == 0)
            {
                UDebug.LogError($"[ValidateGltfFile] Arquivo vazio: {filePath}");
                return false;
            }

            // Verificar extensão
            string extension = Path.GetExtension(filePath).ToLower();
            if (extension != ".glb" && extension != ".gltf")
            {
                UDebug.LogError($"[ValidateGltfFile] Extensão inválida: {extension}");
                return false;
            }

            // Para arquivos GLB, verificar se começam com o magic number correto
            if (extension == ".glb")
            {
                using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    byte[] header = new byte[4];
                    fs.Read(header, 0, 4);
                    
                    // GLB deve começar com "glTF" (0x46546C67)
                    if (header[0] != 0x67 || header[1] != 0x6C || header[2] != 0x54 || header[3] != 0x46)
                    {
                        UDebug.LogError($"[ValidateGltfFile] Magic number inválido para GLB: {filePath}");
                        return false;
                    }
                }
            }

            UDebug.Log($"[ValidateGltfFile] ✅ Arquivo válido: {filePath} ({fileInfo.Length} bytes)");
            return true;
        }
        catch (System.Exception ex)
        {
            UDebug.LogError($"[ValidateGltfFile] Erro na validação: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> LoadOnlyAsync(string modelName, string variant)
    {
        try
        {
            string path = ResolvePath(modelName, variant);
            UDebug.Log($"[LoadOnlyAsync] Carregando: {modelName} ({variant})");
            UDebug.Log($"[LoadOnlyAsync] Caminho: {path}");
            
            if (!System.IO.File.Exists(path))
            {
                UDebug.LogError($"[LoadOnlyAsync] ❌ Arquivo não encontrado: {path}");
                return false;
            }

            // Validar arquivo antes do carregamento
            if (!ValidateGltfFile(path))
            {
                UDebug.LogError($"[LoadOnlyAsync] ❌ Arquivo GLTF inválido: {path}");
                return false;
            }

            // Verifica tamanho do arquivo
            long fileSize = new System.IO.FileInfo(path).Length;
            UDebug.Log($"[LoadOnlyAsync] Tamanho do arquivo: {fileSize} bytes");

            ClearSpawn();

            _currentContainer = new GameObject($"GLTF_{modelName}_{variant}");
            _currentContainer.transform.SetParent(spawnParent, false);

            var gltf = _currentContainer.AddComponent<GltfAsset>();
            gltf.LoadOnStartup = false;

            // URL correta para Unity
            string url = "file://" + path.Replace("\\", "/");
            UDebug.Log($"[LoadOnlyAsync] URL: {url}");
            UDebug.Log($"[LoadOnlyAsync] 🎯 TENTANDO CARREGAR ARQUIVO: {path}");

            bool ok = false;
            try 
            { 
                UDebug.Log($"[LoadOnlyAsync] Iniciando carregamento GLTF...");
                ok = await gltf.Load(url);
                UDebug.Log($"[LoadOnlyAsync] Carregamento concluído: {(ok ? "✅ Sucesso" : "❌ Falha")}");
            }
            catch (System.SystemException ex) 
            { 
                UDebug.LogError($"[LoadOnlyAsync] ❌ Erro no carregamento: {ex.Message}");
                UDebug.LogError($"[LoadOnlyAsync] Stack trace: {ex.StackTrace}");
                ok = false; 
            }
            catch (System.Exception ex)
            {
                UDebug.LogError($"[LoadOnlyAsync] ❌ Erro inesperado: {ex.Message}");
                UDebug.LogError($"[LoadOnlyAsync] Stack trace: {ex.StackTrace}");
                ok = false;
            }

            if (!ok) 
            {
                UDebug.LogError($"[LoadOnlyAsync] ❌ Limpando spawn devido ao erro");
                ClearSpawn(); 
                
                // Notificar erro via eventos
                GameEvents.OnModelLoadError?.Invoke(modelName, variant, "Falha no parsing do arquivo GLTF");
            }
            else
            {
                UDebug.Log($"[LoadOnlyAsync] ✅ Modelo carregado com sucesso!");
                
                // Normaliza a escala do modelo se necessário
                NormalizeModelScale(_currentContainer);
                
                // Notificar sucesso via eventos
                GameEvents.OnModelLoaded?.Invoke(modelName, variant);
            }
            
            return ok;
        }
        catch (System.Exception ex)
        {
            UDebug.LogError($"[LoadOnlyAsync] ❌ Erro geral: {ex.Message}");
            UDebug.LogError($"[LoadOnlyAsync] Stack trace: {ex.StackTrace}");
            ClearSpawn();
            
            // Notificar erro via eventos
            GameEvents.OnModelLoadError?.Invoke(modelName, variant, ex.Message);
            
            return false;
        }
    }

}
