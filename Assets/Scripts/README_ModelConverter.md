# Conversor Automático de Modelos 3D

Este sistema permite converter automaticamente arquivos `.obj`, `.fbx` e outros formatos para `.glb` durante o processo de importação.

## 🚀 Funcionalidades

- ✅ **Conversão automática** de OBJ/FBX para GLB
- ✅ **Múltiplos métodos** de conversão (fallback)
- ✅ **Interface integrada** no WizardController
- ✅ **Suporte a ferramentas externas**
- ✅ **Logs detalhados** para debugging

## 📋 Formatos Suportados

### **Entrada (Conversão Automática)**
- `.obj` - Wavefront OBJ
- `.fbx` - Autodesk FBX
- `.dae` - Collada
- `.3ds` - 3D Studio

### **Saída (Sempre GLB)**
- `.glb` - glTF Binary (recomendado)

## 🔧 Configuração

### **Método 1: Ferramenta Externa (Recomendado)**

1. **Baixe o gltf-pipeline:**
   ```bash
   npm install -g gltf-pipeline
   ```

2. **Crie o executável:**
   ```bash
   # No Windows, crie um .bat que chama o Node.js
   echo @echo off > gltf-pipeline.exe
   echo node "C:\Users\%USERNAME%\AppData\Roaming\npm\node_modules\gltf-pipeline\bin\gltf-pipeline.js" %* >> gltf-pipeline.exe
   ```

3. **Coloque em StreamingAssets/Tools/:**
   ```
   Assets/StreamingAssets/Tools/gltf-pipeline.exe
   ```

### **Método 2: Assimp.NET (Avançado)**

1. **Instale o package Assimp.NET:**
   - Abra Package Manager
   - Adicione: `https://github.com/assimp/assimp-net.git`

2. **Implemente o método `TryConvertWithAssimp`:**
   ```csharp
   using Assimp;
   
   static async Task<bool> TryConvertWithAssimp(string sourcePath, string destPath)
   {
       using (var importer = new AssimpContext())
       {
           var scene = importer.ImportFile(sourcePath, 
               PostProcessSteps.Triangulate | 
               PostProcessSteps.FlipUVs);
           
           using (var exporter = new AssimpContext())
           {
               exporter.ExportFile(scene, destPath, "gltf2");
           }
       }
       return true;
   }
   ```

### **Método 3: FBX Exporter + glTFast (Unity Packages)**

1. **Instale os packages:**
   - FBX Exporter
   - glTFast

2. **Implemente o método `TryConvertWithFbxExporter`:**
   ```csharp
   static async Task<bool> TryConvertWithFbxExporter(string sourcePath, string destPath)
   {
       // Implementação usando FBX Exporter + glTFast
       // TODO: Implementar quando os packages estiverem disponíveis
   }
   ```

## 🎮 Como Usar

### **No Inspector do WizardController:**
1. **Use Editor File Picker**: ✅ Marcado
2. **Selecione arquivo**: Agora aceita `.obj`, `.fbx`, `.glb`, `.gltf`

### **Fluxo de Conversão:**
1. **Usuário seleciona** arquivo `.obj` ou `.fbx`
2. **Sistema detecta** formato automaticamente
3. **Mostra "Convertendo..."** na interface
4. **Tenta conversão** usando métodos disponíveis
5. **Salva como GLB** na pasta correta
6. **Continua** com compressão normal

## 🐛 Troubleshooting

### **Erro: "gltf-pipeline.exe não encontrado"**
- Verifique se o arquivo está em `StreamingAssets/Tools/`
- Teste manualmente: `gltf-pipeline.exe -i input.obj -o output.glb`

### **Erro: "Falha na conversão"**
- Verifique se o arquivo de origem é válido
- Teste com arquivo menor
- Verifique logs no Console do Unity

### **Conversão muito lenta**
- Use arquivos menores
- Considere otimizar o modelo antes da conversão
- Use ferramenta externa (mais rápida)

## 📊 Logs

O sistema gera logs detalhados:
```
[ModelConverter] Convertendo OBJ para GLB...
[ModelConverter] Conversão bem-sucedida com ferramenta externa
[WizardController] Modelo "MyModel" foi importado em: ...
```

## 🔄 Fallback

O sistema tenta múltiplos métodos em ordem:
1. **Assimp.NET** (se disponível)
2. **FBX Exporter** (se disponível)  
3. **Ferramenta Externa** (gltf-pipeline)
4. **Falha** se nenhum funcionar

## 📝 Exemplo de Uso

```csharp
// Conversão manual
bool success = await ModelConverter.ConvertToGlbAsync("model.obj", "model.glb");

// Verificar formato suportado
bool supported = ModelConverter.IsSupportedFormat("model.obj");

// Obter informações do arquivo
var info = ModelConverter.GetModelInfo("model.obj");
Debug.Log($"Tamanho: {info.SizeMB:F2} MB");
```

## 🚀 Próximos Passos

1. **Implementar Assimp.NET** para conversão nativa
2. **Adicionar suporte a mais formatos** (DAE, 3DS, etc.)
3. **Otimizar conversão** para arquivos grandes
4. **Adicionar preview** durante conversão
5. **Suporte a batch conversion** (múltiplos arquivos)
