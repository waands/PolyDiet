using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PolyDiet.Core.ModelLoading.Tools
{
    /// <summary>
    /// Detecta ferramentas externas instaladas no sistema
    /// </summary>
    public static class ToolDetector
    {
        private static readonly bool IsWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        private static readonly bool IsLinux = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
        private static readonly bool IsMacOS = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
        
        /// <summary>
        /// Informações sobre uma ferramenta detectada
        /// </summary>
        public class ToolInfo
        {
            public string Name { get; set; }
            public bool IsInstalled { get; set; }
            public string Path { get; set; }
            public string Version { get; set; }
            public string InstallCommand { get; set; }
        }
        
        /// <summary>
        /// Detecta gltfpack (Meshopt compression)
        /// </summary>
        public static ToolInfo DetectGltfpack()
        {
            var info = new ToolInfo
            {
                Name = "gltfpack",
                InstallCommand = IsWindows 
                    ? "Download from: https://github.com/zeux/meshoptimizer/releases" 
                    : "npm install -g gltfpack"
            };
            
            if (IsWindows)
            {
                // Procura no projeto primeiro
                string projectPath = Path.Combine(
                    Directory.GetParent(Application.dataPath).FullName,
                    "Assets", "StreamingAssets", "Tools", "gltfpack.exe"
                );
                
                if (File.Exists(projectPath))
                {
                    info.IsInstalled = true;
                    info.Path = projectPath;
                    return info;
                }
                
                // Procura no PATH
                string pathEnv = Environment.GetEnvironmentVariable("PATH");
                if (!string.IsNullOrEmpty(pathEnv))
                {
                    foreach (string dir in pathEnv.Split(';'))
                    {
                        string fullPath = Path.Combine(dir, "gltfpack.exe");
                        if (File.Exists(fullPath))
                        {
                            info.IsInstalled = true;
                            info.Path = fullPath;
                            return info;
                        }
                    }
                }
            }
            else if (IsLinux || IsMacOS)
            {
                // Tenta executar 'which gltfpack'
                var result = ExecuteCommand("which", "gltfpack");
                if (result.ExitCode == 0 && !string.IsNullOrWhiteSpace(result.Output))
                {
                    info.IsInstalled = true;
                    info.Path = result.Output.Trim();
                    return info;
                }
                
                // Tenta caminhos comuns
                string[] commonPaths = {
                    "/usr/local/bin/gltfpack",
                    "/usr/bin/gltfpack",
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local/bin/gltfpack")
                };
                
                foreach (string path in commonPaths)
                {
                    if (File.Exists(path))
                    {
                        info.IsInstalled = true;
                        info.Path = path;
                        return info;
                    }
                }
            }
            
            return info;
        }
        
        /// <summary>
        /// Detecta gltf-transform (Draco compression e conversão)
        /// </summary>
        public static ToolInfo DetectGltfTransform()
        {
            var info = new ToolInfo
            {
                Name = "gltf-transform",
                InstallCommand = "npm install -g @gltf-transform/cli"
            };
            
            if (IsWindows)
            {
                // Procura no npm global
                string npmPath = Environment.ExpandEnvironmentVariables(@"%APPDATA%\npm\gltf-transform.cmd");
                if (File.Exists(npmPath))
                {
                    info.IsInstalled = true;
                    info.Path = npmPath;
                    
                    // Tenta obter versão
                    var result = ExecuteCommand(npmPath, "--version");
                    if (result.ExitCode == 0)
                    {
                        info.Version = result.Output.Trim();
                    }
                    
                    return info;
                }
            }
            else if (IsLinux || IsMacOS)
            {
                // Tenta executar 'which gltf-transform'
                var result = ExecuteCommand("which", "gltf-transform");
                if (result.ExitCode == 0 && !string.IsNullOrWhiteSpace(result.Output))
                {
                    info.IsInstalled = true;
                    info.Path = result.Output.Trim();
                    
                    // Tenta obter versão
                    var versionResult = ExecuteCommand(info.Path, "--version");
                    if (versionResult.ExitCode == 0)
                    {
                        info.Version = versionResult.Output.Trim();
                    }
                    
                    return info;
                }
            }
            
            return info;
        }
        
        /// <summary>
        /// Detecta obj2gltf (conversão OBJ → GLTF)
        /// </summary>
        public static ToolInfo DetectObj2Gltf()
        {
            var info = new ToolInfo
            {
                Name = "obj2gltf",
                InstallCommand = "npm install -g obj2gltf"
            };
            
            if (IsWindows)
            {
                string npmPath = Environment.ExpandEnvironmentVariables(@"%APPDATA%\npm\obj2gltf.cmd");
                if (File.Exists(npmPath))
                {
                    info.IsInstalled = true;
                    info.Path = npmPath;
                    return info;
                }
            }
            else if (IsLinux || IsMacOS)
            {
                var result = ExecuteCommand("which", "obj2gltf");
                if (result.ExitCode == 0 && !string.IsNullOrWhiteSpace(result.Output))
                {
                    info.IsInstalled = true;
                    info.Path = result.Output.Trim();
                    return info;
                }
            }
            
            return info;
        }
        
        /// <summary>
        /// Detecta todas as ferramentas
        /// </summary>
        public static ToolInfo[] DetectAllTools()
        {
            return new[]
            {
                DetectGltfpack(),
                DetectGltfTransform(),
                DetectObj2Gltf()
            };
        }
        
        /// <summary>
        /// Verifica se uma ferramenta existe no PATH
        /// </summary>
        public static bool ExecutableExists(string executableName)
        {
            if (IsWindows)
            {
                var result = ExecuteCommand("where", executableName);
                return result.ExitCode == 0;
            }
            else
            {
                var result = ExecuteCommand("which", executableName);
                return result.ExitCode == 0;
            }
        }
        
        /// <summary>
        /// Executa um comando e retorna o resultado
        /// </summary>
        private static (int ExitCode, string Output, string Error) ExecuteCommand(string fileName, string arguments)
        {
            try
            {
                var processInfo = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = arguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };
                
                using (var process = Process.Start(processInfo))
                {
                    if (process == null)
                    {
                        return (-1, string.Empty, "Failed to start process");
                    }
                    
                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();
                    process.WaitForExit();
                    
                    return (process.ExitCode, output, error);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ToolDetector] Failed to execute {fileName}: {ex.Message}");
                return (-1, string.Empty, ex.Message);
            }
        }
        
        /// <summary>
        /// Gera um relatório de ferramentas instaladas
        /// </summary>
        public static string GenerateToolsReport()
        {
            var tools = DetectAllTools();
            var report = "=== Ferramentas Detectadas ===\n\n";
            
            foreach (var tool in tools)
            {
                string status = tool.IsInstalled ? "✅ Instalado" : "❌ Não encontrado";
                report += $"{tool.Name}: {status}\n";
                
                if (tool.IsInstalled)
                {
                    report += $"  Caminho: {tool.Path}\n";
                    if (!string.IsNullOrEmpty(tool.Version))
                    {
                        report += $"  Versão: {tool.Version}\n";
                    }
                }
                else
                {
                    report += $"  Instalar com: {tool.InstallCommand}\n";
                }
                
                report += "\n";
            }
            
            return report;
        }
    }
}

