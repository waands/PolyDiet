using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace PolyDiet.Core.ModelLoading.Wizard
{
    /// <summary>
    /// Estado do wizard de importação
    /// </summary>
    [Serializable]
    public class WizardState
    {
        public WizardStep CurrentStep { get; set; } = WizardStep.Import;
        public string ModelName { get; set; }
        public string SourcePath { get; set; }
        public string DestinationPath { get; set; }
        public bool ConversionDone { get; set; }
        public bool CompressionDone { get; set; }
        public bool MetricsDone { get; set; }
        public List<string> Errors { get; set; }
        public List<string> Warnings { get; set; }
        public Dictionary<string, object> Metadata { get; set; }
        public DateTime LastUpdated { get; set; }
        
        public WizardState()
        {
            Errors = new List<string>();
            Warnings = new List<string>();
            Metadata = new Dictionary<string, object>();
            LastUpdated = DateTime.Now;
        }
        
        /// <summary>
        /// Salva estado em arquivo
        /// </summary>
        public void SaveToFile(string filePath)
        {
            try
            {
                LastUpdated = DateTime.Now;
                string json = JsonUtility.ToJson(this, true);
                File.WriteAllText(filePath, json);
                Debug.Log($"[WizardState] Saved to: {filePath}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[WizardState] Failed to save: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Carrega estado de arquivo
        /// </summary>
        public static WizardState LoadFromFile(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    return new WizardState();
                }
                
                string json = File.ReadAllText(filePath);
                var state = JsonUtility.FromJson<WizardState>(json);
                
                // Validação básica
                if (state.Errors == null) state.Errors = new List<string>();
                if (state.Warnings == null) state.Warnings = new List<string>();
                if (state.Metadata == null) state.Metadata = new Dictionary<string, object>();
                
                Debug.Log($"[WizardState] Loaded from: {filePath}");
                return state;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[WizardState] Failed to load: {ex.Message}");
                return new WizardState();
            }
        }
        
        /// <summary>
        /// Adiciona erro ao estado
        /// </summary>
        public void AddError(string error)
        {
            Errors.Add($"[{DateTime.Now:HH:mm:ss}] {error}");
            LastUpdated = DateTime.Now;
        }
        
        /// <summary>
        /// Adiciona warning ao estado
        /// </summary>
        public void AddWarning(string warning)
        {
            Warnings.Add($"[{DateTime.Now:HH:mm:ss}] {warning}");
            LastUpdated = DateTime.Now;
        }
        
        /// <summary>
        /// Limpa erros e warnings
        /// </summary>
        public void ClearMessages()
        {
            Errors.Clear();
            Warnings.Clear();
            LastUpdated = DateTime.Now;
        }
        
        /// <summary>
        /// Verifica se pode avançar para próximo step
        /// </summary>
        public bool CanAdvanceTo(WizardStep nextStep)
        {
            return nextStep switch
            {
                WizardStep.AskCompress => ConversionDone,
                WizardStep.Compressing => ConversionDone,
                WizardStep.AskRun => CompressionDone,
                WizardStep.Running => CompressionDone,
                WizardStep.Done => MetricsDone,
                _ => true
            };
        }
        
        /// <summary>
        /// Gera relatório do estado atual
        /// </summary>
        public string GenerateReport()
        {
            var report = "=== Wizard State Report ===\n\n";
            report += $"Current Step: {CurrentStep}\n";
            report += $"Model Name: {ModelName ?? \"Not set\"}\n";
            report += $"Source Path: {SourcePath ?? \"Not set\"}\n";
            report += $"Last Updated: {LastUpdated:yyyy-MM-dd HH:mm:ss}\n\n";
            
            report += "Progress:\n";
            report += $"  Conversion: {(ConversionDone ? "✅ Done" : "❌ Pending")}\n";
            report += $"  Compression: {(CompressionDone ? "✅ Done" : "❌ Pending")}\n";
            report += $"  Metrics: {(MetricsDone ? "✅ Done" : "❌ Pending")}\n\n";
            
            if (Errors.Count > 0)
            {
                report += "Errors:\n";
                foreach (var error in Errors)
                {
                    report += $"  ❌ {error}\n";
                }
                report += "\n";
            }
            
            if (Warnings.Count > 0)
            {
                report += "Warnings:\n";
                foreach (var warning in Warnings)
                {
                    report += $"  ⚠️ {warning}\n";
                }
                report += "\n";
            }
            
            return report;
        }
    }
    
    /// <summary>
    /// Passos do wizard
    /// </summary>
    public enum WizardStep
    {
        Import,
        AskCompress,
        Compressing,
        AskRun,
        Running,
        Done,
        AskOverwriteImport,
        AskOverwriteCompress,
        Error
    }
}

