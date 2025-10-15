#!/usr/bin/env python3
import os
import shutil

def safe(s):
    if not s:
        return ""
    return '"' + s.replace('"', "''") + '"'

def simulate_unity_write_csv():
    """Simula exatamente o que o Unity faz quando WriteCsv() é chamado"""
    
    # Backup do arquivo original
    original_file = "/home/wands/Documentos/tcc/PolyDiet/Benchmarks/benchmarks.csv"
    backup_file = "/home/wands/Documentos/tcc/PolyDiet/Benchmarks/benchmarks_backup_final.csv"
    
    if os.path.exists(original_file):
        shutil.copy2(original_file, backup_file)
        print(f"Backup criado: {backup_file}")
    
    # Simulando os valores que o Unity usaria
    scene = "ModelViewer"
    model_name = "suzanne"
    variant = "original"
    
    # Simulando nova entrada (mesmo modelo/variante - deve substituir)
    timestamp = "2025-10-14T20:15:00-03:00"
    run_id = "20251014_201500"
    platform = "LinuxEditor"
    unity_version = "6000.2.4f1"
    file_mb = 0.025
    load_ms = 280.000
    mem_mb = 385.000
    fps_avg = 63.00
    fps_1pc_low = 42.00
    fps_window_s = 5.0
    ok = "true"
    
    # Construindo a nova linha exatamente como o Unity faria
    newline = f"{timestamp},{safe(run_id)},{safe(platform)},{safe(unity_version)},{safe(scene)},{safe(model_name)},{safe(variant)},{file_mb},{load_ms},{mem_mb},{fps_avg},{fps_1pc_low},{fps_window_s},{ok}"
    
    print(f"Nova linha: {newline}")
    
    # Lendo arquivo existente
    if os.path.exists(original_file):
        with open(original_file, 'r', encoding='utf-8') as f:
            lines = f.readlines()
        lines = [line.rstrip('\n\r') for line in lines]
    else:
        lines = []
    
    print(f"Arquivo existente tem {len(lines)} linhas")
    if lines:
        print(f"Primeira linha: {lines[0]}")
    
    # Simulando a lógica do Unity
    header = "timestamp,run_id,platform,unity_version,scene,model,variant,file_mb,load_ms,mem_mb,fps_avg,fps_1pc_low,fps_window_s,ok"
    pattern = "," + safe(scene) + "," + safe(model_name) + "," + safe(variant) + ","
    
    print(f"Padrão de busca: {pattern}")
    
    # Simulando o UPSERT
    result_lines = [header]  # Sempre escreve o cabeçalho primeiro
    
    replaced = False
    for i, line in enumerate(lines):
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
    
    # Escrevendo o arquivo de volta
    with open(original_file, 'w', encoding='utf-8') as f:
        for line in result_lines:
            f.write(line + '\n')
    
    print(f"\nResultado final ({len(result_lines)} linhas):")
    for i, line in enumerate(result_lines):
        print(f"   {i}: {line}")
    
    # Verificando se o cabeçalho ainda está lá
    print(f"\nVerificação:")
    print(f"Primeira linha é cabeçalho: {result_lines[0] == header}")
    print(f"Cabeçalho: {result_lines[0]}")
    
    return result_lines

if __name__ == "__main__":
    print("SIMULAÇÃO FINAL: Salvando modelo novo no Unity")
    print("="*60)
    
    result = simulate_unity_write_csv()
    
    print("\n" + "="*60)
    print("VERIFICAÇÃO FINAL:")
    print("="*60)
    
    # Verificando o arquivo final
    with open("/home/wands/Documentos/tcc/PolyDiet/Benchmarks/benchmarks.csv", 'r') as f:
        final_lines = f.readlines()
    
    print(f"Arquivo final tem {len(final_lines)} linhas")
    if final_lines:
        print(f"Primeira linha: {final_lines[0].strip()}")
        print(f"É cabeçalho: {final_lines[0].strip() == 'timestamp,run_id,platform,unity_version,scene,model,variant,file_mb,load_ms,mem_mb,fps_avg,fps_1pc_low,fps_window_s,ok'}")
    
    print("\nTeste concluído!")
