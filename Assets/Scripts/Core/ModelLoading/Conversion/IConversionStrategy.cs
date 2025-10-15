using System;
using System.Threading;
using System.Threading.Tasks;

namespace PolyDiet.Core.ModelLoading.Conversion
{
    /// <summary>
    /// Interface para estratégias de conversão de modelos 3D
    /// </summary>
    public interface IConversionStrategy
    {
        /// <summary>
        /// Nome da estratégia (ex: "obj2gltf", "gltf-transform")
        /// </summary>
        string Name { get; }
        
        /// <summary>
        /// Verifica se esta estratégia pode converter o formato de origem
        /// </summary>
        /// <param name="sourceExtension">Extensão do arquivo de origem (ex: ".obj", ".fbx")</param>
        bool CanHandle(string sourceExtension);
        
        /// <summary>
        /// Verifica se esta estratégia está disponível (ferramenta instalada, etc)
        /// </summary>
        Task<bool> IsAvailableAsync();
        
        /// <summary>
        /// Converte um arquivo de modelo para GLB
        /// </summary>
        /// <param name="sourcePath">Caminho do arquivo de origem</param>
        /// <param name="destinationPath">Caminho do arquivo de destino (GLB)</param>
        /// <param name="progress">Relatório de progresso (0.0 a 1.0)</param>
        /// <param name="cancellationToken">Token para cancelar operação</param>
        /// <returns>Resultado da conversão</returns>
        Task<ConversionResult> ConvertAsync(
            string sourcePath,
            string destinationPath,
            IProgress<float> progress = null,
            CancellationToken cancellationToken = default
        );
    }
}

