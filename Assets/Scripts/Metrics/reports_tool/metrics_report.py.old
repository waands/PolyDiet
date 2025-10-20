import argparse, os, sys, json, subprocess
from datetime import datetime

# Configurações centralizadas
CONFIG = {
    "base_variant": "original",
    "draco_variant": "draco", 
    "meshopt_variant": "meshopt",
    "default_last_n": 20,
    "default_pdf_engine": "chrome",
    "max_fps_samples": 50,
    "min_frame_delta": 0.001,
    "max_frame_delta": 1.0
}

# Verificar dependências
try:
    import pandas as pd
    print("[py] ✓ pandas carregado")
except ImportError as e:
    print(f"[py] ❌ Erro ao carregar pandas: {e}")
    print("[py] Instale com: pip install pandas")
    sys.exit(1)

try:
    import plotly.graph_objs as go
    import plotly.io as pio
    print("[py] ✓ plotly carregado")
except ImportError as e:
    print(f"[py] ❌ Erro ao carregar plotly: {e}")
    print("[py] Instale com: pip install plotly")
    sys.exit(1)

def parse_args():
    ap = argparse.ArgumentParser()
    # Removido --csv e --auto-discover - agora usamos --csv-files
    ap.add_argument("--csv-files", nargs='+', required=True, help="Lista de caminhos para os arquivos CSV")
    ap.add_argument("--out", required=True)
    ap.add_argument("--model", default="all")  # "all" ou nome de modelo
    ap.add_argument("--variants", default=f"{CONFIG['base_variant']},{CONFIG['draco_variant']},{CONFIG['meshopt_variant']}")
    ap.add_argument("--last-n", type=int, default=CONFIG["default_last_n"])
    ap.add_argument("--html", action="store_true")
    ap.add_argument("--pdf", action="store_true")
    ap.add_argument("--open", action="store_true")
    ap.add_argument("--pdf-engine", default=CONFIG["default_pdf_engine"])  # "chrome" ou "wkhtml"
    ap.add_argument("--pdf-engine-path", default="")
    return ap.parse_args()

# Função discover_model_csvs() removida - não é mais necessária
# O C# agora passa os caminhos diretamente via --csv-files

def load_multiple_csvs(csv_paths):
    """Carrega e combina múltiplos CSVs de modelos"""
    print(f"[py] ========================================")
    print(f"[py] LOAD_MULTIPLE_CSVS - DIAGNÓSTICO")
    print(f"[py] ========================================")
    print(f"[py] CSV paths recebidos: {len(csv_paths) if csv_paths else 0}")
    
    if not csv_paths:
        print("[py] ❌ Nenhum CSV encontrado para carregar. A busca não retornou arquivos.")
        print("[py] ❌ Isso significa que a função discover_model_csvs() não encontrou nenhum arquivo.")
        return pd.DataFrame()
    
    print(f"[py] ✅ {len(csv_paths)} arquivos CSV encontrados:")
    for i, path in enumerate(csv_paths, 1):
        print(f"[py]   {i}. {path}")
        print(f"[py]      Existe: {os.path.exists(path)}")
        if os.path.exists(path):
            size = os.path.getsize(path)
            print(f"[py]      Tamanho: {size} bytes")
    
    all_dfs = []
    for csv_path in csv_paths:
        try:
            df = load_csv(csv_path)
            if not df.empty:
                all_dfs.append(df)
                print(f"[py] Adicionado {len(df)} linhas de {csv_path}")
        except Exception as e:
            print(f"[py] Erro ao carregar {csv_path}: {e}")
    
    if not all_dfs:
        print("[py] Nenhum CSV válido foi carregado")
        return pd.DataFrame()
    
    # Combina todos os DataFrames
    if not all_dfs:
        print("[py] ❌ Nenhum DataFrame válido foi criado")
        return pd.DataFrame()
    
    combined_df = pd.concat(all_dfs, ignore_index=True)
    print(f"[py] ✅ Total combinado: {len(combined_df)} linhas de {len(all_dfs)} arquivos")
    
    # Log detalhado do DataFrame combinado
    print(f"[py] ========================================")
    print(f"[py] DATAFRAME COMBINADO - ANÁLISE")
    print(f"[py] ========================================")
    print(f"[py] Shape: {combined_df.shape}")
    print(f"[py] Colunas: {list(combined_df.columns)}")
    
    if not combined_df.empty:
        if 'model' in combined_df.columns:
            models = combined_df['model'].unique()
            print(f"[py] Modelos encontrados: {list(models)}")
        else:
            print("[py] ⚠️ Coluna 'model' não encontrada no DataFrame")
            
        if 'variant' in combined_df.columns:
            variants = combined_df['variant'].unique()
            print(f"[py] Variantes encontradas: {list(variants)}")
        else:
            print("[py] ⚠️ Coluna 'variant' não encontrada no DataFrame")
            
        print(f"[py] Primeiras 3 linhas:")
        print(combined_df.head(3).to_string())
    else:
        print("[py] ❌ DataFrame está vazio!")
    
    return combined_df

