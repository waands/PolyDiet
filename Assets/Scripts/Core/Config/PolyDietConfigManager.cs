using UnityEngine;

/// <summary>
/// Gerenciador singleton para acessar a configuração do PolyDiet
/// Procura por um asset PolyDietConfig nos Resources ou cria um padrão
/// </summary>
public static class PolyDietConfigManager
{
    private static PolyDietConfig _config;
    private static bool _searched = false;

    /// <summary>
    /// Obtém a configuração do PolyDiet
    /// </summary>
    public static PolyDietConfig GetConfig()
    {
        if (_config != null)
            return _config;

        if (!_searched)
        {
            // Procurar em Resources primeiro (funciona em runtime e editor)
            _config = Resources.Load<PolyDietConfig>("PolyDietConfig");
            
#if UNITY_EDITOR
            // Se não encontrou, procurar em todos os assets (só no editor)
            if (_config == null)
            {
                string[] guids = UnityEditor.AssetDatabase.FindAssets("t:PolyDietConfig");
                if (guids.Length > 0)
                {
                    string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
                    _config = UnityEditor.AssetDatabase.LoadAssetAtPath<PolyDietConfig>(path);
                }
            }
#endif
            _searched = true;
        }

        // Se ainda não encontrou, criar um config padrão em runtime
        if (_config == null)
        {
            Debug.LogWarning("[PolyDietConfig] Nenhum asset PolyDietConfig encontrado. Usando configurações padrão. Crie um asset via: Create > PolyDiet > Config");
            _config = ScriptableObject.CreateInstance<PolyDietConfig>();
        }

        return _config;
    }

    /// <summary>
    /// Força a recarregar a configuração (útil após edições no editor)
    /// </summary>
    public static void Reload()
    {
        _config = null;
        _searched = false;
    }
}

#if UNITY_EDITOR
/// <summary>
/// Editor helper para criar o asset de configuração facilmente
/// </summary>
public class PolyDietConfigEditor
{
    [UnityEditor.MenuItem("Tools/PolyDiet/Criar Config")]
    public static void CreateConfig()
    {
        // Verificar se já existe
        string[] guids = UnityEditor.AssetDatabase.FindAssets("t:PolyDietConfig");
        if (guids.Length > 0)
        {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
            UnityEditor.EditorUtility.DisplayDialog("Config já existe", 
                $"Já existe um PolyDietConfig em:\n{path}\n\nUse esse arquivo para configurar o projeto.", 
                "OK");
            UnityEditor.Selection.activeObject = UnityEditor.AssetDatabase.LoadAssetAtPath<PolyDietConfig>(path);
            return;
        }

        // Criar novo
        PolyDietConfig config = ScriptableObject.CreateInstance<PolyDietConfig>();
        
        // Criar diretório Resources se não existir
        string resourcesPath = "Assets/Resources";
        if (!System.IO.Directory.Exists(resourcesPath))
        {
            System.IO.Directory.CreateDirectory(resourcesPath);
            UnityEditor.AssetDatabase.Refresh();
        }

        string assetPath = "Assets/Resources/PolyDietConfig.asset";
        UnityEditor.AssetDatabase.CreateAsset(config, assetPath);
        UnityEditor.AssetDatabase.SaveAssets();
        UnityEditor.AssetDatabase.Refresh();

        UnityEditor.Selection.activeObject = config;
        UnityEditor.EditorUtility.DisplayDialog("Config criado", 
            $"PolyDietConfig criado em:\n{assetPath}\n\nConfigure todos os caminhos e ferramentas neste arquivo.", 
            "OK");

        Debug.Log($"[PolyDietConfig] Config criado em: {assetPath}");
    }
}
#endif

