#!/usr/bin/env python3
"""
Advanced Metrics Report Generator - Sistema Avançado de Reports

Este script gera relatórios HTML interativos e PDF com análises avançadas de métricas de performance.

Funcionalidades:
- Comparação detalhada entre variantes (original, draco, meshopt)
- Estatísticas avançadas (média, mediana, desvio padrão, percentis)
- Detecção de outliers
- Análise de evolução temporal
- Visualizações avançadas (heatmaps, box plots, scatter plots)
- Informações técnicas de arquivos GLB
- Exportação de dados em JSON
"""

import argparse
import os
import sys
import json
import subprocess
from datetime import datetime
from pathlib import Path

# Configurações centralizadas
CONFIG = {
    "base_variant": "original",
    "draco_variant": "draco",
    "meshopt_variant": "meshopt",
    "default_last_n": 20,
    "default_pdf_engine": "chrome",
    "outlier_threshold": 2.0,  # Z-score para detecção de outliers
}

# Verificar dependências
try:
    import pandas as pd
    import numpy as np
    print("[py] ✓ pandas e numpy carregados")
except ImportError as e:
    print(f"[py] ❌ Erro ao carregar pandas/numpy: {e}")
    print("[py] Instale com: pip install pandas numpy")
    sys.exit(1)

try:
    import plotly.graph_objs as go
    import plotly.io as pio
    from plotly.subplots import make_subplots
    print("[py] ✓ plotly carregado")
except ImportError as e:
    print(f"[py] ❌ Erro ao carregar plotly: {e}")
    print("[py] Instale com: pip install plotly")
    sys.exit(1)


# =====================================================================
# ESTRUTURAS DE DADOS
# =====================================================================

class FileInfo:
    """Informações de um arquivo de modelo"""
    def __init__(self, variant, size_bytes, path):
        self.variant = variant
        self.size_bytes = size_bytes
        self.size_mb = size_bytes / (1024 * 1024)
        self.path = path
        self.gltf_info = None  # Será preenchido pelo gltf_inspector
    
    def to_dict(self):
        return {
            "variant": self.variant,
            "size_bytes": self.size_bytes,
            "size_mb": self.size_mb,
            "path": self.path,
            "gltf_info": self.gltf_info
        }


class VariantStats:
    """Estatísticas avançadas para uma variante"""
    def __init__(self, data):
        if len(data) == 0:
            self.mean = self.median = self.std = self.min = self.max = 0
            self.q25 = self.q75 = self.p1 = self.p99 = 0
            self.count = 0
        else:
            self.mean = float(data.mean())
            self.median = float(data.median())
            self.std = float(data.std()) if len(data) > 1 else 0
            self.min = float(data.min())
            self.max = float(data.max())
            self.q25 = float(data.quantile(0.25))
            self.q75 = float(data.quantile(0.75))
            self.p1 = float(data.quantile(0.01))
            self.p99 = float(data.quantile(0.99))
            self.count = len(data)
    
    def to_dict(self):
        return {
            "mean": self.mean,
            "median": self.median,
            "std": self.std,
            "min": self.min,
            "max": self.max,
            "q25": self.q25,
            "q75": self.q75,
            "p1": self.p1,
            "p99": self.p99,
            "count": self.count
        }


# =====================================================================
# PARSING DE ARGUMENTOS
# =====================================================================

def parse_args():
    ap = argparse.ArgumentParser(description="Advanced Metrics Report Generator")
    ap.add_argument("--csv-files", nargs='+', required=True, help="Lista de caminhos para os arquivos CSV")
    ap.add_argument("--out", required=True, help="Diretório de saída")
    ap.add_argument("--model", required=True, help="Nome do modelo")
    ap.add_argument("--variants", default=f"{CONFIG['base_variant']},{CONFIG['draco_variant']},{CONFIG['meshopt_variant']}")
    ap.add_argument("--last-n", type=int, default=CONFIG["default_last_n"])
    ap.add_argument("--html", action="store_true", help="Gerar HTML")
    ap.add_argument("--pdf", action="store_true", help="Gerar PDF")
    ap.add_argument("--pdf-engine", default=CONFIG["default_pdf_engine"])
    ap.add_argument("--pdf-engine-path", default="")
    ap.add_argument("--file-info", action="append", default=[], help="Informações de arquivos (variant:size:path)")
    return ap.parse_args()


# =====================================================================
# CARREGAMENTO E PROCESSAMENTO DE DADOS
# =====================================================================

