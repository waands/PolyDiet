# 📊 Análise de Refatoração - PolyDiet Unity

## 🎯 Resumo Executivo

**Total de Scripts Analisados:** 33 arquivos `.cs`  
**Scripts Confirmados como Essenciais:** 30  
**Scripts Candidatos à Remoção:** 3  
**Métodos Longos Identificados:** 8  
**Oportunidades de Refatoração:** 15  

---

## 🗑️ Código Morto Identificado

### Scripts Debug (CANDIDATOS À REMOÇÃO)

| Script | Status | Motivo | Linhas |
|--------|--------|--------|--------|
| `Debug/MetricsPanelDiagnostic.cs` | ❌ **REMOVER** | Apenas referenciado em documentação, não usado em código | ~50 |
| `Debug/WizardDebug.cs` | ❌ **REMOVER** | Script de debug temporário com ContextMenu | ~232 |
| `Debug/ModelConverterTest.cs` | ❌ **REMOVER** | Script de teste temporário | ~55 |

**Total de código morto:** ~337 linhas

### Métodos Não Utilizados (JÁ REMOVIDOS)

Baseado na análise do `RESUMO_REORGANIZACAO.md`, os seguintes métodos já foram removidos:

| Classe | Método Removido | Motivo |
|--------|----------------|--------|
| `CrossPlatformHelper.cs` | `GetPlatformInfo()` | Nunca chamado |
| `CrossPlatformHelper.cs` | `ToWindowsPath()` | Nunca chamado |
| `UIInputLock.cs` | `ForceUnlockAll()` | Nunca chamado |
| `CompareLoader.cs` | `LoadBoth()` | Wrapper síncrono desnecessário |
| `HUDController.cs` | `QuickLoadAsync()` | Substituído por lógica em `OnCompareConfirmAsync()` |

**Total já removido:** ~150 linhas

---

## 📈 Estatísticas de Complexidade por Arquivo

### Arquivos Mais Complexos (Prioridade ALTA)

| Arquivo | Linhas | Métodos | Complexidade | Problemas Identificados |
|---------|--------|---------|--------------|------------------------|
| `ModelViewer.cs` | 1050 | 25+ | 🔴 **CRÍTICA** | God Object, múltiplas responsabilidades |
| `HUDController.cs` | 399 | 15+ | 🟠 **ALTA** | Deus Object, lógica de UI complexa |
| `WizardController.cs` | 439 | 20+ | 🟠 **ALTA** | Fluxo complexo, métodos longos |
| `ReportRunner.cs` | 249 | 8+ | 🟡 **MÉDIA** | Falta tratamento de erros robusto |

### Arquivos de Complexidade Média (Prioridade MÉDIA)

| Arquivo | Linhas | Métodos | Complexidade | Problemas Identificados |
|---------|--------|---------|--------------|------------------------|
| `MetricsViewer.cs` | 315 | 12+ | 🟡 **MÉDIA** | Duplicação de código, métodos longos |
| `MetricsStore.cs` | 281 | 15+ | 🟡 **MÉDIA** | Múltiplos formatos CSV, parsing complexo |
| `CompareSplitView.cs` | 310 | 10+ | 🟡 **MÉDIA** | Lógica de shader, configuração de câmeras |

### Arquivos Simples (Prioridade BAIXA)

| Arquivo | Linhas | Métodos | Complexidade | Status |
|---------|--------|---------|--------------|--------|
| `MetricsEntry.cs` | 14 | 0 | 🟢 **BAIXA** | ✅ Data class simples |
| `MetricsConfig.cs` | 45 | 0 | 🟢 **BAIXA** | ✅ Constantes bem organizadas |
| `UIInputLock.cs` | 23 | 3 | 🟢 **BAIXA** | ✅ Singleton simples |
| `CameraPoseFollower.cs` | 24 | 1 | 🟢 **BAIXA** | ✅ Componente focado |

---

## 🔍 Métodos Longos Identificados

### HUDController.cs

| Método | Linhas | Complexidade | Refatoração Proposta |
|--------|--------|--------------|---------------------|
| `OnCompareConfirmAsync()` | 124 | 🔴 **CRÍTICA** | Dividir em: `ValidateCompareRequest()`, `LoadSingleVariant()`, `LoadComparisonMode()` |
| `BuildVariantChips()` | 44 | 🟡 **MÉDIA** | Extrair: `CreateVariantChip()` |
| `OnVariantChipChanged()` | 28 | 🟡 **MÉDIA** | Simplificar lógica de validação |

### ModelViewer.cs

| Método | Linhas | Complexidade | Refatoração Proposta |
|--------|--------|--------------|---------------------|
| `CompressDracoAsync()` | 76 | 🔴 **CRÍTICA** | Mover para `ModelCompressor.cs` |
| `CompressMeshoptAsync()` | 67 | 🔴 **CRÍTICA** | Mover para `ModelCompressor.cs` |
| `ScanModelsAndPopulateUI()` | 68 | 🟠 **ALTA** | Mover para `ModelScanner.cs` |
| `OnClickLoadAsync()` | 108 | 🟠 **ALTA** | Simplificar, extrair validações |

### MetricsViewer.cs

| Método | Linhas | Complexidade | Refatoração Proposta |
|--------|--------|--------------|---------------------|
| `ApplyFilters()` | 52 | 🟡 **MÉDIA** | Dividir em: `GetFilteredEntries()`, `RenderResults()` |
| `RenderCards()` + `RenderTable()` | 28+20 | 🟡 **MÉDIA** | Unificar em método genérico |

---

## 🔗 Análise de Dependências

### Gráfico de Dependências Principais

