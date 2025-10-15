# 📁 Plano de Reestruturação de Pastas - PolyDiet Unity

## 🎯 Resumo Executivo

**Estrutura Atual:** 7 pastas desorganizadas  
**Estrutura Proposta:** 4 pastas principais + 8 subpastas  
**Arquivos a Mover:** 30 scripts  
**Scripts a Remover:** 3 scripts Debug  
**Tempo Estimado:** 2-3 horas  

---

## 📊 Estrutura Atual vs Proposta

### Estrutura Atual (Problemática)

```
Assets/Scripts/
├── Camera/                    (2 arquivos)
│   ├── CameraPoseFollower.cs
│   └── SimpleOrbitCamera.cs
├── Conversion/                (1 arquivo)
│   └── ModelConverter.cs
├── Debug/                     (3 arquivos) ❌ REMOVER
│   ├── MetricsPanelDiagnostic.cs
│   ├── ModelConverterTest.cs
│   └── WizardDebug.cs
├── Metrics/                   (12 arquivos)
│   ├── Core/                  (6 arquivos)
│   │   ├── Metrics.cs
│   │   ├── MetricsAggregator.cs
│   │   ├── MetricsConfig.cs
│   │   ├── MetricsEntry.cs
│   │   ├── MetricsPathProvider.cs
│   │   └── MetricsStore.cs
│   ├── UI/                    (6 arquivos)
│   │   ├── ChartBar.cs
│   │   ├── ChartTexture.cs
│   │   ├── DashboardTheme.cs
│   │   ├── MetricsCardUI.cs
│   │   ├── MetricsDashboard.cs
│   │   ├── MetricsHtmlExporter.cs
│   │   ├── MetricsPanelToggle.cs
│   │   ├── MetricsRowUI.cs
│   │   ├── MetricsToggle.cs
│   │   ├── MetricsViewer.cs
│   │   └── UICapture.cs
│   └── reports_tool/          (Python scripts)
├── ModelLoading/              (4 arquivos)
│   ├── CompareLoader.cs
│   ├── CompareSplitView.cs
│   ├── GLTFValidator.cs
│   └── ModelViewer.cs
├── UI/                        (4 arquivos)
│   ├── HUDController.cs
│   ├── ToggleActive.cs
│   ├── UIInputLock.cs
│   └── WizardController.cs
└── Utilities/                 (1 arquivo)
    └── CrossPlatformHelper.cs
```

### Estrutura Proposta (Limpa e Escalável)

```
Assets/Scripts/
├── Core/                      (8 arquivos)
│   ├── Camera/               (2 arquivos)
│   │   ├── CameraPoseFollower.cs
│   │   └── SimpleOrbitCamera.cs
│   ├── ModelLoading/         (4 arquivos)
│   │   ├── CompareLoader.cs
│   │   ├── CompareSplitView.cs
│   │   ├── GLTFValidator.cs
│   │   └── ModelViewer.cs
│   └── Utilities/            (2 arquivos)
│       ├── CrossPlatformHelper.cs
│       └── UIInputLock.cs
├── Metrics/                   (12 arquivos)
│   ├── Data/                 (5 arquivos)
│   │   ├── MetricsEntry.cs
│   │   ├── MetricsConfig.cs
│   │   ├── MetricsStore.cs
│   │   ├── MetricsPathProvider.cs
│   │   └── MetricsAggregator.cs
│   ├── Collection/           (1 arquivo)
│   │   └── Metrics.cs
│   ├── Reporting/            (1 arquivo)
│   │   └── ReportRunner.cs
│   └── Visualization/        (5 arquivos)
│       ├── MetricsViewer.cs
│       ├── MetricsDashboard.cs
│       ├── DashboardTheme.cs
│       ├── ChartBar.cs
│       ├── ChartTexture.cs
│       ├── MetricsHtmlExporter.cs
│       └── UICapture.cs
├── UI/                       (8 arquivos)
│   ├── Controllers/          (2 arquivos)
│   │   ├── HUDController.cs
│   │   └── WizardController.cs
│   ├── Components/           (5 arquivos)
│   │   ├── MetricsCardUI.cs
│   │   ├── MetricsRowUI.cs
│   │   ├── MetricsPanelToggle.cs
│   │   ├── MetricsToggle.cs
│   │   └── ToggleActive.cs
│   └── Events/               (1 arquivo) ✨ NOVO
│       └── GameEvents.cs
└── Tools/                    (1 pasta)
    ├── Conversion/           (1 arquivo)
    │   └── ModelConverter.cs
    └── reports_tool/         (Python scripts)
```

