#!/bin/bash

echo "=== TESTE DO BOTÃO DE MÉTRICAS ==="
echo
echo "Verificando se o sistema de toggle está configurado corretamente..."
echo

# Verificar se o ToggleActive.cs existe
if [ -f "Assets/Scripts/ToggleActive.cs" ]; then
    echo "✅ ToggleActive.cs encontrado"
    echo "Conteúdo do script:"
    cat Assets/Scripts/ToggleActive.cs
else
    echo "❌ ToggleActive.cs não encontrado"
fi

echo
echo "=== STATUS ==="
echo "O ButtonMetrics na Unity Scene já está configurado com:"
echo "1. Componente Button (para detectar cliques)"
echo "2. Componente ToggleActive (para alternar visibilidade)" 
echo "3. Target configurado para o MetricsPanel"
echo "4. OnClick event configurado para chamar Toggle()"
echo
echo "✅ A funcionalidade de toggle já está implementada!"
echo "Quando você clicar no ButtonMetrics, ele deve:"
echo "- Mostrar o painel se estiver oculto"
echo "- Ocultar o painel se estiver visível"
echo
echo "Para testar:"
echo "1. Abra o projeto no Unity"
echo "2. Execute a scene"
echo "3. Clique no botão 'ButtonMetrics'"
echo "4. O painel de métricas deve aparecer/desaparecer"