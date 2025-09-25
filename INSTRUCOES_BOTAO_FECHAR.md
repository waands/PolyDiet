# INSTRUÇÕES: Como adicionar o botão de fechar ao painel de métricas

## O que foi feito no código:

✅ **MetricsViewer.cs foi modificado** para incluir:
1. Nova propriedade pública `public Button buttonClose;`
2. Conexão do evento no `Awake()`: `buttonClose.onClick.AddListener(ClosePanel);`
3. Novo método `ClosePanel()` que fecha o painel

## O que você precisa fazer na Unity:

### 1. Abrir o projeto no Unity
- Abra a scene `ModelViewer.unity`

### 2. Localizar o MetricsPanel
- Na hierarquia, procure por "MetricsPanel" (deve estar dentro de Canvas)

### 3. Adicionar um botão de fechar
- **Opção A - Criar novo botão:**
  1. Clique direito no MetricsPanel → UI → Button - TextMeshPro
  2. Renomeie para "ButtonClose" ou "Close"
  3. Posicione no canto superior direito do painel
  4. Configure o texto para "×" ou "Fechar"

- **Opção B - Usar botão existente:**
  1. Se já existe um botão de fechar, apenas o selecione

### 4. Conectar o botão ao MetricsViewer
1. Selecione o GameObject que tem o componente **MetricsViewer**
2. No Inspector, procure o campo **Button Close** (novo campo adicionado)
3. Arraste o botão criado/selecionado para esse campo

### 5. Testar
1. Execute a scene (Play)
2. Abra o painel de métricas usando o ButtonMetrics
3. Clique no botão de fechar dentro do painel
4. O painel deve ser fechado

## Resultado esperado:
- O ButtonMetrics continua abrindo/fechando o painel (toggle)
- O novo botão de fechar **dentro do painel** sempre fecha quando clicado
- Ambos os métodos funcionam independentemente

## Estilo sugerido para o botão de fechar:
- Texto: "×" (símbolo de fechar)
- Posição: Canto superior direito do painel
- Tamanho: 24x24 ou 32x32 pixels
- Cor: Vermelha ou cinza escuro