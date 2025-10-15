# 📊 Guia do Sistema Avançado de Reports

## Visão Geral

O Sistema Avançado de Reports do PolyDiet Unity foi completamente refatorado para fornecer análises detalhadas e visualizações interativas das métricas de performance de modelos 3D.

## Nova Estrutura de Arquivos

### Localização dos Reports

Os reports agora são salvos **dentro da pasta de cada modelo**, organizados por timestamp:

```
StreamingAssets/Models/
  └── <nome_do_modelo>/
      ├── original/
      │   └── model.glb
      ├── draco/
      │   └── model.glb
      ├── meshopt/
      │   └── model.glb
      ├── benchmark/
      │   └── benchmarks.csv
      └── reports/                    ← NOVA LOCALIZAÇÃO
          ├── 20251015_191027/        ← Timestamp do report
          │   ├── report.html         ← Report interativo
          │   ├── report.pdf          ← Report em PDF (opcional)
          │   └── data.json           ← Dados exportados
          └── 20251015_193045/        ← Report mais recente
              ├── report.html
              ├── report.pdf
              └── data.json
```

### Benefícios da Nova Estrutura

- **Organização**: Todos os arquivos relacionados ao modelo ficam juntos
- **Histórico**: Mantém múltiplos reports com timestamps diferentes
- **Rastreabilidade**: Fácil comparar evolução ao longo do tempo
- **Backup**: Simples fazer backup de tudo relacionado a um modelo

## Como Gerar Reports

### Através do Unity Editor

1. Execute testes de performance no modelo desejado (Wizard)
2. Abra o painel de Métricas
3. Selecione o modelo no dropdown
4. Clique no botão "Generate Report"
5. O report será salvo em `Models/<modelo>/reports/<timestamp>/`

### Através de Script

```csharp
// Obter ReportRunner
var reportRunner = FindObjectOfType<ReportRunner>();

// Gerar report para modelo específico
reportRunner.RunReportForModel("meu_modelo");

// Obter localização do report
string reportsDir = MetricsPathProvider.GetModelReportsDirectory("meu_modelo");
Debug.Log($"Reports salvos em: {reportsDir}");

// Listar reports anteriores
string[] previousReports = MetricsPathProvider.GetModelReportsList("meu_modelo");
foreach (var report in previousReports)
{
    Debug.Log($"Report: {report}");
}

// Obter report mais recente
string latestReport = MetricsPathProvider.GetLatestModelReport("meu_modelo");
```

## Conteúdo dos Reports

### 1. Resumo Executivo

- Nome do modelo
- Total de testes executados
- Número de variantes testadas
- Período de tempo dos testes

### 2. Informações de Arquivos

Tabela com detalhes de cada variante:

- **Tamanho do arquivo** (em MB)
- **Taxa de compressão** (% de redução em relação ao original)
- **Caminho do arquivo**

**Exemplo:**
- Original: 2.50 MB
- Draco: 0.80 MB (68% menor)
- Meshopt: 1.20 MB (52% menor)

### 3. Comparação entre Variantes

Tabela comparativa mostrando:

- **Métrica** (load_ms, mem_mb, fps_avg)
- **Valor original** vs **Valor da variante**
- **Diferença percentual** (com indicador visual de melhora/piora)

**Interpretação:**
- ↓ verde = Melhor (menor tempo, menos memória, ou mais FPS)
- ↑ vermelho = Pior

### 4. Estatísticas Detalhadas

Para cada variante e métrica:

- **Média**: Valor médio
- **Mediana**: Valor central
- **Desvio Padrão**: Variabilidade dos dados
- **Mínimo/Máximo**: Valores extremos
- **Quartis (Q25, Q75)**: Distribuição dos dados
- **Percentis (P1, P99)**: Valores extremos filtrados

### 5. Gráficos de Barras

Comparação visual das médias:
- Tempo de Carregamento (ms)
- Memória Utilizada (MB)
- FPS Médio

### 6. Box Plots (Distribuição)

Visualização da distribuição de dados:
- Mostra mediana, quartis e outliers
- Permite identificar variabilidade
- Útil para detectar inconsistências

**Como Interpretar:**
- Caixa = Quartis (Q25 a Q75, contém 50% dos dados)
- Linha central = Mediana
- Bigodes = Extensão dos dados (até 1.5x IQR)
- Pontos isolados = Outliers

### 7. Scatter Plots (Relações)

Análise de correlações:
- FPS vs Tempo de Carregamento
- Memória vs FPS
- Identifica padrões e anomalias

### 8. Heatmap de Correlações

Matriz mostrando correlações entre métricas:
- Valores próximos a 1: Correlação positiva forte
- Valores próximos a -1: Correlação negativa forte
- Valores próximos a 0: Sem correlação

### 9. Evolução Temporal

Gráficos de linha mostrando:
- Como as métricas mudaram ao longo dos testes
- Tendências de melhora ou piora
- Estabilidade dos valores

### 10. Detecção de Outliers

Identifica automaticamente:
- Testes com valores anormais
- Possíveis problemas de performance
- Execuções que devem ser investigadas

## Arquivo data.json

### Estrutura

```json
{
  "model": "nome_do_modelo",
  "timestamp": "2025-10-15T19:10:27",
  "file_infos": [
    {
      "variant": "original",
      "size_mb": 2.5,
      "path": "/path/to/original/model.glb"
    },
    {
      "variant": "draco",
      "size_mb": 0.8,
      "path": "/path/to/draco/model.glb"
    }
  ],
  "compression_ratios": {
    "draco": {
      "size_mb": 0.8,
      "compression_pct": 68.0,
      "size_reduction_mb": 1.7
    }
  },
  "comparisons": {
    "draco_load_ms": {
      "variant": "draco",
      "metric": "load_ms",
      "base": 150.5,
      "value": 120.3,
      "diff_abs": -30.2,
      "diff_pct": -20.1,
      "better": true
    }
  },
  "all_stats": {
    "original": {
      "fps_avg": {
        "mean": 60.5,
        "median": 60.2,
        "std": 2.1,
        "min": 55.0,
        "max": 65.0,
        "q25": 59.0,
        "q75": 62.0
      }
    }
  },
  "trends": {
    "fps_avg": {
      "slope": 0.05,
      "improving": true,
      "first_value": 59.5,
      "last_value": 61.0,
      "total_change": 1.5
    }
  }
}
```

