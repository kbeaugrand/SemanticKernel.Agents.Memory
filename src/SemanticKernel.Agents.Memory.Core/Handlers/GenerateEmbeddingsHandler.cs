using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SemanticKernel.Agents.Memory.Core.Handlers;

/// <summary>
/// Embeddings generation pipeline step handler.
/// </summary>
public sealed class GenerateEmbeddingsHandler : IPipelineStepHandler
{
    public const string Name = "generate-embeddings";
    public string StepName => Name;

    public async Task<(ReturnType Result, DataPipelineResult Pipeline)> InvokeAsync(DataPipelineResult pipeline, CancellationToken ct = default)
    {
        // Simulate embedding generation (no external dependencies in this minimal sample)
        int vectors = 0;
        
        // Generate embeddings for both extracted text and text partitions (chunks)
        foreach (var file in pipeline.Files.Where(f => f.ArtifactType == ArtifactTypes.ExtractedText || f.ArtifactType == ArtifactTypes.TextPartition))
        {
            ct.ThrowIfCancellationRequested();
            // Pretend we created one vector per file
            file.GeneratedFiles["embedding.vec"] = new GeneratedFileDetails
            {
                ParentId = file.Id,
                SourcePartitionId = file.Id,
                ContentSHA256 = Guid.NewGuid().ToString("n")
            };
            vectors++;
        }

        pipeline.Log(this, $"Generated {vectors} embedding vector(s).");
        await Task.Yield();
        return (ReturnType.Success, pipeline);
    }
}
