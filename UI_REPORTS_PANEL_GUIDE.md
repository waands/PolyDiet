# Guia de Criação da UI ReportsPanel

## Objetivo
Criar a hierarquia completa do ReportsPanel no Canvas do Unity seguindo o design especificado no plano.

## Passo 1: Criar ReportsPanel GameObject

1. **No Canvas**, clique direito → UI → Panel
2. **Renomeie** para `ReportsPanel`
3. **Configure** o Panel:
   - Width: 1200
   - Height: 800
   - Anchor: Center
   - Position: (0, 0, 0)
   - Color: (0.1, 0.1, 0.1, 0.95) - Fundo escuro semi-transparente

## Passo 2: Adicionar ReportsPanelController

1. **Selecione** o ReportsPanel
2. **Add Component** → ReportsPanelController
3. **Deixe as referências vazias** por enquanto (configuraremos depois)

## Passo 3: Criar Header

1. **Clique direito** no ReportsPanel → UI → Text - TextMeshPro
2. **Renomeie** para `Header`
3. **Configure**:
   - Text: "📊 Relatórios de Performance"
   - Font Size: 24
   - Color: White
   - Alignment: Center
   - Position: (0, 350, 0)
   - Width: 1000, Height: 50

## Passo 4: Criar Seleção de Modelo

1. **Clique direito** no ReportsPanel → UI → Panel
2. **Renomeie** para `ModelSelection`
3. **Add Component** → Horizontal Layout Group
4. **Configure** Horizontal Layout Group:
   - Spacing: 10
   - Child Controls Size: Width ✓, Height ✓
   - Child Force Expand: Width ✓, Height ✗
5. **Configure** o Panel:
   - Position: (0, 300, 0)
   - Width: 1000, Height: 50

### 4.1: Criar LabelModel

1. **Clique direito** no ModelSelection → UI → Text - TextMeshPro
2. **Renomeie** para `LabelModel`
3. **Configure**:
   - Text: "Modelo:"
   - Font Size: 16
   - Color: White
   - Alignment: Middle Left
   - Width: 100, Height: 40

### 4.2: Criar DropdownModel

1. **Clique direito** no ModelSelection → UI → Dropdown - TextMeshPro
2. **Renomeie** para `DropdownModel`
3. **Configure**:
   - Width: 300, Height: 40
   - Background Color: (0.2, 0.2, 0.2, 1)

### 4.3: Criar ButtonRefreshModels

1. **Clique direito** no ModelSelection → UI → Button - TextMeshPro
2. **Renomeie** para `ButtonRefreshModels`
3. **Configure**:
   - Text: "🔄"
   - Font Size: 16
   - Color: White
   - Background Color: (0.3, 0.3, 0.3, 1)
   - Width: 50, Height: 40

## Passo 5: Criar RowButtons (Layout Horizontal)

1. **Clique direito** no ReportsPanel → UI → Panel
2. **Renomeie** para `RowButtons`
3. **Add Component** → Horizontal Layout Group
4. **Configure** Horizontal Layout Group:
   - Spacing: 10
   - Child Controls Size: Width ✓, Height ✓
   - Child Force Expand: Width ✓, Height ✗
5. **Configure** o Panel:
   - Position: (0, 250, 0)
   - Width: 1000, Height: 60

### 4.1: Criar ButtonGenerate

1. **Clique direito** no RowButtons → UI → Button - TextMeshPro
2. **Renomeie** para `ButtonGenerate`
3. **Configure**:
   - Text: "Gerar Relatório"
   - Font Size: 14 
   - Color: White
   - Background Color: (0.2, 0.6, 0.2, 1) - Verde

### 4.2: Criar ButtonOpenHtml

1. **Clique direito** no RowButtons → UI → Button - TextMeshPro
2. **Renomeie** para `ButtonOpenHtml`
3. **Configure**:
   - Text: "Abrir HTML"
   - Font Size: 14
   - Color: White
   - Background Color: (0.2, 0.4, 0.8, 1) - Azul

### 4.3: Criar ButtonOpenPdf

1. **Clique direito** no RowButtons → UI → Button - TextMeshPro
2. **Renomeie** para `ButtonOpenPdf`
3. **Configure**:
   - Text: "Abrir PDF"
   - Font Size: 14
   - Color: White
   - Background Color: (0.8, 0.2, 0.2, 1) - Vermelho

### 4.4: Criar ButtonOpenFolder