def load_csv(csv_path: str) -> pd.DataFrame:
    try:
        df = pd.read_csv(csv_path)
        print(f"[py] CSV carregado: {df.shape[0]} linhas, {df.shape[1]} colunas")
        print(f"[py] Colunas: {df.columns.tolist()}")
    except Exception as e:
        print(f"[py] Erro ao carregar CSV: {e}")
        raise
    
    # Remove linhas vazias ou com dados inválidos
    if 'timestamp' in df.columns:
        df = df.dropna(subset=['timestamp']).copy()
        df = df[df['timestamp'].str.strip() != ''].copy()
    else:
        print("[py] ⚠️ Coluna 'timestamp' não encontrada")
        return df
    
    # Validação de dados - o CSV deve estar correto
    if 'model' in df.columns and 'variant' in df.columns:
        print(f"[py] Modelos encontrados: {df['model'].unique().tolist()}")
        print(f"[py] Variantes encontradas: {df['variant'].unique().tolist()}")
        
        # Verifica se há dados inconsistentes (mas não corrige automaticamente)
        variant_is_numeric = df['variant'].astype(str).str.replace('.', '').str.isdigit().any()
        model_has_variants = df['model'].isin([CONFIG['base_variant'], CONFIG['draco_variant'], CONFIG['meshopt_variant']]).any()
        
        if variant_is_numeric and model_has_variants:
            print("[py] ⚠️ AVISO: Possível inconsistência detectada - variant contém valores numéricos e model contém nomes de variantes")
            print("[py] ⚠️ Verifique se os dados foram salvos corretamente no C#")
            print("[py] ⚠️ O script Python não corrige automaticamente mais - confie nos dados do CSV")
    
    # Compatibilidade com CSV antigo (sem run_id/fps_window_s/test_number)
    for col in ["run_id", "fps_window_s", "test_number"]:
        if col not in df.columns:
            if col == "run_id":
                # cria run_id por "lote" (timestamp truncado a minuto)
                try:
                    df["run_id"] = pd.to_datetime(df["timestamp"]).dt.strftime("%Y%m%d_%H%M")
                except Exception:
                    df["run_id"] = "run"
            elif col == "fps_window_s":
                df["fps_window_s"] = 5.0
            elif col == "test_number":
                df["test_number"] = 1  # Assumir teste único para dados antigos
    # timestamp em datetime
    if "timestamp" in df.columns:
        df["timestamp"] = pd.to_datetime(df["timestamp"], errors="coerce")
    else:
        df["timestamp"] = pd.Timestamp.utcnow()
    # normaliza variantes
    if "variant" in df.columns:
        df["variant"] = df["variant"].str.lower()
    return df

def filter_scope(df: pd.DataFrame, model: str, variants, last_n: int) -> pd.DataFrame:
    print(f"[py] Filtrando: model='{model}', variants={variants}, last_n={last_n}")
    print(f"[py] Dados antes do filtro: {df.shape[0]} linhas")
    
    if model != "all" and "model" in df.columns:
        df = df[df["model"] == model]
        print(f"[py] Após filtro por modelo: {df.shape[0]} linhas")
    
    if "variant" in df.columns:
        df = df[df["variant"].isin(variants)]
        print(f"[py] Após filtro por variantes: {df.shape[0]} linhas")
        print(f"[py] Variantes encontradas: {df['variant'].unique().tolist()}")
    else:
        print("[py] ⚠️ Coluna 'variant' não encontrada")
        return df
    
    df = df.sort_values("timestamp")

    group_cols = ["model", "variant"] if "model" in df.columns else ["variant"]
    # pega as últimas N por grupo, sem .apply (evita FutureWarning)
    result = df.groupby(group_cols, group_keys=False).tail(last_n)
    print(f"[py] Dados finais: {result.shape[0]} linhas")
    return result