### Uso do JSON

O arquivo `data.json` pode ser usado para:

- **Análise externa**: Importar em Python, R, Excel
- **Integração**: Usar em pipelines de CI/CD
- **Comparação**: Comparar múltiplos modelos programaticamente
- **Visualização customizada**: Criar seus próprios gráficos

**Exemplo de uso em Python:**

```python
import json

# Carregar dados
with open('data.json', 'r') as f:
    data = json.load(f)

# Acessar informações
model_name = data['model']
original_fps = data['all_stats']['original']['fps_avg']['mean']
draco_compression = data['compression_ratios']['draco']['compression_pct']

print(f"Modelo: {model_name}")
print(f"FPS Original: {original_fps}")
print(f"Compressão Draco: {draco_compression}%")
```

## Configuração do ReportRunner

### Inspector do Unity

- **Use Advanced Script**: ✓ (Recomendado - usa análises avançadas)
- **Script Path**: Deixe vazio (detecta automaticamente)
- **Gen HTML**: ✓ (Gera report.html)
- **Gen PDF**: Opcion al (requer chrome ou wkhtmltopdf)
- **Last N**: 20 (Usa últimos 20 testes por variante)

### Dependências Python

O script avançado requer:

```bash
pip install pandas numpy plotly
```

**Verificar instalação:**

```bash
python -c "import pandas, numpy, plotly; print('OK')"
```

## Solução de Problemas

### Report não é gerado

**Sintomas:** Botão não funciona ou nenhum arquivo é criado

**Soluções:**
1. Verificar se há dados de benchmark no modelo
2. Verificar se Python está instalado e no PATH
3. Verificar dependências Python (pandas, numpy, plotly)
4. Ver Console do Unity para logs detalhados

### Report está em branco ou incompleto

**Sintomas:** HTML abre mas sem gráficos

**Soluções:**
1. Verificar se há dados suficientes (mínimo 3 testes)
2. Verificar se todas as variantes têm dados
3. Ver Console do Unity para erros do script Python

### PDF não é gerado

**Sintomas:** HTML funciona mas PDF falha

**Soluções:**
1. Verificar se chrome ou wkhtmltopdf está instalado
2. Configurar `PDF Engine Path` no Inspector
3. Desabilitar PDF e usar apenas HTML

### Reports antigos desapareceram

**Sintomas:** Reports gerados anteriormente não estão mais lá

**Verificação:**
- Reports antigos em `persistentDataPath/Reports/` ainda existem
- Sistema novo salva em `StreamingAssets/Models/<modelo>/reports/`
- Ambos são preservados (sem sobrescrever)

## Boas Práticas

### Quando Gerar Reports

1. **Após testes completos**: Execute 3+ testes por variante
2. **Após mudanças**: Quando alterar modelo ou configurações
3. **Periodicamente**: Para rastrear evolução ao longo do tempo
4. **Antes de releases**: Para documentar performance

### Organizando Reports

1. **Use timestamps**: Facilita identificar quando foi gerado
2. **Mantenha histórico**: Não delete reports antigos
3. **Documente mudanças**: Anote o que mudou entre reports
4. **Compare periodicamente**: Use data.json para comparações

### Interpretando Resultados

1. **Foque na mediana**: Mais robusta que média para outliers
2. **Observe desvio padrão**: Indica consistência
3. **Investigue outliers**: Podem indicar problemas
4. **Compare tendências**: Não apenas valores absolutos

## Referência Rápida

### Métricas Principais

- **load_ms**: Tempo de carregamento em milissegundos (menor é melhor)
- **mem_mb**: Memória utilizada em megabytes (menor é melhor)
- **fps_avg**: FPS médio (maior é melhor)
- **fps_min**: FPS mínimo observado
- **fps_max**: FPS máximo observado
- **fps_median**: FPS mediano
- **fps_1pc_low**: FPS do 1º percentil

### Variantes

- **original**: Modelo sem compressão
- **draco**: Comprimido com Draco (menor tamanho, mantém qualidade)
- **meshopt**: Comprimido com Meshopt (otimizado para carregamento)

### Arquivos Gerados

- **report.html**: Report interativo (principal)
- **report.pdf**: Report em PDF (opcional)
- **data.json**: Dados em formato JSON (para análise externa)

## Suporte

Para problemas ou sugestões:

1. Verificar logs no Console do Unity
2. Verificar documentação do Python (pandas, plotly)
3. Verificar issues do GLTFast
4. Consultar documentação do sistema de métricas

## Changelog

### v2.0 (Sistema Avançado)

- ✨ Reports salvos dentro da pasta do modelo
- ✨ Análises complexas (comparações, estatísticas, outliers)
- ✨ Visualizações avançadas (box plots, scatter plots, heatmaps)
- ✨ Exportação de dados em JSON
- ✨ Informações técnicas de arquivos GLB
- ✨ Evolução temporal e tendências
- ✨ Histórico de reports com timestamps
- ✨ Detecção automática de outliers
- 🔧 Removido suporte a reports globais

### v1.0 (Sistema Legado)

- Reports salvos em persistentDataPath
- Gráficos básicos de barras e linha
- Suporte a reports globais

