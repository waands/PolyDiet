#!/bin/bash

# PolyDiet - Script de Instalação Automática
# Detecta o sistema operacional e instala todas as dependências necessárias

set -e  # Parar em caso de erro

# Cores para output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Variáveis
PROJECT_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REQUIREMENTS_FILE="$PROJECT_ROOT/Assets/Scripts/Metrics/reports_tool/requirements.txt"
VENV_DIR="$PROJECT_ROOT/reports_env"
CONFIG_FILE="$PROJECT_ROOT/.polydiet_config"

# Contadores
INSTALLED=0
SKIPPED=0
FAILED=0

# Funções auxiliares
print_header() {
    echo ""
    echo -e "${BLUE}========================================${NC}"
    echo -e "${BLUE}$1${NC}"
    echo -e "${BLUE}========================================${NC}"
    echo ""
}

print_success() {
    echo -e "${GREEN}✓${NC} $1"
    INSTALLED=$((INSTALLED + 1))
}

print_skip() {
    echo -e "${YELLOW}⊘${NC} $1 (já instalado)"
    SKIPPED=$((SKIPPED + 1))
}

print_error() {
    echo -e "${RED}✗${NC} $1"
    FAILED=$((FAILED + 1))
}

print_info() {
    echo -e "${BLUE}ℹ${NC} $1"
}

# Detectar sistema operacional
detect_os() {
    if [[ "$OSTYPE" == "linux-gnu"* ]]; then
        if [ -f /etc/os-release ]; then
            . /etc/os-release
            OS=$ID
            OS_VERSION=$VERSION_ID
        else
            OS="linux"
        fi
    elif [[ "$OSTYPE" == "darwin"* ]]; then
        OS="macos"
    elif [[ "$OSTYPE" == "msys" || "$OSTYPE" == "cygwin" ]]; then
        OS="windows"
    else
        OS="unknown"
    fi
    
    print_info "Sistema detectado: $OS"
}

# Verificar se comando existe
command_exists() {
    command -v "$1" >/dev/null 2>&1
}

# Verificar Node.js
check_nodejs() {
    if command_exists node; then
        NODE_VERSION=$(node --version)
        print_success "Node.js encontrado: $NODE_VERSION"
        NODE_PATH=$(which node)
        return 0
    else
        print_error "Node.js não encontrado"
        return 1
    fi
}

# Verificar npm
check_npm() {
    if command_exists npm; then
        NPM_VERSION=$(npm --version)
        print_success "npm encontrado: $NPM_VERSION"
        return 0
    else
        print_error "npm não encontrado"
        return 1
    fi
}

# Instalar Node.js (Linux)
install_nodejs_linux() {
    print_info "Tentando instalar Node.js..."
    
    # Verificar NVM
    if [ -s "$HOME/.nvm/nvm.sh" ]; then
        print_info "NVM encontrado, usando NVM para instalar Node.js"
        source "$HOME/.nvm/nvm.sh"
        if ! nvm list | grep -q "v22"; then
            nvm install 22
            nvm use 22
        fi
        nvm alias default 22
        source "$HOME/.nvm/nvm.sh"
        check_nodejs
        return $?
    fi
    
    # Tentar instalar via gerenciador de pacotes
    if [ "$OS" == "ubuntu" ] || [ "$OS" == "debian" ]; then
        print_info "Instalando Node.js via apt..."
        sudo apt update
        sudo apt install -y nodejs npm
        check_nodejs
        return $?
    elif [ "$OS" == "arch" ] || [ "$OS" == "manjaro" ]; then
        print_info "Instalando Node.js via pacman..."
        sudo pacman -S --noconfirm nodejs npm
        check_nodejs
        return $?
    else
        print_error "Não foi possível instalar Node.js automaticamente"
        print_info "Por favor, instale Node.js manualmente: https://nodejs.org/"
        return 1
    fi
}

# Instalar packages npm globais
install_npm_packages() {
    print_header "Instalando Packages npm Globais"
    
    # gltf-transform
    if command_exists gltf-transform; then
        GLTF_VERSION=$(gltf-transform --version 2>/dev/null || echo "instalado")
        print_skip "gltf-transform ($GLTF_VERSION)"
    else
        print_info "Instalando @gltf-transform/cli..."
        if npm install -g @gltf-transform/cli; then
            print_success "gltf-transform instalado"
        else
            print_error "Falha ao instalar gltf-transform"
            return 1
        fi
    fi
    
    # obj2gltf
    if command_exists obj2gltf; then
        print_skip "obj2gltf"
    else
        print_info "Instalando obj2gltf..."
        if npm install -g obj2gltf; then
            print_success "obj2gltf instalado"
        else
            print_error "Falha ao instalar obj2gltf"
            return 1
        fi
    fi
}