---

## 🗺️ Mapeamento Completo de Migração

### Scripts Core (8 arquivos)

| Arquivo Atual | Arquivo Novo | Motivo |
|---------------|--------------|--------|
| `Camera/CameraPoseFollower.cs` | `Core/Camera/CameraPoseFollower.cs` | Componente fundamental |
| `Camera/SimpleOrbitCamera.cs` | `Core/Camera/SimpleOrbitCamera.cs` | Componente fundamental |
| `ModelLoading/CompareLoader.cs` | `Core/ModelLoading/CompareLoader.cs` | Funcionalidade core |
| `ModelLoading/CompareSplitView.cs` | `Core/ModelLoading/CompareSplitView.cs` | Funcionalidade core |
| `ModelLoading/GLTFValidator.cs` | `Core/ModelLoading/GLTFValidator.cs` | Validação core |
| `ModelLoading/ModelViewer.cs` | `Core/ModelLoading/ModelViewer.cs` | Funcionalidade core |
| `Utilities/CrossPlatformHelper.cs` | `Core/Utilities/CrossPlatformHelper.cs` | Utilitário core |
| `UI/UIInputLock.cs` | `Core/Utilities/UIInputLock.cs` | Sistema core |

### Scripts Metrics (12 arquivos)

| Arquivo Atual | Arquivo Novo | Categoria |
|---------------|--------------|-----------|
| `Metrics/Core/MetricsEntry.cs` | `Metrics/Data/MetricsEntry.cs` | Data model |
| `Metrics/Core/MetricsConfig.cs` | `Metrics/Data/MetricsConfig.cs` | Configuração |
| `Metrics/Core/MetricsStore.cs` | `Metrics/Data/MetricsStore.cs` | Persistência |
| `Metrics/Core/MetricsPathProvider.cs` | `Metrics/Data/MetricsPathProvider.cs` | Paths |
| `Metrics/Core/MetricsAggregator.cs` | `Metrics/Data/MetricsAggregator.cs` | Agregação |
| `Metrics/Core/Metrics.cs` | `Metrics/Collection/Metrics.cs` | Coleta |
| `Metrics/Core/ReportRunner.cs` | `Metrics/Reporting/ReportRunner.cs` | Relatórios |
| `Metrics/UI/MetricsViewer.cs` | `Metrics/Visualization/MetricsViewer.cs` | Visualização |
| `Metrics/UI/MetricsDashboard.cs` | `Metrics/Visualization/MetricsDashboard.cs` | Dashboard |
| `Metrics/UI/DashboardTheme.cs` | `Metrics/Visualization/DashboardTheme.cs` | Temas |
| `Metrics/UI/ChartBar.cs` | `Metrics/Visualization/ChartBar.cs` | Gráficos |
| `Metrics/UI/ChartTexture.cs` | `Metrics/Visualization/ChartTexture.cs` | Renderização |
| `Metrics/UI/MetricsHtmlExporter.cs` | `Metrics/Visualization/MetricsHtmlExporter.cs` | Export |
| `Metrics/UI/UICapture.cs` | `Metrics/Visualization/UICapture.cs` | Screenshot |

### Scripts UI (7 arquivos)

