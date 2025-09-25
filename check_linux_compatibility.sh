#!/bin/bash

echo "=== PolyDiet - Linux Compatibility Check ==="
echo "Data: $(date)"
echo "Sistema: $(uname -a)"
echo ""

# Cores para output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

success=0
total=0

check_command() {
    local cmd=$1
    local name=$2
    echo -n "Verificando $name... "
    total=$((total + 1))
    
    if command -v "$cmd" &> /dev/null; then
        echo -e "${GREEN}✓ Encontrado${NC}"
        echo "  Caminho: $(which "$cmd")"
        if [[ "$cmd" == "gltf-transform" ]]; then
            echo "  Versão: $(gltf-transform --version 2>/dev/null || echo 'N/A')"
        elif [[ "$cmd" == "gltfpack" ]]; then
            echo "  Versão: $(gltfpack 2>&1 | head -1 | grep -o 'gltfpack [0-9.]*' || echo 'N/A')"
        fi
        success=$((success + 1))
        echo ""
        return 0
    else
        echo -e "${RED}✗ Não encontrado${NC}"
        return 1
    fi
}

echo "🔍 Verificando executáveis necessários:"
echo ""

# Verificar Node.js
check_command "node" "Node.js"

# Verificar npm
check_command "npm" "npm"

# Verificar gltf-transform
if ! check_command "gltf-transform" "gltf-transform"; then
    echo -e "  ${YELLOW}💡 Para instalar: npm install -g @gltf-transform/cli${NC}"
    echo ""
fi

# Verificar gltfpack
if ! check_command "gltfpack" "gltfpack"; then
    echo -e "  ${YELLOW}💡 Para instalar no Ubuntu/Debian: sudo apt install meshoptimizer-tools${NC}"
    echo -e "  ${YELLOW}💡 Para instalar no Arch: sudo pacman -S meshoptimizer${NC}"
    echo ""
fi

# Verificar Unity Hub (se disponível)
check_command "unity-editor" "Unity Editor" || check_command "unityhub" "Unity Hub" || echo "Unity Hub: Verificação manual necessária"

echo "📁 Verificando estrutura de diretórios:"
echo ""

project_root="/home/wands/Documentos/tcc/PolyDiet"
streaming_assets="$project_root/Assets/StreamingAssets"
models_dir="$streaming_assets/Models"

check_directory() {
    local dir=$1
    local name=$2
    echo -n "  $name... "
    
    if [[ -d "$dir" ]]; then
        echo -e "${GREEN}✓ Existe${NC}"
        if [[ "$dir" == "$models_dir" ]]; then
            local model_count=$(find "$dir" -mindepth 1 -maxdepth 1 -type d 2>/dev/null | wc -l)
            echo "    Modelos encontrados: $model_count"
        fi
        return 0
    else
        echo -e "${YELLOW}⚠ Não existe${NC}"
        return 1
    fi
}

check_directory "$project_root" "Diretório do projeto"
check_directory "$streaming_assets" "StreamingAssets"
check_directory "$models_dir" "Diretório Models"

# Verificar permissões de escrita
echo ""
echo -n "📝 Testando permissões de escrita... "
test_file="$streaming_assets/.write_test"
if touch "$test_file" 2>/dev/null && rm "$test_file" 2>/dev/null; then
    echo -e "${GREEN}✓ OK${NC}"
    success=$((success + 1))
else
    echo -e "${RED}✗ Falha${NC}"
    echo -e "  ${YELLOW}💡 Verifique as permissões do diretório StreamingAssets${NC}"
fi
total=$((total + 1))

echo ""
echo "=== Resumo ==="
echo "Verificações passaram: $success/$total"

if [[ $success -eq $total ]]; then
    echo -e "${GREEN}🎉 Tudo pronto! Seu ambiente Linux está configurado corretamente.${NC}"
    exit 0
elif [[ $success -gt $((total / 2)) ]]; then
    echo -e "${YELLOW}⚠ Configuração parcial. Alguns itens precisam de atenção.${NC}"
    exit 1
else
    echo -e "${RED}❌ Várias configurações precisam ser corrigidas.${NC}"
    exit 2
fi