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
    private const string GLTF_TRANSFORM_LINUX = "/home/wands/.nvm/versions/node/v22.19.0/bin/gltf-transform";
    private const string GLTFPACK_LINUX      = "/usr/bin/gltfpack";
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

    string GetFileNameFor(string modelName, string variant)
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
            if (!File.Exists(path))
            {
                SetStatus($"Arquivo não encontrado: {variant}/{fileName}.");
                return;
            }

            string url = "file://" + path.Replace("\\", "/");

            // Iniciar medição de métricas
            if (Metrics.Instance != null)
            {
                Metrics.Instance.BeginLoad(modelName, variant, path);
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
                
                // Define o target da câmera para o modelo carregado
                if (orbitCamera != null && _currentContainer != null)
                {
                    orbitCamera.SetTarget(_currentContainer.transform);
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

        string original = Path.Combine(root, "Models", modelName, "original", baseOriginal);
        string outDraco = Path.Combine(root, "Models", modelName, "draco", baseDraco);
        string outMesh = Path.Combine(root, "Models", modelName, "meshopt", baseMesh);
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
                if (code == 0) UDebug.Log(so); else UDebug.LogError(se);
                tcs.TrySetResult(code);
            }
            catch (Exception ex) { UDebug.LogError(ex); tcs.TrySetResult(-1); }
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
        Directory.CreateDirectory(Path.GetDirectoryName(output)!);

#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
        string exe = GetGltfpackExeWin(); // absoluto
        if (!File.Exists(exe))
        {
            UDebug.LogError($"gltfpack.exe não encontrado: {exe}. Coloque em Assets/StreamingAssets/Tools/");
            return false;
        }
        string args = $"-i {Q(Path.GetFullPath(input))} -o {Q(Path.GetFullPath(output))} -cc";
#elif UNITY_EDITOR_LINUX || UNITY_STANDALONE_LINUX
        string exe = GLTFPACK_LINUX;      // absoluto
        string args = $"-i {Q(input)} -o {Q(output)} -cc";
#else
        string exe = "gltfpack";          // fallback
        string args = $"-i {Q(input)} -o {Q(output)} -cc";
#endif

        int code = await RunProcessAsync(exe, args);
        return code == 0;
    }


    private static string WinPath(string p) => p.Replace('/', '\\');

    private async Task<bool> CompressDracoAsync(string input, string output)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(output)!);

#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
    // 1) Caminho absoluto do .cmd
    string cmdPath = Environment.ExpandEnvironmentVariables(@"%APPDATA%\npm\gltf-transform.cmd");
    if (!File.Exists(cmdPath))
    {
        UDebug.LogError($"gltf-transform.cmd não encontrado: {cmdPath}\nInstale com: npm i -g @gltf-transform/cli");
        return false;
    }

    // 2) Normaliza caminhos para BACKSLASH e usa aspas
    string inPath  = WinPath(Path.GetFullPath(input));
    string outPath = WinPath(Path.GetFullPath(output));

    // 3) QUOTING CORRETO: /c ""C:\...\gltf-transform.cmd" optimize "in" "out" --compress draco"
    string file = Environment.GetEnvironmentVariable("ComSpec") ?? "cmd.exe";
    string args = $"/c \"\"{cmdPath}\" optimize {Q(inPath)} {Q(outPath)} --compress draco\"";
#else
        string file = "/home/wands/.nvm/versions/node/v22.19.0/bin/gltf-transform"; // ajuste se mudar a versão
        string args = $"optimize {Q(input)} {Q(output)} --compress draco";
#endif

        int code = await RunProcessAsync(file, args);
        return code == 0;
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
        if (Metrics.Instance.saveInsideProject)
        {
            var projectRoot = Directory.GetParent(UApp.dataPath)!.FullName;
            return Path.Combine(projectRoot, Metrics.Instance.projectSubDir, Metrics.Instance.csvFileName);
        }
        return Path.Combine(UApp.persistentDataPath, "Benchmarks", Metrics.Instance.csvFileName);
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

    // ======== MÉTODOS PÚBLICOS PARA WIZARD ========

    // Permite outro script pedir um re-scan
    public void RescanModels() => ScanModelsAndPopulateUI();

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
    public async Task<bool> LoadOnlyAsync(string modelName, string variant)
    {
        string path = ResolvePath(modelName, variant);
        if (!System.IO.File.Exists(path)) return false;

        ClearSpawn();

        _currentContainer = new GameObject($"GLTF_{modelName}_{variant}");
        _currentContainer.transform.SetParent(spawnParent, false);

        var gltf = _currentContainer.AddComponent<GltfAsset>();
        gltf.LoadOnStartup = false;

        string url = "file://" + path.Replace("\\", "/");
        bool ok = false;
        try { ok = await gltf.Load(url); }
        catch (System.SystemException ex) { UDebug.LogError(ex); ok = false; }

        if (!ok) ClearSpawn();
        return ok;
    }

}