def compute_aggregates(df: pd.DataFrame):
    # métricas disponíveis
    cols = {
        "load_ms": dict(label="Load", unit="ms", lower_is_better=True),
        "mem_mb": dict(label="Mem",  unit="MB", lower_is_better=True),
        "fps_avg": dict(label="FPS Avg",  unit="FPS", lower_is_better=False),
        "fps_min": dict(label="FPS Min", unit="FPS", lower_is_better=False),
        "fps_max": dict(label="FPS Max", unit="FPS", lower_is_better=False),
        "fps_median": dict(label="FPS Median", unit="FPS", lower_is_better=False),
        "fps_1pc_low": dict(label="FPS 1% low", unit="FPS", lower_is_better=False),
    }
    group_cols = ["model","variant"] if "model" in df.columns else ["variant"]
    
    # Agregação com as novas métricas de FPS
    agg_dict = {}
    
    # Métricas básicas
    if "file_mb" in df.columns:
        agg_dict["file_mb"] = "mean"
    if "load_ms" in df.columns:
        agg_dict["load_ms"] = "mean"
    if "mem_mb" in df.columns:
        agg_dict["mem_mb"] = "mean"
    if "fps_avg" in df.columns:
        agg_dict["fps_avg"] = "mean"
    
    # Adicionar novas métricas de FPS se existirem
    if "fps_min" in df.columns:
        agg_dict["fps_min"] = "mean"
    if "fps_max" in df.columns:
        agg_dict["fps_max"] = "mean"
    if "fps_median" in df.columns:
        agg_dict["fps_median"] = "mean"
    if "fps_1pc_low" in df.columns:
        agg_dict["fps_1pc_low"] = "mean"
    
    print(f"[py] Agregando com colunas: {list(agg_dict.keys())}")
    agg = df.groupby(group_cols).agg(agg_dict).reset_index()
    
    # Adicionar contagem de amostras manualmente
    if "timestamp" in df.columns:
        sample_counts = df.groupby(group_cols).size().reset_index(name='samples')
        agg = agg.merge(sample_counts, on=group_cols, how='left')

    # ganho% vs original (por modelo)
    gain_cols = ["gain_load_ms", "gain_mem_mb", "gain_fps_avg", "gain_fps_min", "gain_fps_max", "gain_fps_median", "gain_fps_low"]
    for col in gain_cols:
        agg[col] = None

    if "model" in agg.columns:
        for model_name, g in agg.groupby("model"):
            orig = g[g["variant"]==CONFIG["base_variant"]]
            if len(orig)==1:
                o = orig.iloc[0]
                idxs = g.index
                # menor é melhor
                agg.loc[idxs, "gain_load_ms"] = (o["load_ms"] - g["load_ms"]) / o["load_ms"] * 100.0
                agg.loc[idxs, "gain_mem_mb"]  = (o["mem_mb"]  - g["mem_mb"])  / o["mem_mb"]  * 100.0
                # maior é melhor
                agg.loc[idxs, "gain_fps_avg"] = (g["fps_avg"] - o["fps_avg"]) / o["fps_avg"] * 100.0
                if "fps_min" in g.columns and "fps_min" in o.index:
                    agg.loc[idxs, "gain_fps_min"] = (g["fps_min"] - o["fps_min"]) / o["fps_min"] * 100.0
                if "fps_max" in g.columns and "fps_max" in o.index:
                    agg.loc[idxs, "gain_fps_max"] = (g["fps_max"] - o["fps_max"]) / o["fps_max"] * 100.0
                if "fps_median" in g.columns and "fps_median" in o.index:
                    agg.loc[idxs, "gain_fps_median"] = (g["fps_median"] - o["fps_median"]) / o["fps_median"] * 100.0
                if "fps_1pc_low" in g.columns and "fps_1pc_low" in o.index:
                    agg.loc[idxs, "gain_fps_low"] = (g["fps_1pc_low"] - o["fps_1pc_low"]) / o["fps_1pc_low"] * 100.0
    else:
        # sem "model", compara tudo contra "original" global (se existir)
        orig = agg[agg["variant"]==CONFIG["base_variant"]]
        if len(orig)==1:
            o = orig.iloc[0]
            idxs = agg.index
            agg.loc[idxs, "gain_load_ms"] = (o["load_ms"] - agg["load_ms"]) / o["load_ms"] * 100.0
            agg.loc[idxs, "gain_mem_mb"]  = (o["mem_mb"]  - agg["mem_mb"])  / o["mem_mb"]  * 100.0
            agg.loc[idxs, "gain_fps_avg"] = (agg["fps_avg"] - o["fps_avg"]) / o["fps_avg"] * 100.0
            if "fps_min" in agg.columns and "fps_min" in o.index:
                agg.loc[idxs, "gain_fps_min"] = (agg["fps_min"] - o["fps_min"]) / o["fps_min"] * 100.0
            if "fps_max" in agg.columns and "fps_max" in o.index:
                agg.loc[idxs, "gain_fps_max"] = (agg["fps_max"] - o["fps_max"]) / o["fps_max"] * 100.0
            if "fps_median" in agg.columns and "fps_median" in o.index:
                agg.loc[idxs, "gain_fps_median"] = (agg["fps_median"] - o["fps_median"]) / o["fps_median"] * 100.0
            if "fps_1pc_low" in agg.columns and "fps_1pc_low" in o.index:
                agg.loc[idxs, "gain_fps_low"] = (agg["fps_1pc_low"] - o["fps_1pc_low"]) / o["fps_1pc_low"] * 100.0

    return agg, cols