# Instalar gltfpack
install_gltfpack() {
    print_header "Instalando gltfpack"
    
    if command_exists gltfpack; then
        print_skip "gltfpack"
        return 0
    fi
    
    if [ "$OS" == "ubuntu" ] || [ "$OS" == "debian" ]; then
        print_info "Instalando gltfpack via apt..."
        if sudo apt install -y meshoptimizer-tools; then
            print_success "gltfpack instalado"
            return 0
        fi
    elif [ "$OS" == "arch" ] || [ "$OS" == "manjaro" ]; then
        print_info "Instalando gltfpack via pacman..."
        if sudo pacman -S --noconfirm meshoptimizer; then
            print_success "gltfpack instalado"
            return 0
        fi
    fi
    
    # Fallback: tentar via npm
    print_info "Tentando instalar gltfpack via npm..."
    if npm install -g gltfpack; then
        print_success "gltfpack instalado via npm"
        return 0
    fi
    
    print_error "Não foi possível instalar gltfpack automaticamente"
    print_info "Instale manualmente ou baixe de: https://github.com/zeux/meshoptimizer/releases"
    return 1
}

# Verificar Python
check_python() {
    if command_exists python3; then
        PYTHON_VERSION=$(python3 --version)
        print_success "Python encontrado: $PYTHON_VERSION"
        PYTHON_PATH=$(which python3)
        return 0
    elif command_exists python; then
        PYTHON_VERSION=$(python --version)
        if python -c "import sys; exit(0 if sys.version_info >= (3, 8) else 1)"; then
            print_success "Python encontrado: $PYTHON_VERSION"
            PYTHON_PATH=$(which python)
            return 0
        fi
    fi
    
    print_error "Python 3.8+ não encontrado"
    return 1
}

# Criar ambiente virtual Python
setup_python_venv() {
    print_header "Configurando Ambiente Virtual Python"
    
    if [ -d "$VENV_DIR" ]; then
        print_skip "Ambiente virtual já existe: $VENV_DIR"
    else
        print_info "Criando ambiente virtual..."
        if python3 -m venv "$VENV_DIR"; then
            print_success "Ambiente virtual criado"
        else
            print_error "Falha ao criar ambiente virtual"
            return 1
        fi
    fi
    
    # Ativar ambiente virtual
    source "$VENV_DIR/bin/activate"
    
    # Atualizar pip
    print_info "Atualizando pip..."
    pip install --upgrade pip --quiet
    
    # Instalar dependências
    if [ -f "$REQUIREMENTS_FILE" ]; then
        print_info "Instalando dependências Python..."
        if pip install -r "$REQUIREMENTS_FILE" --quiet; then
            print_success "Dependências Python instaladas"
        else
            print_error "Falha ao instalar dependências Python"
            return 1
        fi
    else
        print_error "Arquivo requirements.txt não encontrado: $REQUIREMENTS_FILE"
        return 1
    fi
}

# Detectar caminhos automaticamente
detect_paths() {
    print_header "Detectando Caminhos"
    
    # Node.js
    if command_exists node; then
        NODE_PATH=$(which node)
        print_success "Node.js: $NODE_PATH"
    fi
    
    # gltf-transform
    if command_exists gltf-transform; then
        GLTF_PATH=$(which gltf-transform)
        print_success "gltf-transform: $GLTF_PATH"
    fi
    
    # obj2gltf
    if command_exists obj2gltf; then
        OBJ2GLTF_PATH=$(which obj2gltf)
        print_success "obj2gltf: $OBJ2GLTF_PATH"
    fi
    
    # gltfpack
    if command_exists gltfpack; then
        GLTFPACK_PATH=$(which gltfpack)
        print_success "gltfpack: $GLTFPACK_PATH"
    fi
    
    # Python
    if [ -f "$VENV_DIR/bin/python" ]; then
        PYTHON_VENV_PATH="$VENV_DIR/bin/python"
        print_success "Python (venv): $PYTHON_VENV_PATH"
    elif command_exists python3; then
        PYTHON_VENV_PATH=$(which python3)
        print_success "Python: $PYTHON_VENV_PATH"
    fi
    
    # Chromium/Chrome
    if command_exists chromium; then
        CHROMIUM_PATH=$(which chromium)
        print_success "Chromium: $CHROMIUM_PATH"
    elif command_exists chromium-browser; then
        CHROMIUM_PATH=$(which chromium-browser)
        print_success "Chromium: $CHROMIUM_PATH"
    elif command_exists google-chrome; then
        CHROMIUM_PATH=$(which google-chrome)
        print_success "Chrome: $CHROMIUM_PATH"
    else
        print_info "Chromium/Chrome não encontrado (opcional para PDFs)"
    fi
}

