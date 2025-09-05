using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.VectorData;
using SemanticKernel.Agents.Memory.Core.Models;
using SemanticKernel.Agents.Memory.Core.Services;

namespace SemanticKernel.Agents.Memory.Core.Handlers;

/// <summary>
/// Records saving pipeline step handler using Microsoft.Extensions.VectorData concepts.
/// </summary>
public sealed class SaveRecordsHandler<TVectorStore> : IPipelineStepHandler
    where TVectorStore : VectorStore
{
    public const string Name = "save-records";
    public string StepName => Name;

    private readonly ILogger<SaveRecordsHandler<TVectorStore>>? _logger;

    private readonly TVectorStore _vectorStore;

    public SaveRecordsHandler(
        TVectorStore vectorStore,
        ILogger<SaveRecordsHandler<TVectorStore>>? logger = null)
    {
        _vectorStore = vectorStore ?? throw new ArgumentNullException(nameof(_vectorStore));
        _logger = logger;
    }

    public async Task<(ReturnType Result, DataPipelineResult Pipeline)> InvokeAsync(DataPipelineResult pipeline, CancellationToken ct = default)
    {
        var recordsToSave = pipeline.Files.Where(f => f.GeneratedFiles.ContainsKey("embedding.vec")).ToList();
        
        _logger?.LogDebug("Starting record saving for {RecordCount} files with embeddings", recordsToSave.Count);

        if (recordsToSave.Count == 0)
        {
            _logger?.LogInformation("No records with embeddings found to save");
            return (ReturnType.Success, pipeline);
        }

        try
        {
            // Get collection name from pipeline or use default
            var collectionName = !string.IsNullOrEmpty(pipeline.Index) ? pipeline.Index : "memory";

            if (!TryGetEmbeddingsDimension(pipeline, recordsToSave, out int dimensions))
            {
                _logger?.LogError("Failed to get embeddings dimension");
                return (ReturnType.TransientError, pipeline);
            }

            var collection = _vectorStore.GetCollection<string, MemoryRecord>(collectionName,
                                                                    GetMemoryRecordStoreDefinition(dimensions));

            await collection.EnsureCollectionExistsAsync(ct);

            var records = new List<MemoryRecord>();

            // Convert files to memory records
            foreach (var file in recordsToSave)
            {
                ct.ThrowIfCancellationRequested();

                _logger?.LogTrace("Processing record for file '{FileName}' (Type: {ArtifactType}, Partition: {PartitionNumber})",
                    file.Name, file.ArtifactType, file.PartitionNumber);

                // Get the text content from the context
                string textContent = string.Empty;
                string contentKey = $"chunk_text_{file.Id}";

                if (pipeline.ContextArguments.TryGetValue(contentKey, out var textValue) && textValue is string text)
                {
                    textContent = text;
                }
                else
                {
                    _logger?.LogWarning("No text content found for file '{FileName}' with key '{ContentKey}'",
                        file.Name, contentKey);
                }

                // Get the embedding from the context
                ReadOnlyMemory<float> embedding = ReadOnlyMemory<float>.Empty;
                string embeddingKey = $"embedding_{file.Id}";

                if (pipeline.ContextArguments.TryGetValue(embeddingKey, out var embeddingValue) && embeddingValue is float[] embeddingArray)
                {
                    embedding = embeddingArray.AsMemory();
                }
                else
                {
                    _logger?.LogWarning("No embedding found for file '{FileName}' with key '{EmbeddingKey}'",
                        file.Name, embeddingKey);
                    continue; // Skip records without embeddings
                }

                var record = new MemoryRecord
                {
                    Id = file.Id,
                    DocumentId = pipeline.DocumentId,
                    ExecutionId = pipeline.ExecutionId,
                    Index = pipeline.Index,
                    FileName = file.Name,
                    Text = textContent,
                    ArtifactType = file.ArtifactType.ToString(),
                    PartitionNumber = file.PartitionNumber,
                    SectionNumber = file.SectionNumber,
                    Tags = pipeline.Tags,
                    CreatedAt = pipeline.Creation,
                    Embedding = embedding
                };

                records.Add(record);

                _logger?.LogTrace("Created memory record for file '{FileName}' with {EmbeddingDimensions} embedding dimensions",
                    file.Name, embedding.Length);
            }

            if (records.Count > 0)
            {
                // Upsert records to the vector store
                await collection.UpsertAsync(records, ct);

                var logMessage = $"Saved {records.Count} record(s) to vector store collection '{collectionName}'.";
                pipeline.Log(this, logMessage);

                _logger?.LogInformation("Record saving completed: {SavedCount} records saved to vector store collection '{CollectionName}'",
                    records.Count, collectionName);
            }
            else
            {
                _logger?.LogWarning("No valid records to save after processing {FileCount} files", recordsToSave.Count);
            }

            return (ReturnType.Success, pipeline);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to save records to vector store");
            return (ReturnType.TransientError, pipeline);
        }
    }

    private bool TryGetEmbeddingsDimension(DataPipelineResult pipeline, List<FileDetails> recordsToSave, out int dimensions)
    {
        // Determine dimensions from the first embedding
        dimensions = 0;
        if (recordsToSave.Count > 0)
        {
            var firstFile = recordsToSave[0];
            string embeddingKey = $"embedding_{firstFile.Id}";

            if (pipeline.ContextArguments.TryGetValue(embeddingKey, out var embeddingValue) && embeddingValue is float[] embeddingArray)
            {
                dimensions = embeddingArray.Length;
            }
            else
            {
                return false;
            }
        }

        return true;
    }

    private static VectorStoreCollectionDefinition GetMemoryRecordStoreDefinition(int dimensions)
    {
        return new VectorStoreCollectionDefinition
        {
            Properties = new List<VectorStoreProperty>
            {
                new VectorStoreKeyProperty(nameof(MemoryRecord.Id), typeof(string)),
                new VectorStoreDataProperty(nameof(MemoryRecord.DocumentId), typeof(string)) { IsIndexed = true },
                new VectorStoreDataProperty(nameof(MemoryRecord.ExecutionId), typeof(string)) { IsIndexed = true },
                new VectorStoreDataProperty(nameof(MemoryRecord.Index), typeof(string)) { IsIndexed = true },
                new VectorStoreDataProperty(nameof(MemoryRecord.FileName), typeof(string)) { IsIndexed = true },
                new VectorStoreDataProperty(nameof(MemoryRecord.Text), typeof(string)) { IsFullTextIndexed = true },
                new VectorStoreDataProperty(nameof(MemoryRecord.ArtifactType), typeof(string)) { IsIndexed = true },
                new VectorStoreDataProperty(nameof(MemoryRecord.PartitionNumber), typeof(int)),
                new VectorStoreDataProperty(nameof(MemoryRecord.SectionNumber), typeof(int)),
                new VectorStoreDataProperty(nameof(MemoryRecord.Tags), typeof(Dictionary<string, string>)) { IsIndexed = true },
                new VectorStoreVectorProperty(nameof(MemoryRecord.Embedding), typeof(ReadOnlyMemory<float>), dimensions: dimensions)
            }
        };
    }

}
