#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Simple Report Generator for PolyDiet Metrics
Generates PNG charts, HTML report, PDF, and JSON export from benchmark CSV data
"""

import sys
import os
import argparse
from datetime import datetime
import json

# =====================================================================
# SEÇÃO 1: IMPORTS E CONFIGURAÇÃO
# =====================================================================

try:
    import pandas as pd
    import numpy as np
    print("[py] ✓ pandas e numpy carregados")
except ImportError as e:
    print(f"[py] ❌ Erro ao carregar pandas/numpy: {e}")
    sys.exit(1)

try:
    import plotly.graph_objects as go
    from plotly.subplots import make_subplots
    print("[py] ✓ plotly carregado")
except ImportError as e:
    print(f"[py] ❌ Erro ao carregar plotly: {e}")
    sys.exit(1)

# Configuração de cores fixas para as variantes
VARIANT_COLORS = {
    'original': '#3498db',  # Azul
    'draco': '#e74c3c',     # Vermelho
    'meshopt': '#2ecc71'    # Verde
}

# Ordem preferida de variantes
VARIANT_ORDER = ["original", "draco", "meshopt"]

# Configuração dos gráficos
CHART_WIDTH = 1600
CHART_HEIGHT = 800
CHART_FONT_SIZE = 12
CHART_BG_COLOR = 'white'

# Funções auxiliares
def ordered_variants(seq):
    """Retorna variantes na ordem preferida"""
    seen = {v for v in seq}
    return [v for v in VARIANT_ORDER if v in seen]

def fmt_pct(x):
    """Formata percentual com sinal"""
    try: 
        return f"{x:+.1f}%"
    except Exception:
        return "—"

def lower_is_better(metric_id):
    """Retorna True se valores menores são melhores para a métrica"""
    return metric_id in {"load_ms", "mem_mb", "file_mb", "load_ms_avg", "mem_mb_avg", "file_mb_avg"}

def best_index(values, metric_id):
    """Retorna o índice do melhor valor na lista"""
    import math
    vals = [(i,v) for i,v in enumerate(values) if v is not None and not math.isnan(v)]
    if not vals: return None
    return min(vals, key=lambda t: t[1])[0] if lower_is_better(metric_id) else max(vals, key=lambda t: t[1])[0]

# Metadados das métricas
METRICS_INFO = {
    'load_ms': {
        'title': 'Tempo de Carregamento',
        'unit': 'ms',
        'description': 'Tempo em milissegundos necessário para carregar e processar o modelo 3D na memória.',
        'why_important': 'Determina quanto tempo o usuário aguarda antes de poder visualizar o modelo. Tempos altos causam má experiência.',
        'ideal_range': '< 500ms para modelos pequenos/médios, < 1000ms para modelos grandes',
        'formula': 'Média aritmética dos valores de load_ms de todas as amostras da variante',
        'source_column': 'load_ms',
        'interpretation': 'Modelos comprimidos (Draco/Meshopt) geralmente têm tempo de carregamento maior devido à descompressão. Compare o overhead adicional com o ganho de tamanho de arquivo.'
    },
    'mem_mb': {
        'title': 'Uso de Memória',
        'unit': 'MB',
        'description': 'Quantidade de memória RAM consumida pelo modelo após carregamento completo.',
        'why_important': 'Memória é um recurso limitado, especialmente em dispositivos móveis. Alto consumo pode causar crashes ou impactar outras aplicações.',
        'ideal_range': 'Proporcional ao tamanho e complexidade do modelo. Modelos simples devem usar < 100MB.',
        'formula': 'Média aritmética dos valores de mem_mb de todas as amostras da variante',
        'source_column': 'mem_mb',
        'interpretation': 'Modelos comprimidos podem expandir na memória após descompressão. Verifique se o uso de memória permanece aceitável para seu caso de uso.'
    },
    'fps_avg': {
        'title': 'FPS Médio',
        'unit': 'FPS',
        'description': 'Taxa média de quadros por segundo durante a renderização do modelo.',
        'why_important': 'Mede a performance geral de rendering. FPS baixo resulta em animações travadas e má experiência visual.',
        'ideal_range': '≥ 60 FPS (experiência suave), ≥ 30 FPS (aceitável), < 30 FPS (problemático)',
        'formula': 'Média aritmética dos valores de fps_avg de todas as amostras da variante',
        'source_column': 'fps_avg',
        'interpretation': 'Compare entre variantes para identificar impacto na performance. Diferenças menores que 5% são geralmente imperceptíveis.'
    },
    'fps_min': {
        'title': 'FPS Mínimo',
        'unit': 'FPS',
        'description': 'Menor taxa de FPS registrada durante todos os testes.',
        'why_important': 'Identifica os piores momentos de performance (picos de lag). Importante para garantir experiência consistente.',
        'ideal_range': '> 30 FPS (não cair abaixo desse valor)',
        'formula': 'Valor mínimo entre todos os fps_min das amostras da variante',
        'source_column': 'fps_min',
        'interpretation': 'FPS mínimo muito abaixo da média indica instabilidade. Investigate possíveis causas (carregamento, garbage collection, etc).'
    },
    'fps_max': {
        'title': 'FPS Máximo',
        'unit': 'FPS',
        'description': 'Maior taxa de FPS registrada durante todos os testes.',
        'why_important': 'Mostra o potencial máximo de performance em condições ideais.',
        'ideal_range': 'Quanto maior, melhor. Pode ser limitado por VSync ou refresh rate do monitor.',
        'formula': 'Valor máximo entre todos os fps_max das amostras da variante',
        'source_column': 'fps_max',
        'interpretation': 'FPS máximo similar entre variantes indica que a geometria não é o gargalo principal.'
    },
    'fps_median': {
        'title': 'FPS Mediano',
        'unit': 'FPS',
        'description': 'Valor central da distribuição de FPS (50º percentil).',
        'why_important': 'Mais resistente a valores extremos (outliers) do que a média. Representa melhor a experiência típica.',
        'ideal_range': 'Próximo ao FPS médio indica distribuição estável',
        'formula': 'Mediana dos valores de fps_median de todas as amostras da variante',
        'source_column': 'fps_median',
        'interpretation': 'Se fps_median for significativamente diferente de fps_avg, há outliers impactando a média.'
    },
    'fps_1pc': {
        'title': 'FPS 1% Low',
        'unit': 'FPS',
        'description': 'FPS do 1º percentil inferior - representa o pior 1% dos frames capturados.',
        'why_important': 'Métrica crítica para detectar stuttering e travamentos momentâneos que arruínam a experiência do usuário.',
        'ideal_range': '> 50% do FPS médio indica boa consistência',
        'formula': 'Média dos valores de fps_1pc_low de todas as amostras da variante',
        'source_column': 'fps_1pc_low',
        'interpretation': 'FPS 1% Low muito abaixo da média indica problemas de frame pacing ou stuttering. Isso impacta mais a experiência que um FPS médio alto.'
    },
    'file_mb': {
        'title': 'Tamanho do Arquivo',
        'unit': 'MB',
        'description': 'Tamanho do arquivo do modelo em megabytes no disco.',
        'why_important': 'Impacta tempo de download, uso de banda, armazenamento e tempo de carregamento inicial.',
        'ideal_range': 'Quanto menor, melhor - mas sem perder qualidade visual necessária',
        'formula': 'Média dos valores de file_mb de todas as amostras da variante',
        'source_column': 'file_mb',
        'interpretation': 'Calcule a taxa de compressão em relação ao original. Avalie se o ganho de tamanho justifica possíveis perdas de performance.'
    }
}


# =====================================================================
# SEÇÃO 2: FUNÇÕES DE CARREGAMENTO DE DADOS
# =====================================================================

def load_csv_data(csv_files):
    """
    Carrega e combina múltiplos arquivos CSV em um único DataFrame
    
    Args:
        csv_files: Lista de caminhos para arquivos CSV
        
    Returns:
        DataFrame combinado com todos os dados
    """
    dfs = []
    
    for csv_file in csv_files:
        if not os.path.exists(csv_file):
            print(f"[py] ⚠️ Arquivo não encontrado: {csv_file}")
            continue
            
        try:
            df = pd.read_csv(csv_file)
            print(f"[py] CSV carregado: {csv_file} ({len(df)} linhas)")
            dfs.append(df)
        except Exception as e:
            print(f"[py] ⚠️ Erro ao carregar {csv_file}: {e}")
            continue
    
    if not dfs:
        print("[py] ❌ Nenhum CSV válido foi carregado")
        return None
    
    # Combinar todos os DataFrames
    combined_df = pd.concat(dfs, ignore_index=True)
    print(f"[py] Total de linhas combinadas: {len(combined_df)}")
    
    return combined_df


def filter_by_model(df, model_name, last_n=None):
    """
    Filtra DataFrame por nome do modelo e opcionalmente pelas últimas N execuções
    
    Args:
        df: DataFrame com dados
        model_name: Nome do modelo para filtrar
        last_n: Número de últimas execuções por variante (None = todas)
        
    Returns:
        DataFrame filtrado
    """
    if model_name and 'model' in df.columns:
        df = df[df['model'] == model_name]
        print(f"[py] Dados filtrados para modelo '{model_name}': {len(df)} linhas")
    
    # Filtrar últimas N execuções por variante
    if last_n and last_n > 0:
        # Normalizar timestamp
        if 'timestamp' in df.columns:
            df['timestamp'] = pd.to_datetime(df['timestamp'], errors='coerce')
            
            # Manter últimas N por (modelo, variante)
            group_cols = ['variant']
            if 'model' in df.columns:
                group_cols = ['model', 'variant']
            
            df = df.sort_values('timestamp')
            df = df.groupby(group_cols, group_keys=False).tail(last_n)
            print(f"[py] Mantendo últimas {last_n} execuções por variante: {len(df)} linhas")
    
    return df


# =====================================================================
# SEÇÃO 3: FUNÇÕES DE CÁLCULO DE ESTATÍSTICAS
# =====================================================================

def calculate_stats(df):
    """
    Calcula estatísticas por variante com ordenação e robustez melhorada
    
    Args:
        df: DataFrame com dados de benchmark
        
    Returns:
        Dict com estatísticas por variante (ordenado)
    """
    stats = {}
    
    if 'variant' not in df.columns:
        print("[py] ⚠️ Coluna 'variant' não encontrada no CSV")
        return stats
    
    # Agrupa por variante
    for variant, g in df.groupby('variant'):
        # Helper para extrair valores com robustez
        get = lambda col, fn, default=0: (fn(g[col]) if col in g.columns and g[col].notna().any() else default)
        
        stats[variant] = {
            'samples': len(g),
            'file_mb_avg':  get('file_mb',  lambda s: s.mean(), 0.0),
            'load_ms_avg':  get('load_ms',  lambda s: s.mean(), 0.0),
            'load_ms_std':  get('load_ms',  lambda s: s.std(),  0.0),
            'mem_mb_avg':   get('mem_mb',   lambda s: s.mean(), 0.0),
            'mem_mb_std':   get('mem_mb',   lambda s: s.std(),  0.0),
            'fps_avg':      get('fps_avg',  lambda s: s.mean(), 0.0),
            'fps_avg_std':  get('fps_avg',  lambda s: s.std(),  0.0),
            'fps_min':      get('fps_min',  lambda s: s.min(),  0.0),
            'fps_max':      get('fps_max',  lambda s: s.max(),  0.0),
            'fps_median':   get('fps_median',lambda s: s.median() if hasattr(s,'median') else s.mean(), 0.0),
            'fps_1pc_avg':  get('fps_1pc_low',lambda s: s.mean(), 0.0),
        }
    
    # Ordena dicionário na ordem preferida
    ordered = {}
    for v in ordered_variants(stats.keys()):
        ordered[v] = stats[v]
    for v in stats.keys():
        if v not in ordered:
            ordered[v] = stats[v]
    
    print(f"[py] Estatísticas calculadas para {len(ordered)} variantes")
    return ordered


def calculate_comparisons(stats):
    """
    Calcula comparações percentuais entre variantes e o original (versão melhorada com análise de trade-offs)
    
    Args:
        stats: Dict com estatísticas por variante
        
    Returns:
        Dict com comparações e recomendações
    """
    comps = {}
    
    if 'original' not in stats:
        print("[py] ⚠️ Variante 'original' não encontrada para comparações")
        return comps
    
    o = stats['original']
    
    for v, s in stats.items():
        if v == 'original': 
            continue
        
        # Calcular mudanças percentuais
        file_reduction = ((o['file_mb_avg'] - s['file_mb_avg']) / o['file_mb_avg'] * 100.0) if o['file_mb_avg'] else 0.0
        load_change = ((s['load_ms_avg'] - o['load_ms_avg']) / o['load_ms_avg'] * 100.0) if o['load_ms_avg'] else 0.0
        mem_change = ((s['mem_mb_avg'] - o['mem_mb_avg']) / o['mem_mb_avg'] * 100.0) if o['mem_mb_avg'] else 0.0
        fps_change = ((s['fps_avg'] - o['fps_avg']) / o['fps_avg'] * 100.0) if o['fps_avg'] else 0.0
        
        # Calcular score de eficiência (0-100)
        # Positivos: redução de tamanho, melhoria de FPS
        # Negativos: aumento de load time, perda de FPS
        compression_score = min(file_reduction, 100)  # Max 100 pontos
        performance_penalty = abs(min(fps_change, 0)) + (max(load_change, 0) / 10)  # Penalidade
        efficiency_score = max(0, compression_score - performance_penalty)
        
        # Determinar recomendação
        if file_reduction > 50 and fps_change > -5 and load_change < 50:
            recommendation = "✅ Recomendado"
            reason = "Excelente compressão com impacto mínimo na performance"
        elif file_reduction > 30 and fps_change > -10:
            recommendation = "✅ Bom"
            reason = "Boa compressão com trade-off aceitável"
        elif file_reduction > 20 and fps_change > -15:
            recommendation = "⚠️ Considerar"
            reason = "Compressão moderada, avalie se o trade-off vale a pena"
        else:
            recommendation = "❌ Não Recomendado"
            reason = "Trade-off desfavorável entre compressão e performance"
        
        comps[f'{v}_vs_original'] = {
            'variant': v,
            'file_size_reduction_pct': file_reduction,
            'load_time_change_pct': load_change,
            'mem_change_pct': mem_change,
            'fps_change_pct': fps_change,
            'efficiency_score': efficiency_score,
            'recommendation': recommendation,
            'reason': reason,
            # Valores absolutos para referência
            'file_mb': s['file_mb_avg'],
            'load_ms': s['load_ms_avg'],
            'fps_avg': s['fps_avg'],
            'mem_mb': s['mem_mb_avg'],
        }
    
    print(f"[py] Comparações calculadas para {len(comps)} pares")
    return comps


# =====================================================================
# SEÇÃO 4: FUNÇÕES DE GERAÇÃO DE GRÁFICOS PNG
# =====================================================================

def create_bar_chart(stats, metric_key, metric_info, output_path):
    """
    Cria um gráfico de barras comparando variantes para uma métrica (com barras de erro e destaque)
    
    Args:
        stats: Dict com estatísticas por variante
        metric_key: Chave da métrica (ex: 'load_ms_avg')
        metric_info: Dict com informações da métrica
        output_path: Caminho para salvar o PNG
    """
    variants = ordered_variants(list(stats.keys()))
    vals = [stats[v].get(metric_key, None) for v in variants]
    
    # Barras de erro (se existir *_std)
    std_key = metric_key.replace('_avg','_std') if '_avg' in metric_key else f"{metric_key}_std"
    errs = [stats[v].get(std_key, None) for v in variants]
    
    colors = [VARIANT_COLORS.get(v, '#95a5a6') for v in variants]
    winner = best_index(vals, metric_info.get('source_column', metric_key))
    
    # Criar gráfico
    fig = go.Figure()
    
    fig.add_trace(go.Bar(
        x=variants,
        y=vals,
        marker_color=colors,
        error_y=dict(
            type='data',
            array=[e if e is not None else 0 for e in errs],
            thickness=1.5,
            width=3,
            visible=any(e is not None for e in errs)
        ),
        text=[(f"{v:.4f} {metric_info['unit']}" if v is not None and metric_key in ['file_mb_avg', 'file_mb'] else f"{v:.2f} {metric_info['unit']}" if v is not None else "—") for v in vals],
        textposition='inside',
        textangle=0,
        textfont=dict(size=CHART_FONT_SIZE + 2, color='white', family='Arial Black'),
        hovertemplate='%{x}<br>%{y:.4f} ' + metric_info['unit'] + '<extra></extra>' if metric_key in ['file_mb_avg', 'file_mb'] else '%{x}<br>%{y:.2f} ' + metric_info['unit'] + '<extra></extra>'
    ))
    
    # Halo no melhor
    if winner is not None:
        fig.add_vrect(
            x0=winner-0.5, x1=winner+0.5, 
            fillcolor="rgba(0,0,0,0.05)", 
            line_width=0
        )
    
    fig.update_layout(
        title=dict(
            text=metric_info['title'],
            font=dict(size=CHART_FONT_SIZE + 4)
        ),
        xaxis=dict(
            title=dict(
                text='Variante',
                font=dict(size=CHART_FONT_SIZE)
            ),
            tickfont=dict(size=CHART_FONT_SIZE)
        ),
        yaxis=dict(
            title=dict(
                text=f"{metric_info['title']} ({metric_info['unit']})",
                font=dict(size=CHART_FONT_SIZE)
            ),
            tickfont=dict(size=CHART_FONT_SIZE),
            gridcolor='#ecf0f1'
        ),
        plot_bgcolor=CHART_BG_COLOR,
        paper_bgcolor=CHART_BG_COLOR,
        template='plotly_white',
        height=CHART_HEIGHT//2,
        margin=dict(l=60, r=60, t=80, b=60),  # Margin top reduzido pois textos estão dentro das barras
        showlegend=False
    )
    
    # Salvar PNG
    try:
        fig.write_image(output_path, width=CHART_WIDTH//2, height=CHART_HEIGHT//2, scale=2)
        print(f"[py] PNG: {output_path}")
    except Exception as e:
        print(f"[py] ⚠️ Erro ao salvar PNG {output_path}: {e}")


def box_plot(df, column, title, unit, output_path):
    """Box plot para mostrar distribuição"""
    if column not in df.columns or df[column].isna().all():
        print(f"[py] ⚠️ BoxPlot: coluna '{column}' ausente/vazia")
        return
    
    fig = go.Figure()
    for v in ordered_variants(df['variant'].dropna().unique()):
        g = df[df['variant']==v]
        if g.empty or g[column].isna().all(): 
            continue
        fig.add_trace(go.Box(
            y=g[column], 
            name=v, 
            boxmean='sd', 
            marker_color=VARIANT_COLORS.get(v,'#95a5a6')
        ))
    
    fig.update_layout(
        title=title,
        yaxis_title=f"{title} ({unit})",
        template='plotly_white',
        height=CHART_HEIGHT//2,
        margin=dict(l=60,r=40,t=100,b=50)  # Aumentado margin top para 100px
    )
    
    try:
        fig.write_image(output_path, width=CHART_WIDTH//2, height=CHART_HEIGHT//2, scale=2)
        print(f"[py] PNG: {output_path}")
    except Exception as e:
        print(f"[py] ⚠️ Erro ao salvar {output_path}: {e}")


def scatter_fps_vs_load(df, output_path):
    """Scatter plot mostrando relação entre FPS e Load Time"""
    if not {'fps_avg','load_ms','variant'} <= set(df.columns):
        print("[py] ⚠️ Scatter: colunas faltando")
        return
    
    fig = go.Figure()
    for v in ordered_variants(df['variant'].dropna().unique()):
        g = df[df['variant']==v]
        if g.empty: continue
        fig.add_trace(go.Scatter(
            x=g['load_ms'], y=g['fps_avg'],
            mode='markers',
            name=v, 
            marker=dict(size=10, color=VARIANT_COLORS.get(v,'#95a5a6')),
            hovertemplate=f"Load: %{{x:.1f}} ms<br>FPS: %{{y:.1f}}<extra>{v}</extra>"
        ))
    
    fig.update_layout(
        title="Relação FPS vs Tempo de Carregamento",
        xaxis_title="Load (ms)",
        yaxis_title="FPS médio",
        template="plotly_white",
        height=CHART_HEIGHT//2,
        margin=dict(l=60,r=40,t=100,b=50)  # Aumentado margin top para 100px
    )
    
    try:
        fig.write_image(output_path, width=CHART_WIDTH//2, height=CHART_HEIGHT//2, scale=2)
        print(f"[py] PNG: {output_path}")
    except Exception as e:
        print(f"[py] ⚠️ Erro ao salvar {output_path}: {e}")


def correlation_heatmap(df, output_path):
    """Heatmap de correlação entre métricas"""
    cols = [c for c in ['file_mb','load_ms','mem_mb','fps_avg','fps_1pc_low'] if c in df.columns]
    if len(cols) < 2:
        print("[py] ⚠️ Heatmap: métricas insuficientes")
        return
    
    corr = df[cols].corr(numeric_only=True)
    
    fig = go.Figure(data=go.Heatmap(
        z=corr.values, 
        x=cols, 
        y=cols, 
        colorscale='RdBu', 
        zmin=-1, 
        zmax=1,
        hovertemplate="%{x} × %{y}: %{z:.2f}<extra></extra>"
    ))
    
    fig.update_layout(
        title="Correlação entre Métricas",
        template='plotly_white',
        height=CHART_HEIGHT//2,
        margin=dict(l=80,r=40,t=100,b=60)  # Aumentado margin top para 100px
    )
    
    try:
        fig.write_image(output_path, width=CHART_WIDTH//2, height=CHART_HEIGHT//2, scale=2)
        print(f"[py] PNG: {output_path}")
    except Exception as e:
        print(f"[py] ⚠️ Erro ao salvar {output_path}: {e}")


def generate_all_charts(stats, output_dir, df):
    """
    Gera todos os gráficos PNG (barras + análises avançadas)
    
    Args:
        stats: Dict com estatísticas por variante
        output_dir: Diretório para salvar os PNGs
        df: DataFrame original para gráficos avançados
    """
    images_dir = os.path.join(output_dir, 'images')
    os.makedirs(images_dir, exist_ok=True)
    
    print("[py] Gerando gráficos PNG...")
    
    # Mapeamento de métricas para chaves de stats
    metrics_mapping = [
        ('load_ms', 'load_ms_avg', 'bars_load.png'),
        ('mem_mb', 'mem_mb_avg', 'bars_mem.png'),
        ('fps_avg', 'fps_avg', 'bars_fps.png'),
        ('fps_min', 'fps_min', 'bars_fps_min.png'),
        ('fps_max', 'fps_max', 'bars_fps_max.png'),
        ('fps_median', 'fps_median', 'bars_fps_median.png'),
        ('fps_1pc', 'fps_1pc_avg', 'bars_fps_1pc.png'),
        ('file_mb', 'file_mb_avg', 'bars_file_size.png'),
    ]
    
    for metric_id, stat_key, filename in metrics_mapping:
        metric_info = METRICS_INFO[metric_id]
        output_path = os.path.join(images_dir, filename)
        
        try:
            create_bar_chart(stats, stat_key, metric_info, output_path)
        except Exception as e:
            print(f"[py] ⚠️ Erro ao gerar {filename}: {e}")
    
    # Removidos box plots, scatter e heatmap pois não são usados no HTML
    # Isso acelera a geração do relatório em ~30-40%
    
    print("[py] ✓ Todos os gráficos PNG gerados (8 gráficos de barras)")


# =====================================================================
# SEÇÃO 5: FUNÇÃO DE GERAÇÃO DE HTML
# =====================================================================

def generate_html(model_name, stats, comparisons, df, output_path):
    """
    Gera relatório HTML completo com textos explicativos
    
    Args:
        model_name: Nome do modelo
        stats: Dict com estatísticas
        comparisons: Dict com comparações
        df: DataFrame original com dados brutos
        output_path: Caminho para salvar o HTML
    """
    print("[py] Gerando HTML...")
    
    timestamp = datetime.now().strftime("%Y-%m-%d %H:%M:%S")
    
    # CSS inline
    css = """
    <style>
        body {
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            max-width: 1400px;
            margin: 0 auto;
            padding: 20px;
            background: #f5f6fa;
            color: #2c3e50;
        }
        h1 {
            color: #2c3e50;
            border-bottom: 3px solid #3498db;
            padding-bottom: 10px;
        }
        h2 {
            color: #34495e;
            margin-top: 40px;
            border-left: 4px solid #3498db;
            padding-left: 15px;
        }
        h3 {
            color: #7f8c8d;
            margin-top: 25px;
        }
        .header {
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            color: white;
            padding: 30px;
            border-radius: 10px;
            margin-bottom: 30px;
        }
        .header h1 {
            color: white;
            border: none;
            margin: 0;
        }
        .timestamp {
            opacity: 0.9;
            font-size: 0.9em;
        }
        .summary-table {
            width: 100%;
            border-collapse: collapse;
            margin: 20px 0;
            background: white;
            box-shadow: 0 2px 4px rgba(0,0,0,0.1);
            border-radius: 8px;
            overflow: hidden;
        }
        .summary-table th {
            background: #34495e;
            color: white;
            padding: 12px;
            text-align: left;
        }
        .summary-table td {
            padding: 10px 12px;
            border-bottom: 1px solid #ecf0f1;
        }
        .summary-table tr:hover {
            background: #f8f9fa;
        }
        .metric-section {
            background: white;
            padding: 25px;
            margin: 30px 0;
            border-radius: 10px;
            box-shadow: 0 2px 8px rgba(0,0,0,0.1);
        }
        .metric-description {
            background: #e8f4f8;
            padding: 15px;
            border-left: 4px solid #3498db;
            margin: 15px 0;
            border-radius: 4px;
        }
        .metric-formula {
            background: #fff3cd;
            padding: 15px;
            border-left: 4px solid #ffc107;
            margin: 15px 0;
            border-radius: 4px;
            font-family: 'Courier New', monospace;
        }
        .metric-interpretation {
            background: #d4edda;
            padding: 15px;
            border-left: 4px solid #28a745;
            margin: 15px 0;
            border-radius: 4px;
        }
        .chart-container {
            text-align: center;
            margin: 20px 0;
        }
        .chart-container img {
            max-width: 100%;
            height: auto;
            border-radius: 8px;
            box-shadow: 0 2px 6px rgba(0,0,0,0.15);
        }
        .data-table {
            width: 100%;
            border-collapse: collapse;
            margin: 15px 0;
            font-size: 0.9em;
            background: white;
        }
        .data-table th {
            background: #95a5a6;
            color: white;
            padding: 8px;
            text-align: left;
            position: sticky;
            top: 0;
        }
        .data-table td {
            padding: 6px 8px;
            border-bottom: 1px solid #ddd;
        }
        .data-table tr:nth-child(even) {
            background: #f8f9fa;
        }
        .variant-original { color: #3498db; font-weight: bold; }
        .variant-draco { color: #e74c3c; font-weight: bold; }
        .variant-meshopt { color: #2ecc71; font-weight: bold; }
        .comparison-positive { color: #27ae60; font-weight: bold; }
        .comparison-negative { color: #e74c3c; font-weight: bold; }
        .details {
            cursor: pointer;
            user-select: none;
        }
        .details-content {
            display: none;
            margin-top: 10px;
        }
        .details.open .details-content {
            display: block;
        }
        .badge {
            display: inline-block;
            padding: 4px 8px;
            border-radius: 4px;
            font-size: 0.85em;
            font-weight: bold;
            margin-left: 8px;
        }
        .badge-good { background: #d4edda; color: #155724; }
        .badge-warning { background: #fff3cd; color: #856404; }
        .badge-bad { background: #f8d7da; color: #721c24; }
        
        /* Estilos para descrições de seção */
        .section-description {
            font-size: 1.05em;
            color: #7f8c8d;
            margin: 15px 0 25px 0;
            line-height: 1.6;
            padding: 12px;
            background: #f8f9fa;
            border-radius: 6px;
            border-left: 3px solid #3498db;
        }
        
        /* Estilos para cards de decisão */
        .decision-cards {
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(400px, 1fr));
            gap: 25px;
            margin: 30px 0;
        }
        
        .decision-card {
            background: white;
            border-radius: 12px;
            box-shadow: 0 4px 12px rgba(0,0,0,0.1);
            overflow: hidden;
            transition: transform 0.2s, box-shadow 0.2s;
        }
        
        .decision-card:hover {
            transform: translateY(-5px);
            box-shadow: 0 8px 20px rgba(0,0,0,0.15);
        }
        
        .decision-card-excellent {
            border: 3px solid #27ae60;
        }
        
        .decision-card-good {
            border: 3px solid #2ecc71;
        }
        
        .decision-card-warning {
            border: 3px solid #f39c12;
        }
        
        .decision-card-bad {
            border: 3px solid #e74c3c;
        }
        
        .decision-card-header {
            padding: 20px;
            color: white;
            position: relative;
        }
        
        .decision-card-header h3 {
            margin: 0;
            color: white;
            font-size: 1.5em;
        }
        
        .recommendation-badge {
            position: absolute;
            top: 20px;
            right: 20px;
            background: rgba(255,255,255,0.25);
            backdrop-filter: blur(10px);
            padding: 8px 16px;
            border-radius: 20px;
            font-weight: bold;
            font-size: 0.9em;
        }
        
        .decision-card-body {
            padding: 25px;
        }
        
        .efficiency-score {
            text-align: center;
            margin-bottom: 20px;
            padding: 15px;
            background: #f8f9fa;
            border-radius: 8px;
        }
        
        .score-label {
            font-size: 0.9em;
            color: #7f8c8d;
            text-transform: uppercase;
            letter-spacing: 1px;
            margin-bottom: 8px;
        }
        
        .score-value {
            font-size: 2em;
            font-weight: bold;
            color: #2c3e50;
            margin: 10px 0;
        }
        
        .score-bar {
            width: 100%;
            height: 12px;
            background: #ecf0f1;
            border-radius: 6px;
            overflow: hidden;
            margin-top: 10px;
        }
        
        .score-fill {
            height: 100%;
            background: linear-gradient(90deg, #3498db, #2ecc71);
            transition: width 0.5s ease;
        }
        
        .recommendation-reason {
            background: #e8f5e9;
            padding: 12px;
            border-radius: 6px;
            border-left: 3px solid #27ae60;
            margin: 15px 0 20px 0;
            font-style: italic;
        }
        
        .metrics-grid {
            display: grid;
            grid-template-columns: repeat(2, 1fr);
            gap: 15px;
            margin-top: 20px;
        }
        
        .metric-item {
            background: #f8f9fa;
            padding: 15px;
            border-radius: 8px;
            border-left: 3px solid #3498db;
        }
        
        .metric-label {
            font-size: 0.85em;
            color: #7f8c8d;
            text-transform: uppercase;
            letter-spacing: 0.5px;
            margin-bottom: 8px;
        }
        
        .metric-value {
            font-size: 1.4em;
            font-weight: bold;
            color: #2c3e50;
            margin: 5px 0;
        }
        
        .metric-change {
            font-size: 0.9em;
            font-weight: bold;
            margin-top: 5px;
        }
        
        .metric-change.positive {
            color: #27ae60;
        }
        
        .metric-change.negative {
            color: #e74c3c;
        }
    </style>
    """
    
    # JavaScript para detalhes expansíveis
    js = """
    <script>
        function toggleDetails(id) {
            var elem = document.getElementById(id);
            elem.classList.toggle('open');
        }
    </script>
    """
    
    html = f"""<!DOCTYPE html>
<html lang="pt-BR">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Relatório de Performance - {model_name}</title>
    {css}
</head>
<body>
    <div class="header">
        <h1>📊 Relatório de Performance - {model_name}</h1>
        <p class="timestamp">Gerado em: {timestamp}</p>
    </div>
"""
    
    # Seção 1: Visão Geral
    html += "<h2>📋 Visão Geral das Variantes</h2>\n"
    html += '<p class="section-description">Comparação rápida entre todas as variantes testadas. Os valores mostram médias de todas as execuções.</p>\n'
    html += '<table class="summary-table">\n'
    html += '<tr><th>Variante</th><th>Tamanho (MB)</th><th>FPS Médio</th><th>Load Time (ms)</th><th>Memória (MB)</th><th>Amostras</th></tr>\n'
    
    for variant, data in stats.items():
        variant_class = f"variant-{variant}"
        html += f'<tr><td class="{variant_class}">{variant}</td>'
        html += f'<td>{data["file_mb_avg"]:.4f}</td>'  # 4 casas decimais para precisão
        html += f'<td>{data["fps_avg"]:.2f}</td>'     # 2 casas decimais
        html += f'<td>{data["load_ms_avg"]:.2f}</td>' # 2 casas decimais
        html += f'<td>{data["mem_mb_avg"]:.2f}</td>'  # 2 casas decimais
        html += f'<td>{data["samples"]}</td></tr>\n'
    
    html += '</table>\n'
    
    # Seção 2: Análise de Trade-offs e Recomendações
    if comparisons:
        html += "<h2>🎯 Análise de Trade-offs: Vale a Pena Comprimir?</h2>\n"
        html += '<p class="section-description">Compare os benefícios (redução de tamanho) com os custos (performance e tempo de carregamento). Use esta análise para decidir qual compressão usar.</p>\n'
        
        html += '<div class="decision-cards">\n'
        
        for comp_name, comp_data in comparisons.items():
            variant_name = comp_data['variant']
            variant_color = VARIANT_COLORS.get(variant_name, '#95a5a6')
            
            file_reduction = comp_data['file_size_reduction_pct']
            load_change = comp_data['load_time_change_pct']
            fps_change = comp_data['fps_change_pct']
            mem_change = comp_data['mem_change_pct']
            score = comp_data['efficiency_score']
            recommendation = comp_data['recommendation']
            reason = comp_data['reason']
            
            # Determinar classe do card baseado na recomendação
            if "✅ Recomendado" in recommendation:
                card_class = "decision-card-excellent"
            elif "✅ Bom" in recommendation:
                card_class = "decision-card-good"
            elif "⚠️" in recommendation:
                card_class = "decision-card-warning"
            else:
                card_class = "decision-card-bad"
            
            html += f'<div class="decision-card {card_class}">\n'
            html += f'<div class="decision-card-header" style="background-color: {variant_color};">\n'
            html += f'<h3>{variant_name.upper()}</h3>\n'
            html += f'<div class="recommendation-badge">{recommendation}</div>\n'
            html += f'</div>\n'
            
            html += f'<div class="decision-card-body">\n'
            
            # Score visual
            html += f'<div class="efficiency-score">\n'
            html += f'<div class="score-label">Score de Eficiência</div>\n'
            html += f'<div class="score-value">{score:.1f}/100</div>\n'
            html += f'<div class="score-bar"><div class="score-fill" style="width: {score}%;"></div></div>\n'
            html += f'</div>\n'
            
            # Motivo da recomendação
            html += f'<p class="recommendation-reason">💡 {reason}</p>\n'
            
            # Métricas detalhadas
            html += '<div class="metrics-grid">\n'
            
            # Tamanho do arquivo
            html += '<div class="metric-item">\n'
            html += '<div class="metric-label">📦 Tamanho do Arquivo</div>\n'
            html += f'<div class="metric-value">{comp_data["file_mb"]:.4f} MB</div>\n'
            html += f'<div class="metric-change {"positive" if file_reduction > 0 else "negative"}">{fmt_pct(file_reduction)} vs original</div>\n'
            html += '</div>\n'
            
            # FPS
            html += '<div class="metric-item">\n'
            html += '<div class="metric-label">🎮 FPS Médio</div>\n'
            html += f'<div class="metric-value">{comp_data["fps_avg"]:.2f} FPS</div>\n'
            html += f'<div class="metric-change {"positive" if fps_change >= 0 else "negative"}">{fmt_pct(fps_change)} vs original</div>\n'
            html += '</div>\n'
            
            # Load Time
            html += '<div class="metric-item">\n'
            html += '<div class="metric-label">⏱️ Tempo de Carregamento</div>\n'
            html += f'<div class="metric-value">{comp_data["load_ms"]:.2f} ms</div>\n'
            html += f'<div class="metric-change {"positive" if load_change <= 0 else "negative"}">{fmt_pct(load_change)} vs original</div>\n'
            html += '</div>\n'
            
            # Memória
            html += '<div class="metric-item">\n'
            html += '<div class="metric-label">💾 Memória</div>\n'
            html += f'<div class="metric-value">{comp_data["mem_mb"]:.2f} MB</div>\n'
            html += f'<div class="metric-change {"positive" if mem_change <= 0 else "negative"}">{fmt_pct(mem_change)} vs original</div>\n'
            html += '</div>\n'
            
            html += '</div>\n'  # metrics-grid
            html += '</div>\n'  # decision-card-body
            html += '</div>\n'  # decision-card
        
        html += '</div>\n'  # decision-cards
    
    # Seção 3: Métricas Detalhadas
    metrics_list = [
        ('load_ms', 'load_ms_avg', 'bars_load.png'),
        ('mem_mb', 'mem_mb_avg', 'bars_mem.png'),
        ('fps_avg', 'fps_avg', 'bars_fps.png'),
        ('fps_min', 'fps_min', 'bars_fps_min.png'),
        ('fps_max', 'fps_max', 'bars_fps_max.png'),
        ('fps_median', 'fps_median', 'bars_fps_median.png'),
        ('fps_1pc', 'fps_1pc_avg', 'bars_fps_1pc.png'),
        ('file_mb', 'file_mb_avg', 'bars_file_size.png'),
    ]
    
    for metric_id, stat_key, img_filename in metrics_list:
        metric_info = METRICS_INFO[metric_id]
        
        html += f'<div class="metric-section">\n'
        html += f'<h2>{metric_info["title"]}</h2>\n'
        
        # Descrição
        html += f'<div class="metric-description">\n'
        html += f'<h3>📖 O que é?</h3>\n'
        html += f'<p><strong>{metric_info["description"]}</strong></p>\n'
        html += f'<p><strong>Por que é importante:</strong> {metric_info["why_important"]}</p>\n'
        html += f'<p><strong>Valores ideais:</strong> {metric_info["ideal_range"]}</p>\n'
        html += f'</div>\n'
        
        # Gráfico
        html += f'<div class="chart-container">\n'
        html += f'<img src="images/{img_filename}" alt="{metric_info["title"]}">\n'
        html += f'</div>\n'
        
        # Fórmula
        html += f'<div class="metric-formula">\n'
        html += f'<h3>🔢 Como é calculada</h3>\n'
        html += f'<p><strong>Fórmula:</strong> {metric_info["formula"]}</p>\n'
        html += f'<p><strong>Fonte dos dados:</strong> Coluna <code>{metric_info["source_column"]}</code> no CSV</p>\n'
        html += f'</div>\n'
        
        # Análise
        html += f'<div class="metric-interpretation">\n'
        html += f'<h3>💡 Como analisar</h3>\n'
        html += f'<p>{metric_info["interpretation"]}</p>\n'
        
        # Valores específicos por variante
        html += f'<h4>Valores medidos:</h4>\n'
        html += '<ul>\n'
        for variant, data in stats.items():
            value = data.get(stat_key, 0)
            # Usar 4 casas decimais para tamanho de arquivo, 2 para outros
            precision = 4 if metric_id == 'file_mb' else 2
            html += f'<li class="variant-{variant}"><strong>{variant}:</strong> {value:.{precision}f} {metric_info["unit"]}</li>\n'
        html += '</ul>\n'
        html += f'</div>\n'
        
        # Dados brutos (expansível)
        if metric_id in df.columns:
            html += f'<div class="details" id="details-{metric_id}" onclick="toggleDetails(\'details-{metric_id}\')">\n'
            html += f'<h3>📊 Dados Brutos ▼ (clique para expandir)</h3>\n'
            html += f'<div class="details-content">\n'
            
            # Tabela com dados por variante
            for variant in stats.keys():
                variant_data = df[df['variant'] == variant]
                if len(variant_data) > 0 and metric_info['source_column'] in variant_data.columns:
                    html += f'<h4 class="variant-{variant}">{variant.capitalize()}:</h4>\n'
                    html += '<table class="data-table">\n'
                    html += '<tr><th>Timestamp</th><th>Valor</th></tr>\n'
                    
                    for _, row in variant_data.iterrows():
                        timestamp_val = row.get('timestamp', 'N/A')
                        metric_val = row.get(metric_info['source_column'], 0)
                        html += f'<tr><td>{timestamp_val}</td><td>{metric_val:.2f} {metric_info["unit"]}</td></tr>\n'
                    
                    html += '</table>\n'
            
            html += '</div>\n'
            html += '</div>\n'
        
        html += '</div>\n'
    
    # Seção 4: Metodologia
    html += '<div class="metric-section">\n'
    html += '<h2>🔬 Metodologia</h2>\n'
    html += '<h3>Como os testes foram executados</h3>\n'
    html += '<p>Os benchmarks foram realizados usando o sistema de métricas do PolyDiet, que:</p>\n'
    html += '<ul>\n'
    html += '<li>Carrega cada variante do modelo múltiplas vezes</li>\n'
    html += '<li>Mede tempo de carregamento, uso de memória e performance de rendering</li>\n'
    html += '<li>Captura FPS por 5 segundos em cada teste</li>\n'
    html += '<li>Calcula estatísticas agregadas (média, mínimo, máximo, percentis)</li>\n'
    html += '</ul>\n'
    
    # Info do ambiente
    if len(df) > 0:
        platform = df.iloc[0].get('platform', 'N/A')
        unity_version = df.iloc[0].get('unity_version', 'N/A')
        html += f'<p><strong>Ambiente:</strong></p>\n'
        html += f'<ul>\n'
        html += f'<li>Plataforma: {platform}</li>\n'
        html += f'<li>Unity Version: {unity_version}</li>\n'
        html += f'<li>Total de amostras: {len(df)}</li>\n'
        html += f'</ul>\n'
    
    html += '</div>\n'
    
    # Rodapé
    html += '<div style="text-align: center; margin-top: 50px; padding: 20px; color: #7f8c8d;">\n'
    html += '<p>Relatório gerado automaticamente pelo PolyDiet Report Generator</p>\n'
    html += '</div>\n'
    
    html += js
    html += '</body>\n</html>'
    
    # Salvar HTML
    try:
        with open(output_path, 'w', encoding='utf-8') as f:
            f.write(html)
        print(f"[py] ✓ HTML gerado: {output_path}")
    except Exception as e:
        print(f"[py] ❌ Erro ao salvar HTML: {e}")


# =====================================================================
# SEÇÃO 6: FUNÇÃO DE GERAÇÃO DE PDF
# =====================================================================

def generate_pdf_from_html(html_path, pdf_path, engine_path="/usr/bin/chromium"):
    """
    Gera PDF a partir do HTML usando Chromium headless (garante fidelidade visual)
    
    Args:
        html_path: Caminho do HTML gerado
        pdf_path: Caminho para salvar o PDF
        engine_path: Caminho do executável do Chromium/Chrome
    """
    import shutil, subprocess
    
    print("[py] Gerando PDF via Chromium...")
    
    # Encontrar Chromium/Chrome
    bin_ = engine_path if os.path.exists(engine_path) else shutil.which("chromium") or shutil.which("google-chrome")
    if not bin_:
        print("[py] ⚠️ Chromium/Chrome não encontrado; pulando PDF.")
        return
    
    # Comando para gerar PDF
    cmd = [
        bin_, 
        "--headless=new", 
        "--disable-gpu", 
        f"--print-to-pdf={pdf_path}", 
        html_path
    ]
    
    print(f"[py] PDF cmd: {' '.join(cmd)}")
    
    try:
        subprocess.run(cmd, check=False, timeout=30)
        if os.path.exists(pdf_path):
            print(f"[py] ✓ PDF gerado: {pdf_path}")
        else:
            print(f"[py] ⚠️ PDF não foi criado")
    except Exception as e:
        print(f"[py] ⚠️ Erro ao gerar PDF: {e}")


# =====================================================================
# SEÇÃO 7: FUNÇÃO DE EXPORTAÇÃO JSON
# =====================================================================

def export_json(model_name, stats, comparisons, output_path):
    """
    Exporta dados em formato JSON
    
    Args:
        model_name: Nome do modelo
        stats: Dict com estatísticas
        comparisons: Dict com comparações
        output_path: Caminho para salvar o JSON
    """
    print("[py] Gerando JSON...")
    
    data = {
        'model': model_name,
        'timestamp': datetime.now().isoformat(),
        'variants': stats,
        'comparisons': comparisons
    }
    
    try:
        with open(output_path, 'w', encoding='utf-8') as f:
            json.dump(data, f, indent=2, ensure_ascii=False)
        print(f"[py] ✓ JSON gerado: {output_path}")
    except Exception as e:
        print(f"[py] ❌ Erro ao salvar JSON: {e}")


# =====================================================================
# SEÇÃO 8: MAIN FUNCTION
# =====================================================================

def main():
    """Função principal"""
    parser = argparse.ArgumentParser(description='Gerador de Relatórios de Performance PolyDiet')
    parser.add_argument('--csv-files', nargs='+', required=True, help='Arquivos CSV de benchmark')
    parser.add_argument('--out', required=True, help='Diretório de saída')
    parser.add_argument('--model', required=True, help='Nome do modelo')
    parser.add_argument('--html', action='store_true', help='Gerar HTML')
    parser.add_argument('--pdf', action='store_true', help='Gerar PDF')
    parser.add_argument('--last-n', type=int, default=20, help='Usar as últimas N execuções por variante (padrão: 20)')
    
    args = parser.parse_args()
    
    print("[py] ========================================")
    print("[py] SIMPLE REPORT GENERATOR")
    print("[py] ========================================")
    print(f"[py] Python: {sys.executable}")
    print(f"[py] Versão: {sys.version}")
    print(f"[py] Diretório: {os.getcwd()}")
    print(f"[py] Modelo: {args.model}")
    print(f"[py] Output: {args.out}")
    print(f"[py] CSV Files: {len(args.csv_files)}")
    print(f"[py] Last-N: {args.last_n}")
    
    # Criar diretório de saída
    os.makedirs(args.out, exist_ok=True)
    
    try:
        # 1. Carregar dados
        df = load_csv_data(args.csv_files)
        if df is None or len(df) == 0:
            print("[py] ❌ Nenhum dado válido para processar")
            return 1
        
        # 2. Filtrar por modelo e últimas N execuções
        df = filter_by_model(df, args.model, args.last_n)
        if len(df) == 0:
            print(f"[py] ❌ Nenhum dado encontrado para o modelo '{args.model}'")
            return 1
        
        # 3. Calcular estatísticas
        stats = calculate_stats(df)
        if not stats:
            print("[py] ❌ Não foi possível calcular estatísticas")
            return 1
        
        # 4. Calcular comparações
        comparisons = calculate_comparisons(stats)
        
        # 5. Gerar gráficos PNG (agora passa df também)
        generate_all_charts(stats, args.out, df)
        
        # 6. Gerar HTML
        html_path = None
        if args.html:
            html_path = os.path.join(args.out, 'report.html')
            generate_html(args.model, stats, comparisons, df, html_path)
        
        # 7. Gerar PDF via Chromium (usa o mesmo HTML)
        if args.pdf and html_path and os.path.exists(html_path):
            pdf_path = os.path.join(args.out, 'report.pdf')
            generate_pdf_from_html(html_path, pdf_path)
        
        # 8. Exportar JSON
        json_path = os.path.join(args.out, 'data.json')
        export_json(args.model, stats, comparisons, json_path)
        
        print("[py] ========================================")
        print("[py] ✓ RELATÓRIO GERADO COM SUCESSO")
        print("[py] ========================================")
        return 0
        
    except Exception as e:
        print(f"[py] ❌ Erro fatal: {e}")
        import traceback
        traceback.print_exc()
        return 1


if __name__ == '__main__':
    sys.exit(main())