| Arquivo Atual | Arquivo Novo | Categoria |
|---------------|--------------|-----------|
| `UI/HUDController.cs` | `UI/Controllers/HUDController.cs` | Controller |
| `UI/WizardController.cs` | `UI/Controllers/WizardController.cs` | Controller |
| `Metrics/UI/MetricsCardUI.cs` | `UI/Components/MetricsCardUI.cs` | Componente |
| `Metrics/UI/MetricsRowUI.cs` | `UI/Components/MetricsRowUI.cs` | Componente |
| `Metrics/UI/MetricsPanelToggle.cs` | `UI/Components/MetricsPanelToggle.cs` | Componente |
| `Metrics/UI/MetricsToggle.cs` | `UI/Components/MetricsToggle.cs` | Componente |
| `UI/ToggleActive.cs` | `UI/Components/ToggleActive.cs` | Componente |

### Scripts Tools (1 arquivo)

| Arquivo Atual | Arquivo Novo | Motivo |
|---------------|--------------|--------|
| `Conversion/ModelConverter.cs` | `Tools/Conversion/ModelConverter.cs` | Ferramenta externa |

### Scripts a Remover (3 arquivos)

| Arquivo | Motivo |
|---------|--------|
| `Debug/MetricsPanelDiagnostic.cs` | Script de debug temporário |
| `Debug/ModelConverterTest.cs` | Script de teste temporário |
| `Debug/WizardDebug.cs` | Script de debug temporário |

---

## 🛠️ Script de Migração Automática

### Script Bash (Linux/macOS)

