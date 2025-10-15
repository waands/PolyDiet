#!/usr/bin/env python3

# Simulando a lógica do Unity
header = "timestamp,run_id,platform,unity_version,scene,model,variant,file_mb,load_ms,mem_mb,fps_avg,fps_1pc_low,fps_window_s,ok"

# Simulando o conteúdo do arquivo CSV
lines = [
    "timestamp,run_id,platform,unity_version,scene,model,variant,file_mb,load_ms,mem_mb,fps_avg,fps_1pc_low,fps_window_s,ok",
    '2025-10-14T19:54:09-03:00,"20251014_195204","LinuxEditor","6000.2.4f1","ModelViewer","suzanne","original",0.019,255.912,379.754,62.29,40.94,5,true',
    '2025-10-14T19:54:15-03:00,"20251014_195204","LinuxEditor","6000.2.4f1","ModelViewer","suzanne","meshopt",0.006,72.544,377.3,62.18,36.67,5,true'
]

print("Testando detecção de cabeçalho:")
print(f"Header esperado: {header}")
print(f"Primeira linha: {lines[0]}")
print(f"Header == primeira linha: {header == lines[0]}")

print("\nSimulando a lógica do UPSERT:")
print("1. Sempre escreve o cabeçalho primeiro")
print("2. Percorre as linhas existentes")
for i, line in enumerate(lines):
    if i == 0 and line == header:
        print(f"   Linha {i}: PULANDO (é cabeçalho)")
    else:
        print(f"   Linha {i}: {line}")

print("\nResultado esperado:")
print("1. Cabeçalho")
print("2. Todas as linhas de dados (incluindo a primeira se não for cabeçalho)")
print("3. Nova linha (se não foi substituída)")

# Vamos testar o que acontece se a primeira linha NÃO for cabeçalho
print("\n" + "="*50)
print("TESTE: Primeira linha NÃO é cabeçalho")
lines_no_header = [
    '2025-10-14T19:54:09-03:00,"20251014_195204","LinuxEditor","6000.2.4f1","ModelViewer","suzanne","original",0.019,255.912,379.754,62.29,40.94,5,true',
    '2025-10-14T19:54:15-03:00,"20251014_195204","LinuxEditor","6000.2.4f1","ModelViewer","suzanne","meshopt",0.006,72.544,377.3,62.18,36.67,5,true'
]

print(f"Header esperado: {header}")
print(f"Primeira linha: {lines_no_header[0]}")
print(f"Header == primeira linha: {header == lines_no_header[0]}")

print("\nSimulando a lógica do UPSERT (sem cabeçalho):")
print("1. Sempre escreve o cabeçalho primeiro")
print("2. Percorre as linhas existentes")
for i, line in enumerate(lines_no_header):
    if i == 0 and line == header:
        print(f"   Linha {i}: PULANDO (é cabeçalho)")
    else:
        print(f"   Linha {i}: {line}")

print("\nResultado esperado:")
print("1. Cabeçalho")
print("2. Todas as linhas de dados (incluindo a primeira)")
print("3. Nova linha (se não foi substituída)")
