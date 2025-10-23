using UnityEditor;
using UnityEngine;

/// <summary>
/// Script de editor para forçar o ReportRunner a usar advanced_metrics_report.py
/// Execute via: Tools > Fix ReportRunner Script Path
/// </summary>
public class FixReportRunnerScript : EditorWindow
{
    [MenuItem("Tools/Fix ReportRunner Script Path")]
    public static void FixScriptPath()
    {
        ReportRunner[] runners = FindObjectsOfType<ReportRunner>();
        
        if (runners.Length == 0)
        {
            Debug.LogWarning("[FixReportRunner] Nenhum ReportRunner encontrado na cena. Abra a cena ModelViewer primeiro.");
            return;
        }
        
        foreach (ReportRunner runner in runners)
        {
            // Limpar o scriptPath para forçar uso do padrão (advanced)
            SerializedObject so = new SerializedObject(runner);
            SerializedProperty scriptPathProp = so.FindProperty("scriptPath");
            
            string currentPath = scriptPathProp.stringValue;
            Debug.Log($"[FixReportRunner] ReportRunner atual scriptPath: '{currentPath}'");
            
            if (!string.IsNullOrEmpty(currentPath) && currentPath.Contains("simple_report_generator.py"))
            {
                scriptPathProp.stringValue = "";
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(runner);
                
                Debug.Log($"<color=green>[FixReportRunner] ✓ Script path limpo! Agora usará advanced_metrics_report.py por padrão.</color>");
            }
            else if (string.IsNullOrEmpty(currentPath))
            {
                Debug.Log($"<color=green>[FixReportRunner] ✓ Script path já está vazio. Usará advanced_metrics_report.py por padrão.</color>");
            }
            else
            {
                Debug.Log($"[FixReportRunner] Script path atual: {currentPath}");
            }
        }
        
        // Salvar a cena
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene()
        );
        
        Debug.Log("<color=cyan>[FixReportRunner] Concluído! Salve a cena (Ctrl+S) para persistir as mudanças.</color>");
    }
}