def load_multiple_csvs(csv_paths):
    """Carrega e combina múltiplos CSVs"""
    dfs = []
    for path in csv_paths:
        try:
            df = pd.read_csv(path)
            dfs.append(df)
            print(f"[py] CSV carregado: {path} ({len(df)} linhas)")
        except Exception as e:
            print(f"[py] ⚠️ Erro ao carregar {path}: {e}")
    
    if not dfs:
        raise ValueError("Nenhum CSV foi carregado com sucesso")
    
    combined_df = pd.concat(dfs, ignore_index=True)
    print(f"[py] Total de linhas combinadas: {len(combined_df)}")
    return combined_df


def parse_file_info(file_info_list):
    """Parseia informações de arquivos do formato variant:size:path"""
    file_infos = []
    for info_str in file_info_list:
        try:
            parts = info_str.split(':', 2)
            if len(parts) == 3:
                variant, size_str, path = parts
                file_info = FileInfo(variant, int(size_str), path)
                file_infos.append(file_info)
                print(f"[py] File info: {variant} = {file_info.size_mb:.2f} MB")
        except Exception as e:
            print(f"[py] ⚠️ Erro ao parsear file-info '{info_str}': {e}")
    
    return file_infos


# =====================================================================
# ANÁLISES COMPLEXAS
# =====================================================================

def compare_variants(df, variants, base_variant="original"):
    """Compara variantes e calcula ganhos/perdas percentuais"""
    comparisons = {}
    metrics = ["load_ms", "mem_mb", "fps_avg"]
    
    # Calcula médias para a variante base
    base_data = df[df['variant'] == base_variant]
    if len(base_data) == 0:
        print(f"[py] ⚠️ Variante base '{base_variant}' não encontrada")
        return comparisons
    
    for metric in metrics:
        base_value = base_data[metric].mean()
        
        for variant in variants:
            if variant == base_variant:
                continue
            
            variant_data = df[df['variant'] == variant]
            if len(variant_data) == 0:
                continue
            
            variant_value = variant_data[metric].mean()
            diff_abs = variant_value - base_value
            diff_pct = (diff_abs / base_value) * 100 if base_value != 0 else 0
            
            # Para load_ms e mem_mb, menor é melhor (negative diff é better)
            # Para fps_avg, maior é melhor (positive diff é better)
            better = diff_pct < 0 if metric in ["load_ms", "mem_mb"] else diff_pct > 0
            
            comparisons[f"{variant}_{metric}"] = {
                "variant": variant,
                "metric": metric,
                "base": base_value,
                "value": variant_value,
                "diff_abs": diff_abs,
                "diff_pct": diff_pct,
                "better": better
            }
    
    return comparisons


def analyze_temporal_evolution(df):
    """Analisa como as métricas evoluíram ao longo dos testes"""
    df_sorted = df.sort_values('timestamp').copy()
    trends = {}
    
    for metric in ["fps_avg", "load_ms", "mem_mb"]:
        if len(df_sorted) > 1:
            x = np.arange(len(df_sorted))
            y = df_sorted[metric].values
            
            # Calcula tendência linear
            coeffs = np.polyfit(x, y, 1)
            slope = coeffs[0]
            
            # Para load_ms e mem_mb, slope negativo é improving
            # Para fps_avg, slope positivo é improving
            improving = slope < 0 if metric in ["load_ms", "mem_mb"] else slope > 0
            
            trends[metric] = {
                "slope": float(slope),
                "improving": improving,
                "first_value": float(y[0]),
                "last_value": float(y[-1]),
                "total_change": float(y[-1] - y[0])
            }
    
    return trends


def detect_outliers(df, metric, threshold=2.0):
    """Detecta outliers usando z-score"""
    if len(df) < 3:
        return pd.DataFrame()
    
    mean = df[metric].mean()
    std = df[metric].std()
    
    if std == 0:
        return pd.DataFrame()
    
    z_scores = (df[metric] - mean) / std
    outliers = df[abs(z_scores) > threshold].copy()
    outliers['z_score'] = z_scores[outliers.index]
    
    return outliers


def calculate_compression_ratios(file_infos):
    """Calcula taxas de compressão entre variantes"""
    original_file = next((f for f in file_infos if f.variant == "original"), None)
    
    if not original_file:
        return {}
    
    original_size = original_file.size_bytes
    ratios = {}
    
    for file_info in file_infos:
        if file_info.variant != "original":
            compression_pct = (1 - file_info.size_bytes / original_size) * 100
            size_reduction_mb = (original_size - file_info.size_bytes) / (1024 * 1024)
            
            ratios[file_info.variant] = {
                "size_mb": file_info.size_mb,
                "compression_pct": compression_pct,
                "size_reduction_mb": size_reduction_mb
            }
    
    return ratios