1. **Clique direito** no RowButtons → UI → Button - TextMeshPro
2. **Renomeie** para `ButtonOpenFolder`
3. **Configure**:
   - Text: "Abrir Pasta"
   - Font Size: 14
   - Color: White
   - Background Color: (0.6, 0.3, 0.6, 1) - Roxo

## Passo 5: Criar Previews (Layout Horizontal)

1. **Clique direito** no ReportsPanel → UI → Panel
2. **Renomeie** para `Previews`
3. **Add Component** → Horizontal Layout Group
4. **Configure** Horizontal Layout Group:
   - Spacing: 20
   - Child Controls Size: Width ✓, Height ✓
   - Child Force Expand: Width ✓, Height ✗
5. **Configure** o Panel:
   - Position: (0, 0, 0)
   - Width: 1000, Height: 400

### 5.1: Criar PreviewLoad (Layout Vertical)

1. **Clique direito** no Previews → UI → Panel
2. **Renomeie** para `PreviewLoad`
3. **Add Component** → Vertical Layout Group
4. **Configure** Vertical Layout Group:
   - Spacing: 10
   - Child Controls Size: Width ✓, Height ✗
   - Child Force Expand: Width ✓, Height ✗
5. **Configure** o Panel:
   - Width: 300, Height: 400
   - Background Color: (0.15, 0.15, 0.15, 0.8)

#### 5.1.1: Criar LabelLoad

1. **Clique direito** no PreviewLoad → UI → Text - TextMeshPro
2. **Renomeie** para `LabelLoad`
3. **Configure**:
   - Text: "Tempo de Carregamento"
   - Font Size: 16
   - Color: White
   - Alignment: Center
   - Height: 30

#### 5.1.2: Criar ImgLoad

1. **Clique direito** no PreviewLoad → UI → Raw Image
2. **Renomeie** para `ImgLoad`
3. **Configure**:
   - Width: 400, Height: 500
   - Color: White

### 5.2: Criar PreviewMem (Layout Vertical)

1. **Clique direito** no Previews → UI → Panel
2. **Renomeie** para `PreviewMem`
3. **Add Component** → Vertical Layout Group
4. **Configure** Vertical Layout Group:
   - Spacing: 10
   - Child Controls Size: Width ✓, Height ✗
   - Child Force Expand: Width ✓, Height ✗
5. **Configure** o Panel:
   - Width: 300, Height: 400
   - Background Color: (0.15, 0.15, 0.15, 0.8)

#### 5.2.1: Criar LabelMem

1. **Clique direito** no PreviewMem → UI → Text - TextMeshPro
2. **Renomeie** para `LabelMem`
3. **Configure**:
   - Text: "Uso de Memória"
   - Font Size: 16
   - Color: White
   - Alignment: Center
   - Height: 30

#### 5.2.2: Criar ImgMem

1. **Clique direito** no PreviewMem → UI → Raw Image
2. **Renomeie** para `ImgMem`
3. **Configure**:
   - Width: 400, Height: 500
   - Color: White

### 5.3: Criar PreviewFps (Layout Vertical)

1. **Clique direito** no Previews → UI → Panel
2. **Renomeie** para `PreviewFps`
3. **Add Component** → Vertical Layout Group
4. **Configure** Vertical Layout Group:
   - Spacing: 10
   - Child Controls Size: Width ✓, Height ✗
   - Child Force Expand: Width ✓, Height ✗
5. **Configure** o Panel:
   - Width: 300, Height: 400
   - Background Color: (0.15, 0.15, 0.15, 0.8)

#### 5.3.1: Criar LabelFps

1. **Clique direito** no PreviewFps → UI → Text - TextMeshPro
2. **Renomeie** para `LabelFps`
3. **Configure**:
   - Text: "Performance FPS"
   - Font Size: 16
   - Color: White
   - Alignment: Center
   - Height: 30

#### 5.3.2: Criar ImgFps

1. **Clique direito** no PreviewFps → UI → Raw Image
2. **Renomeie** para `ImgFps`
3. **Configure**:
   - Width: 400, Height: 500
   - Color: White

## Passo 6: Criar ScrollView para Imagens

Este ScrollView exibirá todas as 8 imagens geradas pelo relatório em um layout vertical com scroll.

### 6.1: Criar ScrollView Base

1. **Clique direito** no ReportsPanel → UI → Scroll View
2. **Renomeie** para `ScrollViewImages`
3. **Configure** o RectTransform:
   - Anchor: Center-Middle
   - Pos X: 0, Pos Y: -100 (ajustar conforme layout)
   - Width: 890 (largura disponível)
   - Height: 600 (altura visível)

