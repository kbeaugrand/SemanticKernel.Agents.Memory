using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SemanticKernel.Agents.Memory.Core;
using SemanticKernel.Agents.Memory.Core.Extensions;
using SemanticKernel.Agents.Memory.Core.Handlers;

namespace SemanticKernel.Agents.Memory.Samples;

/// <summary>
/// Demo implementation using the pipeline configuration.
/// </summary>
public static class PipelineDemo
{
    /// <summary>
    /// Simple context implementation for demo purposes.
    /// </summary>
    private sealed class NoopContext : IContext { }

    /// <summary>
    /// Runs a complete pipeline demo with configuration.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A tuple containing the document ID and pipeline logs.</returns>
    public static async Task<(string DocumentId, IReadOnlyList<PipelineLogEntry> Logs)> RunAsync(CancellationToken ct = default)
    {
        // Configure services
        var services = new ServiceCollection();
        
        // Add logging
        services.AddLogging(builder => 
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Debug);
        });

        // Configure memory ingestion pipeline using the fluent API
        services.ConfigureMemoryIngestion(options =>
        {
            options
                .WithMarkitDownTextExtraction("http://localhost:5000")
                .WithSimpleTextChunking(() => new TextChunkingOptions
                {
                    MaxChunkSize = 500,
                    TextOverlap = 50
                })
                .WithEmbeddingsGeneration<GenerateEmbeddingsHandler>()
                .WithSaveRecords<SaveRecordsHandler>();
        });

        var serviceProvider = services.BuildServiceProvider();

        try
        {
            // Get the orchestrator from the service provider
            var orchestrator = serviceProvider.GetRequiredService<ImportOrchestrator>();
            
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
        finally
        {
            serviceProvider.Dispose();
        }
    }

    /// <summary>
    /// Demonstrates alternative configuration with semantic chunking.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A tuple containing the document ID and pipeline logs.</returns>
    public static async Task<(string DocumentId, IReadOnlyList<PipelineLogEntry> Logs)> RunSemanticChunkingAsync(CancellationToken ct = default)
    {
        // Configure services with semantic chunking
        var services = new ServiceCollection();
        
        // Add logging
        services.AddLogging(builder => 
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Debug);
        });

        // Configure memory ingestion pipeline with semantic chunking
        services.ConfigureMemoryIngestion(options =>
        {
            options
                .WithMarkitDownTextExtraction("http://localhost:5000")
                .WithSemanticChunking()  // Use semantic chunking instead of simple chunking
                .WithEmbeddingsGeneration<GenerateEmbeddingsHandler>()
                .WithSaveRecords<SaveRecordsHandler>();
        });

        var serviceProvider = services.BuildServiceProvider();

        try
        {
            // Get the orchestrator from the service provider
            var orchestrator = serviceProvider.GetRequiredService<ImportOrchestrator>();
            
            // Create a more structured document for semantic chunking
            var request = new DocumentUploadRequest
            {
                Files =
                {
                    new UploadedFile{ 
                        FileName = "structured-document.md", 
                        Bytes = Encoding.UTF8.GetBytes(@"# Main Document Title

This is the introduction paragraph that provides context for the entire document.

## First Major Section

This section covers the first major topic. It contains several paragraphs of detailed information about this topic.

### Subsection A

More detailed information about a specific aspect of the first major section.

### Subsection B

Additional details about another aspect of the first major section.

## Second Major Section

This section introduces the second major topic. It has different content structure.

### Technical Details

Technical specifications and implementation details.

## Conclusion

Final thoughts and summary of the document content."),
                        MimeType = "text/markdown"
                    }
                }
            };

            var pipeline = orchestrator.PrepareNewDocumentUpload(index: "docs", request, context: new NoopContext());
            await orchestrator.RunPipelineAsync(pipeline, ct);
            return (pipeline.DocumentId, pipeline.Logs);
        }
        finally
        {
            serviceProvider.Dispose();
        }
    }

    /// <summary>
    /// Demonstrates custom handler registration.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A tuple containing the document ID and pipeline logs.</returns>
    public static async Task<(string DocumentId, IReadOnlyList<PipelineLogEntry> Logs)> RunCustomHandlerAsync(CancellationToken ct = default)
    {
        // Configure services with custom handler and services
        var services = new ServiceCollection();
        
        // Add logging
        services.AddLogging(builder => 
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Debug);
        });

        // Configure memory ingestion pipeline with custom services
        services.ConfigureMemoryIngestion(options =>
        {
            options
                .WithServices(serviceCollection =>
                {
                    // Add custom services here
                    serviceCollection.AddHttpClient("CustomClient");
                })
                .WithMarkitDownTextExtraction(client =>
                {
                    client.Timeout = TimeSpan.FromMinutes(10); // Custom timeout
                })
                .WithSimpleTextChunking(() => new TextChunkingOptions
                {
                    MaxChunkSize = 1000,
                    TextOverlap = 100,
                    SplitCharacters = new[] { "\n\n", "\n", ". " }
                })
                .WithEmbeddingsGeneration<GenerateEmbeddingsHandler>()
                .WithSaveRecords<SaveRecordsHandler>();
        });

        var serviceProvider = services.BuildServiceProvider();

        try
        {
            // Get the orchestrator from the service provider
            var orchestrator = serviceProvider.GetRequiredService<ImportOrchestrator>();
            
            // Create sample files
            var request = new DocumentUploadRequest
            {
                Files =
                {
                    new UploadedFile{ 
                        FileName = "large-document.txt", 
                        Bytes = Encoding.UTF8.GetBytes(GenerateLargeText()),
                        MimeType = "text/plain"
                    }
                }
            };

            var pipeline = orchestrator.PrepareNewDocumentUpload(index: "docs", request, context: new NoopContext());
            await orchestrator.RunPipelineAsync(pipeline, ct);
            return (pipeline.DocumentId, pipeline.Logs);
        }
        finally
        {
            serviceProvider.Dispose();
        }
    }

    private static string GenerateLargeText()
    {
        var sb = new StringBuilder();
        for (int i = 1; i <= 10; i++)
        {
            sb.AppendLine($"# Section {i}");
            sb.AppendLine();
            for (int j = 1; j <= 5; j++)
            {
                sb.AppendLine($"This is paragraph {j} of section {i}. It contains some meaningful content that will be processed by the chunking algorithm. The content is designed to test various aspects of the text processing pipeline including chunking, embedding generation, and record saving.");
                sb.AppendLine();
            }
        }
        return sb.ToString();
    }

    /// <summary>
    /// Demo showing configurable semantic chunking options.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A tuple containing the document ID and pipeline logs.</returns>
    public static async Task<(string DocumentId, IReadOnlyList<PipelineLogEntry> Logs)> RunSemanticChunkingConfigDemo(CancellationToken ct = default)
    {
        // Configure services
        var services = new ServiceCollection();
        
        // Add logging
        services.AddLogging(builder => 
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Debug);
        });

        // Configure memory ingestion pipeline with custom semantic chunking options
        services.ConfigureMemoryIngestion(options =>
        {
            options
                .WithMarkitDownTextExtraction("http://localhost:5000")
                .WithSemanticChunking(new SemanticChunkingOptions
                {
                    MaxChunkSize = 3000,           // Larger chunks for more context
                    MinChunkSize = 200,            // Larger minimum to avoid tiny chunks
                    TitleLevelThreshold = 1,       // Split on H1 headers
                    IncludeTitleContext = true     // Include title context in chunks
                })
                .WithEmbeddingsGeneration<GenerateEmbeddingsHandler>()
                .WithSaveRecords<SaveRecordsHandler>();
        });

        var serviceProvider = services.BuildServiceProvider();

        try
        {
            // Get the orchestrator from the service provider
            var orchestrator = serviceProvider.GetRequiredService<ImportOrchestrator>();
            
            // Create a more complex document with multiple heading levels
            var request = new DocumentUploadRequest
            {
                Files =
                {
                    new UploadedFile{ 
                        FileName = "complex-document.md", 
                        Bytes = Encoding.UTF8.GetBytes(@"# Main Title

This is the introduction section of our document. It provides an overview of what we'll be covering.

## Section 1: Basic Concepts

This section covers the fundamental concepts that are important to understand.

### Subsection 1.1: Definitions

Here we define key terms and concepts.

### Subsection 1.2: Examples

This subsection provides practical examples to illustrate the concepts.

## Section 2: Advanced Topics

This section delves into more complex subjects.

### Subsection 2.1: Technical Details

Technical implementation details are covered here.

### Subsection 2.2: Best Practices

This covers recommended approaches and methodologies.

## Section 3: Conclusion

This section summarizes the key points and provides final thoughts.

The conclusion ties together all the concepts discussed in the previous sections."),
                        MimeType = "text/markdown"
                    }
                }
            };
            
            // Execute the pipeline
            var pipeline = orchestrator.PrepareNewDocumentUpload(index: "docs", request, context: new NoopContext());
            await orchestrator.RunPipelineAsync(pipeline, ct);
            return (pipeline.DocumentId, pipeline.Logs);
        }
        finally
        {
            // Clean up
            serviceProvider.Dispose();
        }
    }
}