def bar_chart(agg: pd.DataFrame, metric: str, variants_order, title: str, unit: str, color_map: dict):
    # média por variante (em todos os modelos do escopo)
    by_var = agg.groupby("variant", as_index=False).mean(numeric_only=True)
    
    # Cria um DataFrame com todas as variantes esperadas
    result_data = []
    for variant in variants_order:
        variant_data = by_var[by_var['variant'] == variant]
        if len(variant_data) > 0:
            value = variant_data[metric].iloc[0] if metric in variant_data.columns else None
        else:
            value = None
        result_data.append({'variant': variant, metric: value})
    
    result_df = pd.DataFrame(result_data)
    y = result_df[metric].tolist()
    # None para NaN (Plotly não desenha a barra, mas mantém categoria)
    y = [v if pd.notna(v) else None for v in y]
    
    texts = [f"{v:.3f} {unit}" if v is not None else "—" for v in y]
    colors = [color_map.get(v, "#888") for v in variants_order]

    fig = go.Figure(go.Bar(x=variants_order, y=y, text=texts, textposition="auto", marker_color=colors))
    fig.update_layout(title=title, xaxis_title="Variante", yaxis_title=f"{title} ({unit})",
                      template="plotly_white", margin=dict(l=40,r=20,t=60,b=40), height=360)
    return fig

def timeline(df: pd.DataFrame, ycol: str, title: str, unit: str, by="index", color_map=None):
    # por variante, desenha linha vs ordem (index) OU data (timestamp real)
    fig = go.Figure()
    for v, g in df.groupby("variant"):
        g2 = g.sort_values("timestamp").reset_index(drop=True)
        if by=="index":
            x = g2.index + 1
            x_title = "Execução (ordem)"
        else:
            x = g2["timestamp"]
            x_title = "Data/Hora"
        fig.add_trace(go.Scatter(
            x=x, y=g2[ycol], mode="lines+markers", name=v,
            line=dict(width=2, color=color_map.get(v,"#888888") if color_map else None)
        ))
    fig.update_layout(
        title=title,
        xaxis_title=x_title,
        yaxis_title=f"{title} ({unit})",
        template="plotly_white",
        margin=dict(l=40,r=20,t=60,b=40),
        height=360,
        legend=dict(orientation="h", yanchor="bottom", y=1.02, xanchor="left", x=0)
    )
    return fig

def color_theme():
    return {
        "bg": "#FAFAFE",
        "text": "#1F1F29",
        "muted": "#6B7280",
        CONFIG["base_variant"]: "#BFC7D5",
        CONFIG["draco_variant"]: "#47BD85",
        CONFIG["meshopt_variant"]: "#4794FA",
        "good": "#2ECC71",
        "bad": "#E74C3C",
    }

