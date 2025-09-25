using UnityEngine;
using System.IO;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;

public class LinuxCompatibilityTester : EditorWindow
{
    [MenuItem("Tools/Test Linux Compatibility")]
    public static void ShowWindow()
    {
        GetWindow<LinuxCompatibilityTester>("Linux Compatibility Test");
    }

    void OnGUI()
    {
        EditorGUILayout.LabelField("Linux Compatibility Test", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Executable Detection:", EditorStyles.boldLabel);

#if UNITY_EDITOR_LINUX
        // Test gltf-transform detection
        string gltfTransform = DetectGltfTransform();
        EditorGUILayout.LabelField("gltf-transform:", gltfTransform);
        EditorGUILayout.LabelField("Exists:", File.Exists(gltfTransform) ? "✅ Yes" : "❌ No");

        // Test gltfpack detection
        string gltfpack = DetectGltfpack();
        EditorGUILayout.LabelField("gltfpack:", gltfpack);
        EditorGUILayout.LabelField("Exists:", File.Exists(gltfpack) ? "✅ Yes" : "❌ No");
#else
        EditorGUILayout.LabelField("This test is designed for Linux Editor only");
#endif

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Unity Platform:", Application.platform.ToString());
        EditorGUILayout.LabelField("Data Path:", Application.dataPath);
        EditorGUILayout.LabelField("Persistent Data Path:", Application.persistentDataPath);
        EditorGUILayout.LabelField("Streaming Assets Path:", Application.streamingAssetsPath);
        
        EditorGUILayout.Space();
        if (GUILayout.Button("Log Test Results"))
        {
            LogTestResults();
        }
    }

#if UNITY_EDITOR_LINUX
    private string DetectGltfTransform()
    {
        var nvmPath = System.Environment.GetEnvironmentVariable("HOME");
        if (!string.IsNullOrEmpty(nvmPath))
        {
            var nvmGltfTransform = Path.Combine(nvmPath, ".nvm/versions/node/v22.19.0/bin/gltf-transform");
            if (File.Exists(nvmGltfTransform)) return nvmGltfTransform;
            
            var nodeVersionsDir = Path.Combine(nvmPath, ".nvm/versions/node");
            if (Directory.Exists(nodeVersionsDir))
            {
                var versions = Directory.GetDirectories(nodeVersionsDir)
                    .OrderByDescending(d => d)
                    .ToArray();
                
                foreach (var versionDir in versions)
                {
                    var gltfTransformPath = Path.Combine(versionDir, "bin/gltf-transform");
                    if (File.Exists(gltfTransformPath)) return gltfTransformPath;
                }
            }
        }
        return "gltf-transform";
    }
    
    private string DetectGltfpack()
    {
        if (File.Exists("/usr/bin/gltfpack")) return "/usr/bin/gltfpack";
        if (File.Exists("/usr/local/bin/gltfpack")) return "/usr/local/bin/gltfpack";
        return "gltfpack";
    }
#endif

    private void LogTestResults()
    {
        Debug.Log("=== Linux Compatibility Test Results ===");
        Debug.Log($"Platform: {Application.platform}");
        Debug.Log($"Unity Version: {Application.unityVersion}");
        Debug.Log($"Data Path: {Application.dataPath}");
        Debug.Log($"StreamingAssets Path: {Application.streamingAssetsPath}");
        
#if UNITY_EDITOR_LINUX
        string gltfTransform = DetectGltfTransform();
        string gltfpack = DetectGltfpack();
        
        Debug.Log($"gltf-transform: {gltfTransform} (exists: {File.Exists(gltfTransform)})");
        Debug.Log($"gltfpack: {gltfpack} (exists: {File.Exists(gltfpack)})");
        
        // Test directory structure
        string modelsDir = Path.Combine(Application.streamingAssetsPath, "Models");
        Debug.Log($"Models directory: {modelsDir} (exists: {Directory.Exists(modelsDir)})");
        
        if (Directory.Exists(modelsDir))
        {
            var modelDirs = Directory.GetDirectories(modelsDir);
            Debug.Log($"Found {modelDirs.Length} model directories:");
            foreach (var dir in modelDirs)
            {
                Debug.Log($"  - {Path.GetFileName(dir)}");
            }
        }
#endif
        Debug.Log("=== End Test Results ===");
    }
}
#endif