def calculate_all_stats(df, variants):
    """Calcula estatísticas para todas as variantes e métricas"""
    all_stats = {}
    metrics = ["load_ms", "mem_mb", "fps_avg", "fps_min", "fps_max", "fps_median"]
    
    for variant in variants:
        variant_data = df[df['variant'] == variant]
        if len(variant_data) == 0:
            continue
        
        all_stats[variant] = {}
        for metric in metrics:
            if metric in variant_data.columns:
                all_stats[variant][metric] = VariantStats(variant_data[metric])
    
    return all_stats


# =====================================================================
# VISUALIZAÇÕES
# =====================================================================

def create_bar_chart(df, variants, metric, title, unit, color_map):
    """Cria gráfico de barras comparativo melhorado"""
    values = []
    colors = []
    errors = []
    
    for variant in variants:
        variant_data = df[df['variant'] == variant]
        if len(variant_data) > 0:
            mean_val = variant_data[metric].mean()
            std_val = variant_data[metric].std()
            values.append(mean_val)
            errors.append(std_val)
            colors.append(color_map.get(variant, '#999'))
        else:
            values.append(0)
            errors.append(0)
            colors.append('#999')
    
    fig = go.Figure(data=[
        go.Bar(
            x=variants,
            y=values,
            error_y=dict(type='data', array=errors, visible=True),
            marker_color=colors,
            text=[f"{v:.1f}±{e:.1f}" for v, e in zip(values, errors)],
            textposition='outside',
            textfont=dict(size=12, color='black'),
            marker_line=dict(width=2, color='white'),
            opacity=0.8
        )
    ])
    
    fig.update_layout(
        title=dict(text=title, font=dict(size=16, color='#2c3e50')),
        xaxis=dict(title=dict(text="Variante", font=dict(size=14))),
        yaxis=dict(title=dict(text=unit, font=dict(size=14))),
        template='plotly_white',
        plot_bgcolor='rgba(0,0,0,0)',
        paper_bgcolor='rgba(0,0,0,0)',
        margin=dict(l=50, r=50, t=60, b=50),
        height=400
    )
    
    return fig


def create_box_plots(df, variants, metric, title, unit, color_map):
    """Cria box plots melhorados para análise de distribuição"""
    fig = go.Figure()
    
    for variant in variants:
        variant_data = df[df['variant'] == variant]
        if len(variant_data) > 0:
            fig.add_trace(go.Box(
                y=variant_data[metric],
                name=variant,
                marker_color=color_map.get(variant, '#999'),
                boxmean='sd',
                boxpoints='outliers',
                jitter=0.3,
                pointpos=-1.8,
                marker=dict(size=6, opacity=0.7),
                line=dict(width=2),
                fillcolor=f"rgba({int(color_map.get(variant, '#999')[1:3], 16)}, {int(color_map.get(variant, '#999')[3:5], 16)}, {int(color_map.get(variant, '#999')[5:7], 16)}, 0.3)"
            ))
    
    fig.update_layout(
        title=dict(text=title, font=dict(size=16, color='#2c3e50')),
        yaxis=dict(title=dict(text=unit, font=dict(size=14))),
        template='plotly_white',
        plot_bgcolor='rgba(0,0,0,0)',
        paper_bgcolor='rgba(0,0,0,0)',
        showlegend=True,
        margin=dict(l=50, r=50, t=60, b=50),
        height=400
    )
    
    return fig


def create_scatter_plot(df, variants, x_metric, y_metric, title, color_map):
    """Cria scatter plot melhorado para análise de relações"""
    fig = go.Figure()
    
    for variant in variants:
        variant_data = df[df['variant'] == variant]
        if len(variant_data) > 0:
            fig.add_trace(go.Scatter(
                x=variant_data[x_metric],
                y=variant_data[y_metric],
                mode='markers',
                name=variant,
                marker=dict(
                    size=12,
                    color=color_map.get(variant, '#999'),
                    opacity=0.7,
                    line=dict(width=2, color='white')
                ),
                text=[f"Teste {i+1}" for i in range(len(variant_data))],
                hovertemplate=f"<b>{variant}</b><br>" +
                            f"{x_metric}: %{{x}}<br>" +
                            f"{y_metric}: %{{y}}<br>" +
                            "Teste: %{text}<extra></extra>"
            ))
    
    fig.update_layout(
        title=dict(text=title, font=dict(size=16, color='#2c3e50')),
        xaxis=dict(title=dict(text=x_metric.replace('_', ' ').title(), font=dict(size=14))),
        yaxis=dict(title=dict(text=y_metric.replace('_', ' ').title(), font=dict(size=14))),
        template='plotly_white',
        plot_bgcolor='rgba(0,0,0,0)',
        paper_bgcolor='rgba(0,0,0,0)',
        margin=dict(l=50, r=50, t=60, b=50),
        height=400
    )
    
    return fig