```bash
#!/bin/bash
# Script de migração automática - PolyDiet Unity

echo "🚀 Iniciando migração de pastas..."

# Criar estrutura de pastas
echo "📁 Criando estrutura de pastas..."
mkdir -p "Assets/Scripts/Core/Camera"
mkdir -p "Assets/Scripts/Core/ModelLoading"
mkdir -p "Assets/Scripts/Core/Utilities"
mkdir -p "Assets/Scripts/Metrics/Data"
mkdir -p "Assets/Scripts/Metrics/Collection"
mkdir -p "Assets/Scripts/Metrics/Reporting"
mkdir -p "Assets/Scripts/Metrics/Visualization"
mkdir -p "Assets/Scripts/UI/Controllers"
mkdir -p "Assets/Scripts/UI/Components"
mkdir -p "Assets/Scripts/UI/Events"
mkdir -p "Assets/Scripts/Tools/Conversion"

# Mover arquivos Core
echo "🔄 Movendo arquivos Core..."
mv "Assets/Scripts/Camera/CameraPoseFollower.cs" "Assets/Scripts/Core/Camera/"
mv "Assets/Scripts/Camera/CameraPoseFollower.cs.meta" "Assets/Scripts/Core/Camera/"
mv "Assets/Scripts/Camera/SimpleOrbitCamera.cs" "Assets/Scripts/Core/Camera/"
mv "Assets/Scripts/Camera/SimpleOrbitCamera.cs.meta" "Assets/Scripts/Core/Camera/"

mv "Assets/Scripts/ModelLoading/CompareLoader.cs" "Assets/Scripts/Core/ModelLoading/"
mv "Assets/Scripts/ModelLoading/CompareLoader.cs.meta" "Assets/Scripts/Core/ModelLoading/"
mv "Assets/Scripts/ModelLoading/CompareSplitView.cs" "Assets/Scripts/Core/ModelLoading/"
mv "Assets/Scripts/ModelLoading/CompareSplitView.cs.meta" "Assets/Scripts/Core/ModelLoading/"
mv "Assets/Scripts/ModelLoading/GLTFValidator.cs" "Assets/Scripts/Core/ModelLoading/"
mv "Assets/Scripts/ModelLoading/GLTFValidator.cs.meta" "Assets/Scripts/Core/ModelLoading/"
mv "Assets/Scripts/ModelLoading/ModelViewer.cs" "Assets/Scripts/Core/ModelLoading/"
mv "Assets/Scripts/ModelLoading/ModelViewer.cs.meta" "Assets/Scripts/Core/ModelLoading/"

mv "Assets/Scripts/Utilities/CrossPlatformHelper.cs" "Assets/Scripts/Core/Utilities/"
mv "Assets/Scripts/Utilities/CrossPlatformHelper.cs.meta" "Assets/Scripts/Core/Utilities/"
mv "Assets/Scripts/UI/UIInputLock.cs" "Assets/Scripts/Core/Utilities/"
mv "Assets/Scripts/UI/UIInputLock.cs.meta" "Assets/Scripts/Core/Utilities/"

# Mover arquivos Metrics
echo "🔄 Movendo arquivos Metrics..."
mv "Assets/Scripts/Metrics/Core/MetricsEntry.cs" "Assets/Scripts/Metrics/Data/"
mv "Assets/Scripts/Metrics/Core/MetricsEntry.cs.meta" "Assets/Scripts/Metrics/Data/"
mv "Assets/Scripts/Metrics/Core/MetricsConfig.cs" "Assets/Scripts/Metrics/Data/"
mv "Assets/Scripts/Metrics/Core/MetricsConfig.cs.meta" "Assets/Scripts/Metrics/Data/"
mv "Assets/Scripts/Metrics/Core/MetricsStore.cs" "Assets/Scripts/Metrics/Data/"
mv "Assets/Scripts/Metrics/Core/MetricsStore.cs.meta" "Assets/Scripts/Metrics/Data/"
mv "Assets/Scripts/Metrics/Core/MetricsPathProvider.cs" "Assets/Scripts/Metrics/Data/"
mv "Assets/Scripts/Metrics/Core/MetricsPathProvider.cs.meta" "Assets/Scripts/Metrics/Data/"
mv "Assets/Scripts/Metrics/Core/MetricsAggregator.cs" "Assets/Scripts/Metrics/Data/"
mv "Assets/Scripts/Metrics/Core/MetricsAggregator.cs.meta" "Assets/Scripts/Metrics/Data/"

mv "Assets/Scripts/Metrics/Core/Metrics.cs" "Assets/Scripts/Metrics/Collection/"
mv "Assets/Scripts/Metrics/Core/Metrics.cs.meta" "Assets/Scripts/Metrics/Collection/"

mv "Assets/Scripts/Metrics/Core/ReportRunner.cs" "Assets/Scripts/Metrics/Reporting/"
mv "Assets/Scripts/Metrics/Core/ReportRunner.cs.meta" "Assets/Scripts/Metrics/Reporting/"

mv "Assets/Scripts/Metrics/UI/MetricsViewer.cs" "Assets/Scripts/Metrics/Visualization/"
mv "Assets/Scripts/Metrics/UI/MetricsViewer.cs.meta" "Assets/Scripts/Metrics/Visualization/"
mv "Assets/Scripts/Metrics/UI/MetricsDashboard.cs" "Assets/Scripts/Metrics/Visualization/"
mv "Assets/Scripts/Metrics/UI/MetricsDashboard.cs.meta" "Assets/Scripts/Metrics/Visualization/"
mv "Assets/Scripts/Metrics/UI/DashboardTheme.cs" "Assets/Scripts/Metrics/Visualization/"
mv "Assets/Scripts/Metrics/UI/DashboardTheme.cs.meta" "Assets/Scripts/Metrics/Visualization/"
mv "Assets/Scripts/Metrics/UI/ChartBar.cs" "Assets/Scripts/Metrics/Visualization/"
mv "Assets/Scripts/Metrics/UI/ChartBar.cs.meta" "Assets/Scripts/Metrics/Visualization/"
mv "Assets/Scripts/Metrics/UI/ChartTexture.cs" "Assets/Scripts/Metrics/Visualization/"
mv "Assets/Scripts/Metrics/UI/ChartTexture.cs.meta" "Assets/Scripts/Metrics/Visualization/"
mv "Assets/Scripts/Metrics/UI/MetricsHtmlExporter.cs" "Assets/Scripts/Metrics/Visualization/"
mv "Assets/Scripts/Metrics/UI/MetricsHtmlExporter.cs.meta" "Assets/Scripts/Metrics/Visualization/"
mv "Assets/Scripts/Metrics/UI/UICapture.cs" "Assets/Scripts/Metrics/Visualization/"
mv "Assets/Scripts/Metrics/UI/UICapture.cs.meta" "Assets/Scripts/Metrics/Visualization/"

# Mover arquivos UI
echo "🔄 Movendo arquivos UI..."
mv "Assets/Scripts/UI/HUDController.cs" "Assets/Scripts/UI/Controllers/"
mv "Assets/Scripts/UI/HUDController.cs.meta" "Assets/Scripts/UI/Controllers/"
mv "Assets/Scripts/UI/WizardController.cs" "Assets/Scripts/UI/Controllers/"
mv "Assets/Scripts/UI/WizardController.cs.meta" "Assets/Scripts/UI/Controllers/"

mv "Assets/Scripts/Metrics/UI/MetricsCardUI.cs" "Assets/Scripts/UI/Components/"
mv "Assets/Scripts/Metrics/UI/MetricsCardUI.cs.meta" "Assets/Scripts/UI/Components/"
mv "Assets/Scripts/Metrics/UI/MetricsRowUI.cs" "Assets/Scripts/UI/Components/"
mv "Assets/Scripts/Metrics/UI/MetricsRowUI.cs.meta" "Assets/Scripts/UI/Components/"
mv "Assets/Scripts/Metrics/UI/MetricsPanelToggle.cs" "Assets/Scripts/UI/Components/"
mv "Assets/Scripts/Metrics/UI/MetricsPanelToggle.cs.meta" "Assets/Scripts/UI/Components/"
mv "Assets/Scripts/Metrics/UI/MetricsToggle.cs" "Assets/Scripts/UI/Components/"
mv "Assets/Scripts/Metrics/UI/MetricsToggle.cs.meta" "Assets/Scripts/UI/Components/"
mv "Assets/Scripts/UI/ToggleActive.cs" "Assets/Scripts/UI/Components/"
mv "Assets/Scripts/UI/ToggleActive.cs.meta" "Assets/Scripts/UI/Components/"

# Mover arquivos Tools
echo "🔄 Movendo arquivos Tools..."
mv "Assets/Scripts/Conversion/ModelConverter.cs" "Assets/Scripts/Tools/Conversion/"
mv "Assets/Scripts/Conversion/ModelConverter.cs.meta" "Assets/Scripts/Tools/Conversion/"

# Remover arquivos Debug
echo "🗑️ Removendo arquivos Debug..."
rm -rf "Assets/Scripts/Debug"

# Remover pastas vazias
echo "🧹 Removendo pastas vazias..."
rmdir "Assets/Scripts/Camera" 2>/dev/null || true
rmdir "Assets/Scripts/Conversion" 2>/dev/null || true
rmdir "Assets/Scripts/ModelLoading" 2>/dev/null || true
rmdir "Assets/Scripts/Utilities" 2>/dev/null || true
rmdir "Assets/Scripts/Metrics/Core" 2>/dev/null || true
rmdir "Assets/Scripts/Metrics/UI" 2>/dev/null || true

echo "✅ Migração concluída!"
echo "📋 Próximos passos:"
echo "   1. Abrir Unity e verificar se não há erros"
echo "   2. Executar testes básicos"
echo "   3. Verificar se prefabs ainda funcionam"
```

