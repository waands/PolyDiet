# Adaptação Linux - PolyDiet

Este documento descreve as adaptações realizadas para tornar o projeto PolyDiet compatível com Linux, migrando de um ambiente Windows original.

## Problemas Identificados e Soluções

### 1. Erro do Pacote Draco WebGL
**Problema**: TimeoutException no SubPackageRemover do pacote `com.unity.cloud.draco@4b657c9cdeaa`

**Solução**: 
- Downgrade da versão do pacote `com.unity.cloud.draco` de `5.2.0` para `5.1.0` no `Packages/manifest.json`
- Esta versão é mais estável e não apresenta os problemas de timeout

### 2. Caminhos de Executáveis Hardcoded
**Problema**: Os caminhos para `gltf-transform` e `gltfpack` estavam fixos para uma configuração específica

**Solução**:
- Criação de funções dinâmicas de detecção:
  - `GetGltfTransformLinux()`: Detecta automaticamente o gltf-transform do NVM ou PATH
  - `GetGltfpackLinux()`: Procura gltfpack em locais comuns (`/usr/bin/`, `/usr/local/bin/`)
- Fallback para PATH do sistema quando não encontrado em locais específicos

### 3. Separadores de Caminho
**Problema**: Uso inconsistente de separadores de caminho (/ vs \\)

**Solução**:
- Criação da classe `CrossPlatformHelper` com métodos utilitários:
  - `NormalizePath()`: Normaliza caminhos para a plataforma atual
  - `CombinePaths()`: Wrapper para `Path.Combine()` 
  - `ToUnixPath()` / `ToWindowsPath()`: Conversores específicos
  - `EnsureDirectoryExists()`: Criação segura de diretórios

### 4. Validação de Executáveis
**Problema**: Falhas silenciosas quando executáveis não estão disponíveis

**Solução**:
- Método `CrossPlatformHelper.ExecutableExists()` para verificar disponibilidade
- Validação antes da execução com mensagens de erro informativas
- Instruções claras de instalação quando executáveis não são encontrados

## Arquivos Modificados

### Scripts Principais
- `Assets/Scripts/ModelViewer.cs`: Lógica principal de compressão adaptada
- `Assets/Scripts/Metrics.cs`: Sistema de métricas com caminhos cross-platform
- `Assets/Scripts/CrossPlatformHelper.cs`: **NOVO** - Utilitários cross-platform

### Editor
- `Assets/Editor/GltfCompressorWindow.cs`: Ferramenta do editor adaptada
- `Assets/Editor/LinuxCompatibilityTester.cs`: **NOVO** - Ferramenta de teste

### Configuração
- `Packages/manifest.json`: Versão do Draco ajustada

## Como Usar no Linux

### Pré-requisitos
1. **gltf-transform**: Instalar via npm
   ```bash
   npm install -g @gltf-transform/cli
   ```

2. **gltfpack**: Instalar via package manager
   ```bash
   # Ubuntu/Debian
   sudo apt install meshoptimizer-tools
   
   # Arch Linux
   sudo pacman -S meshoptimizer
   
   # Ou compilar do fonte: https://github.com/zeux/meshoptimizer
   ```

### Verificação da Instalação
1. Abra o Unity Editor
2. Vá em `Tools > Test Linux Compatibility`
3. Verifique se todos os executáveis são detectados corretamente
4. Use o botão "Log Test Results" para detalhes no console

### Estrutura de Modelos
Certifique-se de que seus modelos estão organizados em:
```
Assets/StreamingAssets/Models/
├── modelo1/
│   ├── original/
│   │   └── model.glb
│   ├── draco/          # Gerado automaticamente
│   └── meshopt/        # Gerado automaticamente
└── modelo2/
    └── ...
```

## Detecção Automática

O sistema agora detecta automaticamente:
- **Node.js/NVM**: Procura em `~/.nvm/versions/node/*/bin/gltf-transform`
- **Sistema**: Verifica PATH do sistema
- **Caminhos Comuns**: `/usr/bin/`, `/usr/local/bin/`

## Logs e Debugging

O sistema agora fornece logs mais detalhados:
- ✅ Sucesso com informações de compressão
- ❌ Falhas com detalhes específicos
- 📍 Caminhos detectados e utilizados
- 📊 Estatísticas de compressão (ratio, tamanhos)

## Compatibilidade

- ✅ **Linux**: Totalmente suportado
- ✅ **Windows**: Compatibilidade mantida
- ✅ **Editor Unity**: Ambas as plataformas
- ✅ **Build**: Standalone para ambas as plataformas

## Troubleshooting

### gltf-transform não encontrado
```
[CompressDraco] gltf-transform não encontrado: gltf-transform
[CompressDraco] Instale com: npm i -g @gltf-transform/cli
```
**Solução**: Instalar gltf-transform globalmente via npm

### gltfpack não encontrado
```
[CompressMeshopt] Executável não encontrado no PATH: gltfpack
[CompressMeshopt] Instale o gltfpack ou ajuste o caminho
```
**Solução**: Instalar meshoptimizer-tools ou compilar do fonte

### Permissões de arquivo
Se houver problemas de permissão, certifique-se de que:
- O diretório `StreamingAssets` tem permissões de escrita
- Os executáveis têm permissão de execução (`chmod +x`)

## Melhorias Futuras

- [ ] Detecção automática de versões do Node.js via `node --version`
- [ ] Cache de caminhos detectados para melhor performance
- [ ] Interface gráfica para configuração manual de caminhos
- [ ] Suporte a outros compressores (Basis Universal, etc.)