def create_heatmap(df, metrics):
    """Cria heatmap melhorado de correlações entre métricas"""
    corr_matrix = df[metrics].corr()
    
    fig = go.Figure(data=go.Heatmap(
        z=corr_matrix.values,
        x=corr_matrix.columns,
        y=corr_matrix.columns,
        colorscale='RdBu',
        zmid=0,
        text=corr_matrix.values.round(2),
        texttemplate='%{text}',
        textfont={"size": 12, "color": "white"},
        hoverongaps=False,
        hovertemplate='<b>%{y}</b> vs <b>%{x}</b><br>Correlação: %{z:.3f}<extra></extra>'
    ))
    
    fig.update_layout(
        title=dict(text="Correlação entre Métricas", font=dict(size=16, color='#2c3e50')),
        template='plotly_white',
        plot_bgcolor='rgba(0,0,0,0)',
        paper_bgcolor='rgba(0,0,0,0)',
        margin=dict(l=50, r=50, t=60, b=50),
        height=400
    )
    
    return fig


def create_timeline_chart(df, variants, metric, title, unit, color_map):
    """Cria gráfico melhorado de evolução temporal"""
    df_sorted = df.sort_values('timestamp').copy()
    fig = go.Figure()
    
    for variant in variants:
        variant_data = df_sorted[df_sorted['variant'] == variant]
        if len(variant_data) > 0:
            fig.add_trace(go.Scatter(
                x=variant_data['timestamp'],
                y=variant_data[metric],
                mode='lines+markers',
                name=variant,
                line=dict(color=color_map.get(variant, '#999'), width=3),
                marker=dict(size=8, color=color_map.get(variant, '#999')),
                text=[f"Teste {i+1}" for i in range(len(variant_data))],
                hovertemplate=f"<b>{variant}</b><br>" +
                            f"Timestamp: %{{x}}<br>" +
                            f"{metric}: %{{y}}<br>" +
                            "Teste: %{text}<extra></extra>"
            ))
    
    fig.update_layout(
        title=dict(text=title, font=dict(size=16, color='#2c3e50')),
        xaxis=dict(title=dict(text="Timestamp", font=dict(size=14))),
        yaxis=dict(title=dict(text=unit, font=dict(size=14))),
        template='plotly_white',
        plot_bgcolor='rgba(0,0,0,0)',
        paper_bgcolor='rgba(0,0,0,0)',
        margin=dict(l=50, r=50, t=60, b=50),
        height=400
    )
    
    return fig


def create_file_size_chart(file_infos, color_map):
    """Cria gráfico melhorado de tamanho de arquivos"""
    variants = [f.variant for f in file_infos]
    sizes = [f.size_mb for f in file_infos]
    colors = [color_map.get(v, '#999') for v in variants]
    
    fig = go.Figure(data=[
        go.Bar(
            x=variants,
            y=sizes,
            marker_color=colors,
            text=[f"{s:.2f} MB" for s in sizes],
            textposition='outside',
            textfont=dict(size=12, color='black'),
            marker_line=dict(width=2, color='white'),
            opacity=0.8
        )
    ])
    
    fig.update_layout(
        title=dict(text="Tamanho dos Arquivos por Variante", font=dict(size=16, color='#2c3e50')),
        xaxis=dict(title=dict(text="Variante", font=dict(size=14))),
        yaxis=dict(title=dict(text="Tamanho (MB)", font=dict(size=14))),
        template='plotly_white',
        plot_bgcolor='rgba(0,0,0,0)',
        paper_bgcolor='rgba(0,0,0,0)',
        margin=dict(l=50, r=50, t=60, b=50),
        height=400
    )
    
    return fig


# =====================================================================
# GERAÇÃO DE HTML
# =====================================================================

def create_html_section(title, content):
    """Cria uma seção HTML"""
    return f"""
    <div class="section">
        <h2>{title}</h2>
        {content}
    </div>
    """


def create_executive_summary(model, df, variants, comparisons, file_infos):
    """Cria resumo executivo"""
    content = f"""
    <div class="summary-grid">
        <div class="summary-card">
            <h3>Modelo</h3>
            <p class="big-number">{model}</p>
        </div>
        <div class="summary-card">
            <h3>Total de Testes</h3>
            <p class="big-number">{len(df)}</p>
        </div>
        <div class="summary-card">
            <h3>Variantes</h3>
            <p class="big-number">{len(variants)}</p>
        </div>
        <div class="summary-card">
            <h3>Período</h3>
            <p class="big-number">{df['timestamp'].min()} a {df['timestamp'].max()}</p>
        </div>
    </div>
    """
    
    return create_html_section("Resumo Executivo", content)