### Script PowerShell (Windows)

```powershell
# Script de migração automática - PolyDiet Unity (Windows)

Write-Host "🚀 Iniciando migração de pastas..." -ForegroundColor Green

# Criar estrutura de pastas
Write-Host "📁 Criando estrutura de pastas..." -ForegroundColor Yellow
New-Item -ItemType Directory -Path "Assets\Scripts\Core\Camera" -Force | Out-Null
New-Item -ItemType Directory -Path "Assets\Scripts\Core\ModelLoading" -Force | Out-Null
New-Item -ItemType Directory -Path "Assets\Scripts\Core\Utilities" -Force | Out-Null
New-Item -ItemType Directory -Path "Assets\Scripts\Metrics\Data" -Force | Out-Null
New-Item -ItemType Directory -Path "Assets\Scripts\Metrics\Collection" -Force | Out-Null
New-Item -ItemType Directory -Path "Assets\Scripts\Metrics\Reporting" -Force | Out-Null
New-Item -ItemType Directory -Path "Assets\Scripts\Metrics\Visualization" -Force | Out-Null
New-Item -ItemType Directory -Path "Assets\Scripts\UI\Controllers" -Force | Out-Null
New-Item -ItemType Directory -Path "Assets\Scripts\UI\Components" -Force | Out-Null
New-Item -ItemType Directory -Path "Assets\Scripts\UI\Events" -Force | Out-Null
New-Item -ItemType Directory -Path "Assets\Scripts\Tools\Conversion" -Force | Out-Null

# Mover arquivos Core
Write-Host "🔄 Movendo arquivos Core..." -ForegroundColor Yellow
Move-Item "Assets\Scripts\Camera\*" "Assets\Scripts\Core\Camera\" -Force
Move-Item "Assets\Scripts\ModelLoading\*" "Assets\Scripts\Core\ModelLoading\" -Force
Move-Item "Assets\Scripts\Utilities\CrossPlatformHelper.*" "Assets\Scripts\Core\Utilities\" -Force
Move-Item "Assets\Scripts\UI\UIInputLock.*" "Assets\Scripts\Core\Utilities\" -Force

# Mover arquivos Metrics
Write-Host "🔄 Movendo arquivos Metrics..." -ForegroundColor Yellow
Move-Item "Assets\Scripts\Metrics\Core\MetricsEntry.*" "Assets\Scripts\Metrics\Data\" -Force
Move-Item "Assets\Scripts\Metrics\Core\MetricsConfig.*" "Assets\Scripts\Metrics\Data\" -Force
Move-Item "Assets\Scripts\Metrics\Core\MetricsStore.*" "Assets\Scripts\Metrics\Data\" -Force
Move-Item "Assets\Scripts\Metrics\Core\MetricsPathProvider.*" "Assets\Scripts\Metrics\Data\" -Force
Move-Item "Assets\Scripts\Metrics\Core\MetricsAggregator.*" "Assets\Scripts\Metrics\Data\" -Force
Move-Item "Assets\Scripts\Metrics\Core\Metrics.*" "Assets\Scripts\Metrics\Collection\" -Force
Move-Item "Assets\Scripts\Metrics\Core\ReportRunner.*" "Assets\Scripts\Metrics\Reporting\" -Force
Move-Item "Assets\Scripts\Metrics\UI\MetricsViewer.*" "Assets\Scripts\Metrics\Visualization\" -Force
Move-Item "Assets\Scripts\Metrics\UI\MetricsDashboard.*" "Assets\Scripts\Metrics\Visualization\" -Force
Move-Item "Assets\Scripts\Metrics\UI\DashboardTheme.*" "Assets\Scripts\Metrics\Visualization\" -Force
Move-Item "Assets\Scripts\Metrics\UI\ChartBar.*" "Assets\Scripts\Metrics\Visualization\" -Force
Move-Item "Assets\Scripts\Metrics\UI\ChartTexture.*" "Assets\Scripts\Metrics\Visualization\" -Force
Move-Item "Assets\Scripts\Metrics\UI\MetricsHtmlExporter.*" "Assets\Scripts\Metrics\Visualization\" -Force
Move-Item "Assets\Scripts\Metrics\UI\UICapture.*" "Assets\Scripts\Metrics\Visualization\" -Force

# Mover arquivos UI
Write-Host "🔄 Movendo arquivos UI..." -ForegroundColor Yellow
Move-Item "Assets\Scripts\UI\HUDController.*" "Assets\Scripts\UI\Controllers\" -Force
Move-Item "Assets\Scripts\UI\WizardController.*" "Assets\Scripts\UI\Controllers\" -Force
Move-Item "Assets\Scripts\Metrics\UI\MetricsCardUI.*" "Assets\Scripts\UI\Components\" -Force
Move-Item "Assets\Scripts\Metrics\UI\MetricsRowUI.*" "Assets\Scripts\UI\Components\" -Force
Move-Item "Assets\Scripts\Metrics\UI\MetricsPanelToggle.*" "Assets\Scripts\UI\Components\" -Force
Move-Item "Assets\Scripts\Metrics\UI\MetricsToggle.*" "Assets\Scripts\UI\Components\" -Force
Move-Item "Assets\Scripts\UI\ToggleActive.*" "Assets\Scripts\UI\Components\" -Force

# Mover arquivos Tools
Write-Host "🔄 Movendo arquivos Tools..." -ForegroundColor Yellow
Move-Item "Assets\Scripts\Conversion\*" "Assets\Scripts\Tools\Conversion\" -Force

# Remover arquivos Debug
Write-Host "🗑️ Removendo arquivos Debug..." -ForegroundColor Red
Remove-Item "Assets\Scripts\Debug" -Recurse -Force

# Remover pastas vazias
Write-Host "🧹 Removendo pastas vazias..." -ForegroundColor Yellow
Remove-Item "Assets\Scripts\Camera" -Force -ErrorAction SilentlyContinue
Remove-Item "Assets\Scripts\Conversion" -Force -ErrorAction SilentlyContinue
Remove-Item "Assets\Scripts\ModelLoading" -Force -ErrorAction SilentlyContinue
Remove-Item "Assets\Scripts\Utilities" -Force -ErrorAction SilentlyContinue
Remove-Item "Assets\Scripts\Metrics\Core" -Force -ErrorAction SilentlyContinue
Remove-Item "Assets\Scripts\Metrics\UI" -Force -ErrorAction SilentlyContinue

Write-Host "✅ Migração concluída!" -ForegroundColor Green
Write-Host "📋 Próximos passos:" -ForegroundColor Cyan
Write-Host "   1. Abrir Unity e verificar se não há erros" -ForegroundColor White
Write-Host "   2. Executar testes básicos" -ForegroundColor White
Write-Host "   3. Verificar se prefabs ainda funcionam" -ForegroundColor White
```

