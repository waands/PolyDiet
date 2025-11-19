# PolyDiet

Projeto de Trabalho de Conclusão de Curso (TCC) - Ferramenta de análise e comparação de performance de modelos 3D com diferentes técnicas de compressão.

## Sobre o Projeto

O PolyDiet é uma ferramenta desenvolvida em Unity para análise comparativa de performance de modelos 3D. O sistema permite carregar, comprimir (usando Draco e Meshopt), comparar visualmente e coletar métricas detalhadas de performance (FPS, tempo de carregamento, uso de memória) de diferentes variantes de modelos 3D.

Este projeto foi desenvolvido como parte de um Trabalho de Conclusão de Curso, focando na análise de técnicas de compressão de geometria 3D e seu impacto na performance de renderização.

**Link para o TCC**: [Adicione aqui o link para o seu trabalho de conclusão de curso]

## Funcionalidades Principais

### Gerenciamento de Modelos
- Carregamento de modelos 3D com suporte para formatos GLB/GLTF nativos
- Conversão automática de OBJ/FBX para GLB durante a importação
- Validação automática de arquivos GLTF/GLB
- Descoberta dinâmica de modelos em `StreamingAssets/Models/`

### Compressão de Modelos
- Compressão Draco para redução de tamanho usando algoritmo Draco (via gltf-transform)
- Compressão Meshopt para otimização de geometria (via gltfpack)
- Configuração de níveis de compressão

### Comparação Visual
- Modo Split-View para visualização lado a lado de duas variantes do mesmo modelo
- Sincronização de câmeras para comparação precisa
- Controle da proporção da divisão da tela

### Coleta de Métricas
- Benchmark automático com execução de testes de performance
- Métricas coletadas:
  - Tempo de carregamento (milissegundos)
  - FPS médio durante renderização
  - Uso de memória (bytes)
- Armazenamento dos dados em CSV para análise posterior

### Geração de Relatórios
- Relatórios HTML interativos com visualização detalhada dos resultados
- Gráficos PNG com gráficos de barras e comparações visuais
- Exportação para PDF para documentação
- Exportação JSON com dados estruturados para análise programática
- Estatísticas avançadas: médias, desvios padrão, percentis e comparações percentuais

## Requisitos do Sistema

### Software Base
- Unity Editor versão 6000.2.4f1 ou compatível
- Node.js versão 18+ (recomendado via NVM)
- Python versão 3.8 ou superior
- Chromium/Chrome para geração de PDFs (opcional, mas recomendado)

### Dependências Node.js (instaladas globalmente)
- `@gltf-transform/cli` - Ferramenta para compressão Draco e conversão GLTF→GLB
- `obj2gltf` - Conversor de OBJ para GLB

### Dependências Python
- `pandas` - Manipulação e análise de dados
- `numpy` - Operações numéricas
- `plotly` - Geração de gráficos interativos
- `jinja2` - Templates para relatórios
- `kaleido` - Exportação de gráficos Plotly para PNG

### Ferramentas do Sistema
- gltfpack - Compressão Meshopt (instalado via gerenciador de pacotes do sistema ou npm)

## Instalação

### Opção 1: Instalação Automática (Recomendado)

Execute o script de instalação que detecta automaticamente seu sistema e instala as dependências:

```bash
chmod +x setup.sh
./setup.sh
```

O script vai:
- Detectar seu sistema operacional
- Verificar dependências já instaladas
- Instalar Node.js packages globalmente
- Criar ambiente virtual Python e instalar dependências
- Tentar instalar gltfpack via gerenciador de pacotes
- Detectar caminhos automaticamente
- Validar todas as instalações

### Opção 2: Instalação Manual

#### 1. Instalar Node.js

**Linux (via NVM - Recomendado):**
```bash
curl -o- https://raw.githubusercontent.com/nvm-sh/nvm/v0.39.0/install.sh | bash
source ~/.bashrc  # ou ~/.zshrc
nvm install 22
nvm use 22
```

**Linux (via gerenciador de pacotes):**
```bash
# Ubuntu/Debian
sudo apt update
sudo apt install nodejs npm

# Arch Linux
sudo pacman -S nodejs npm
```

**Windows:**
- Baixe o instalador em https://nodejs.org/
- Instale a versão LTS

#### 2. Instalar Packages Node.js Globais

```bash
npm install -g @gltf-transform/cli
npm install -g obj2gltf
```

#### 3. Instalar gltfpack

