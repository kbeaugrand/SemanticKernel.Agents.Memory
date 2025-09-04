using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SemanticKernel.Agents.Memory.Core;
using SemanticKernel.Agents.Memory.Core.Handlers;

namespace SemanticKernel.Agents.Memory.Samples;

/// <summary>
/// Minimal demo implementation for testing the pipeline.
/// </summary>
public static class PipelineDemo
{
    /// <summary>
    /// Simple context implementation for demo purposes.
    /// </summary>
    private sealed class NoopContext : IContext { }

    /// <summary>
    /// Runs a complete pipeline demo with sample files.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A tuple containing the document ID and pipeline logs.</returns>
    public static async Task<(string DocumentId, IReadOnlyList<PipelineLogEntry> Logs)> RunAsync(CancellationToken ct = default)
    {
        var orchestrator = new ImportOrchestrator();
        orchestrator.AddHandler(new TextExtractionHandler());
        
        // Add text chunking with custom options
        var chunkingOptions = new TextChunkingOptions
        {
            MaxChunkSize = 500,
            TextOverlap = 50
        };
        orchestrator.AddHandler(new TextChunkingHandler(chunkingOptions));
        
        orchestrator.AddHandler(new GenerateEmbeddingsHandler());
        orchestrator.AddHandler(new SaveRecordsHandler());

        var request = new DocumentUploadRequest
        {
            Files =
            {
                new UploadedFile{ FileName = "hello.txt", Bytes = Encoding.UTF8.GetBytes("hello world") },
                new UploadedFile{ FileName = "lorem.txt", Bytes = Encoding.UTF8.GetBytes("lorem ipsum") }
            }
        };

        var pipeline = orchestrator.PrepareNewDocumentUpload(index: "docs", request, context: new NoopContext());
        await orchestrator.RunPipelineAsync(pipeline, ct);
        return (pipeline.DocumentId, pipeline.Logs);
    }

    /// <summary>
    /// Creates a simple import context for testing.
    /// </summary>
    /// <param name="index">The index name.</param>
    /// <param name="files">The files to import.</param>
    /// <returns>An ImportContext ready for use.</returns>
    public static ImportContext CreateTestContext(string index = "test", params Document[] files)
    {
        var uploadRequest = new DocumentUploadRequest();
        
        foreach (var file in files)
        {
            uploadRequest.Files.Add(file.ToUploadedFile());
        }

        return new ImportContext
        {
            Index = index,
            UploadRequest = uploadRequest
        };
    }

    /// <summary>
    /// Creates a sample document for testing.
    /// </summary>
    /// <param name="fileName">The file name.</param>
    /// <param name="content">The content as a string.</param>
    /// <param name="mimeType">The MIME type.</param>
    /// <returns>A Document instance.</returns>
    public static Document CreateSampleDocument(string fileName, string content, string mimeType = "text/plain")
    {
        var bytes = Encoding.UTF8.GetBytes(content);
        return new Document
        {
            FileName = fileName,
            Content = bytes,
            Size = bytes.Length,
            MimeType = mimeType,
            DocumentId = Guid.NewGuid().ToString("n"),
            ImportedAt = DateTime.UtcNow,
            Tags = new Dictionary<string, string>()
        };
    }
}