---

## 📋 Checklist de Atualização de Referências

### Referências em Código

Após a migração, verificar e atualizar as seguintes referências:

#### 1. Imports/Using Statements

| Arquivo | Referência Atual | Referência Nova |
|---------|------------------|-----------------|
| `HUDController.cs` | `using ModelLoading;` | `using Core.ModelLoading;` |
| `WizardController.cs` | `using ModelLoading;` | `using Core.ModelLoading;` |
| `MetricsViewer.cs` | `using Metrics.Core;` | `using Metrics.Data;` |

#### 2. Referências de Componentes

Verificar se os seguintes componentes ainda estão attachados corretamente:

- **HUDController:**
  - `ModelViewer viewer` → `Core/ModelLoading/ModelViewer`
  - `SimpleOrbitCamera orbitCamera` → `Core/Camera/SimpleOrbitCamera`
  - `CompareLoader compareLoader` → `Core/ModelLoading/CompareLoader`
  - `CompareSplitView splitView` → `Core/ModelLoading/CompareSplitView`

- **WizardController:**
  - `ModelViewer viewer` → `Core/ModelLoading/ModelViewer`
  - `Metrics metrics` → `Metrics/Collection/Metrics`

- **MetricsViewer:**
  - `Metrics metrics` → `Metrics/Collection/Metrics`
  - `ReportRunner reportRunner` → `Metrics/Reporting/ReportRunner`