def engine_cmd(engine: str, engine_path: str, html_path: str, pdf_path: str):
    if engine == "chrome":
        bin_ = engine_path or ("google-chrome" if not sys.platform.startswith("win") else "chrome.exe")
        # Chromium geralmente é "chromium"
        if not shutil_which(bin_):
            alt = "chromium" if not sys.platform.startswith("win") else bin_
            if shutil_which(alt):
                bin_ = alt
        return [bin_, "--headless=new", "--disable-gpu", f"--print-to-pdf={pdf_path}", html_path]
    else:
        bin_ = engine_path or "wkhtmltopdf"
        return [bin_, html_path, pdf_path]

def shutil_which(cmd):
    from shutil import which
    return which(cmd) is not None

def figs_to_html_blocks(figs):
    """Gera blocos HTML: o 1º inclui plotly.js via CDN, os demais não."""
    blocks = []
    for i, fig in enumerate(figs):
        include = "cdn" if i == 0 else False  # 100% standalone + leve
        blocks.append(pio.to_html(fig, full_html=False, include_plotlyjs=include))
    return blocks

def build_html(title, blocks, theme):
    css = f"""
    body {{ background:{theme['bg']}; color:{theme['text']}; font-family: -apple-system, BlinkMacSystemFont, Segoe UI, Roboto, Inter, Arial, sans-serif; }}
    .wrap {{ max-width: 1100px; margin: 24px auto; padding: 0 12px; }}
    h1 {{ margin: 8px 0 24px; font-size: 24px; }}
    .grid {{ display: grid; grid-template-columns: 1fr 1fr; gap: 16px; }}
    .block {{ background:#fff; border-radius:12px; padding:12px; box-shadow: 0 6px 20px rgba(0,0,0,0.06); }}
    .full {{ grid-column: 1 / -1; }}
    """
    return f"""<!doctype html>
<html>
<head>
<meta charset="utf-8">
<title>{title}</title>
<style>{css}</style>
</head>
<body>
<div class="wrap">
  <h1>{title}</h1>
  <div class="grid">
    <div class="block">{blocks[0]}</div>
    <div class="block">{blocks[1]}</div>
    <div class="block">{blocks[2]}</div>
    <div class="block full">{blocks[3]}</div>
    <div class="block full">{blocks[4]}</div>
  </div>
</div>
</body>
</html>"""