```
UI Layer:
├── HUDController → ModelViewer, SimpleOrbitCamera, CompareLoader, CompareSplitView
├── WizardController → ModelViewer, Metrics, HUDController
└── MetricsViewer → Metrics, ReportRunner

Core Layer:
├── ModelViewer → GLTFast, CrossPlatformHelper, Metrics
├── CompareLoader → ModelViewer, CompareSplitView
├── CompareSplitView → CameraPoseFollower
└── SimpleOrbitCamera → UIInputLock

Metrics Layer:
├── Metrics → MetricsConfig, MetricsPathProvider
├── MetricsStore → MetricsConfig, MetricsPathProvider, MetricsEntry
├── ReportRunner → MetricsPathProvider, MetricsConfig
└── MetricsViewer → MetricsStore, MetricsCardUI, MetricsRowUI

Utilities:
├── CrossPlatformHelper → (standalone)
├── UIInputLock → (standalone)
└── ModelConverter → (standalone)
```

### Problemas de Acoplamento Identificados

1. **HUDController** é muito acoplado (6 dependências diretas)
2. **ModelViewer** tem responsabilidades demais (scan + load + compress)
3. **WizardController** conhece muitos detalhes internos
4. **CompareSplitView** mistura UI com lógica de câmera

---

## 🎯 Oportunidades de Refatoração

### 1. Extração de Classes (Prioridade ALTA)

| Classe Atual | Nova Classe | Responsabilidade | Benefício |
|--------------|-------------|------------------|-----------|
| `ModelViewer` | `ModelScanner` | Scan de modelos disponíveis | Reduzir ModelViewer de 1050 → ~800 linhas |
| `ModelViewer` | `ModelCompressor` | Compressão Draco/Meshopt | Isolar lógica de compressão |
| `HUDController` | `CompareUIController` | UI de comparação | Reduzir HUDController de 399 → ~250 linhas |

### 2. Sistema de Eventos (Prioridade ALTA)

**Problema:** Comunicação direta entre componentes cria acoplamento forte.

**Solução:** Implementar sistema de eventos C# puro:

```csharp
// Novo: UI/Events/GameEvents.cs
public static class GameEvents
{
    public static event System.Action<string, string> OnModelLoaded;
    public static event System.Action<bool> OnCompareModeChanged;
    public static event System.Action OnCameraResetRequested;
    // ... mais eventos
}
```

**Benefícios:**
- Reduzir acoplamento entre HUDController ↔ ModelViewer ↔ CompareSplitView
- Facilitar testes unitários
- Permitir extensibilidade futura

### 3. Melhoria de Tratamento de Erros (Prioridade MÉDIA)

| Classe | Problema Atual | Solução Proposta |
|--------|----------------|------------------|
| `ReportRunner` | `try-catch` básico | Validação prévia + timeout + retry |
| `ModelViewer` | Falhas silenciosas | Logs estruturados + fallbacks |
| `CompareLoader` | Sem validação de layers | Validação de configuração |

### 4. Remoção de Duplicação (Prioridade MÉDIA)

| Duplicação Identificada | Solução |
|-------------------------|---------|
| `RenderCards()` + `RenderTable()` | Método genérico `RenderEntries<T>()` |
| Logs de debug repetitivos | Método `LogDebug(context, message)` |
| Validações de null repetidas | Extension methods |

---

## 📊 Métricas de Qualidade

### Antes da Refatoração

| Métrica | Valor Atual | Meta |
|---------|-------------|------|
| Arquivos com >500 linhas | 2 | 0 |
| Métodos com >50 linhas | 8 | 0 |
| Acoplamento médio | Alto | Baixo |
| Cobertura de comentários | 30% | 80% |
| Scripts não utilizados | 3 | 0 |

### Após Refatoração (Projetado)

| Métrica | Valor Projetado | Melhoria |
|---------|-----------------|----------|
| Arquivos com >500 linhas | 0 | -100% |
| Métodos com >50 linhas | 0 | -100% |
| Acoplamento médio | Baixo | -70% |
| Cobertura de comentários | 80% | +167% |
| Scripts não utilizados | 0 | -100% |

---

## 🚀 Plano de Execução Recomendado

### Fase 1: Limpeza (1-2 dias)
1. ✅ Remover scripts Debug (`MetricsPanelDiagnostic`, `WizardDebug`, `ModelConverterTest`)
2. ✅ Validar que não há referências quebradas
3. ✅ Executar testes básicos

### Fase 2: Reestruturação (2-3 dias)
1. ✅ Implementar sistema de eventos (`GameEvents.cs`)
2. ✅ Extrair `ModelScanner` e `ModelCompressor`
3. ✅ Refatorar `HUDController` usando eventos

### Fase 3: Refatoração de Métodos (2-3 dias)
1. ✅ Dividir métodos longos identificados
2. ✅ Remover duplicação de código
3. ✅ Melhorar tratamento de erros

### Fase 4: Documentação (1 dia)
1. ✅ Adicionar comentários XML
2. ✅ Documentar decisões de design
3. ✅ Atualizar guias de estilo

---

## 📋 Checklist de Validação

- [ ] Todos os scripts Debug removidos
- [ ] Sistema de eventos implementado e testado
- [ ] Classes extraídas funcionando corretamente
- [ ] Métodos longos divididos
- [ ] Duplicação removida
- [ ] Tratamento de erros melhorado
- [ ] Comentários adicionados
- [ ] Testes básicos passando
- [ ] Performance mantida ou melhorada

---

**Data da Análise:** Janeiro 2025  
**Analisado por:** AI Assistant  
**Próximo documento:** `ARCHITECTURE_ISSUES.md`
