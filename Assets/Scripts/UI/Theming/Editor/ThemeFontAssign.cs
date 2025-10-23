// Tools to auto-assign Roboto fonts to the selected UiTheme
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using TMPro;

public static class ThemeFontAssign {
    [MenuItem("Tools/PolyDiet UI/Assign Roboto to Theme")]
    public static void AssignRoboto(){
        var theme = Selection.activeObject as UiTheme;
        if (!theme){
            EditorUtility.DisplayDialog("Assign Roboto to Theme", "Selecione um asset UiTheme na Project Window e rode o comando novamente.", "Ok");
            return;
        }
        var regular = FindFontAsset("Roboto-Regular");
        var medium  = FindFontAsset("Roboto-Medium");
        var bold    = FindFontAsset("Roboto-Bold");
        if (regular) theme.fontRegular = regular;
        if (medium)  theme.fontMedium  = medium;
        if (bold)    theme.fontBold    = bold;
        EditorUtility.SetDirty(theme);
        AssetDatabase.SaveAssets();
        EditorGUIUtility.PingObject(theme);
        Debug.Log("Roboto atribuído ao tema (quando encontrado)." );
    }

    private static TMP_FontAsset FindFontAsset(string name){
        var guids = AssetDatabase.FindAssets($"t:{nameof(TMP_FontAsset)} {name}");
        foreach (var guid in guids){
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var fa = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            if (fa && fa.name.Contains(name)) return fa;
        }
        return null;
    }
}
#endif