def create_performance_comparison_table(comparisons):
    """Cria tabela organizada de comparação de performance"""
    if not comparisons:
        return ""
    
    # Separar por métrica
    fps_comparisons = {k: v for k, v in comparisons.items() if 'fps' in v['metric']}
    load_comparisons = {k: v for k, v in comparisons.items() if 'load' in v['metric']}
    mem_comparisons = {k: v for k, v in comparisons.items() if 'mem' in v['metric']}
    
    def create_metric_table(comparisons_dict, title, icon):
        if not comparisons_dict:
            return ""
        
        rows = []
        for key, comp in comparisons_dict.items():
            arrow = "↓" if comp['better'] else "↑"
            color = "#28a745" if comp['better'] else "#dc3545"
            improvement = "Melhora" if comp['better'] else "Piora"
            
            rows.append(f"""
            <tr>
                <td><span class="variant-badge variant-{comp['variant']}">{comp['variant']}</span></td>
                <td>{comp['base']:.1f}</td>
                <td>{comp['value']:.1f}</td>
                <td style="color: {color}; font-weight: bold;">
                    <span class="arrow">{arrow}</span> {comp['diff_pct']:.1f}%
                    <small>({improvement})</small>
                </td>
            </tr>
            """)
        
        return f"""
        <div class="metric-table">
            <h3>{icon} {title}</h3>
            <table class="comparison-table">
                <thead>
                    <tr>
                        <th>Variante</th>
                        <th>Original</th>
                        <th>Valor</th>
                        <th>Diferença</th>
                    </tr>
                </thead>
                <tbody>
                    {''.join(rows)}
                </tbody>
            </table>
        </div>
        """
    
    fps_table = create_metric_table(fps_comparisons, "Performance FPS", "🎯")
    load_table = create_metric_table(load_comparisons, "Tempo de Carregamento", "⏱️")
    mem_table = create_metric_table(mem_comparisons, "Uso de Memória", "💾")
    
    combined_tables = f"""
    <div class="comparison-grid">
        {fps_table}
        {load_table}
        {mem_table}
    </div>
    """
    
    return create_html_section("📊 Comparação de Performance", combined_tables)


def create_detailed_stats_tables(all_stats):
    """Cria tabelas detalhadas de estatísticas organizadas por variante"""
    if not all_stats:
        return ""
    
    def create_variant_stats_table(variant, metrics_stats):
        if not metrics_stats:
            return ""
        
        rows = []
        for metric_name, stats in metrics_stats.items():
            rows.append(f"""
            <tr>
                <td class="metric-name">{metric_name.replace('_', ' ').title()}</td>
                <td>{stats.mean:.1f}</td>
                <td>{stats.median:.1f}</td>
                <td>{stats.std:.1f}</td>
                <td>{stats.min:.1f}</td>
                <td>{stats.max:.1f}</td>
                <td>{stats.count}</td>
            </tr>
            """)
        
        return f"""
        <div class="variant-stats">
            <h3><span class="variant-badge variant-{variant}">{variant}</span> - Estatísticas Detalhadas</h3>
            <table class="stats-table">
                <thead>
                    <tr>
                        <th>Métrica</th>
                        <th>Média</th>
                        <th>Mediana</th>
                        <th>Desvio</th>
                        <th>Mínimo</th>
                        <th>Máximo</th>
                        <th>Testes</th>
                    </tr>
                </thead>
                <tbody>
                    {''.join(rows)}
                </tbody>
            </table>
        </div>
        """
    
    tables = []
    for variant, metrics_stats in all_stats.items():
        tables.append(create_variant_stats_table(variant, metrics_stats))
    
    combined_tables = f"""
    <div class="stats-grid">
        {''.join(tables)}
    </div>
    """
    
    return create_html_section("📈 Estatísticas Detalhadas por Variante", combined_tables)


