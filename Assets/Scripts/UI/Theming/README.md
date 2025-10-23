# UI Theming (uGUI + TMP)

Este diretório contém um sistema simples de tema para uGUI que aplica cores e estilos a componentes padrões via ScriptableObject.

## Componentes
- UiTheme (ScriptableObject): define paleta de cores e sprite arredondado 9-sliced.
- ThemeManager: aplica o tema a todos os componentes que implementam `IThemed` na cena.
- CardSkin: estiliza cards (Image + Shadow + TMP cores).
- ButtonSkin: estilos Primary/Secondary + micro animação de hover/press.
 - ButtonSkin: estilos Primary/Secondary/Tertiary/Ghost/Outline/Success/Danger + micro animação.
- BadgeSkin: selos de status (Excellent/Good/Moderate/Poor/Baseline).
- ChipToggleSkin: chips de seleção (Toggle) com cores On/Off.
 - DropdownSkinTMP: skin para TMP_Dropdown (bg/border/label/arrow + template).

## Passo-a-passo
1) Crie o asset do tema:
   - Opção A: Project Window → RMB → Create → UI → Theme (DashboardTheme_Light)
   - Opção B (fallback): Menu superior → Tools → PolyDiet UI → Create Theme (Dashboard Light)
   - Opção C: Project Window → RMB → Create → UI → Theme (Dashboard Light) — atalho adicionado pelo script Editor
2) Opcional: importe um sprite 9-sliced (PNG 32x32):
   - Texture Type: Sprite (2D and UI)
   - Mesh Type: Full Rect
   - Border: 8,8,8,8
   - Arraste para o campo `roundedSprite` do tema.
3) Na cena, adicione um GameObject `ThemeManager` e arraste o asset do tema.
4) Em cada UI:
   - Card → Add Component `CardSkin` e referencie os TMPs (title/value/subtitle).
    - Button → Add Component `ButtonSkin` (Primary/Secondary/Tertiary/Ghost/Outline/Success/Danger) e a label TMP.
   - Badge → Add Component `BadgeSkin` e selecione o Kind.
   - Chip (Toggle) → Add Component `ChipToggleSkin` e referencie bg/label.
       - (Opcional) Checkmark: arraste a imagem do ícone de check para o campo `checkmark`.
          Se deixar vazio, o script tenta usar `Toggle.graphic` quando for `Image`.

## Dicas de troubleshooting
- Botões não clicam: verifique se o `Button.targetGraphic` aponta para a Image do próprio botão (o `ButtonSkin` já força isso no Awake). Cheque também se não há outra `Image` com `Raycast Target` cobrindo o botão.

## Tipografia (Roboto)
1) Importe a fonte Roboto (otf/ttf) para o projeto (ex.: `Assets/Fonts/Roboto/`).
2) Crie `TMP Font Asset` para os pesos desejados (Window → TextMeshPro → Font Asset Creator).
   - Gere ao menos: Regular, Medium, Bold.
3) Abra seu asset de tema `UiTheme` e preencha os campos em "Typography (TMP)":
   - fontRegular = Roboto-Regular SDF
   - fontMedium = Roboto-Medium SDF
   - fontBold = Roboto-Bold SDF
4) Os skins aplicam automaticamente as fontes:
   - Button: Medium (fallback Regular)
   - Card: title=Medium, value=Bold, subtitle=Regular
   - Badge: Medium
   - Chip: Regular
   - Dropdown (caption): Regular

Ao editar o asset `UiTheme`, os componentes serão atualizados na próxima carga de cena ou quando o `ThemeManager` rodar (Awake).

## Dicas
- Use Layout Groups (Grid/Vertical/Horizontal) com padding e spacing para replicar o layout do seu CSS.
- Duplique `Shadow` nos cards para sombras mais fortes com deslocamentos diferentes.
- Tipografia: use TMP com fontes como Inter/Roboto; títulos 20–24, valores 36–60, subtítulos 12–14.

## Notas sobre estilos de botão
- Secondary: agora com melhor contraste (bg claro #F3F4F6, texto #1F2937) e borda opcional #D1D5DB.
- Tertiary: fundo branco com borda sutil (#E5E7EB) para áreas claras (parecido com "card button").
- Ghost: sem fundo, cor de texto da brand; ganha hover com leve bg.
- Outline: sem fundo, apenas borda e hover; útil em fundos coloridos ou cards.
- Success/Danger: variantes semânticas rápidas para confirmações e ações destrutivas.

## Badge: quando usar
Use `BadgeSkin` para indicar status/qualidade de uma métrica ou item (ex.: Excellent/Good/Moderate/Poor) ou marcar "Baseline" em comparações. Ele colore o fundo com a cor da categoria e deixa o texto branco para leitura rápida.

## Dropdown (TMP_Dropdown)
1) No GO do dropdown (TMP_Dropdown):
   - Garanta um `Image` de fundo (BG) e opcional `Border`.
   - Arraste `DropdownSkinTMP` e preencha: `bg`, `border` (opcional), `label` (TMP_Text do caption), `arrow` (Image do ícone).
2) Template (lista aberta): o script tenta pintar viewport/item text/item bg se as referências padrão do TMP estiverem configuradas.
3) Cores são definidas no `UiTheme` em "Dropdowns (TMP)".
