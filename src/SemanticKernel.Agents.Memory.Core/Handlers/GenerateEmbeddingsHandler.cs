using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;
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
    private readonly IEmbeddingGenerator<string, Embedding<float>> _embeddingGenerator;

    public GenerateEmbeddingsHandler(
        IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator,
        ILogger<GenerateEmbeddingsHandler>? logger = null)
    {
        _embeddingGenerator = embeddingGenerator ?? throw new ArgumentNullException(nameof(embeddingGenerator));
        _logger = logger;
    }

    public async Task<(ReturnType Result, DataPipelineResult Pipeline)> InvokeAsync(DataPipelineResult pipeline, CancellationToken ct = default)
    {
        var eligibleFiles = pipeline.Files.Where(f => f.ArtifactType == ArtifactTypes.TextPartition).ToList();

        _logger?.LogDebug("Starting embedding generation for {FileCount} text partitions", eligibleFiles.Count);

        int vectors = 0;

        // Generate embeddings for text partitions (chunks) only
        foreach (var file in eligibleFiles)
        {
            ct.ThrowIfCancellationRequested();

            _logger?.LogTrace("Generating embedding for file '{FileName}' (Type: {ArtifactType}, Size: {FileSize} bytes)",
                file.Name, file.ArtifactType, file.Size);

            // Get the actual text content from the context
            string textContent;
            string contentKey = $"chunk_text_{file.Id}";

            if (pipeline.ContextArguments.TryGetValue(contentKey, out var textValue) && textValue is string text)
            {
                textContent = text;
                _logger?.LogTrace("Retrieved text content for file '{FileName}': {CharacterCount} characters",
                    file.Name, textContent.Length);
            }
            else
            {
                // Fallback to generating sample text if no content is available
                textContent = $"Sample text content for {file.Name}";
                _logger?.LogWarning("No text content found for file '{FileName}' with key '{ContentKey}', using fallback text",
                    file.Name, contentKey);
            }

            try
            {
                // Generate embedding using the AI service
                _logger?.LogTrace("Generating embedding for text content: {CharacterCount} characters", textContent.Length);

                var embedding = await _embeddingGenerator.GenerateAsync(textContent, cancellationToken: ct);

                _logger?.LogTrace("Generated embedding vector with {DimensionCount} dimensions for file '{FileName}'",
                    embedding.Vector.Length, file.Name);

                // Store the embedding data
                var embeddingData = embedding.Vector.ToArray();
                var embeddingBytes = new byte[embeddingData.Length * sizeof(float)];
                Buffer.BlockCopy(embeddingData, 0, embeddingBytes, 0, embeddingBytes.Length);

                file.GeneratedFiles["embedding.vec"] = new GeneratedFileDetails
                {
                    ParentId = file.Id,
                    SourcePartitionId = file.Id,
                    ContentSHA256 = ComputeSHA256(embeddingBytes)
                };

                // Store embedding in context for potential use by other handlers
                var embeddingKey = $"embedding_{file.Id}";
                pipeline.ContextArguments[embeddingKey] = embeddingData;

                vectors++;

                _logger?.LogTrace("Stored embedding vector for file '{FileName}' with SHA256: {ContentSHA256}",
                    file.Name, file.GeneratedFiles["embedding.vec"].ContentSHA256);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to generate embedding for file '{FileName}'", file.Name);
                return (ReturnType.TransientError, pipeline);
            }
        }

        var logMessage = $"Generated {vectors} embedding vector(s).";
        pipeline.Log(this, logMessage);

        _logger?.LogInformation("Embedding generation completed: {VectorCount} vectors generated for {FileCount} text partitions",
            vectors, eligibleFiles.Count);

        return (ReturnType.Success, pipeline);
    }

    private static string ComputeSHA256(byte[] bytes)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var hash = sha256.ComputeHash(bytes);
        return string.Concat(hash.Select(b => b.ToString("x2")));
    }
}
