# Conversão OBJ para GLB - Guia de Uso

## ✅ **Sistema Configurado com Sucesso!**

O sistema de conversão OBJ → GLB está funcionando corretamente no seu Linux. Aqui está como usar:

### 🛠️ **Ferramentas Instaladas**

- **obj2gltf**: v3.1.6 ✅
- **gltf-transform**: v4.2.1 ✅
- **gltfpack**: v0.25 ✅

### 📂 **Como Usar no Unity**

1. **Colocar arquivos OBJ**:
   - Coloque seus arquivos `.obj` em: `Assets/StreamingAssets/Models/[nome_modelo]/original/`
   - Certifique-se de que arquivos associados (.mtl, texturas) estejam na mesma pasta

2. **Converter via Código**:
   ```csharp
   // Exemplo de uso
   string objPath = "caminho/para/modelo.obj";
   string glbPath = "caminho/para/modelo.glb";
   
   bool success = await ModelConverter.ConvertToGlbAsync(objPath, glbPath);
   if (success)
   {
       Debug.Log("Conversão bem-sucedida!");
   }
   ```

3. **Usar no WizardController** (se disponível):
   - O sistema será chamado automaticamente quando você tentar converter modelos OBJ

### 🔧 **Como Funciona**

O `ModelConverter` agora usa uma abordagem em camadas:

1. **Método 1**: Assimp.NET (não implementado - placeholder)
2. **Método 2**: FBX Exporter (não implementado - placeholder)  
3. **Método 3**: **obj2gltf** ← **FUNCIONAL**
   - Converte OBJ diretamente para GLB usando `obj2gltf --binary`
   - Suporta texturas, materiais e geometria complexa
4. **Método 4**: Conversão simples (cópia para GLB/GLTF já existentes)

### 📋 **Formatos Suportados**

- **Entrada**: `.obj` (com materiais .mtl e texturas)
- **Saída**: `.glb` (formato binário otimizado)

### 🚨 **Resolução de Problemas**

#### "obj2gltf não encontrado"
```bash
npm install -g obj2gltf
```

#### "UV index out of bounds" 
- O arquivo OBJ tem problemas de UV mapping
- Verifique o arquivo .mtl e texturas associadas
- Use um editor 3D para reparar o modelo

#### "Conversão falhou"
- Verifique os logs detalhados no Console do Unity
- O arquivo OBJ deve estar bem formatado
- Texturas referenciadas no .mtl devem existir

### 🧪 **Teste Rápido**

Para testar se está funcionando:

```bash
cd /home/wands/Documentos/tcc/PolyDiet
./test_conversion.sh
```

### 📊 **Logs de Debug**

O sistema fornece logs detalhados:
- `✅ Conversão bem-sucedida` - Tudo funcionou
- `❌ obj2gltf falhou` - Problema na conversão
- `⚠️ obj2gltf não encontrado` - Ferramenta não instalada

### 🎯 **Exemplo Completo de Uso**

```csharp
public async Task ConvertMyModel()
{
    string sourceObj = "Assets/StreamingAssets/Models/chair/original/chair.obj";
    string destGlb = "Assets/StreamingAssets/Models/chair/original/chair.glb";
    
    Debug.Log("Iniciando conversão...");
    
    bool success = await ModelConverter.ConvertToGlbAsync(sourceObj, destGlb);
    
    if (success)
    {
        Debug.Log($"✅ Modelo convertido com sucesso: {destGlb}");
        
        // Agora você pode usar o GLB no seu sistema de compressão
        // ou carregamento de modelos
    }
    else
    {
        Debug.LogError("❌ Falha na conversão. Verifique os logs acima.");
    }
}
```

## 🎉 **Conclusão**

Você tem agora um sistema completo de conversão OBJ → GLB funcionando no Linux, integrado ao Unity e compatível com seu projeto PolyDiet!