def create_file_info_section(file_infos, compression_ratios):
    """Cria seção melhorada de informações de arquivos"""
    if not file_infos:
        return ""
    
    def format_size(size_bytes):
        if size_bytes < 1024:
            return f"{size_bytes:.0f} B"
        elif size_bytes < 1024 * 1024:
            return f"{size_bytes/1024:.1f} KB"
        else:
            return f"{size_bytes/(1024*1024):.2f} MB"
    
    rows = []
    for file_info in file_infos:
        compression_text = ""
        compression_badge = ""
        if file_info.variant in compression_ratios:
            comp = compression_ratios[file_info.variant]
            compression_text = f"({comp['compression_pct']:.1f}% menor)"
            if comp['compression_pct'] > 50:
                compression_badge = '<span class="badge badge-success">Excelente</span>'
            elif comp['compression_pct'] > 25:
                compression_badge = '<span class="badge badge-warning">Bom</span>'
            else:
                compression_badge = '<span class="badge badge-info">Moderado</span>'
        
        rows.append(f"""
        <tr>
            <td><span class="variant-badge variant-{file_info.variant}">{file_info.variant}</span></td>
            <td class="size-cell">
                <strong>{file_info.size_mb:.2f} MB</strong>
                <small>{compression_text}</small>
                {compression_badge}
            </td>
            <td class="path-cell">
                <code>{file_info.path}</code>
            </td>
        </tr>
        """)
    
    table = f"""
    <div class="file-info-table">
        <table class="file-table">
            <thead>
                <tr>
                    <th>Variante</th>
                    <th>Tamanho & Compressão</th>
                    <th>Caminho do Arquivo</th>
                </tr>
            </thead>
            <tbody>
                {''.join(rows)}
            </tbody>
        </table>
    </div>
    """
    
    return create_html_section("📁 Informações dos Arquivos", table)


