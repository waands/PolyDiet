using GLTFast;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using static System.Net.Mime.MediaTypeNames;
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

    // NOVO: botões de compressão
    public UI.Button buttonCompressDraco;
    public UI.Button buttonCompressMeshopt;

    // NOVO: caminhos dos CLIs (preencher no Inspector)
    [Header("CLI Paths")]
    [Tooltip("Ex.: Assets/Tools/gltfpack.exe")]
    public string gltfpackPath = "Assets/Tools/gltfpack.exe"; // Meshopt
    [Tooltip("Ex.: C:/Users/<voce>/AppData/Roaming/npm/gltf-transform.cmd OU .ps1")]
    public string gltfTransformPath = @"C:\Users\wande\AppData\Roaming\npm\gltf-transform.cmd"; // Draco

    [Header("Scene")]
    public Transform spawnParent;

    [System.Serializable]
    public class ModelEntry
    {
        public string name = "Duck";        // pasta do modelo
        public string fileName = "model.glb"; // nome do arquivo dentro de cada variante
    }
    public ModelEntry[] models = new ModelEntry[] {
        new ModelEntry { name = "Duck", fileName = "model.glb" }
    };

    private readonly string[] _variants = new[] { "original", "draco", "meshopt" };
    private GameObject _currentContainer;

    void Awake()
    {
        // Popular dropdowns
        dropdownModel.ClearOptions();
        dropdownModel.AddOptions(models.Select(m => m.name).ToList());
        dropdownVariant.ClearOptions();
        dropdownVariant.AddOptions(_variants.ToList());

        buttonLoad.onClick.AddListener(() => _ = OnClickLoadAsync());

        // NOVO: listeners dos botões de compressão
        if (buttonCompressDraco != null)
            buttonCompressDraco.onClick.AddListener(() => _ = OnClickCompressDracoAsync());
        if (buttonCompressMeshopt != null)
            buttonCompressMeshopt.onClick.AddListener(() => _ = OnClickCompressMeshoptAsync());

        SetStatus("Pronto.");
    }

    private void SetStatus(string msg)
    {
        if (textStatus != null) textStatus.text = msg;
    }

    private void ClearSpawn()
    {
        if (_currentContainer != null)
        {
            Destroy(_currentContainer);
            _currentContainer = null;
        }
        for (int i = spawnParent.childCount - 1; i >= 0; i--)
            Destroy(spawnParent.GetChild(i).gameObject);
    }

    private async Task OnClickLoadAsync()
    {
        buttonLoad.interactable = false;
        SetStatus("Carregando...");

        ClearSpawn();

        var model = models[Mathf.Clamp(dropdownModel.value, 0, models.Length - 1)];
        var variant = _variants[Mathf.Clamp(dropdownVariant.value, 0, _variants.Length - 1)];

        string root = UApp.streamingAssetsPath;
        string path = Path.Combine(root, "Models", model.name, variant, model.fileName);
        string url = "file://" + path.Replace("\\", "/");

        _currentContainer = new GameObject($"GLTF_{model.name}_{variant}");
        _currentContainer.transform.SetParent(spawnParent, false);

        var gltf = _currentContainer.AddComponent<GltfAsset>();
        gltf.LoadOnStartup = false;

        bool ok = false;
        try { ok = await gltf.Load(url); }
        catch (System.SystemException ex) { UDebug.LogError(ex); ok = false; }

        if (!ok)
        {
            SetStatus($"Falha ao carregar: {model.name}/{variant}");
            ClearSpawn();
        }
        else
        {
            SetStatus($"Carregado: {model.name} ({variant})");
        }

        buttonLoad.interactable = true;
    }

    // =========================
    //  COMPRESSÃO (CLI)
    // =========================

    private (string inputOriginal, string outDraco, string outMeshopt) GetCurrentPaths()
    {
        var model = models[Mathf.Clamp(dropdownModel.value, 0, models.Length - 1)];
        string root = UApp.streamingAssetsPath;

        string original = Path.Combine(root, "Models", model.name, "original", model.fileName);
        string outDraco = Path.Combine(root, "Models", model.name, "draco", model.fileName);
        string outMesh = Path.Combine(root, "Models", model.name, "meshopt", model.fileName);
        return (original, outDraco, outMesh);
    }

    private (string file, string args) WrapIfNeeded(string exe, string args)
    {
        // Aceita .exe/.cmd/.bat/.ps1
        var ext = Path.GetExtension(exe).ToLowerInvariant();
        if (ext == ".ps1") return ("powershell.exe", $"-ExecutionPolicy Bypass -File \"{exe}\" {args}");
        if (ext == ".cmd" || ext == ".bat") return ("cmd.exe", $"/c \"\"{exe}\" {args}\"");
        return (exe, args); // .exe ou nome no PATH
    }

    private Task<int> RunProcessAsync(string file, string args)
    {
        var tcs = new TaskCompletionSource<int>();
        var psi = new Diagnostics.ProcessStartInfo(file, args)
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            WorkingDirectory = Directory.GetCurrentDirectory()
        };
        var p = new Diagnostics.Process { StartInfo = psi, EnableRaisingEvents = true };
        p.Exited += (s, e) =>
        {
            int code = p.ExitCode;
            string stdout = p.StandardOutput.ReadToEnd();
            string stderr = p.StandardError.ReadToEnd();
            if (code == 0) UDebug.Log(stdout); else UDebug.LogError(stderr);
            p.Dispose();
            tcs.TrySetResult(code);
        };
        try { p.Start(); } catch (System.Exception ex) { UDebug.LogError(ex); tcs.TrySetResult(-1); }
        return tcs.Task;
    }

    private async Task<bool> CompressMeshoptAsync(string input, string output)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(output));
        var (file, args) = WrapIfNeeded(gltfpackPath, $"-i \"{input}\" -o \"{output}\" -cc");
        int code = await RunProcessAsync(file, args);
        return code == 0;
    }

    private async Task<bool> CompressDracoAsync(string input, string output)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(output));
        var (file, args) = WrapIfNeeded(gltfTransformPath, $"optimize \"{input}\" \"{output}\" --compress draco");
        int code = await RunProcessAsync(file, args);
        return code == 0;
    }

    private async Task OnClickCompressMeshoptAsync()
    {
        if (buttonCompressMeshopt) buttonCompressMeshopt.interactable = false;
        var (original, _, outMesh) = GetCurrentPaths();

        if (!File.Exists(original))
        {
            SetStatus("Não achei o original. Verifique a pasta original/model.glb");
            if (buttonCompressMeshopt) buttonCompressMeshopt.interactable = true;
            return;
        }

        SetStatus("Comprimindo (Meshopt)...");
        bool ok = await CompressMeshoptAsync(original, outMesh);
        SetStatus(ok ? "Meshopt OK" : "Meshopt FAIL (veja Console)");
        if (buttonCompressMeshopt) buttonCompressMeshopt.interactable = true;
    }

    private async Task OnClickCompressDracoAsync()
    {
        if (buttonCompressDraco) buttonCompressDraco.interactable = false;
        var (original, outDraco, _) = GetCurrentPaths();

        if (!File.Exists(original))
        {
            SetStatus("Não achei o original. Verifique a pasta original/model.glb");
            if (buttonCompressDraco) buttonCompressDraco.interactable = true;
            return;
        }

        SetStatus("Comprimindo (Draco)...");
        bool ok = await CompressDracoAsync(original, outDraco);
        SetStatus(ok ? "Draco OK" : "Draco FAIL (veja Console)");
        if (buttonCompressDraco) buttonCompressDraco.interactable = true;
    }
}
