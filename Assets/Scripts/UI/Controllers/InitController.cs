using System;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace PolyDiet.UI.Controllers
{
    public class StartScreenController : MonoBehaviour
    {
        [Header("UI")]
        public Button buttonStart;
        public Button buttonDatabase;
        public TMP_Text versionLabel;

        [Header("Navegação")]
        [Tooltip("Nome da cena principal (padrão: ModelViewer)")]
        public string mainSceneName = "ModelViewer";
        [Tooltip("Se informado, Iniciar apenas esconde este painel e mostra o root da página principal")]
        public GameObject mainPageRoot;
        [Tooltip("Raiz do painel/tela inicial para ocultar após iniciar (opcional)")]
        public GameObject startPanelRoot;

        [Header("Configuração")]
        [Tooltip("Configuração centralizada do PolyDiet (opcional)")]
        public PolyDietConfig config;

        [Header("Base de Dados")]
        [Tooltip("URL ou caminho local da base de dados (deixe vazio para usar PolyDietConfig ou pasta de Benchmarks padrão)")]
        public string databaseUrlOrPath = "";

        void Awake()
        {
            if (buttonStart) buttonStart.onClick.AddListener(OnClickStart);
            if (buttonDatabase) buttonDatabase.onClick.AddListener(OnClickDatabase);
        }

        void Start()
        {
            // Exibe versão do app e do Unity
            string product = Application.productName;
            string appVer = string.IsNullOrEmpty(Application.version) ? "dev" : Application.version;
            string unityVer = Application.unityVersion;
            if (versionLabel) versionLabel.SetText($"{product} v{appVer} — Unity {unityVer}");
        }

        void OnClickStart()
        {
            // Se a página principal já está nesta cena, só alterna visibilidade
            if (mainPageRoot != null)
            {
                mainPageRoot.SetActive(true);
                if (startPanelRoot != null) startPanelRoot.SetActive(false);
                return;
            }

            // Caso contrário, carrega a cena principal (atual: "ModelViewer")
            if (!string.IsNullOrWhiteSpace(mainSceneName))
            {
                SceneManager.LoadScene(mainSceneName, LoadSceneMode.Single);
            }
        }

        void OnClickDatabase()
        {
            string target = ResolveDatabaseTarget();
            try
            {
                Application.OpenURL(FormatForOpenURL(target));
            }
            catch (Exception ex)
            {
                Debug.LogError($"[StartScreen] Falha ao abrir base de dados: {target} - {ex.Message}");
            }
        }

        string ResolveDatabaseTarget()
        {
            // Usar override se fornecido
            if (!string.IsNullOrWhiteSpace(databaseUrlOrPath))
                return databaseUrlOrPath;

            // Tentar usar config
            var cfg = config != null ? config : PolyDietConfigManager.GetConfig();
            if (!string.IsNullOrWhiteSpace(cfg.repositoryUrl))
                return cfg.repositoryUrl;

            // Padrão: pasta de Benchmarks usada pelo sistema de métricas
            // (mesmo fallback usado no MetricsViewer)
            var dir = Path.Combine(Application.persistentDataPath, "Benchmarks");
            Directory.CreateDirectory(dir);
            return dir;
        }

        static string FormatForOpenURL(string pathOrUrl)
        {
            if (pathOrUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                pathOrUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                return pathOrUrl;

            string p = pathOrUrl.Replace("\\", "/");
            return p.StartsWith("file://", StringComparison.OrdinalIgnoreCase) ? p : "file://" + p;
        }
    }
}