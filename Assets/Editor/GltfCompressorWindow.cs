#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

using Diagnostics = System.Diagnostics;   // <- alias p/ System.Diagnostics
using UDebug = UnityEngine.Debug;         // <- alias p/ Unity Debug

public class GltfCompressorWindow : EditorWindow
{
    string streamingModelsRoot;
    // Ajuste se quiser:
    string gltfpackExe = "Assets/Tools/gltfpack.exe"; // Meshopt
    string gltfTransformExe = "gltf-transform";       // Draco (global via npm)

    [MenuItem("Tools/GLB Compressor")]
    public static void Open() => GetWindow<GltfCompressorWindow>("GLB Compressor");

    void OnEnable() => streamingModelsRoot = Path.Combine(Application.dataPath, "StreamingAssets/Models");

    void OnGUI()
    {
        streamingModelsRoot = EditorGUILayout.TextField("Models Root", streamingModelsRoot);
        gltfpackExe = EditorGUILayout.TextField("gltfpack (meshopt)", gltfpackExe);
        gltfTransformExe = EditorGUILayout.TextField("gltf-transform (draco)", gltfTransformExe);

        EditorGUILayout.Space();
        if (GUILayout.Button("Comprimir TODOS (original → draco & meshopt)")) CompressAll();
    }

    void CompressAll()
    {
        if (!Directory.Exists(streamingModelsRoot))
        {
            Debug.LogWarning("Pasta Models não encontrada: " + streamingModelsRoot);
            return;
        }

        foreach (var modelDir in Directory.GetDirectories(streamingModelsRoot))
        {
            var original = Path.Combine(modelDir, "original", "model.glb");
            if (!File.Exists(original)) { Debug.LogWarning("Sem original: " + original); continue; }

            var dracoDir = Path.Combine(modelDir, "draco");
            var meshoptDir = Path.Combine(modelDir, "meshopt");
            Directory.CreateDirectory(dracoDir);
            Directory.CreateDirectory(meshoptDir);

            var outDraco = Path.Combine(dracoDir, "model.glb");
            var outMesh = Path.Combine(meshoptDir, "model.glb");

            // Meshopt (gltfpack): -cc = compressão extra
            RunCli(gltfpackExe, $"-i \"{original}\" -o \"{outMesh}\" -cc", "Meshopt OK", "Meshopt FAIL");

            // Draco (gltf-transform): optimize + --compress draco
            RunCli(gltfTransformExe, $"optimize \"{original}\" \"{outDraco}\" --compress draco", "Draco OK", "Draco FAIL");
        }

        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("GLB Compressor", "Concluído!", "OK");
    }

    static void RunCli(string exe, string args, string okMsg, string errMsg)
    {
        try
        {
            var psi = new Diagnostics.ProcessStartInfo(exe, args)
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                WorkingDirectory = Directory.GetCurrentDirectory()
            };
            using var p = Diagnostics.Process.Start(psi);
            string stdout = p.StandardOutput.ReadToEnd();
            string stderr = p.StandardError.ReadToEnd();
            p.WaitForExit();

            if (p.ExitCode == 0) UDebug.Log($"{okMsg}\n{stdout}");
            else UDebug.LogError($"{errMsg}\n{stderr}");
        }
        catch (System.Exception e)
        {
            UDebug.LogError($"{errMsg}\n{e}");
        }
    }
}
#endif
