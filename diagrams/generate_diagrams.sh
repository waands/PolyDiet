#!/bin/bash
# Script para gerar diagramas PlantUML em múltiplos formatos
# Requer: plantuml instalado (ou docker)

set -e  # Exit on error

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$SCRIPT_DIR"

echo "=========================================="
echo "  Gerador de Diagramas UML - PolyDiet"
echo "=========================================="
echo ""

# Verifica se PlantUML está disponível
if command -v plantuml &> /dev/null; then
    GENERATOR="plantuml"
    echo "✓ PlantUML encontrado: $(which plantuml)"
elif [ -f "plantuml.jar" ]; then
    GENERATOR="java -jar plantuml.jar"
    echo "✓ Usando plantuml.jar local"
elif command -v docker &> /dev/null; then
    GENERATOR="docker run --rm -v $(pwd):/data -w /data plantuml/plantuml"
    echo "✓ Usando Docker plantuml/plantuml"
else
    echo "❌ Erro: PlantUML não encontrado!"
    echo ""
    echo "Instale uma das opções:"
    echo "  1. PlantUML via package manager:"
    echo "     - Ubuntu/Debian: sudo apt install plantuml"
    echo "     - Arch: sudo pacman -S plantuml"
    echo "     - macOS: brew install plantuml"
    echo "  2. Baixe plantuml.jar:"
    echo "     wget https://github.com/plantuml/plantuml/releases/download/v1.2024.7/plantuml-1.2024.7.jar -O plantuml.jar"
    echo "  3. Use Docker:"
    echo "     docker pull plantuml/plantuml"
    exit 1
fi

echo ""
echo "Arquivos encontrados:"
PUML_FILES=(*.puml)
for file in "${PUML_FILES[@]}"; do
    echo "  - $file"
done
echo ""

# Função para gerar diagramas
generate() {
    local format=$1
    local extension=$2
    echo "----------------------------------------"
    echo "Gerando diagramas em formato $format..."
    echo "----------------------------------------"
    
    for file in "${PUML_FILES[@]}"; do
        echo -n "  $file → ${file%.puml}.$extension ... "
        if [[ "$GENERATOR" == *"docker"* ]]; then
            $GENERATOR -t$format "$file" 2>&1 | grep -v "Warning" || true
        else
            $GENERATOR -t$format "$file" 2>&1 | grep -v "Warning" || true
        fi
        
        if [ -f "${file%.puml}.$extension" ]; then
            echo "✓"
        else
            echo "✗ (falhou)"
        fi
    done
    echo ""
}

# Menu de seleção
echo "Selecione o formato de saída:"
echo "  1) PNG (bitmap, padrão)"
echo "  2) SVG (vetorial, recomendado)"
echo "  3) PDF (via LaTeX, requer pdflatex)"
echo "  4) Todos os formatos"
echo "  5) Cancelar"
echo ""
read -p "Opção [1]: " choice
choice=${choice:-1}

case $choice in
    1)
        generate "png" "png"
        ;;
    2)
        generate "svg" "svg"
        ;;
    3)
        generate "pdf" "pdf"
        ;;
    4)
        generate "png" "png"
        generate "svg" "svg"
        ;;
    5)
        echo "Cancelado."
        exit 0
        ;;
    *)
        echo "Opção inválida. Usando PNG como padrão."
        generate "png" "png"
        ;;
esac

echo "=========================================="
echo "✓ Geração concluída!"
echo "=========================================="
echo ""
echo "Arquivos gerados em: $SCRIPT_DIR"
ls -lh *.png *.svg *.pdf 2>/dev/null || echo "(nenhum arquivo gerado)"
echo ""