def build_html(model, sections):
    """Constrói HTML completo"""
    
    html = f"""
<!DOCTYPE html>
<html>
<head>
    <meta charset="UTF-8">
    <title>Advanced Report - {model}</title>
    <style>
        * {{
            margin: 0;
            padding: 0;
            box-sizing: border-box;
        }}
        
        body {{
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            min-height: 100vh;
            padding: 20px;
        }}
        
        .container {{
            max-width: 1600px;
            margin: 0 auto;
            background: white;
            border-radius: 15px;
            box-shadow: 0 20px 40px rgba(0,0,0,0.1);
            overflow: hidden;
        }}
        
        .header {{
            background: linear-gradient(135deg, #4CAF50 0%, #45a049 100%);
            color: white;
            padding: 40px;
            text-align: center;
        }}
        
        .header h1 {{
            font-size: 2.5em;
            margin-bottom: 10px;
            text-shadow: 2px 2px 4px rgba(0,0,0,0.3);
        }}
        
        .header .subtitle {{
            font-size: 1.2em;
            opacity: 0.9;
        }}
        
        .content {{
            padding: 40px;
        }}
        
        .section {{
            margin: 50px 0;
            padding: 30px;
            background: #f8f9fa;
            border-radius: 10px;
            border-left: 5px solid #4CAF50;
        }}
        
        .section h2 {{
            color: #2c3e50;
            font-size: 1.8em;
            margin-bottom: 25px;
            display: flex;
            align-items: center;
            gap: 10px;
        }}
        
        .summary-grid {{
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(250px, 1fr));
            gap: 25px;
            margin: 25px 0;
        }}
        
        .summary-card {{
            background: white;
            padding: 25px;
            border-radius: 10px;
            text-align: center;
            box-shadow: 0 5px 15px rgba(0,0,0,0.1);
            transition: transform 0.3s ease;
        }}
        
        .summary-card:hover {{
            transform: translateY(-5px);
        }}
        
        .summary-card h3 {{
            color: #666;
            font-size: 14px;
            margin-bottom: 15px;
            text-transform: uppercase;
            letter-spacing: 1px;
        }}
        
        .big-number {{
            font-size: 2.5em;
            font-weight: bold;
            color: #4CAF50;
            margin: 0;
        }}
        
        .comparison-grid {{
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(400px, 1fr));
            gap: 30px;
            margin: 25px 0;
        }}
        
        .metric-table {{
            background: white;
            padding: 25px;
            border-radius: 10px;
            box-shadow: 0 5px 15px rgba(0,0,0,0.1);
        }}
        
        .metric-table h3 {{
            color: #2c3e50;
            margin-bottom: 20px;
            font-size: 1.3em;
        }}
        
        .stats-grid {{
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(500px, 1fr));
            gap: 30px;
            margin: 25px 0;
        }}
        
        .variant-stats {{
            background: white;
            padding: 25px;
            border-radius: 10px;
            box-shadow: 0 5px 15px rgba(0,0,0,0.1);
        }}
        
        .variant-stats h3 {{
            color: #2c3e50;
            margin-bottom: 20px;
            font-size: 1.3em;
        }}
        
        table {{
            width: 100%;
            border-collapse: collapse;
            margin: 20px 0;
            background: white;
            border-radius: 8px;
            overflow: hidden;
            box-shadow: 0 2px 10px rgba(0,0,0,0.1);
        }}
        
        th {{
            background: linear-gradient(135deg, #4CAF50 0%, #45a049 100%);
            color: white;
            padding: 15px;
            text-align: left;
            font-weight: bold;
            font-size: 14px;
            text-transform: uppercase;
            letter-spacing: 0.5px;
        }}
        
        td {{
            padding: 15px;
            border-bottom: 1px solid #eee;
            vertical-align: middle;
        }}
        
        tr:hover {{
            background: #f8f9fa;
        }}
        
        .variant-badge {{
            display: inline-block;
            padding: 5px 12px;
            border-radius: 20px;
            font-size: 12px;
            font-weight: bold;
            text-transform: uppercase;
            letter-spacing: 0.5px;
        }}
        
        .variant-original {{
            background: #2196F3;
            color: white;
        }}
        
        .variant-draco {{
            background: #FF9800;
            color: white;
        }}
        
        .variant-meshopt {{
            background: #4CAF50;
            color: white;
        }}
        
        .badge {{
            display: inline-block;
            padding: 3px 8px;
            border-radius: 12px;
            font-size: 10px;
            font-weight: bold;
            margin-left: 8px;
        }}
        
        .badge-success {{
            background: #28a745;
            color: white;
        }}
        
        .badge-warning {{
            background: #ffc107;
            color: #212529;
        }}
        
        .badge-info {{
            background: #17a2b8;
            color: white;
        }}
        
        .arrow {{
            font-size: 16px;
            margin-right: 5px;
        }}
        
        .chart {{
            margin: 30px 0;
            background: white;
            padding: 20px;
            border-radius: 10px;
            box-shadow: 0 5px 15px rgba(0,0,0,0.1);
        }}
        
        .metric-name {{
            font-weight: bold;
            color: #2c3e50;
        }}
        
        .size-cell {{
            text-align: center;
        }}
        
        .path-cell {{
            font-family: 'Courier New', monospace;
            font-size: 12px;
        }}
        
        .timestamp {{
            color: #888;
            font-size: 14px;
            text-align: center;
            margin-top: 40px;
            padding: 20px;
            background: #f8f9fa;
            border-radius: 8px;
        }}
        
        @media (max-width: 768px) {{
            .comparison-grid,
            .stats-grid {{
                grid-template-columns: 1fr;
            }}
            
            .summary-grid {{
                grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
            }}
            
            .container {{
                margin: 10px;
                border-radius: 10px;
            }}
            
            .content {{
                padding: 20px;
            }}
            
            .section {{
                padding: 20px;
            }}
        }}
    </style>
    <script src="https://cdn.plot.ly/plotly-latest.min.js"></script>
</head>
<body>
    <div class="container">
        <div class="header">
            <h1>📊 Advanced Metrics Report</h1>
            <div class="subtitle">{model}</div>
        </div>
        <div class="content">
            {''.join(sections)}
            <div class="timestamp">
                Gerado em: {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}
            </div>
        </div>
    </div>
</body>
</html>
"""
    
    return html


# =====================================================================
# MAIN
# =====================================================================

