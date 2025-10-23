// Editor utility to create UiTheme assets via menu
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;

public static class ThemeAssetCreator {
    [MenuItem("Assets/Create/UI/Theme (Dashboard Light)")]
    public static void CreateThemeAsset() {
        CreateThemeAtSelection("DashboardTheme_Light.asset");
    }

    [MenuItem("Tools/PolyDiet UI/Create Theme (Dashboard Light)")]
    public static void CreateThemeAssetTools() {
        CreateThemeAtPath("Assets/Resources/Themes/DashboardTheme_Light.asset");
    }

    private static void CreateThemeAtSelection(string defaultName) {
        var path = GetSelectedPathOrFallback();
        var assetPathAndName = AssetDatabase.GenerateUniqueAssetPath(Path.Combine(path, defaultName));
        CreateThemeAtPath(assetPathAndName);
    }

    private static void CreateThemeAtPath(string assetPath) {
        var theme = ScriptableObject.CreateInstance<UiTheme>();
        EnsureDirectory(assetPath);
        AssetDatabase.CreateAsset(theme, assetPath);
        AssetDatabase.SaveAssets();
        Selection.activeObject = theme;
        EditorGUIUtility.PingObject(theme);
    }

    private static void EnsureDirectory(string assetPath) {
        var dir = Path.GetDirectoryName(assetPath);
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
    }

    private static string GetSelectedPathOrFallback() {
        var path = "Assets";
        foreach (var obj in Selection.GetFiltered(typeof(Object), SelectionMode.Assets)) {
            path = AssetDatabase.GetAssetPath(obj);
            if (File.Exists(path)) { path = Path.GetDirectoryName(path); break; }
        }
        return path;
    }
}
#endif