4. **Configure** o componente ScrollRect:
   - Horizontal: ✗ (desabilitado)
   - Vertical: ✓ (habilitado)
   - Movement Type: Elastic
   - Inertia: ✓
   - Scroll Sensitivity: 20

### 6.2: Configurar Viewport

1. **Selecione** `ScrollViewImages/Viewport`
2. Já vem configurado por padrão com:
   - Componente Mask
   - Componente Image
3. Verifique se está tudo correto

### 6.3: Configurar Content (Container das Imagens)

1. **Selecione** `ScrollViewImages/Viewport/Content`
2. **Configure** o RectTransform:
   - Anchor: Top-Center
   - Pivot: X: 0.5, Y: 1
   - Pos X: 0, Pos Y: 0
   - Width: 840 (com margem)
   - Height: ajustado dinamicamente pelo código

3. **Adicionar** Vertical Layout Group:
   - Clique em Add Component → Layout → Vertical Layout Group
   - Child Alignment: Upper Center
   - Control Child Size: ✗ Width, ✗ Height
   - Child Force Expand: ✓ Width, ✗ Height
   - Spacing: 8
   - Padding: Left 20, Right 20, Top 10, Bottom 10

4. **Adicionar** Content Size Fitter:
   - Clique em Add Component → Layout → Content Size Fitter
   - Horizontal Fit: Unconstrained
   - Vertical Fit: Preferred Size

### 6.4: Estrutura Final do ScrollView

```
ScrollViewImages (ScrollRect)
└── Viewport (Mask + Image)
    └── Content (Vertical Layout Group + Content Size Fitter)
        └── [Imagens serão criadas dinamicamente pelo código C#]
```

### 6.5: Comportamento Esperado

Ao gerar um relatório, o código C# irá:
1. Limpar o Content (remover filhos antigos)
2. Criar 8 GameObjects filhos (um para cada PNG)
3. Cada filho terá:
   - Title (TextMeshProUGUI) - nome do gráfico
   - Image (RawImage com AspectRatioFitter) - imagem do gráfico
4. Calcular altura total do Content baseada nos aspect ratios
5. ScrollView permitirá rolar verticalmente para ver todas as imagens

### 6.6: Lista de Imagens Exibidas

As 8 imagens serão exibidas nesta ordem:
1. **Tempo de Carregamento** (`bars_load.png`)
2. **Uso de Memória** (`bars_mem.png`)
3. **FPS Médio** (`bars_fps.png`)
4. **FPS Mínimo** (`bars_fps_min.png`)
5. **FPS Máximo** (`bars_fps_max.png`)
6. **FPS Mediano** (`bars_fps_median.png`)
7. **FPS 1% Low** (`bars_fps_1pc.png`)
8. **Tamanho do Arquivo** (`bars_file_size.png`)

## Passo 7: Criar StatusLabel

1. **Clique direito** no ReportsPanel → UI → Text - TextMeshPro
2. **Renomeie** para `StatusLabel`
3. **Configure**:
   - Text: "Selecione um modelo e clique em Gerar"
   - Font Size: 14
   - Color: (0.8, 0.8, 0.8, 1) - Cinza claro
   - Alignment: Center
   - Position: (0, -350, 0)
   - Width: 1000, Height: 30

## Passo 7: Criar StatusLabel

1. **Clique direito** no ReportsPanel → UI → Text - TextMeshPro
2. **Renomeie** para `StatusLabel`
3. **Configure**:
   - Text: "Selecione um modelo e clique em Gerar"
   - Font Size: 14
   - Color: (0.8, 0.8, 0.8, 1) - Cinza claro
   - Alignment: Center
   - Position: (0, -350, 0)
   - Width: 1000, Height: 30

## Passo 8: Configurar ReportsPanelController

1. **Selecione** o ReportsPanel
2. **No ReportsPanelController**, configure as referências:

### Referências Obrigatórias:
- **Report Runner**: Arraste o ReportRunner da cena
- **Dropdown Model**: Arraste o DropdownModel
- **Button Refresh Models**: Arraste o ButtonRefreshModels
- **Preview Load**: Arraste o ImgLoad
- **Preview Mem**: Arraste o ImgMem
- **Preview Fps**: Arraste o ImgFps

### Referências ScrollView (Nova seção):
- **Scroll View**: Arraste o ScrollViewImages
- **Scroll Content**: Arraste o ScrollViewImages/Viewport/Content
- **Image Item Prefab**: Deixe vazio (código cria dinamicamente)

