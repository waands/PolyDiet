# Refatoração Completa do Sistema de Métricas e Relatórios

## Resumo das Alterações Realizadas

### 1. Centralização do Gerenciamento de Caminhos ✅

**Arquivo:** `Assets/Scripts/Metrics/Core/MetricsPathProvider.cs`

- **Criada classe estática** para centralizar toda a lógica de construção de caminhos
- **Métodos implementados:**
  - `GetModelDirectory(string modelName)` - Diretório raiz de um modelo
  - `GetBenchmarkDirectory(string modelName)` - Diretório de benchmark de um modelo
  - `GetCsvPath(string modelName)` - Caminho completo do arquivo CSV
  - `GetFallbackCsvPath()` - Caminho de fallback para CSV antigo
  - `GetReportsDirectory()` - Diretório de saída dos relatórios
  - `GetAllModelCsvPaths()` - Todos os caminhos de CSV de modelos disponíveis

**Arquivos atualizados para usar MetricsPathProvider:**
- `Metrics.cs` - Métodos `GetOutputDir()` e `GetCsvPathPublic()`
- `MetricsStore.cs` - Método `LoadAllModels()` e `LoadFallbackData()`
- `ReportRunner.cs` - Métodos `CsvPathDefault()` e `OutDirDefault()`

### 2. Padronização e Robustez do Manuseio de CSV ✅

**Arquivo:** `Assets/Scripts/Metrics/Core/MetricsStore.cs`

- **Refatorado método `Load()`** para usar parsing específico por formato
- **Criados métodos de parsing específicos:**
  - `ParseV1Entry()` - Formato antigo básico (12 colunas)
  - `ParseV2Entry()` - Formato antigo com run_id (14 colunas)  
  - `ParseV3Entry()` - Formato novo com test_number e novas métricas FPS (19 colunas)
- **Melhorado tratamento de erros** com try-catch e logging detalhado
- **Eliminados números mágicos** substituídos por constantes de `MetricsConfig`

**Arquivo:** `Assets/Scripts/Metrics/Core/Metrics.cs`

- **Corrigido escape de CSV** na função `Safe()` - agora usa `""` em vez de `''`
- **Melhorado tratamento de erros** em `SafeFileMB()` com logging

### 3. Configuração Centralizada ✅

**Arquivo:** `Assets/Scripts/Metrics/Core/MetricsConfig.cs`

- **Criada classe estática** com todas as constantes de configuração
- **Constantes definidas:**
  - Variantes: `BASE_VARIANT`, `DRACO_VARIANT`, `MESHOPT_VARIANT`
  - FPS: `DEFAULT_FPS_WINDOW_SECONDS`, `DEFAULT_NUMBER_OF_TESTS`
  - Arquivos: `CSV_FILENAME`, `LEGACY_CSV_FILENAME`
  - Formatos CSV: `V1_COLUMN_COUNT`, `V2_COLUMN_COUNT`, `V3_COLUMN_COUNT`
  - Relatórios: `DEFAULT_LAST_N`, `DEFAULT_PDF_ENGINE`
  - Diretórios: `MODELS_DIR_NAME`, `BENCHMARK_DIR_NAME`, etc.

**Arquivos atualizados para usar MetricsConfig:**
- `Metrics.cs` - Valores padrão e constantes
- `MetricsStore.cs` - Referências às variantes e contagens de colunas
- `ReportRunner.cs` - Valores padrão de configuração

### 4. Correção da Lógica de Execução ✅

**Arquivo:** `Assets/Scripts/Metrics/Core/ReportRunner.cs`

- **Refatorado método `RunReport()`** para executar o script Python uma única vez
- **Lógica corrigida:**
  - Se há CSVs de modelos: executa com `--auto-discover` uma vez
  - Se há CSV específico: executa com `--csv` uma vez
  - Removido loop que executava o script para cada CSV
- **Melhorado tratamento de erros** e logging

### 5. Eliminação de "Gambiarras" no Python ✅

**Arquivo:** `Assets/Scripts/Metrics/reports_tool/metrics_report.py`

- **Removida "gambiarra" de troca de colunas** - agora apenas valida e avisa
- **Adicionadas configurações centralizadas** no topo do arquivo
- **Atualizadas referências** para usar `CONFIG` em vez de valores hardcoded
- **Melhorado logging** para detectar inconsistências sem corrigir automaticamente

### 6. Melhorias Gerais de Qualidade ✅

**Arquivo:** `Assets/Scripts/Metrics/Core/Metrics.cs`

- **Refatorado cálculo de FPS** - criado método `CalculateFpsStatistics()` compartilhado
- **Eliminada duplicação** entre `MeasureFpsWindow()` e `MeasureFpsWindowWithCallback()`
- **Melhorado tratamento de erros** com logging adequado

**Arquivo:** `Assets/Scripts/Metrics/Core/MetricsStore.cs`

- **Melhorado método `LoadAllModels()`** para usar `MetricsPathProvider`
- **Adicionado logging detalhado** para debugging
- **Melhorado tratamento de erros** com try-catch

## Benefícios da Refatoração

### ✅ **Manutenibilidade**
- Configurações centralizadas em um só lugar
- Código mais limpo e organizado
- Eliminação de duplicação de código

### ✅ **Robustez**
- Melhor tratamento de erros com logging
- Parsing de CSV mais robusto e específico por formato
- Validação de dados sem correções automáticas perigosas

### ✅ **Consistência**
- Uso consistente de constantes em todo o código
- Lógica de execução simplificada e correta
- Escape de CSV padronizado

### ✅ **Debugging**
- Logging detalhado em todas as operações
- Mensagens de erro mais informativas
- Validação de dados com avisos claros

## Arquivos Criados
- `Assets/Scripts/Metrics/Core/MetricsConfig.cs`
- `Assets/Scripts/Metrics/Core/MetricsPathProvider.cs`
- `Assets/Scripts/Metrics/Core/MetricsConfig.cs.meta`
- `Assets/Scripts/Metrics/Core/MetricsPathProvider.cs.meta`

## Arquivos Modificados
- `Assets/Scripts/Metrics/Core/Metrics.cs`
- `Assets/Scripts/Metrics/Core/MetricsStore.cs`
- `Assets/Scripts/Metrics/Core/ReportRunner.cs`
- `Assets/Scripts/Metrics/reports_tool/metrics_report.py`

## Próximos Passos Recomendados

1. **Testar a refatoração** com dados reais de benchmark
2. **Verificar se o ReportRunner** executa corretamente com a nova lógica
3. **Validar se o parsing de CSV** funciona com todos os formatos
4. **Confirmar que não há regressões** na funcionalidade existente