def main():
    print("[py] Python:", sys.executable)
    print("[py] Versão Python:", sys.version)
    print("[py] Diretório de trabalho:", os.getcwd())
    print("[py] ========================================")
    print("[py] INICIANDO DIAGNÓSTICO DETALHADO")
    print("[py] ========================================")
    
    args = parse_args()
    print(f"[py] Argumentos recebidos:")
    print(f"[py] - Processando CSV Files: {args.csv_files}")
    print(f"[py] - Output: {args.out}")
    print(f"[py] - Model: {args.model}")
    print(f"[py] - Variants: {args.variants}")
    print(f"[py] - Last N: {args.last_n}")
    #print(f"[py] - Auto Discover: {args.auto_discover}")
    print(f"[py] - HTML: {args.html}")
    print(f"[py] - PDF: {args.pdf}")
    
    os.makedirs(args.out, exist_ok=True)

    # MODIFICAÇÃO: Usar a lista de arquivos passada diretamente
    print("[py] ========================================")
    print("[py] MODO CSV-FILES - COMUNICAÇÃO EXPLÍCITA")
    print("[py] ========================================")
    print(f"[py] - Processando CSV Files: {args.csv_files}")
    for i, f in enumerate(args.csv_files, 1):
        print(f"[py]   {i}. {f}")
        print(f"[py]      Existe: {os.path.exists(f)}")
        if os.path.exists(f):
            size = os.path.getsize(f)
            print(f"[py]      Tamanho: {size} bytes")

    if not args.csv_files:
        print("[py] ❌ Nenhum arquivo CSV foi fornecido.")
        return 1

    try:
        # A função load_multiple_csvs já aceita uma lista de caminhos
        df = load_multiple_csvs(args.csv_files) 
    except Exception as e:
        print(f"[py] ❌ Erro ao carregar CSVs: {e}")
        import traceback
        traceback.print_exc()
        return 1

    variants = [v.strip().lower() for v in args.variants.split(",") if v.strip()]
    print(f"[py] ========================================")
    print(f"[py] FILTRO DE DADOS - DIAGNÓSTICO")
    print(f"[py] ========================================")
    print(f"[py] DataFrame antes do filtro:")
    print(f"[py] - Shape: {df.shape}")
    print(f"[py] - Modelo solicitado: {args.model}")
    print(f"[py] - Variantes solicitadas: {variants}")
    print(f"[py] - Last N: {args.last_n}")
    
    if not df.empty:
        print(f"[py] - Modelos disponíveis no DF: {df['model'].unique().tolist() if 'model' in df.columns else 'N/A'}")
        print(f"[py] - Variantes disponíveis no DF: {df['variant'].unique().tolist() if 'variant' in df.columns else 'N/A'}")
    
    try:
        df_f = filter_scope(df, args.model, variants, args.last_n)
        print(f"[py] DataFrame após filtro:")
        print(f"[py] - Shape: {df_f.shape}")
    except Exception as e:
        print(f"[py] ❌ Erro ao filtrar dados: {e}")
        import traceback
        traceback.print_exc()
        return 1
    
    if df_f.empty:
        print("[py] ========================================")
        print("[py] ❌ NENHUM DADO APÓS FILTRO")
        print("[py] ========================================")
        print(f"[py] Modelo solicitado: {args.model}")
        print(f"[py] Variantes solicitadas: {variants}")
        print(f"[py] Last N: {args.last_n}")
        print("[py] Possíveis causas:")
        print("[py] 1. Modelo não existe no DataFrame")
        print("[py] 2. Variantes não existem no DataFrame")
        print("[py] 3. Filtro Last N muito restritivo")
        print("[py] 4. DataFrame original está vazio")
        return 1

    agg, meta = compute_aggregates(df_f)

    theme = color_theme()
    cmap = {
        CONFIG["base_variant"]: theme[CONFIG["base_variant"]], 
        CONFIG["draco_variant"]: theme[CONFIG["draco_variant"]], 
        CONFIG["meshopt_variant"]: theme[CONFIG["meshopt_variant"]]
    }

    figs = []

    # Barras comparativas (3 principais)
    figs.append(bar_chart(agg, "load_ms", variants, "Tempo de carregamento", "ms", cmap))
    figs.append(bar_chart(agg, "mem_mb",  variants, "Memória (média)", "MB", cmap))
    figs.append(bar_chart(agg, "fps_avg", variants, "FPS (média)", "FPS", cmap))

    # Timelines (por ordem e por data) — vamos usar fps_avg como primeiro exemplo
    figs.append(timeline(df_f, "fps_avg", "FPS por execução (ordem)", "FPS", by="index", color_map=cmap))
    figs.append(timeline(df_f, "fps_avg", "FPS ao longo do tempo", "FPS", by="time",  color_map=cmap))

    model_for_title = "Global" if args.model == "all" else args.model
    title = f"Relatório de Métricas — {model_for_title} — {datetime.now().strftime('%Y-%m-%d %H:%M')}"
    html_blocks = figs_to_html_blocks(figs)
    html = build_html(title, html_blocks, theme)

    html_path = os.path.join(args.out, "report.html")
    with open(html_path, "w", encoding="utf-8") as f:
        f.write(html)
    print("[py] HTML gerado:", html_path)

    if args.pdf:
        pdf_path = os.path.join(args.out, "report.pdf")
        cmd = engine_cmd(args.pdf_engine, args.pdf_engine_path, html_path, pdf_path)
        print("[py] PDF cmd:", " ".join(cmd))
        try:
            subprocess.run(cmd, check=False)
            print("[py] PDF gerado:", pdf_path)
        except Exception as e:
            print("[py] Erro ao gerar PDF:", e)

    if args.open:
        if sys.platform.startswith("win"):
            os.startfile(html_path)  # type: ignore
        elif sys.platform.startswith("darwin"):
            os.system(f'open "{html_path}"')
        else:
            os.system(f'xdg-open "{html_path}"')

    print("[py] OK.")
    return 0

if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except Exception as e:
        import traceback
        traceback.print_exc()
        raise
