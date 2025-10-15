#!/usr/bin/env python3

def safe(s):
    if not s:
        return ""
    return '"' + s.replace('"', "''") + '"'

# Simulando os valores que seriam usados
scene = "ModelViewer"
model_name = "suzanne"
variant = "original"

# Padrão atual
pattern = "," + safe(scene) + "," + safe(model_name) + "," + safe(variant) + ","
print(f"Padrão: {pattern}")

# Linha de exemplo do CSV
csv_line = '2025-10-14T19:54:09-03:00,"20251014_195204","LinuxEditor","6000.2.4f1","ModelViewer","suzanne","original",0.019,255.912,379.754,62.29,40.94,5,true'
print(f"Linha CSV: {csv_line}")

# Verificar se o padrão encontra a linha
found = pattern in csv_line
print(f"Padrão encontrado: {found}")

# Vamos ver as posições dos campos
fields = csv_line.split(',')
print(f"Total de campos: {len(fields)}")
for i, field in enumerate(fields):
    print(f"Campo {i}: {field}")

# Vamos testar o padrão correto
# O CSV tem: timestamp,run_id,platform,unity_version,scene,model,variant,file_mb,...
# Então scene está na posição 4, model na 5, variant na 6
# Mas o padrão está procurando por: ,scene,model,variant,
# Isso significa que ele está procurando por: ,"ModelViewer","suzanne","original",
# Mas na linha CSV, isso aparece como: ,"ModelViewer","suzanne","original",

print("\nTestando padrão correto:")
correct_pattern = ',"ModelViewer","suzanne","original",'
print(f"Padrão correto: {correct_pattern}")
found_correct = correct_pattern in csv_line
print(f"Padrão correto encontrado: {found_correct}")