#### 3. Prefabs e Cenas

Verificar se os seguintes prefabs ainda funcionam:

- `LegacyMetricsPanel` (MetricsComparisonPanel)
- `CompareRoot`
- `Wizard`
- `HUD`

### Referências em Documentação

Atualizar os seguintes arquivos de documentação:

- `ORGANIZACAO.md` → Nova estrutura de pastas
- `README.md` → Novos caminhos
- Comentários inline → Novos namespaces

---

## 🧪 Plano de Testes Pós-Migração

### Testes Básicos

1. **Compilação**
   - [ ] Unity compila sem erros
   - [ ] Não há warnings de referências quebradas
   - [ ] Todos os scripts são encontrados

2. **Funcionalidade Core**
   - [ ] Carregamento de modelos funciona
   - [ ] Câmera orbital funciona
   - [ ] Sistema de métricas funciona
   - [ ] Comparação lado a lado funciona

3. **UI**
   - [ ] HUD abre e fecha corretamente
   - [ ] Wizard funciona
   - [ ] Painel de métricas funciona
   - [ ] Botões respondem

4. **Integração**
   - [ ] Fluxo completo funciona
   - [ ] Relatórios são gerados
   - [ ] Compressão de modelos funciona

### Testes de Regressão

1. **Performance**
   - [ ] Tempo de carregamento mantido
   - [ ] FPS mantido
   - [ ] Uso de memória mantido

