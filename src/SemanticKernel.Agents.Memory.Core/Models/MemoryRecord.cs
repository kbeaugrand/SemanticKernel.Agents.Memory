using System;
using System.Collections.Generic;

namespace SemanticKernel.Agents.Memory.Core.Models;

/// <summary>
/// Represents a memory record that can be stored in a vector database.
/// </summary>
public sealed class MemoryRecord
{
    /// <summary>
    /// The unique identifier for this memory record.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// The document ID that this record belongs to.
    /// </summary>
    public string DocumentId { get; set; } = string.Empty;

    /// <summary>
    /// The execution ID of the pipeline that created this record.
    /// </summary>
    public string ExecutionId { get; set; } = string.Empty;

    /// <summary>
    /// The index name where this record belongs.
    /// </summary>
    public string Index { get; set; } = string.Empty;

    /// <summary>
    /// The file name or chunk name associated with this record.
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// The text content of this memory record.
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// The artifact type of the source file.
    /// </summary>
    public string ArtifactType { get; set; } = string.Empty;

    /// <summary>
    /// The partition number if this record comes from a chunked document.
    /// </summary>
    public int PartitionNumber { get; set; }

    /// <summary>
    /// The section number if this record comes from a sectioned document.
    /// </summary>
    public int SectionNumber { get; set; }

    /// <summary>
    /// Tags associated with this memory record.
    /// </summary>
    public Dictionary<string, string> Tags { get; set; } = new();

    /// <summary>
    /// When this record was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// The embedding vector for this memory record.
    /// </summary>
    public ReadOnlyMemory<float> Embedding { get; set; }
}
