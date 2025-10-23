# Decisões de Implementação

## 1. Bibliotecas e ferramentas

* **pandas e numpy**: manipulação e agregação de dados de benchmark a partir dos arquivos CSV gerados pelo Unity.
* **plotly**: geração de gráficos interativos em PNG (barras, box plots) com suporte a exportação de alta qualidade.
* **subprocess (Python)**: execução de processos externos para compressão (gltf-transform, gltfpack) e conversão (obj2gltf).
* **gltf-transform**: CLI para compressão Draco e conversão GLTF para GLB, instalado via npm.
* **gltfpack**: CLI para compressão Meshopt, instalado via npm ou pacote do sistema.
* **obj2gltf**: CLI para conversão OBJ para GLB, estratégia principal de conversão no projeto.
* **Chromium headless**: geração de PDF a partir do HTML usando `--print-to-pdf` (alta fidelidade visual).
* **Unity Profiler API**: coleta de uso de memória via `Profiler.GetTotalAllocatedMemoryLong()`.
* **System.Diagnostics.Stopwatch**: medição de tempo de carregamento com precisão de microssegundos.

## 2. Pipeline do relatório

* **Fluxo**: Unity gera CSV com métricas brutas por execução, Python lê CSVs, calcula estatísticas agregadas, gera gráficos PNG, monta HTML, exporta PDF e JSON.
* **Ingestão**: script Python `simple_report_generator.py` recebe lista de arquivos CSV via `--csv-files` e combina dados com `pd.concat()`.
* **Cálculo de métricas**: agregação por variante usando `groupby('variant')` para calcular médias, desvios padrão, mínimos, máximos e percentis.
* **Geração de gráficos**: 8 gráficos de barras PNG salvos em subdiretório `images/` usando `plotly.write_image()` com escala 2× para qualidade.
* **Montagem HTML**: template inline com CSS embutido, cards de decisão visual, tabelas responsivas e JavaScript para expansão de detalhes.
* **Exportação PDF**: chamada a Chromium via subprocess com timeout de 30 segundos (conversão de HTML para PDF preservando layout).
* **Exportação JSON**: estrutura aninhada com estatísticas por variante, comparações percentuais e timestamp ISO 8601.
* **Cache**: não implementado (cada execução regenera tudo), gráficos salvos em `{modelo}/benchmark/images/`.
* **Nomenclatura**: arquivos seguem padrão `bars_{metrica}.png`, `report.html`, `report.pdf`, `data.json` dentro de `StreamingAssets/Models/{modelo}/benchmark/`.

## 3. Como as métricas são calculadas

* **Tempo de carregamento**: diferença entre início (`BeginLoad`) e fim (`EndLoad`) do carregamento, medida com `Stopwatch.Elapsed.TotalMilliseconds`.
* **FPS médio**: média aritmética de todas as amostras de FPS coletadas durante a janela de 5 segundos, calculado como `1/deltaTime` para cada frame.
* **FPS mínimo**: menor valor de FPS observado em todas as amostras da janela de medição.
* **FPS máximo**: maior valor de FPS observado em todas as amostras da janela de medição.
* **FPS mediana**: valor central (50º percentil) da distribuição de FPS, calculado após ordenação das amostras.
* **FPS 1% low**: 1º percentil inferior da distribuição de FPS, representa os piores 1% dos frames (índice `floor(n * 0.01) - 1`).
* **Uso de memória**: total de memória alocada pelo Unity imediatamente após o carregamento, obtido via `Profiler.GetTotalAllocatedMemoryLong()` e convertido para MB.
* **Tamanho do arquivo**: tamanho em bytes do arquivo GLB no disco, obtido via `FileInfo.Length` e convertido para MB (divisor 1024×1024).
* **Contagem de triângulos/vértices**: extraída durante validação de arquivos OBJ (parse manual de linhas `v` e `f`), não coletada para GLB (requer parser GLTF completo, não implementado).
* **Tempo de descompressão**: não medido separadamente, incluído no tempo de carregamento total.

## 4. Como as métricas são coletadas