# Criar arquivo de configuração
create_config() {
    print_header "Criando Arquivo de Configuração"
    
    cat > "$CONFIG_FILE" << EOF
# PolyDiet - Configuração de Caminhos
# Este arquivo foi gerado automaticamente pelo setup.sh
# Você pode editar manualmente se necessário

# Node.js
NODE_PATH="${NODE_PATH:-}"

# Ferramentas npm
GLTF_TRANSFORM_PATH="${GLTF_PATH:-}"
OBJ2GLTF_PATH="${OBJ2GLTF_PATH:-}"

# gltfpack
GLTFPACK_PATH="${GLTFPACK_PATH:-}"

# Python
PYTHON_VENV_PATH="${PYTHON_VENV_PATH:-}"
PYTHON_SYSTEM_PATH="${PYTHON_PATH:-}"

# Chromium/Chrome (opcional)
CHROMIUM_PATH="${CHROMIUM_PATH:-}"

# Ambiente Virtual
VENV_DIR="${VENV_DIR}"

# Data de geração
GENERATED_DATE="$(date)"
EOF

    print_success "Arquivo de configuração criado: $CONFIG_FILE"
}

# Validar instalação
validate_installation() {
    print_header "Validando Instalação"
    
    local all_ok=true
    
    # Node.js
    if ! command_exists node; then
        print_error "Node.js não está disponível"
        all_ok=false
    fi
    
    # npm
    if ! command_exists npm; then
        print_error "npm não está disponível"
        all_ok=false
    fi
    
    # gltf-transform
    if ! command_exists gltf-transform; then
        print_error "gltf-transform não está disponível"
        all_ok=false
    fi
    
    # obj2gltf
    if ! command_exists obj2gltf; then
        print_error "obj2gltf não está disponível"
        all_ok=false
    fi
    
    # gltfpack
    if ! command_exists gltfpack; then
        print_error "gltfpack não está disponível"
        all_ok=false
    fi
    
    # Python
    if [ ! -f "$VENV_DIR/bin/python" ] && ! command_exists python3; then
        print_error "Python não está disponível"
        all_ok=false
    fi
    
    # Dependências Python
    if [ -f "$VENV_DIR/bin/python" ]; then
        source "$VENV_DIR/bin/activate"
        if ! python -c "import pandas, numpy, plotly" 2>/dev/null; then
            print_error "Dependências Python não estão instaladas corretamente"
            all_ok=false
        fi
    fi
    
    if [ "$all_ok" = true ]; then
        print_success "Todas as dependências principais estão instaladas!"
        return 0
    else
        print_error "Algumas dependências estão faltando. Verifique os erros acima."
        return 1
    fi
}

# Resumo final
print_summary() {
    print_header "Resumo da Instalação"
    
    echo "Instalado: $INSTALLED"
    echo "Já existente: $SKIPPED"
    echo "Falhas: $FAILED"
    echo ""
    
    if [ $FAILED -eq 0 ]; then
        echo -e "${GREEN}✓ Instalação concluída com sucesso!${NC}"
        echo ""
        echo "Próximos passos:"
        echo "1. Abra o projeto no Unity (versão 6000.2.4f1)"
        echo "2. Coloque seus modelos em Assets/StreamingAssets/Models/"
        echo "3. Execute o projeto e comece a usar!"
    else
        echo -e "${YELLOW}⚠ Instalação concluída com avisos${NC}"
        echo "Algumas dependências precisam ser instaladas manualmente."
        echo "Consulte o README.md para instruções detalhadas."
    fi
}

# Função principal
main() {
    print_header "PolyDiet - Instalação Automática"
    echo "Projeto: $PROJECT_ROOT"
    echo "Data: $(date)"
    echo ""
    
    # Detectar OS
    detect_os
    
    # Verificar/Instalar Node.js
    if ! check_nodejs; then
        if [ "$OS" == "linux" ]; then
            install_nodejs_linux
        else
            print_error "Por favor, instale Node.js manualmente: https://nodejs.org/"
            exit 1
        fi
    fi
    
    # Verificar npm
    if ! check_npm; then
        print_error "npm não está disponível. Instale Node.js primeiro."
        exit 1
    fi
    
    # Instalar packages npm
    install_npm_packages
    
    # Instalar gltfpack
    install_gltfpack
    
    # Verificar Python
    if ! check_python; then
        print_error "Por favor, instale Python 3.8+ manualmente"
        exit 1
    fi
    
    # Configurar Python
    setup_python_venv
    
    # Detectar caminhos
    detect_paths
    
    # Criar configuração
    create_config
    
    # Validar
    validate_installation
    
    # Resumo
    print_summary
}

# Executar
main

