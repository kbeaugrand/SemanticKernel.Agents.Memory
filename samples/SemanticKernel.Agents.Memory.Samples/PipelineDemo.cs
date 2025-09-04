using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
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
    /// <param name="serviceProvider">Service provider for dependency injection.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A tuple containing the document ID and pipeline logs.</returns>
    public static async Task<(string DocumentId, IReadOnlyList<PipelineLogEntry> Logs)> RunAsync(
        IServiceProvider serviceProvider, 
        CancellationToken ct = default)
    {
        var orchestrator = new ImportOrchestrator();
        
        // Get handlers from DI container
        var textExtractionHandler = serviceProvider.GetRequiredService<TextExtractionHandler>();
        orchestrator.AddHandler(textExtractionHandler);
        
        // Add other handlers (these don't require DI yet)
        var chunkingOptions = new TextChunkingOptions
        {
            MaxChunkSize = 500,
            TextOverlap = 50
        };
        orchestrator.AddHandler(new TextChunkingHandler(chunkingOptions));
        
        orchestrator.AddHandler(new GenerateEmbeddingsHandler());
        orchestrator.AddHandler(new SaveRecordsHandler());

        // Create sample files to test different formats
        var request = new DocumentUploadRequest
        {
            Files =
            {
                new UploadedFile{ 
                    FileName = "hello.txt", 
                    Bytes = Encoding.UTF8.GetBytes("# Hello World\n\nThis is a simple text file for testing."),
                    MimeType = "text/plain"
                },
                new UploadedFile{ 
                    FileName = "document.md", 
                    Bytes = Encoding.UTF8.GetBytes("# Lorem Ipsum\n\n**Lorem ipsum** dolor sit amet, *consectetur* adipiscing elit.\n\n## Section 2\n\nSed do eiusmod tempor incididunt ut labore."),
                    MimeType = "text/markdown"
                }
            }
        };

        var pipeline = orchestrator.PrepareNewDocumentUpload(index: "docs", request, context: new NoopContext());
        await orchestrator.RunPipelineAsync(pipeline, ct);
        return (pipeline.DocumentId, pipeline.Logs);
    }

    /// <summary>
    /// Runs the pipeline demo with a basic fallback (no DI).
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A tuple containing the document ID and pipeline logs.</returns>
    public static Task<(string DocumentId, IReadOnlyList<PipelineLogEntry> Logs)> RunAsync(CancellationToken ct = default)
    {
        // This method is kept for backward compatibility but will use basic text extraction
        throw new InvalidOperationException(
            "Please use RunAsync(IServiceProvider serviceProvider) to properly configure MarkitDown text extraction. " +
            "Basic text extraction without DI is no longer supported.");
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
