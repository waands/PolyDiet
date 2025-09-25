#!/bin/bash

echo "=== Teste de Conversão OBJ → GLB ==="
echo ""

# Verificar se obj2gltf está disponível
if ! command -v obj2gltf &> /dev/null; then
    echo "❌ obj2gltf não encontrado"
    exit 1
fi

echo "✅ obj2gltf encontrado: $(which obj2gltf)"
echo "   Versão: $(obj2gltf --version 2>/dev/null | head -1 || echo 'N/A')"
echo ""

# Verificar se gltf-transform está disponível
if ! command -v gltf-transform &> /dev/null; then
    echo "❌ gltf-transform não encontrado"
    exit 1
fi

echo "✅ gltf-transform encontrado: $(which gltf-transform)"
echo "   Versão: $(gltf-transform --version 2>/dev/null || echo 'N/A')"
echo ""

# Procurar por arquivos OBJ de teste
echo "🔍 Procurando arquivos OBJ para teste..."
OBJ_FILES=($(find /home/wands/Documentos/tcc/PolyDiet -name "*.obj" 2>/dev/null | head -3))

if [ ${#OBJ_FILES[@]} -eq 0 ]; then
    echo "ℹ️  Nenhum arquivo OBJ encontrado no projeto"
    echo "   Para testar, coloque um arquivo .obj em Assets/StreamingAssets/Models/[nome]/original/"
    exit 0
fi

echo "   Encontrados ${#OBJ_FILES[@]} arquivos OBJ:"
for obj_file in "${OBJ_FILES[@]}"; do
    echo "   - $obj_file"
done
echo ""

# Testar conversão com o primeiro arquivo encontrado
TEST_OBJ="${OBJ_FILES[0]}"
TEST_GLB="/tmp/test_conversion.glb"

echo "🧪 Testando conversão: $(basename "$TEST_OBJ") → $(basename "$TEST_GLB")"

if obj2gltf --binary -i "$TEST_OBJ" -o "$TEST_GLB" 2>/dev/null; then
    if [ -f "$TEST_GLB" ]; then
        FILE_SIZE=$(stat -c%s "$TEST_GLB" 2>/dev/null || echo "0")
        FILE_SIZE_KB=$((FILE_SIZE / 1024))
        echo "✅ Conversão bem-sucedida!"
        echo "   Arquivo GLB criado: $TEST_GLB (${FILE_SIZE_KB} KB)"
        
        # Limpar arquivo de teste
        rm -f "$TEST_GLB"
    else
        echo "❌ Arquivo GLB não foi criado"
        exit 1
    fi
else
    echo "❌ Falha na conversão"
    exit 1
fi

echo ""
echo "🎉 Todos os testes passaram! O sistema está pronto para converter OBJ → GLB"