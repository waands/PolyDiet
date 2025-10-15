using System;

namespace PolyDiet.Core.ModelLoading.Validation
{
    /// <summary>
    /// Informações sobre um arquivo GLTF/GLB
    /// </summary>
    [Serializable]
    public class GltfFileInfo
    {
        public string FilePath { get; set; }
        public long FileSizeBytes { get; set; }
        public string FileType { get; set; } // "GLB" ou "GLTF"
        
        // Informações do header GLB (se aplicável)
        public uint GlbVersion { get; set; }
        public uint GlbLength { get; set; }
        
        // Informações de conteúdo (se disponível)
        public int MeshCount { get; set; }
        public int NodeCount { get; set; }
        public int MaterialCount { get; set; }
        public int TextureCount { get; set; }
        public int AnimationCount { get; set; }
        
        // Estimativas (se disponível)
        public int EstimatedVertexCount { get; set; }
        public int EstimatedTriangleCount { get; set; }
        
        public override string ToString()
        {
            return $"{FileType} - {FileSizeBytes} bytes - {MeshCount} meshes";
        }
    }
}