### Referências de Botões:
- **Button Generate**: Arraste o ButtonGenerate
- **Button Open Html**: Arraste o ButtonOpenHtml
- **Button Open Pdf**: Arraste o ButtonOpenPdf
- **Button Open Folder**: Arraste o ButtonOpenFolder

### Referências de Status e Labels:
- **Status Label**: Arraste o StatusLabel
- **Label Load**: Arraste o LabelLoad
- **Label Mem**: Arraste o LabelMem
- **Label Fps**: Arraste o LabelFps

## Passo 9: Configurar Botões (OnClick Events)

### ButtonGenerate:
1. **Selecione** ButtonGenerate
2. **No Button component**, clique no "+" em OnClick
3. **Arraste** o ReportsPanel para o slot
4. **Selecione** ReportsPanelController → OnClickGenerate

### ButtonOpenHtml:
1. **Selecione** ButtonOpenHtml
2. **No Button component**, clique no "+" em OnClick
3. **Arraste** o ReportsPanel para o slot
4. **Selecione** ReportsPanelController → OnClickOpenHtml

### ButtonOpenPdf:
1. **Selecione** ButtonOpenPdf
2. **No Button component**, clique no "+" em OnClick
3. **Arraste** o ReportsPanel para o slot
4. **Selecione** ReportsPanelController → OnClickOpenPdf

### ButtonOpenFolder:
1. **Selecione** ButtonOpenFolder
2. **No Button component**, clique no "+" em OnClick
3. **Arraste** o ReportsPanel para o slot
4. **Selecione** ReportsPanelController → OnClickOpenFolder

### ButtonRefreshModels:
1. **Selecione** ButtonRefreshModels
2. **No Button component**, clique no "+" em OnClick
3. **Arraste** o ReportsPanel para o slot
4. **Selecione** ReportsPanelController → OnClickRefreshModels

## Passo 10: Configurar ReportRunner Callback

1. **Selecione** o ReportRunner na cena
2. **No ReportRunner component**, encontre o campo "On Report Complete" na seção "Events"
3. **Clique no "+"** para adicionar um listener
4. **Arraste** o ReportsPanel para o slot "None (GameObject)"
5. **No dropdown**, selecione ReportsPanelController → OnReportGenerated
6. **No campo de texto**, digite o caminho do relatório (pode deixar vazio, será preenchido automaticamente)

## Passo 11: Teste Inicial

1. **Desative** o ReportsPanel (uncheck no Inspector)
2. **Execute** a cena
3. **Verifique** se não há erros no Console
4. **Ative** o ReportsPanel manualmente para testar

## Estrutura Final Esperada:

```
Canvas/
└─ ReportsPanel (Panel + ReportsPanelController)
   ├─ Header (TMP_Text)
   ├─ ModelSelection (Panel + Horizontal Layout Group)
   │  ├─ LabelModel (TMP_Text)
   │  ├─ DropdownModel (Dropdown)
   │  └─ ButtonRefreshModels (Button)
   ├─ RowButtons (Panel + Horizontal Layout Group)
   │  ├─ ButtonGenerate (Button)
   │  ├─ ButtonOpenHtml (Button)
   │  ├─ ButtonOpenPdf (Button)
   │  └─ ButtonOpenFolder (Button)
   ├─ Previews (Panel + Horizontal Layout Group)
   │  ├─ PreviewLoad (Panel + Vertical Layout Group)
   │  │  ├─ LabelLoad (TMP_Text)
   │  │  └─ ImgLoad (RawImage)
   │  ├─ PreviewMem (Panel + Vertical Layout Group)
   │  │  ├─ LabelMem (TMP_Text)
   │  │  └─ ImgMem (RawImage)
   │  └─ PreviewFps (Panel + Vertical Layout Group)
   │     ├─ LabelFps (TMP_Text)
   │     └─ ImgFps (RawImage)
   ├─ ScrollViewImages (ScrollRect) ← NOVO
   │  └─ Viewport (Mask + Image)
   │     └─ Content (Vertical Layout Group + Content Size Fitter)
   │        └─ [8 Imagens criadas dinamicamente]
   └─ StatusLabel (TMP_Text)
```

## Próximos Passos:

Após criar a UI, você precisará:
1. **Adicionar botão "📈 Relatórios" no HUDController**
2. **Testar o workflow completo**
3. **Ajustar posicionamento se necessário**

## Dicas:

- **Use Ctrl+D** para duplicar elementos similares
- **Use Shift+Click** para selecionar múltiplos elementos
- **Use Alt+Click** para criar elementos filhos
- **Teste** cada botão individualmente antes de integrar
- **Verifique** se todas as referências estão conectadas