**Linux:**
```bash
# Ubuntu/Debian
sudo apt install meshoptimizer-tools

# Arch Linux
sudo pacman -S meshoptimizer
```

**Windows:**
- Baixe o binário em https://github.com/zeux/meshoptimizer/releases
- Coloque `gltfpack.exe` em `Assets/StreamingAssets/Tools/` ou adicione ao PATH

#### 4. Configurar Python

**Criar ambiente virtual:**
```bash
cd /caminho/para/PolyDiet
python3 -m venv reports_env
source reports_env/bin/activate  # Linux/Mac
# ou
reports_env\Scripts\activate  # Windows
```

**Instalar dependências Python:**
```bash
pip install -r Assets/Scripts/Metrics/reports_tool/requirements.txt
```

#### 5. Verificar Chromium/Chrome (para PDFs)

**Linux:**
```bash
# Ubuntu/Debian
sudo apt install chromium-browser

# Arch Linux
sudo pacman -S chromium
```

**Windows/Mac:**
- Instale Chrome normalmente em https://www.google.com/chrome/

## Configuração

### Configuração Centralizada (Recomendado)

O PolyDiet agora usa um sistema de configuração centralizada através de um ScriptableObject. Isso permite configurar todos os caminhos e ferramentas em um único lugar:

1. **Criar o arquivo de configuração:**
   - No Unity, vá em `Tools > PolyDiet > Criar Config`
   - Ou crie manualmente: `Create > PolyDiet > Config`
   - O arquivo será criado em `Assets/Resources/PolyDietConfig.asset`

2. **Configurar os caminhos:**
   - Abra o asset `PolyDietConfig` criado
   - Configure todos os caminhos necessários:
     - **Python**: Caminho para o executável Python (deixe vazio para auto-detecção)
     - **Script Python**: Caminho para `simple_report_generator.py` (deixe vazio para padrão)
     - **gltf-transform**: Caminho para gltf-transform (deixe vazio para auto-detecção)
     - **obj2gltf**: Caminho para obj2gltf (deixe vazio para auto-detecção)
     - **gltfpack**: Caminho para gltfpack (deixe vazio para auto-detecção)
     - **Chromium**: Caminho para Chromium/Chrome (deixe vazio para auto-detecção)
     - **URL do Repositório**: URL do GitHub (já vem com padrão)

3. **Usar a configuração:**
   - A configuração é carregada automaticamente
   - Você pode referenciar o asset `PolyDietConfig` nos componentes `ReportRunner` e `StartScreenController` se quiser usar uma config específica
   - Se não referenciar, o sistema usa a config de `Resources/PolyDietConfig.asset` automaticamente

### Caminhos Automáticos

Se você deixar os campos vazios no `PolyDietConfig`, o sistema tenta detectar automaticamente:

- **Node.js/NVM**: `~/.nvm/versions/node/*/bin/`
- **gltf-transform**: No PATH ou em `~/.nvm/versions/node/*/bin/gltf-transform`
- **gltfpack**: `/usr/bin/gltfpack`, `/usr/local/bin/gltfpack` ou no PATH
- **Python**: Ambiente virtual em `reports_env/bin/python` ou Python do sistema
- **Chromium**: `chromium`, `chromium-browser` ou `google-chrome` no PATH

### Configuração Manual (Legado - ainda funciona)

Se preferir configurar manualmente nos componentes individuais (método antigo):

1. Abra o projeto no Unity
2. No `ReportRunner`, configure os campos de Path se necessário
3. No `StartScreenController`, configure a URL do repositório se necessário

**Nota**: Os campos nos componentes individuais têm prioridade sobre a config centralizada se preenchidos.

### Estrutura de Diretórios

O projeto espera a seguinte estrutura:

```
PolyDiet/
├── Assets/
│   ├── StreamingAssets/
│   │   └── Models/
│   │       └── {nome-do-modelo}/
│   │           ├── model.glb          # Modelo principal
│   │           ├── model_draco.glb    # Variante Draco (opcional)
│   │           ├── model_meshopt.glb  # Variante Meshopt (opcional)
│   │           └── benchmark/
│   │               ├── benchmarks.csv  # Dados de métricas
│   │               └── reports/        # Relatórios gerados
│   └── Scripts/
│       └── Metrics/
│           └── reports_tool/
│               ├── simple_report_generator.py
│               └── requirements.txt
└── reports_env/  # Ambiente virtual Python (criado durante instalação)
```

## Como Usar

### 1. Preparar Modelos

