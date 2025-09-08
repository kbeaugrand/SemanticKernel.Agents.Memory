using System;
using System.Collections.Generic;
using Microsoft.Extensions.VectorData;
using SemanticKernel.Agents.Memory.Core.Models;

namespace SemanticKernel.Agents.Memory.Core.Services;

/// <summary>
/// Provides vector store collection definitions for memory records.
/// </summary>
public static class MemoryRecordStoreDefinitionProvider
{
    /// <summary>
    /// Default embedding dimensions (OpenAI text-embedding-ada-002 and text-embedding-3-small).
    /// </summary>
    public const int DefaultEmbeddingDimensions = 1536;

    /// <summary>
    /// Creates a vector store collection definition for MemoryRecord with the specified embedding dimensions.
    /// </summary>
    /// <param name="dimensions">The number of dimensions for the embedding vector. Defaults to 1536.</param>
    /// <returns>A VectorStoreCollectionDefinition configured for MemoryRecord storage.</returns>
    public static VectorStoreCollectionDefinition GetMemoryRecordStoreDefinition(int dimensions = DefaultEmbeddingDimensions)
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
                new VectorStoreDataProperty(nameof(MemoryRecord.CreatedAt), typeof(DateTimeOffset)),
                new VectorStoreVectorProperty(nameof(MemoryRecord.Embedding), typeof(ReadOnlyMemory<float>), dimensions: dimensions)
            }
        };
    }
}