def main():
    print("[py] ========================================")
    print("[py] ADVANCED METRICS REPORT GENERATOR")
    print("[py] ========================================")
    print(f"[py] Python: {sys.executable}")
    print(f"[py] Versão: {sys.version}")
    print(f"[py] Diretório: {os.getcwd()}")
    
    args = parse_args()
    
    print(f"[py] Modelo: {args.model}")
    print(f"[py] Output: {args.out}")
    print(f"[py] CSV Files: {len(args.csv_files)}")
    print(f"[py] File Info: {len(args.file_info)}")
    
    os.makedirs(args.out, exist_ok=True)
    
    # Carregar dados
    df = load_multiple_csvs(args.csv_files)
    variants = [v.strip().lower() for v in args.variants.split(",") if v.strip()]
    
    # Filtrar dados
    df = df[df['variant'].isin(variants)]
    df = df.tail(args.last_n * len(variants)) if args.last_n > 0 else df
    
    print(f"[py] Dados filtrados: {len(df)} linhas")
    
    # Parsear informações de arquivos
    file_infos = parse_file_info(args.file_info)
    
    # Análises
    print("[py] Executando análises...")
    comparisons = compare_variants(df, variants)
    trends = analyze_temporal_evolution(df)
    compression_ratios = calculate_compression_ratios(file_infos)
    all_stats = calculate_all_stats(df, variants)
    
    # Color map
    color_map = {
        "original": "#2196F3",
        "draco": "#FF9800",
        "meshopt": "#4CAF50"
    }
    
    # Criar visualizações
    print("[py] Criando visualizações...")
    sections = []
    
    # 1. Resumo Executivo
    sections.append(create_executive_summary(args.model, df, variants, comparisons, file_infos))
    
    # 2. Informações de Arquivos
    if file_infos:
        sections.append(create_file_info_section(file_infos, compression_ratios))
        fig = create_file_size_chart(file_infos, color_map)
        sections.append(create_html_section("Tamanho dos Arquivos", f'<div class="chart">{fig.to_html(include_plotlyjs=False, div_id="file_size")}</div>'))
    
    # 3. Tabelas de Comparação Organizadas
    sections.append(create_performance_comparison_table(comparisons))
    
    # 4. Estatísticas Detalhadas por Variante
    sections.append(create_detailed_stats_tables(all_stats))
    
    # 5. Gráficos de Barras
    for metric, title, unit in [("load_ms", "Tempo de Carregamento", "ms"), 
                                  ("mem_mb", "Memória (média)", "MB"),
                                  ("fps_avg", "FPS (média)", "FPS")]:
        fig = create_bar_chart(df, variants, metric, title, unit, color_map)
        sections.append(create_html_section(title, f'<div class="chart">{fig.to_html(include_plotlyjs=False, div_id=f"bar_{metric}")}</div>'))
    
    # 6. Box Plots
    for metric, title, unit in [("fps_avg", "Distribuição de FPS", "FPS"),
                                  ("load_ms", "Distribuição de Tempo de Carregamento", "ms")]:
        fig = create_box_plots(df, variants, metric, title, unit, color_map)
        sections.append(create_html_section(f"{title} (Box Plot)", f'<div class="chart">{fig.to_html(include_plotlyjs=False, div_id=f"box_{metric}")}</div>'))
    
    # 7. Scatter Plots
    fig = create_scatter_plot(df, variants, "load_ms", "fps_avg", "FPS vs Tempo de Carregamento", color_map)
    sections.append(create_html_section("Relação FPS vs Load Time", f'<div class="chart">{fig.to_html(include_plotlyjs=False, div_id="scatter_fps_load")}</div>'))
    
    # 8. Heatmap
    metrics_for_corr = ["load_ms", "mem_mb", "fps_avg", "fps_min", "fps_max"]
    fig = create_heatmap(df, metrics_for_corr)
    sections.append(create_html_section("Correlação entre Métricas", f'<div class="chart">{fig.to_html(include_plotlyjs=False, div_id="heatmap")}</div>'))
    
    # 9. Evolução Temporal
    fig = create_timeline_chart(df, variants, "fps_avg", "Evolução de FPS ao Longo do Tempo", "FPS", color_map)
    sections.append(create_html_section("Evolução Temporal", f'<div class="chart">{fig.to_html(include_plotlyjs=False, div_id="timeline_fps")}</div>'))
    
    # Construir HTML
    html = build_html(args.model, sections)
    
    # Salvar HTML
    html_path = os.path.join(args.out, "report.html")
    with open(html_path, 'w', encoding='utf-8') as f:
        f.write(html)
    print(f"[py] HTML gerado: {html_path}")
    
    # Exportar JSON
    json_data = {
        "model": args.model,
        "timestamp": datetime.now().isoformat(),
        "file_infos": [f.to_dict() for f in file_infos],
        "compression_ratios": compression_ratios,
        "comparisons": comparisons,
        "all_stats": {v: {m: s.to_dict() for m, s in metrics.items()} for v, metrics in all_stats.items()},
        "trends": trends,
        "total_tests": len(df),
        "variants": variants
    }
    
    json_path = os.path.join(args.out, "data.json")
    with open(json_path, 'w', encoding='utf-8') as f:
        json.dump(json_data, f, indent=2)
    print(f"[py] JSON gerado: {json_path}")
    
    # PDF (se solicitado)
    if args.pdf:
        pdf_path = os.path.join(args.out, "report.pdf")
        print(f"[py] Gerando PDF: {pdf_path}")
        # TODO: Implementar geração de PDF
        print(f"[py] ⚠️ Geração de PDF não implementada ainda")
    
    print("[py] ✓ Report gerado com sucesso!")
    return 0


if __name__ == "__main__":
    try:
        sys.exit(main())
    except Exception as e:
        print(f"[py] ❌ Erro fatal: {e}")
        import traceback
        traceback.print_exc()
        sys.exit(1)

