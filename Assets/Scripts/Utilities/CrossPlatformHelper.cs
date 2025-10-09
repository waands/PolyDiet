using UnityEngine;
using System.IO;

/// <summary>
/// Classe utilitária para garantir compatibilidade cross-platform
/// Especialmente útil para adaptação Windows -> Linux
/// </summary>
public static class CrossPlatformHelper
{
    /// <summary>
    /// Normaliza um caminho para a plataforma atual
    /// </summary>
    public static string NormalizePath(string path)
    {
        if (string.IsNullOrEmpty(path)) return path;
        
        // Converte todos os separadores para o separador da plataforma atual
        return path.Replace('\\', Path.DirectorySeparatorChar)
                  .Replace('/', Path.DirectorySeparatorChar);
    }

    /// <summary>
    /// Combina caminhos garantindo separadores corretos
    /// </summary>
    public static string CombinePaths(params string[] paths)
    {
        return Path.Combine(paths);
    }

    /// <summary>
    /// Verifica se um executável existe no PATH ou em um caminho específico
    /// </summary>
    public static bool ExecutableExists(string executableName)
    {
        // Se for um caminho absoluto, verifica diretamente
        if (Path.IsPathRooted(executableName))
        {
            return File.Exists(executableName);
        }

        // Senão, procura no PATH
        var pathVariable = System.Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrEmpty(pathVariable)) return false;

        var pathSeparator = System.Environment.OSVersion.Platform == System.PlatformID.Win32NT ? ';' : ':';
        var paths = pathVariable.Split(pathSeparator);

        foreach (var path in paths)
        {
            try
            {
                var fullPath = Path.Combine(path.Trim(), executableName);
                if (File.Exists(fullPath)) return true;

                // No Linux, também tentar sem extensão se não encontrou
                if (System.Environment.OSVersion.Platform != System.PlatformID.Win32NT)
                {
                    if (File.Exists(fullPath)) return true;
                }
            }
            catch
            {
                // Ignorar erros de paths inválidos
                continue;
            }
        }

        return false;
    }

    /// <summary>
    /// Cria um diretório se ele não existir, com tratamento de erros
    /// </summary>
    public static bool EnsureDirectoryExists(string directoryPath)
    {
        try
        {
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
                Debug.Log($"[CrossPlatformHelper] Diretório criado: {directoryPath}");
            }
            return true;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[CrossPlatformHelper] Erro ao criar diretório {directoryPath}: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Converte um path para usar barras Unix (útil para URLs)
    /// </summary>
    public static string ToUnixPath(string path)
    {
        return path?.Replace('\\', '/');
    }
}