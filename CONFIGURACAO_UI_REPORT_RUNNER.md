# Configuração da UI para ReportRunner

## 🎯 **Problema Resolvido**

O `ReportRunner.cs` estava ignorando a seleção do dropdown e usando sua própria lógica de detecção de modelo. Agora implementamos uma **comunicação direta** entre a UI e o ReportRunner.

## ✅ **Modificações Implementadas**

### **1. ReportRunner.cs - Novo Método Público**
```csharp
/// <summary>
/// Inicia a geração de um relatório para um modelo específico,
/// sobrescrevendo a lógica de auto-detecção.
/// </summary>
public void RunReportForModel(string modelName)
{
    Debug.Log($"[ReportRunner] Recebido pedido para gerar relatório específico para: {modelName}");
    this.modelOverride = modelName; // Define o override com o modelo recebido
    RunReport(); // Executa a lógica de relatório existente
}
```

### **2. MetricsViewer.cs - Referência e Método de Trigger**
```csharp
[Header("Refs")]
public Metrics metrics;                      // arraste o GO Metrics
public ReportRunner reportRunner;            // arraste o GO ReportRunner

/// <summary>
/// Pega o modelo selecionado no dropdown e solicita
/// ao ReportRunner que gere um relatório para ele.
/// </summary>
public void TriggerReportGeneration()
{
    // Validações de segurança
    if (reportRunner == null) { /* erro */ return; }
    if (dropdownModel == null || dropdownModel.options.Count == 0) { /* aviso */ return; }
    
    // Pega o modelo selecionado no dropdown
    string selectedModel = dropdownModel.options[dropdownModel.value].text;
    
    // Validação do modelo selecionado
    if (string.IsNullOrEmpty(selectedModel) || selectedModel.Contains("(sem dados)")) { /* aviso */ return; }
    
    // Chama o ReportRunner com o modelo específico
    reportRunner.RunReportForModel(selectedModel);
}
```

## 🔧 **Configuração no Unity Editor**

### **Passo 1: Configurar Referência do ReportRunner**

1. **Selecione o GameObject** que contém o script `MetricsViewer` (provavelmente o painel da UI de métricas)
2. **No Inspector**, você verá o novo campo **"Report Runner"** na seção "Refs"
3. **Arraste o GameObject** que contém o script `ReportRunner` para este campo

### **Passo 2: Configurar o Botão de Geração de Relatório**

1. **Selecione o botão** de "Gerar Relatório" na hierarquia da sua cena
2. **No Inspector**, encontre o componente **"Button"** e a seção **"On Click ()"**
3. **Remova o evento antigo** que chama `ReportRunner.RunReport` (clique no sinal de menos **-**)
4. **Adicione um novo evento** (clique no sinal de mais **+**)
5. **Configure o novo evento:**
   - **Arraste o GameObject** que contém o `MetricsViewer` para o campo de objeto
   - **No menu suspenso de funções**, selecione `MetricsViewer -> TriggerReportGeneration()`

## 🎯 **Como Funciona Agora**

### **Fluxo de Execução:**

1. **Usuário seleciona** "xyzrgb_dragon" no dropdown do MetricsViewer
2. **Usuário clica** no botão "Gerar Relatório"
3. **Botão chama** `MetricsViewer.TriggerReportGeneration()`
4. **MetricsViewer pega** o modelo selecionado do dropdown
5. **MetricsViewer chama** `ReportRunner.RunReportForModel("xyzrgb_dragon")`
6. **ReportRunner define** `modelOverride = "xyzrgb_dragon"`
7. **ReportRunner executa** `RunReport()` com o modelo específico
8. **Relatório é gerado** para o modelo correto

### **Logs de Diagnóstico:**
```
[MetricsViewer] Gerando relatório para modelo selecionado: xyzrgb_dragon
[ReportRunner] Recebido pedido para gerar relatório específico para: xyzrgb_dragon
[Report] Usando 2 arquivos CSV encontrados:
  - StreamingAssets/Models/suzanne/benchmark/metrics.csv
  - StreamingAssets/Models/xyzrgb_dragon/benchmark/metrics.csv
[Report] Executando comando único:
python metrics_report.py --out "/path/to/reports" --model xyzrgb_dragon --variants original,draco,meshopt --last-n 20 --csv-files "..." --html --pdf
```

## 🚀 **Vantagens da Solução**

### **✅ Comunicação Direta**
- **UI controla** qual modelo usar
- **ReportRunner obedece** a seleção do usuário
- **Sem "mágica"** de auto-detecção

### **✅ Robustez**
- **Validações de segurança** em cada etapa
- **Logs detalhados** para diagnóstico
- **Tratamento de erros** adequado

### **✅ Flexibilidade**
- **Mantém compatibilidade** com o método `RunReport()` original
- **Fácil de estender** para novos casos
- **Interface clara** e bem documentada

## 🎉 **Resultado Final**

Agora quando você:
1. **Selecionar "xyzrgb_dragon"** no dropdown
2. **Clicar em "Gerar Relatório"**

O sistema irá:
- ✅ **Usar exatamente** o modelo selecionado
- ✅ **Ignorar** o modelo ativo na cena 3D
- ✅ **Gerar relatório** para o modelo correto
- ✅ **Mostrar logs** claros do processo

**A UI agora está completamente conectada à lógica de geração de relatórios!** 🎯




