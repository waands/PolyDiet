using System;

class Program
{
    static void Main()
    {
        // Simulando os valores que seriam usados
        string scene = "ModelViewer";
        string modelName = "suzanne";
        string variant = "original";
        
        // Função Safe simulada
        string Safe(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return "\"" + s.Replace("\"", "''") + "\"";
        }
        
        // Padrão atual
        var pattern = "," + Safe(scene) + "," + Safe(modelName) + "," + Safe(variant) + ",";
        Console.WriteLine($"Padrão: {pattern}");
        
        // Linha de exemplo do CSV
        string csvLine = "2025-10-14T19:54:09-03:00,\"20251014_195204\",\"LinuxEditor\",\"6000.2.4f1\",\"ModelViewer\",\"suzanne\",\"original\",0.019,255.912,379.754,62.29,40.94,5,true";
        Console.WriteLine($"Linha CSV: {csvLine}");
        
        // Verificar se o padrão encontra a linha
        bool found = csvLine.Contains(pattern, StringComparison.Ordinal);
        Console.WriteLine($"Padrão encontrado: {found}");
        
        // Vamos ver as posições dos campos
        string[] fields = csvLine.Split(',');
        Console.WriteLine($"Total de campos: {fields.Length}");
        for (int i = 0; i < fields.Length; i++)
        {
            Console.WriteLine($"Campo {i}: {fields[i]}");
        }
    }
}
