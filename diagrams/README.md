# Diagramas UML - PolyDiet

Este diretório contém os diagramas UML do sistema PolyDiet em formato PlantUML.

## Diagramas Disponíveis

### 1. Diagrama de Casos de Uso (`use_case_diagram.puml`)
- **Atores**: Pesquisador, Sistema Unity, Ferramentas CLI
- **Pacotes**:
  - Gerenciamento de Modelos
  - Conversão de Formato
  - Compressão de Modelos
  - Comparação Visual
  - Coleta de Métricas
  - Geração de Relatórios
  - Controle de Câmera
- **Relacionamentos**: include, extend entre casos de uso

### 2. Diagrama de Componentes (`component_diagram.puml`)
- **Camadas**:
  - Apresentação (Unity UI)
  - Visualização 3D
  - Gerenciamento de Modelos
  - Conversão de Formato
  - Compressão
  - Coleta de Métricas
  - Geração de Relatórios (Python)
  - Ferramentas CLI Externas
  - Utilitários
  - Armazenamento (StreamingAssets)
- **Interfaces**: ICamera, ILoader, IConversionStrategy, ICompressor, IMetricsCollector

## Como Visualizar

### Opção 1: Online (PlantUML Web Server)
1. Acesse: http://www.plantuml.com/plantuml/uml/
2. Cole o conteúdo do arquivo `.puml`
3. Visualize o diagrama renderizado

### Opção 2: VS Code (Extensão PlantUML)
1. Instale a extensão: `PlantUML` (jebbs.plantuml)
2. Abra o arquivo `.puml`
3. Pressione `Alt+D` para visualizar preview
4. Use `Ctrl+Shift+P` → "PlantUML: Export Current Diagram" para exportar

### Opção 3: CLI (plantuml.jar)
Instale PlantUML e execute:

```bash
# Gerar PNG
java -jar plantuml.jar use_case_diagram.puml
java -jar plantuml.jar component_diagram.puml

# Gerar SVG (qualidade vetorial)
java -jar plantuml.jar -tsvg use_case_diagram.puml
java -jar plantuml.jar -tsvg component_diagram.puml
```

### Opção 4: Docker
```bash
# Gerar todos os diagramas em PNG
docker run --rm -v $(pwd):/data plantuml/plantuml *.puml

# Gerar em SVG
docker run --rm -v $(pwd):/data plantuml/plantuml -tsvg *.puml
```

### Opção 5: Script Bash (Linux/Mac)
Execute o script fornecido:
```bash
chmod +x generate_diagrams.sh
./generate_diagrams.sh
```

## Dependências

### Para renderização local:
- **Java 8+** (para executar plantuml.jar)
- **GraphViz** (opcional, melhora renderização)
  - Ubuntu/Debian: `sudo apt install graphviz`
  - Arch: `sudo pacman -S graphviz`
  - macOS: `brew install graphviz`

### Para VS Code:
- Extensão: **PlantUML** (jebbs.plantuml)
- Servidor local ou servidor web PlantUML

## Formatos de Exportação

PlantUML suporta múltiplos formatos:
- **PNG** (bitmap, para documentos)
- **SVG** (vetorial, escalável)
- **PDF** (via LaTeX)
- **EPS** (PostScript)
- **ASCII Art** (texto puro)

## Notas

- Os diagramas foram gerados a partir da análise do código-fonte do PolyDiet
- Refletem a arquitetura implementada em 2025-10-20
- Para alterações, edite os arquivos `.puml` diretamente
- Sintaxe PlantUML: https://plantuml.com/

## Integração com LaTeX

Para incluir no seu TCC (LaTeX):

```latex
\begin{figure}[H]
  \centering
  \includegraphics[width=\textwidth]{diagrams/use_case_diagram.png}
  \caption{Diagrama de Casos de Uso do Sistema PolyDiet}
  \label{fig:use_case}
\end{figure}
```

## Licença

Estes diagramas fazem parte do projeto PolyDiet (TCC).