* **Instrumentação de tempo**: `System.Diagnostics.Stopwatch` iniciado em `BeginLoad()` e finalizado em `EndLoad()`, precisão de alta resolução do sistema operacional.
* **Amostragem de FPS**: coleta de `Time.unscaledDeltaTime` a cada frame via `Task.Yield()` durante 5 segundos, filtrando valores entre 0.001s e 1.0s para evitar outliers extremos.
* **Warm-up**: não implementado (primeira execução pode ter cache frio, planejado como melhoria futura).
* **Repetição**: 3 execuções por variante configuradas via `numberOfTests = 3` em `MetricsConfig.cs`.
* **Descarte de outliers**: não implementado no nível de execução individual (todas as 3 rodadas são salvas), filtragem apenas de frames com deltaTime fora do range válido.
* **Fonte de memória**: API Unity Profiler lê memória gerenciada total (heap C#), não inclui memória nativa de GPU ou assets não gerenciados.
* **Controle de ambiente**: executado em LinuxEditor (Unity 6000.2.4f1), sem controle explícito de driver, energia ou apps em segundo plano (limitação conhecida).
* **Sincronização de tempo**: uso de `unscaledDeltaTime` para não sofrer impacto de `Time.timeScale`, garante medições consistentes.
* **Agregação**: Python calcula estatísticas finais (média, std, min, max, mediana) a partir das 3 execuções salvas no CSV.

## 5. Descrição das métricas (lista curta)

* **Tempo de carregamento (ms)**: do início do parse GLB até o retorno de `EndLoad()`, inclui descompressão e setup de GameObjects.
* **Uso de memória (MB)**: snapshot de memória gerenciada imediatamente após `EndLoad()`, captura apenas heap C# do Unity.
* **FPS médio (FPS)**: média de frames por segundo durante janela de 5 segundos após carregamento completo.
* **FPS mínimo (FPS)**: pior frame observado na janela de 5 segundos, indica stuttering ou drops pontuais.
* **FPS máximo (FPS)**: melhor frame observado, pode estar limitado por VSync ou refresh rate do monitor.
* **FPS mediano (FPS)**: 50º percentil, mais robusto que média para detectar distribuições assimétricas.
* **FPS 1% low (FPS)**: pior 1% dos frames, métrica crítica para avaliar consistência de performance e microfreezes.
* **Tamanho do arquivo (MB)**: tamanho do GLB em disco, medido antes do carregamento via `FileInfo.Length`.

## 6. Como o modelo é mostrado

* **Viewer**: Unity 6000.2.4f1 em modo LinuxEditor, renderização em tempo real com pipeline URP (Universal Render Pipeline).
* **Câmera**: `SimpleOrbitCamera` com controle de órbita livre (mouse drag para rotação, scroll para zoom), ângulos iniciais 30° yaw e 20° pitch.
* **Iluminação**: skybox padrão do Unity (`SkySeries Freebie`) com luz direcional fixa, sem sombras ou GI em tempo real (configuração padrão URP).
* **Material**: material padrão URP com shader `Lit`, albedo extraído de texturas embutidas no GLB ou branco se ausente.
* **FOV**: 60° (padrão Unity), distância da câmera ajustada automaticamente via `FrameTarget()` para enquadrar bounding box do modelo.
* **Fundo**: skybox azul gradiente, não afeta métricas de performance (mesma cena para todas as variantes).
* **Normalização**: modelo posicionado em (0,0,0), escala não normalizada (usa escala original do arquivo), eixos Y-up conforme convenção GLB.
* **Captura de screenshots**: não automatizada (comparação visual feita manualmente via split-view interativo com slider).
* **Resolução de viewport**: 1920×1080 (16:9), fullscreen no editor Unity durante testes.

## 7. Comparação visual entre modelos/variantes

* **Split-view interativo**: `CompareSplitView.cs` renderiza duas câmeras (A e B) em RenderTextures separadas, compostas via shader `CompareComposite`.
* **Slider de comparação**: controle UI horizontal permite ajustar divisão de 0 a 1, linha vertical marca ponto de corte.
* **Sincronização de câmeras**: `CameraPoseFollower` copia transform e parâmetros de câmera principal para A e B, garantindo ângulo idêntico.
* **Shader de composição**: mistura texturas A e B com split vertical baseado em coordenada X normalizada, feather opcional (padrão 0 para corte brusco).
* **Labels**: TextMeshPro indica variante esquerda e direita na UI, ativados apenas durante modo de comparação.
* **Parâmetros fixos**: mesma iluminação, skybox, FOV e distância de câmera para ambas as variantes durante comparação.
* **Modo wireframe**: não implementado (renderização solid apenas).
* **Heatmap de diferença**: não implementado (comparação puramente visual, sem métricas SSIM ou PSNR).
* **Critérios de equivalência visual**: avaliação manual pelo usuário via split-view, sem threshold automático de aceitação.

## 8. Protocolo de testes e duração

* **Número de execuções**: 3 rodadas por variante (configurado em `MetricsConfig.DEFAULT_NUMBER_OF_TESTS`).
* **Warm-up**: não implementado (primeira execução pode sofrer impacto de cache frio do sistema operacional e Unity).
* **Duração por rodada**: 5 segundos de coleta de FPS após carregamento completo, configurado em `MetricsConfig.DEFAULT_FPS_WINDOW_SECONDS`.
* **Pausa entre rodadas**: não implementada (execução sequencial imediata, planejada como melhoria futura para estabilização de sistema).
* **Ordem de execução**: fixa (original, meshopt, draco conforme `VARIANT_ORDER`), sem randomização.
* **Seed fixa**: não aplicável (testes determinísticos sem componente aleatório nas medições).
* **Limpeza de cache**: não realizada entre testes (arquivos GLB permanecem em disco, Unity mantém cache interno de assets).
* **Reinicialização do viewer**: não necessária (Unity reutiliza mesma cena, destroi GameObject anterior e instancia novo modelo).
* **Run ID**: timestamp gerado em `Awake()` do singleton `Metrics` agrupa todas as 3 execuções da mesma sessão (formato `yyyyMMdd_HHmmss`).
* **Duração total estimada**: aproximadamente 20-30 segundos por modelo completo (3 variantes × 3 execuções × ~2s por teste).

## 9. Compressão: decisões e parâmetros

* **Draco via gltf-transform**: comando `gltf-transform optimize {input} {output} --compress draco --draco-compression-level {N}`.
* **Níveis Draco**: Low (1), Default (5), High (8), Maximum (10), testes usam Default (nível 5) para balancear tamanho e velocidade.
* **Quantization Draco**: não especificada explicitamente (usa padrões de gltf-transform: 14 bits posição, 10 bits normais, 12 bits UVs).
* **Meshopt via gltfpack**: comando `gltfpack -i {input} -o {output} {flags}`.
* **Níveis Meshopt**: Low (`-c`), Default (`-cc`), High (`-cc -si`), Maximum (`-cc -si -sa`), testes usam Default (compressão extra sem simplificação).
* **Flags Meshopt**: `-c` (compressão básica), `-cc` (compressão extra de geometria e texturas), `-si` (simplificação de índices), `-sa` (análise agressiva).
* **Trade-offs Draco**: reduz tamanho 70-90% mas aumenta tempo de carregamento 20-50% devido a descompressão em CPU.
* **Trade-offs Meshopt**: reduz tamanho 50-80%, descompressão mais rápida que Draco (otimizada para cache de GPU), perda mínima de FPS.
* **Versões de ferramentas**: gltf-transform e gltfpack instalados via npm (versões não fixadas no código, depende do ambiente, limitação conhecida).
* **Validação pós-compressão**: `GltfValidator.QuickValidate()` verifica header GLB e estrutura JSON básica, não valida fidelidade geométrica.

## 10. Conversão OBJ → GLB

* **Motivação**: GLB é formato binário compacto, suporta compressão nativa (Draco/Meshopt), texturas embutidas e carregamento mais rápido que OBJ.
* **Ferramenta principal**: obj2gltf (estratégia prioritária em `ConversionManager`, fallback para gltf-transform se disponível).
* **Triangulação**: automática (obj2gltf converte faces n-gon para triângulos durante conversão).
* **Eixos e unidades**: preserva eixos originais do OBJ (geralmente Y-up), unidades mantidas sem conversão.
* **Merge de materiais**: obj2gltf cria material PBR básico se MTL ausente, caso contrário mapeia propriedades Phong para PBR aproximado.
* **Normais**: preservadas se presentes no OBJ (linhas `vn`), recalculadas automaticamente se ausentes.
* **Tangentes**: não preservadas (OBJ não suporta), calculadas pelo Unity durante importação se necessário para normal mapping.
* **UVs**: preservadas se presentes (linhas `vt`), textura incorporada no GLB se referenciada no MTL.
* **Compressão embutida**: não aplicada durante conversão (GLB gerado é descomprimido, compressão feita em etapa separada).
* **Incorporação de texturas**: obj2gltf embute texturas PNG/JPG no GLB como base64 (arquivo único portável).
* **Checagem de escala**: não realizada (conversão preserva escala original, normalização manual necessária se modelos tiverem escalas muito diferentes).
* **Validação pós-conversão**: `GltfValidator.QuickValidate()` verifica estrutura GLB, contagem de vértices/faces logada mas não validada contra original.

## 11. Geração de gráficos e layout do relatório

* **Tipos de gráficos**: barras verticais para todas as 8 métricas (load_ms, mem_mb, fps_avg, fps_min, fps_max, fps_median, fps_1pc, file_mb).
* **Paleta consistente**: azul (#3498db) para original, vermelho (#e74c3c) para Draco, verde (#2ecc71) para Meshopt, fixado em `VARIANT_COLORS`.
* **Barras de erro**: desvio padrão exibido quando disponível (campos `*_std` no CSV), espessura 1.5px, largura 3px.
* **Destaque de melhor**: halo cinza claro (`fillcolor rgba(0,0,0,0.05)`) na barra com melhor valor (menor para load/mem/file, maior para FPS).
* **Ordenação**: variantes sempre na ordem `["original", "draco", "meshopt"]` definida em `VARIANT_ORDER`.
* **Acessibilidade**: texto branco em negrito dentro das barras, fonte Arial Black tamanho 14, hover mostra tooltip com valor preciso.
* **Resolução de gráficos**: 800×400 pixels (width/2, height/2) com scale=2× para qualidade Retina (imagem final 1600×800).
* **Layout HTML**: cards de decisão em grid responsivo (`grid-template-columns: repeat(auto-fit, minmax(400px, 1fr))`), seções expansíveis com JavaScript.
* **Tema visual**: degradê roxo (#667eea → #764ba2) no header, cards com bordas coloridas (verde para recomendado, amarelo para considerar, vermelho para não recomendado).
* **Score de eficiência**: fórmula `compression_score - performance_penalty` onde penalty = `|min(fps_change, 0)| + max(load_change, 0)/10`, exibido com barra de progresso visual.
* **Responsividade**: layout adapta para mobile (grid colapsa para coluna única), imagens `max-width: 100%`.

## 12. Reprodutibilidade e governança

* **Fixação de versões**: não implementada para ferramentas npm (gltf-transform, gltfpack, obj2gltf usam versão instalada no sistema, planejado usar `package.json` com lock).
* **Registro de commit hash**: não implementado (relatórios não incluem hash git do código usado, planejado adicionar ao JSON de saída).
* **Manifesto de execução**: parcialmente implementado (CSV inclui `platform`, `unity_version`, `timestamp`, `run_id`, falta CPU/GPU/driver).
* **Hardware no CSV**: não capturado automaticamente (plataforma genérica "LinuxEditor", não há identificação de CPU/GPU/RAM).
* **Versões de bibliotecas Python**: não salvas (depende de `requirements.txt` manual, não há snapshot automático de versões pip no relatório).
* **Scripts de automação**: `simple_report_generator.py` chamado via linha de comando, argumentos `--csv-files`, `--out`, `--model`, `--html`, `--pdf`, `--last-n`.
* **Estrutura de pastas**: `StreamingAssets/Models/{modelo}/benchmark/` para dados brutos (CSV) e processados (imagens, HTML, PDF, JSON).
* **Makefile/CLI**: não implementado (execução manual ou via Unity `ReportRunner.cs`, planejado criar wrapper shell script).
* **Versionamento de CSVs**: append-only (novas execuções adicionadas ao mesmo `benchmarks.csv`, histórico completo preservado com timestamps).
* **Backup de resultados**: não automatizado (usuário deve copiar manualmente pasta `benchmark/` antes de regerar, planejado usar diretórios datados).

## 13. Limitações conhecidas

* **Ruído de máquina**: sem controle de processos em segundo plano, governor de CPU ou desligamento de rede (variabilidade de 5-10% observada entre execuções idênticas).
* **Variação de driver**: versão de driver gráfico não registrada (mudanças de driver podem afetar performance de rendering, especialmente em Linux).
* **Amostra de modelos**: apenas 5 modelos testados (suzanne, dragon, bunny, duck, duck2), não cobre modelos com texturas pesadas, skinning ou animações.
* **Cenários não cobertos**: testes apenas com modelos estáticos, sem animações, sem LOD, sem instanciamento múltiplo (cenários de jogos reais não representados).
* **Métricas de GPU**: não coletadas (sem profiling de draw calls, tempo de GPU, uso de VRAM, batch counts).
* **Métricas visuais**: SSIM, PSNR, VMAF não implementadas (comparação visual puramente qualitativa via split-view manual).
* **Warm-up ausente**: primeira execução pode ter overhead de cache frio (sistema operacional, Unity shader compilation).
* **Ordem fixa de testes**: sem randomização pode introduzir viés de aquecimento progressivo (segunda variante pode se beneficiar de cache da primeira).
* **Descompressão na CPU**: Draco descomprime na CPU (não aproveita GPU), gargalo para modelos grandes (>10MB comprimidos).
* **Falta de repetibilidade de versões**: ferramentas npm não fixadas (atualização de gltf-transform pode mudar algoritmos de compressão).



