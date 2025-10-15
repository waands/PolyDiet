#!/usr/bin/env python3
import os

def safe(s):
    if not s:
        return ""
    return '"' + s.replace('"', "''") + '"'

def simulate_unity_upsert():
    # Simulando o que o Unity faria
    header = "timestamp,run_id,platform,unity_version,scene,model,variant,file_mb,load_ms,mem_mb,fps_avg,fps_1pc_low,fps_window_s,ok"
    
    # Simulando dados existentes (com cabeçalho)
    existing_lines = [
        "timestamp,run_id,platform,unity_version,scene,model,variant,file_mb,load_ms,mem_mb,fps_avg,fps_1pc_low,fps_window_s,ok",
        '2025-10-14T19:54:09-03:00,"20251014_195204","LinuxEditor","6000.2.4f1","ModelViewer","suzanne","original",0.019,255.912,379.754,62.29,40.94,5,true',
        '2025-10-14T19:54:15-03:00,"20251014_195204","LinuxEditor","6000.2.4f1","ModelViewer","suzanne","meshopt",0.006,72.544,377.3,62.18,36.67,5,true'
    ]
    
    # Simulando nova entrada (mesmo modelo/variante - deve substituir)
    scene = "ModelViewer"
    model_name = "suzanne"
    variant = "original"
    
    # Nova linha
    newline = '2025-10-14T20:10:00-03:00,"20251014_201000","LinuxEditor","6000.2.4f1","ModelViewer","suzanne","original",0.020,260.000,380.000,62.50,41.00,5,true'
    
    # Padrão para busca
    pattern = "," + safe(scene) + "," + safe(model_name) + "," + safe(variant) + ","
    print(f"Padrão de busca: {pattern}")
    
    # Simulando a lógica do UPSERT
    print("\n=== SIMULAÇÃO UPSERT ===")
    print("1. Sempre escreve o cabeçalho primeiro")
    result_lines = [header]
    
    print("2. Percorre linhas existentes:")
    replaced = False
    for i, line in enumerate(existing_lines):
        if i == 0 and line == header:
            print(f"   Linha {i}: PULANDO (é cabeçalho)")
            continue
        
        print(f"   Linha {i}: {line}")
        
        if not replaced and pattern in line:
            print(f"   -> SUBSTITUINDO por nova linha")
            result_lines.append(newline)
            replaced = True
        else:
            result_lines.append(line)
    
    if not replaced:
        print("   -> Adicionando nova linha no final")
        result_lines.append(newline)
    
    print(f"\nResultado final ({len(result_lines)} linhas):")
    for i, line in enumerate(result_lines):
        print(f"   {i}: {line}")
    
    return result_lines

def test_without_header():
    # Testando o cenário onde o arquivo não tem cabeçalho
    print("\n" + "="*60)
    print("TESTE: Arquivo SEM cabeçalho")
    print("="*60)
    
    header = "timestamp,run_id,platform,unity_version,scene,model,variant,file_mb,load_ms,mem_mb,fps_avg,fps_1pc_low,fps_window_s,ok"
    
    # Simulando dados existentes (SEM cabeçalho)
    existing_lines = [
        '2025-10-14T19:54:09-03:00,"20251014_195204","LinuxEditor","6000.2.4f1","ModelViewer","suzanne","original",0.019,255.912,379.754,62.29,40.94,5,true',
        '2025-10-14T19:54:15-03:00,"20251014_195204","LinuxEditor","6000.2.4f1","ModelViewer","suzanne","meshopt",0.006,72.544,377.3,62.18,36.67,5,true'
    ]
    
    # Simulando nova entrada
    scene = "ModelViewer"
    model_name = "suzanne"
    variant = "original"
    
    newline = '2025-10-14T20:10:00-03:00,"20251014_201000","LinuxEditor","6000.2.4f1","ModelViewer","suzanne","original",0.020,260.000,380.000,62.50,41.00,5,true'
    
    pattern = "," + safe(scene) + "," + safe(model_name) + "," + safe(variant) + ","
    print(f"Padrão de busca: {pattern}")
    
    print("\n=== SIMULAÇÃO UPSERT (sem cabeçalho) ===")
    print("1. Sempre escreve o cabeçalho primeiro")
    result_lines = [header]
    
    print("2. Percorre linhas existentes:")
    replaced = False
    for i, line in enumerate(existing_lines):
        if i == 0 and line == header:
            print(f"   Linha {i}: PULANDO (é cabeçalho)")
            continue
        
        print(f"   Linha {i}: {line}")
        
        if not replaced and pattern in line:
            print(f"   -> SUBSTITUINDO por nova linha")
            result_lines.append(newline)
            replaced = True
        else:
            result_lines.append(line)
    
    if not replaced:
        print("   -> Adicionando nova linha no final")
        result_lines.append(newline)
    
    print(f"\nResultado final ({len(result_lines)} linhas):")
    for i, line in enumerate(result_lines):
        print(f"   {i}: {line}")
    
    return result_lines

if __name__ == "__main__":
    print("TESTE 1: Arquivo COM cabeçalho")
    result1 = simulate_unity_upsert()
    
    print("\nTESTE 2: Arquivo SEM cabeçalho")
    result2 = test_without_header()
    
    print("\n" + "="*60)
    print("VERIFICAÇÃO: Ambos os casos devem ter cabeçalho no final")
    print("="*60)
    print(f"Teste 1 - Primeira linha: {result1[0]}")
    print(f"Teste 2 - Primeira linha: {result2[0]}")
    print(f"Ambos têm cabeçalho: {result1[0] == result2[0] == 'timestamp,run_id,platform,unity_version,scene,model,variant,file_mb,load_ms,mem_mb,fps_avg,fps_1pc_low,fps_window_s,ok'}")
