using GLTFast;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Collections.Generic; // <— novo
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

    // ===== Descoberta dinâmica =====
    private readonly string[] _allKnownVariants = new[] { "original", "draco", "meshopt" };
    private readonly List<string> _modelNames = new(); // nomes (ex.: "Duck")
    // mapeia: modelo -> (variante -> fileName.glb)
    private readonly Dictionary<string, Dictionary<string, string>> _fileByModelVariant = new();

    // ===== Caminhos dos CLIs (mantenha como estão pra você) =====
#if UNITY_EDITOR_LINUX || UNITY_STANDALONE_LINUX
    private const string GLTF_TRANSFORM = "/home/wands/.nvm/versions/node/v22.19.0/bin/gltf-transform";
    private const string GLTFPACK      = "/usr/bin/gltfpack";
#elif UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
    private const string GLTF_TRANSFORM = "C:\\Users\\wande\\AppData\\Roaming\\npm\\gltf-transform.cmd";
    private static readonly string GLTFPACK = Path.Combine("Assets","Tools","gltfpack.exe");
#else
    private const string GLTF_TRANSFORM = "gltf-transform";
    private const string GLTFPACK      = "gltfpack";
#endif

    private void SetStatus(string msg) => textStatus?.SetText(msg);

    private GameObject _currentContainer; 

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
                            // tenta priorizar algum arquivo com "model" no nome, senão pega o primeiro
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

                    // salvar APENAS o fileName relativo à pasta da variante
                    map[variant] = Path.GetFileName(f);
                }

                // Inclui o modelo se tiver ao menos "original" (recomendado p/ compress)
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
        // Obtém o modelo atual e lista variantes disponíveis na ordem: original, draco, meshopt, (extras)
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

        // ordem canônica
        foreach (var v in _allKnownVariants)
            if (map.ContainsKey(v)) list.Add(v);

        // extras (se algum dia existirem)
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
        SetStatus("Carregando...");

        ClearSpawn();

        var modelName = GetSelectedModelName();
        var variants = GetAvailableVariants(modelName);
        if (string.IsNullOrEmpty(modelName) || variants.Count == 0)
        {
            SetStatus("Nenhum modelo/variante disponível.");
            buttonLoad.interactable = true;
            return;
        }

        string variant = variants[Mathf.Clamp(dropdownVariant.value, 0, variants.Count - 1)];
        string fileName = GetFileNameFor(modelName, variant) ?? "model.glb";

        string root = UApp.streamingAssetsPath;
        string path = Path.Combine(root, "Models", modelName, variant, fileName);

        if (!File.Exists(path))
        {
            SetStatus($"Arquivo não encontrado: {variant}/{fileName}. Gere a variante e tente de novo.");
            buttonLoad.interactable = true;
            return;
        }

        string url = "file://" + path.Replace("\\", "/");

        _currentContainer = new GameObject($"GLTF_{modelName}_{variant}");
        _currentContainer.transform.SetParent(spawnParent, false);

        var gltf = _currentContainer.AddComponent<GltfAsset>();
        gltf.LoadOnStartup = false;

        bool ok = false;
        try { ok = await gltf.Load(url); }
        catch (System.SystemException ex) { UDebug.LogError(ex); ok = false; }

        if (!ok)
        {
            SetStatus($"Falha ao carregar: {modelName}/{variant}");
            ClearSpawn();
        }
        else
        {
            SetStatus($"Carregado: {modelName} ({variant})");
        }

        buttonLoad.interactable = true;
    }

    private (string inputOriginal, string outDraco, string outMeshopt) GetCurrentPaths()
    {
        var modelName = GetSelectedModelName();
        string root = UApp.streamingAssetsPath;

        // fileName base = o do "original" (se existir), senão "model.glb"
        string baseOriginal = GetFileNameFor(modelName, "original") ?? "model.glb";
        string baseDraco    = GetFileNameFor(modelName, "draco")    ?? baseOriginal;
        string baseMesh     = GetFileNameFor(modelName, "meshopt")  ?? baseOriginal;

        string original = Path.Combine(root, "Models", modelName, "original", baseOriginal);
        string outDraco = Path.Combine(root, "Models", modelName, "draco",    baseDraco);
        string outMesh  = Path.Combine(root, "Models", modelName, "meshopt",  baseMesh);
        return (original, outDraco, outMesh);
    }

    // ======== EXEC CLI ========

    private Task<int> RunProcessAsync(string file, string args)
    {
        var tcs = new TaskCompletionSource<int>();
        var psi = new Diagnostics.ProcessStartInfo(file, args)
        {
            UseShellExecute        = false,
            RedirectStandardOutput = true,
            RedirectStandardError  = true,
            CreateNoWindow         = true
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
            UDebug.Log($"[Exec] {file} {args}");
            p.Start();
        }
        catch (Exception ex) { UDebug.LogError(ex); tcs.TrySetResult(-1); }

        return tcs.Task;
    }

    private async Task<bool> CompressMeshoptAsync(string input, string output)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(output));
        string args = $"-i \"{input}\" -o \"{output}\" -cc";
        int code = await RunProcessAsync(GLTFPACK, args);
        return code == 0;
    }

    private async Task<bool> CompressDracoAsync(string input, string output)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(output));
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
        string file = "cmd.exe";
        string args = $"/c \"{GLTF_TRANSFORM}\" optimize \"{input}\" \"{output}\" --compress draco";
#else
        string file = GLTF_TRANSFORM;
        string args = $"optimize \"{input}\" \"{output}\" --compress draco";
#endif
        int code = await RunProcessAsync(file, args);
        return code == 0;
    }

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
        // registra o arquivo no mapa (para aparecer no dropdown)
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
        // registra o arquivo no mapa (para aparecer no dropdown)
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
}