2. **Compatibilidade**
   - [ ] Funciona no Linux
   - [ ] Funciona no Windows
   - [ ] CSVs existentes ainda funcionam

---

## 📊 Benefícios da Nova Estrutura

### Organizacionais

| Benefício | Descrição |
|-----------|-----------|
| **Clareza** | Responsabilidades bem definidas |
| **Escalabilidade** | Fácil adicionar novas funcionalidades |
| **Manutenibilidade** | Código mais fácil de encontrar e modificar |
| **Testabilidade** | Componentes isolados são mais fáceis de testar |

### Técnicos

| Benefício | Descrição |
|-----------|-----------|
| **Separação de Concerns** | Core vs Features vs UI |
| **Reutilização** | Componentes podem ser reutilizados |
| **Modularidade** | Sistema mais modular |
| **Debugging** | Mais fácil localizar problemas |

### Para Desenvolvedores

| Benefício | Descrição |
|-----------|-----------|
| **Onboarding** | Novos desenvolvedores entendem mais rápido |
| **Colaboração** | Menos conflitos de merge |
| **Documentação** | Estrutura auto-documentada |
| **Padrões** | Convenções claras |

---

## ⚠️ Riscos e Mitigações

### Riscos Identificados

| Risco | Probabilidade | Impacto | Mitigação |
|-------|---------------|---------|-----------|
| Referências quebradas | Alta | Médio | Script de migração + checklist |
| Prefabs quebrados | Média | Alto | Testes de regressão |
| Performance degradada | Baixa | Baixo | Testes de performance |
| Bugs de integração | Média | Médio | Testes extensivos |

### Plano de Rollback

Se algo der errado:

1. **Backup Antes da Migração**
   ```bash
   cp -r Assets/Scripts Assets/Scripts_backup
   ```

2. **Restaurar Estrutura Original**
   ```bash
   rm -rf Assets/Scripts
   mv Assets/Scripts_backup Assets/Scripts
   ```

3. **Verificar Funcionamento**
   - Abrir Unity
   - Executar testes básicos
   - Confirmar que tudo funciona

---

## 📅 Cronograma de Execução

### Fase 1: Preparação (30 min)
- [ ] Backup da estrutura atual
- [ ] Executar script de migração
- [ ] Verificar compilação

### Fase 2: Validação (60 min)
- [ ] Executar testes básicos
- [ ] Verificar prefabs
- [ ] Testar funcionalidades principais

### Fase 3: Ajustes (30 min)
- [ ] Corrigir referências quebradas
- [ ] Atualizar documentação
- [ ] Executar testes de regressão

### Fase 4: Finalização (30 min)
- [ ] Documentar mudanças
- [ ] Commit das alterações
- [ ] Notificar equipe

**Total Estimado:** 2.5 horas

---

**Data do Plano:** Janeiro 2025  
**Criado por:** AI Assistant  
**Próximo documento:** `CODE_STYLE_GUIDE.md`