Coloque seus modelos 3D em `Assets/StreamingAssets/Models/{nome-do-modelo}/`:
- Modelo original: `model.glb`
- Variante Draco: `model_draco.glb` (opcional)
- Variante Meshopt: `model_meshopt.glb` (opcional)

### 2. Abrir o Projeto no Unity

1. Abra o Unity Hub
2. Adicione o projeto (selecione a pasta `PolyDiet`)
3. Abra o projeto (Unity 6000.2.4f1)

### 3. Executar Benchmark

1. Na cena `ModelViewer`, selecione um modelo no dropdown
2. Clique em "Executar Benchmark" ou use o painel de métricas
3. O sistema irá:
   - Carregar o modelo original
   - Coletar métricas (tempo de carregamento, FPS, memória)
   - Repetir para variantes comprimidas (se disponíveis)
   - Salvar dados em CSV

### 4. Gerar Relatórios

1. Abra o painel de relatórios
2. Selecione o modelo desejado
3. Clique em "Gerar Relatório"
4. O sistema irá:
   - Processar dados CSV
   - Gerar gráficos PNG
   - Criar relatório HTML
   - Exportar PDF (se Chromium disponível)
   - Exportar JSON com dados estruturados

### 5. Comparar Visualmente

1. Use o modo Split-View para comparar duas variantes lado a lado
2. Ajuste a divisão da tela conforme necessário
3. As câmeras são sincronizadas automaticamente

## Estrutura do Projeto

```
PolyDiet/
├── Assets/
│   ├── Scripts/
│   │   ├── Core/
│   │   │   ├── Camera/          # Controle de câmera
│   │   │   ├── ModelLoading/    # Carregamento de modelos
│   │   │   └── Utilities/      # Utilitários cross-platform
│   │   ├── Metrics/
│   │   │   ├── Core/           # Coleta de métricas
│   │   │   ├── Data/           # Armazenamento CSV
│   │   │   ├── Reporting/      # Integração com Python
│   │   │   └── reports_tool/   # Scripts Python
│   │   ├── Tools/
│   │   │   └── Conversion/     # Conversão de formatos
│   │   └── UI/                 # Interface do usuário
│   └── StreamingAssets/
│       └── Models/             # Modelos 3D
├── diagrams/                  # Diagramas UML
├── setup.sh                   # Script de instalação
└── README.md                  # Este arquivo
```

## Troubleshooting

### Problema: gltf-transform não encontrado

**Solução:**
```bash
# Verificar se está instalado
npm list -g @gltf-transform/cli

# Se não estiver, instalar
npm install -g @gltf-transform/cli

# Verificar PATH
which gltf-transform  # Linux/Mac
where gltf-transform  # Windows
```

### Problema: Python não encontrado

**Solução:**
- Verifique se o ambiente virtual está ativado
- Configure manualmente o pythonPath no ReportRunner
- Certifique-se de que Python 3.8+ está instalado

### Problema: gltfpack não encontrado

**Solução Linux:**
```bash
# Instalar via gerenciador de pacotes
sudo apt install meshoptimizer-tools  # Ubuntu/Debian
sudo pacman -S meshoptimizer          # Arch

# Ou via npm
npm install -g gltfpack
```

**Solução Windows:**
- Baixe gltfpack.exe de https://github.com/zeux/meshoptimizer/releases
- Coloque em `Assets/StreamingAssets/Tools/gltfpack.exe`

### Problema: Erro ao gerar PDF

**Solução:**
- Certifique-se de que Chromium/Chrome está instalado
- Verifique se o caminho está no PATH
- O sistema vai tentar usar chromium, chromium-browser ou google-chrome

### Problema: Modelos não aparecem no dropdown

**Solução:**
- Verifique se os modelos estão em `Assets/StreamingAssets/Models/{nome}/model.glb`
- Certifique-se de que os arquivos têm extensão `.glb` ou `.gltf`
- Use o botão "Atualizar Lista" no painel

## Documentação Adicional

- [Decisões de Implementação](decisoes_implementacao.md) - Detalhes técnicos das escolhas de implementação
- [Organização dos Scripts](Assets/Scripts/ORGANIZACAO.md) - Estrutura e organização do código
- [Diagramas UML](diagrams/README.md) - Diagramas de casos de uso e componentes

---

**Repositório**: [https://github.com/waands/PolyDiet](https://github.com/waands/PolyDiet)

## Contribuindo

Este é um projeto de TCC. Para questões ou sugestões, abra uma issue no repositório.

## Licença

Este projeto foi desenvolvido como parte de um Trabalho de Conclusão de Curso.

