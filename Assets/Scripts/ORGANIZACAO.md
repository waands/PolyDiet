# Organização dos Scripts - PolyDiet

## Estrutura de Pastas

### 📷 Camera/
Scripts relacionados ao controle da câmera:
- `CameraPoseFollower.cs` - Faz a câmera seguir a pose de outra câmera
- `SimpleOrbitCamera.cs` - Controle de órbita da câmera principal

### 🎯 ModelLoading/
Scripts para carregamento e comparação de modelos 3D:
- `ModelViewer.cs` - Carregamento e visualização de modelos GLB/GLTF
- `CompareLoader.cs` - Carregamento de dois modelos para comparação
- `CompareSplitView.cs` - Sistema de visualização split-screen para comparar modelos

### 📊 Metrics/
Scripts para coleta, armazenamento e visualização de métricas de performance:

#### Core/
- `Metrics.cs` - Singleton que coleta métricas (FPS, tempo de carregamento, memória)
- `MetricsEntry.cs` - Estrutura de dados para uma entrada de métrica
- `MetricsStore.cs` - Carregamento e manipulação de dados CSV de métricas

#### UI/
- `MetricsViewer.cs` - Interface para visualizar métricas coletadas
- `MetricsCardUI.cs` - Card UI para exibir uma métrica específica
- `MetricsRowUI.cs` - Row UI para tabela de métricas
- `MetricsToggle.cs` - Controle de visibilidade do painel de métricas

### 🎨 UI/
Scripts de interface do usuário e controle geral:
- `HUDController.cs` - Controlador principal do HUD e interface
- `WizardController.cs` - Wizard guiado para importar/comprimir/testar modelos
- `UIInputLock.cs` - Sistema global para bloquear inputs quando painéis estão abertos
- `ToggleActive.cs` - Utilitário simples para mostrar/ocultar GameObjects

### 🔄 Conversion/
Scripts para conversão de formatos de modelo:
- `ModelConverter.cs` - Conversor automático de OBJ/FBX para GLB/GLTF

### 🛠️ Utilities/
Scripts auxiliares e helpers:
- `CrossPlatformHelper.cs` - Funções para garantir compatibilidade cross-platform (Linux/Windows)

### 🐛 Debug/
Scripts para debug, diagnóstico e testes:
- `ModelConverterTest.cs` - Testes e diagnóstico do conversor de modelos
- `WizardDebug.cs` - Debug do wizard controller
- `MetricsPanelDiagnostic.cs` - Diagnóstico do painel de métricas

## Fluxo Principal de Uso

### 1. Importação de Modelo
```
WizardController → ModelViewer.ScanModelsAndPopulateUI()
```

### 2. Carregamento e Visualização
```
HUDController → ModelViewer.LoadOnlyAsync() → SimpleOrbitCamera (auto-frame)
```

### 3. Comparação de Variantes
```
HUDController → CompareLoader.LoadBothAsync() → CompareSplitView.SetCompareActive()
```

### 4. Coleta de Métricas
```
Metrics.BeginLoad() → ModelViewer.Load → Metrics.EndLoad() → Metrics.MeasureFpsWindow() → Metrics.WriteCsv()
```

### 5. Visualização de Resultados
```
MetricsViewer.Refresh() → MetricsStore.Load() → MetricsCardUI/MetricsRowUI
```

## Dependências entre Módulos

```
UI/ → ModelLoading/ → Metrics/Core/
    → Camera/
    → Conversion/
    → Utilities/

Metrics/UI/ → Metrics/Core/

Debug/ → (todos os módulos para testes)
```

## Convenções de Código

### Nomenclatura
- Scripts principais: `PascalCase` (ex: `ModelViewer`)
- Métodos públicos: `PascalCase` (ex: `LoadOnlyAsync`)
- Métodos privados: `PascalCase` (padrão C#)
- Variáveis privadas: `_camelCase` com underscore (ex: `_currentContainer`)
- Variáveis locais: `camelCase` (ex: `modelName`)

### Padrões
- **Async/Await**: Métodos assíncronos sempre com sufixo `Async`
- **Singleton**: `Metrics.Instance` para acesso global
- **Events**: Uso de `UnityEvent` para comunicação entre componentes
- **Logs**: Prefixo `[NomeClasse]` para facilitar debug

## Funções Removidas (Não Utilizadas)

As seguintes funções foram identificadas e removidas por não estarem em uso:

### CrossPlatformHelper.cs
- ✂️ `GetPlatformInfo()` - Nunca chamado no projeto
- ✂️ `ToWindowsPath()` - Nunca chamado no projeto

### UIInputLock.cs
- ✂️ `ForceUnlockAll()` - Nunca chamado no projeto

### CompareLoader.cs
- ✂️ `LoadBoth()` - Wrapper síncrono não utilizado (usa-se `LoadBothAsync()`)

### HUDController.cs
- ✂️ `QuickLoadAsync()` - Método privado substituído por lógica no `OnCompareConfirmAsync()`

## Próximos Passos Recomendados

1. **Documentação inline**: Adicionar XML comments aos métodos públicos principais
2. **Testes unitários**: Criar testes para `MetricsStore` e `CrossPlatformHelper`
3. **Refatoração**: Considerar separar `ModelViewer.cs` (1053 linhas) em classes menores
4. **Validação**: Adicionar mais validações de entrada nos métodos públicos

## Notas de Manutenção

- **Unity Meta Files**: Sempre mover os arquivos `.meta` junto com os `.cs`
- **Referencias de Prefab**: Verificar se há prefabs que referenciam scripts movidos
- **Scene References**: Validar se as cenas não perderam referências após reorganização

---

**Data de Organização**: Outubro 2025  
**Versão Unity**: 2022.3+  
**Estrutura criada por**: Reorganização automática


