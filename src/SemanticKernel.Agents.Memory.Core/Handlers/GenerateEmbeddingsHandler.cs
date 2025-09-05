using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace SemanticKernel.Agents.Memory.Core.Handlers;

/// <summary>
/// Embeddings generation pipeline step handler.
/// </summary>
public sealed class GenerateEmbeddingsHandler : IPipelineStepHandler
{
    public const string Name = "generate-embeddings";
    public string StepName => Name;

    private readonly ILogger<GenerateEmbeddingsHandler>? _logger;

    public GenerateEmbeddingsHandler(ILogger<GenerateEmbeddingsHandler>? logger = null)
    {
        _logger = logger;
    }

    public async Task<(ReturnType Result, DataPipelineResult Pipeline)> InvokeAsync(DataPipelineResult pipeline, CancellationToken ct = default)
    {
        var eligibleFiles = pipeline.Files.Where(f => f.ArtifactType == ArtifactTypes.ExtractedText || f.ArtifactType == ArtifactTypes.TextPartition).ToList();
        
        _logger?.LogDebug("Starting embedding generation for {FileCount} files", eligibleFiles.Count);
        
        // Simulate embedding generation (no external dependencies in this minimal sample)
        int vectors = 0;
        
        // Generate embeddings for both extracted text and text partitions (chunks)
        foreach (var file in eligibleFiles)
        {
            ct.ThrowIfCancellationRequested();
            
            _logger?.LogTrace("Generating embedding for file '{FileName}' (Type: {ArtifactType}, Size: {FileSize} bytes)", 
                file.Name, file.ArtifactType, file.Size);
            
            // Pretend we created one vector per file
            file.GeneratedFiles["embedding.vec"] = new GeneratedFileDetails
            {
                ParentId = file.Id,
                SourcePartitionId = file.Id,
                ContentSHA256 = Guid.NewGuid().ToString("n")
            };
            vectors++;
            
            _logger?.LogTrace("Generated embedding vector for file '{FileName}' with SHA256: {ContentSHA256}", 
                file.Name, file.GeneratedFiles["embedding.vec"].ContentSHA256);
        }

        var logMessage = $"Generated {vectors} embedding vector(s).";
        pipeline.Log(this, logMessage);
        
        _logger?.LogInformation("Embedding generation completed: {VectorCount} vectors generated for {FileCount} files", 
            vectors, eligibleFiles.Count);
        
        await Task.Yield();
        return (ReturnType.Success, pipeline);
    }
}
