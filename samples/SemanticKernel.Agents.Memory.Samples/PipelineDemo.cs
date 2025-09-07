using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.AI.OpenAI;
using Azure.Core;
using Azure.Identity;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel.Connectors.InMemory;
using OpenAI.Embeddings;
using SemanticKernel.Agents.Memory.Core;
using SemanticKernel.Agents.Memory.Core.Extensions;
using SemanticKernel.Agents.Memory.Core.Handlers;
using SemanticKernel.Agents.Memory.Samples.Configuration;

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
    /// <param name="configuration">The configuration instance.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A tuple containing the document ID and pipeline logs.</returns>
    public static async Task<(string DocumentId, IReadOnlyList<PipelineLogEntry> Logs)> RunAsync(IConfiguration configuration, CancellationToken ct = default)
    {
        // Configure services
        var services = new ServiceCollection();

        // Add configuration
        services.AddSingleton(configuration);
        services.Configure<AzureOpenAIOptions>(configuration.GetSection(AzureOpenAIOptions.SectionName));
        services.Configure<MarkitDownOptions>(configuration.GetSection(MarkitDownOptions.SectionName));
        services.Configure<TextChunkingConfig>(configuration.GetSection(TextChunkingConfig.SectionName));
        services.Configure<PipelineOptions>(configuration.GetSection(PipelineOptions.SectionName));

        // Add logging
        services.AddLogging(builder =>
        {
            builder.AddConfiguration(configuration.GetSection("Logging"));
            builder.AddConsole();
        });

        // Configure Azure OpenAI embedding generator
        ConfigureAzureOpenAIEmbeddings(services);

        // Get configuration options
        var chunkingConfig = configuration.GetSection(TextChunkingConfig.SectionName).Get<TextChunkingConfig>() ?? new TextChunkingConfig();
        var markitDownConfig = configuration.GetSection(MarkitDownOptions.SectionName).Get<MarkitDownOptions>() ?? new MarkitDownOptions();
        var pipelineConfig = configuration.GetSection(PipelineOptions.SectionName).Get<PipelineOptions>() ?? new PipelineOptions();

        // Configure memory ingestion pipeline using the fluent API
        services.ConfigureMemoryIngestion(options =>
        {
            options
                .WithMarkitDownTextExtraction(markitDownConfig.ServiceUrl)
                .WithSimpleTextChunking(() => new TextChunkingOptions
                {
                    MaxChunkSize = chunkingConfig.Simple.MaxChunkSize,
                    TextOverlap = chunkingConfig.Simple.TextOverlap
                })
                .WithDefaultEmbeddingsGeneration()
                .WithSaveRecords(new InMemoryVectorStore());
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

            var pipeline = orchestrator.PrepareNewDocumentUpload(index: pipelineConfig.DefaultIndex, request, context: new NoopContext());
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
    /// <param name="configuration">The configuration instance.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A tuple containing the document ID and pipeline logs.</returns>
    public static async Task<(string DocumentId, IReadOnlyList<PipelineLogEntry> Logs)> RunSemanticChunkingAsync(IConfiguration configuration, CancellationToken ct = default)
    {
        // Configure services with semantic chunking
        var services = new ServiceCollection();

        // Add configuration
        services.AddSingleton(configuration);
        services.Configure<AzureOpenAIOptions>(configuration.GetSection(AzureOpenAIOptions.SectionName));
        services.Configure<MarkitDownOptions>(configuration.GetSection(MarkitDownOptions.SectionName));
        services.Configure<TextChunkingConfig>(configuration.GetSection(TextChunkingConfig.SectionName));
        services.Configure<PipelineOptions>(configuration.GetSection(PipelineOptions.SectionName));

        // Add logging
        services.AddLogging(builder =>
        {
            builder.AddConfiguration(configuration.GetSection("Logging"));
            builder.AddConsole();
        });

        // Configure Azure OpenAI embedding generator
        ConfigureAzureOpenAIEmbeddings(services);

        // Get configuration options
        var chunkingConfig = configuration.GetSection(TextChunkingConfig.SectionName).Get<TextChunkingConfig>() ?? new TextChunkingConfig();
        var markitDownConfig = configuration.GetSection(MarkitDownOptions.SectionName).Get<MarkitDownOptions>() ?? new MarkitDownOptions();
        var pipelineConfig = configuration.GetSection(PipelineOptions.SectionName).Get<PipelineOptions>() ?? new PipelineOptions();

        // Configure memory ingestion pipeline with semantic chunking
        services.ConfigureMemoryIngestion(options =>
        {
            options
                .WithMarkitDownTextExtraction(markitDownConfig.ServiceUrl)
                .WithSemanticChunking(new SemanticKernel.Agents.Memory.Core.Handlers.SemanticChunkingOptions
                {
                    MaxChunkSize = chunkingConfig.Semantic.MaxChunkSize,
                    MinChunkSize = chunkingConfig.Semantic.MinChunkSize,
                    TitleLevelThreshold = chunkingConfig.Semantic.TitleLevelThreshold,
                    IncludeTitleContext = chunkingConfig.Semantic.IncludeTitleContext
                })
                .WithEmbeddingsGeneration<GenerateEmbeddingsHandler>()
                .WithSaveRecords(new InMemoryVectorStore());
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

            var pipeline = orchestrator.PrepareNewDocumentUpload(index: pipelineConfig.DefaultIndex, request, context: new NoopContext());
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
    /// <param name="configuration">The configuration instance.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A tuple containing the document ID and pipeline logs.</returns>
    public static async Task<(string DocumentId, IReadOnlyList<PipelineLogEntry> Logs)> RunCustomHandlerAsync(IConfiguration configuration, CancellationToken ct = default)
    {
        // Configure services with custom handler and services
        var services = new ServiceCollection();

        // Add configuration
        services.AddSingleton(configuration);
        services.Configure<AzureOpenAIOptions>(configuration.GetSection(AzureOpenAIOptions.SectionName));
        services.Configure<MarkitDownOptions>(configuration.GetSection(MarkitDownOptions.SectionName));
        services.Configure<TextChunkingConfig>(configuration.GetSection(TextChunkingConfig.SectionName));
        services.Configure<PipelineOptions>(configuration.GetSection(PipelineOptions.SectionName));

        // Add logging
        services.AddLogging(builder =>
        {
            builder.AddConfiguration(configuration.GetSection("Logging"));
            builder.AddConsole();
        });

        // Configure Azure OpenAI embedding generator
        ConfigureAzureOpenAIEmbeddings(services);

        // Get configuration options
        var chunkingConfig = configuration.GetSection(TextChunkingConfig.SectionName).Get<TextChunkingConfig>() ?? new TextChunkingConfig();
        var markitDownConfig = configuration.GetSection(MarkitDownOptions.SectionName).Get<MarkitDownOptions>() ?? new MarkitDownOptions();
        var pipelineConfig = configuration.GetSection(PipelineOptions.SectionName).Get<PipelineOptions>() ?? new PipelineOptions();

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
                    client.Timeout = pipelineConfig.HttpClientTimeout;
                })
                .WithSimpleTextChunking(() => new TextChunkingOptions
                {
                    MaxChunkSize = chunkingConfig.Simple.MaxChunkSize * 2, // Custom larger size
                    TextOverlap = chunkingConfig.Simple.TextOverlap * 2,
                    SplitCharacters = chunkingConfig.Simple.SplitCharacters
                })
                .WithEmbeddingsGeneration<GenerateEmbeddingsHandler>()
                .WithSaveRecords(new InMemoryVectorStore());
        });

        var serviceProvider = services.BuildServiceProvider();

        try
        {
            // Get the orchestrator from the service provider
            var orchestrator = serviceProvider.GetRequiredService<ImportOrchestrator>();

            // Create sample files using the fluent API
            var (documentId, logs) = await orchestrator.ProcessUploadAsync(
                index: pipelineConfig.DefaultIndex,
                builder: orchestrator.NewDocumentUpload()
                    .WithFile("large-document.txt", Encoding.UTF8.GetBytes(GenerateLargeText()))
                    .WithTag("demo", "fluent-api")
                    .WithTag("size", "large"),
                context: new NoopContext(),
                ct);

            return (documentId, logs);
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
    /// Configures Azure OpenAI embedding generation services.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    private static void ConfigureAzureOpenAIEmbeddings(IServiceCollection services)
    {
        // Configure the Azure OpenAI embedding generator
        services.AddSingleton(serviceProvider =>
        {
            var logger = serviceProvider.GetService<ILogger<IEmbeddingGenerator<string, Embedding<float>>>>();
            var azureOpenAIOptions = serviceProvider.GetService<IOptions<AzureOpenAIOptions>>()?.Value ?? new AzureOpenAIOptions();

            // Check if we have real credentials (not placeholders)
            bool hasRealCredentials = azureOpenAIOptions.IsValid();

            if (!hasRealCredentials)
            {
                logger?.LogWarning("Azure OpenAI credentials are not set or are using placeholder values. Please configure your Azure OpenAI Endpoint and ApiKey in the application settings.");
                throw new InvalidOperationException("Azure OpenAI credentials are not configured properly.");
            }

            try
            {
                // Create Azure OpenAI client
                var azureOpenAIClient = new AzureOpenAIClient(new Uri(azureOpenAIOptions.Endpoint), new AzureKeyCredential(azureOpenAIOptions.ApiKey));

                // Get embedding generator
                var embeddingGenerator = azureOpenAIClient.GetEmbeddingClient(azureOpenAIOptions.EmbeddingModel)
                                                            .AsIEmbeddingGenerator();

                logger?.LogInformation("Azure OpenAI embedding generator configured successfully with model: {ModelName}", azureOpenAIOptions.EmbeddingModel);

                return embeddingGenerator;
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Failed to configure Azure OpenAI embedding generator. Please ensure your Azure OpenAI credentials are correct.");

                throw new InvalidOperationException("Failed to configure Azure OpenAI embedding generator. See inner exception for details.", ex);
            }
        });
    }

    /// <summary>
    /// Demo showing configurable semantic chunking options.
    /// </summary>
    /// <param name="configuration">The configuration instance.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A tuple containing the document ID and pipeline logs.</returns>
    public static async Task<(string DocumentId, IReadOnlyList<PipelineLogEntry> Logs)> RunSemanticChunkingConfigDemo(IConfiguration configuration, CancellationToken ct = default)
    {
        // Configure services
        var services = new ServiceCollection();

        // Add configuration
        services.AddSingleton(configuration);
        services.Configure<AzureOpenAIOptions>(configuration.GetSection(AzureOpenAIOptions.SectionName));
        services.Configure<MarkitDownOptions>(configuration.GetSection(MarkitDownOptions.SectionName));
        services.Configure<TextChunkingConfig>(configuration.GetSection(TextChunkingConfig.SectionName));
        services.Configure<PipelineOptions>(configuration.GetSection(PipelineOptions.SectionName));

        // Add logging
        services.AddLogging(builder =>
        {
            builder.AddConfiguration(configuration.GetSection("Logging"));
            builder.AddConsole();
        });

        // Configure Azure OpenAI embedding generator
        ConfigureAzureOpenAIEmbeddings(services);

        // Get configuration options
        var chunkingConfig = configuration.GetSection(TextChunkingConfig.SectionName).Get<TextChunkingConfig>() ?? new TextChunkingConfig();
        var markitDownConfig = configuration.GetSection(MarkitDownOptions.SectionName).Get<MarkitDownOptions>() ?? new MarkitDownOptions();
        var pipelineConfig = configuration.GetSection(PipelineOptions.SectionName).Get<PipelineOptions>() ?? new PipelineOptions();

        // Configure memory ingestion pipeline with custom semantic chunking options
        services.ConfigureMemoryIngestion(options =>
        {
            options
                .WithMarkitDownTextExtraction(markitDownConfig.ServiceUrl)
                .WithSemanticChunking(new SemanticKernel.Agents.Memory.Core.Handlers.SemanticChunkingOptions
                {
                    MaxChunkSize = chunkingConfig.Semantic.MaxChunkSize,
                    MinChunkSize = chunkingConfig.Semantic.MinChunkSize,
                    TitleLevelThreshold = chunkingConfig.Semantic.TitleLevelThreshold,
                    IncludeTitleContext = chunkingConfig.Semantic.IncludeTitleContext
                })
                .WithEmbeddingsGeneration<GenerateEmbeddingsHandler>()
                .WithSaveRecords(new InMemoryVectorStore());
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
            var pipeline = orchestrator.PrepareNewDocumentUpload(index: pipelineConfig.DefaultIndex, request, context: new NoopContext());
            await orchestrator.RunPipelineAsync(pipeline, ct);
            return (pipeline.DocumentId, pipeline.Logs);
        }
        finally
        {
            // Clean up
            serviceProvider.Dispose();
        }
    }

    /// <summary>
    /// Demonstrates the fluent API for file uploads with various upload methods.
    /// This method is integrated into the main pipeline demo.
    /// </summary>
    /// <param name="configuration">The configuration instance.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A tuple containing the document ID and pipeline logs.</returns>
    public static async Task<(string DocumentId, IReadOnlyList<PipelineLogEntry> Logs)> RunFluentApiDemo(IConfiguration configuration, CancellationToken ct = default)
    {
        // Configure services
        var services = new ServiceCollection();

        // Add configuration
        services.AddSingleton(configuration);
        services.Configure<AzureOpenAIOptions>(configuration.GetSection(AzureOpenAIOptions.SectionName));
        services.Configure<MarkitDownOptions>(configuration.GetSection(MarkitDownOptions.SectionName));
        services.Configure<TextChunkingConfig>(configuration.GetSection(TextChunkingConfig.SectionName));
        services.Configure<PipelineOptions>(configuration.GetSection(PipelineOptions.SectionName));

        // Add logging
        services.AddLogging(builder =>
        {
            builder.AddConfiguration(configuration.GetSection("Logging"));
            builder.AddConsole();
        });

        // Configure Azure OpenAI embedding generator
        ConfigureAzureOpenAIEmbeddings(services);

        // Get configuration options
        var chunkingConfig = configuration.GetSection(TextChunkingConfig.SectionName).Get<TextChunkingConfig>() ?? new TextChunkingConfig();
        var markitDownConfig = configuration.GetSection(MarkitDownOptions.SectionName).Get<MarkitDownOptions>() ?? new MarkitDownOptions();
        var pipelineConfig = configuration.GetSection(PipelineOptions.SectionName).Get<PipelineOptions>() ?? new PipelineOptions();

        // Configure memory ingestion pipeline
        services.ConfigureMemoryIngestion(options =>
        {
            options
                .WithMarkitDownTextExtraction(markitDownConfig.ServiceUrl)
                .WithSimpleTextChunking(() => new TextChunkingOptions
                {
                    MaxChunkSize = chunkingConfig.Simple.MaxChunkSize,
                    TextOverlap = chunkingConfig.Simple.TextOverlap
                })
                .WithDefaultEmbeddingsGeneration()
                .WithSaveRecords(new InMemoryVectorStore());
        });

        var serviceProvider = services.BuildServiceProvider();

        try
        {
            // Get the orchestrator from the service provider
            var orchestrator = serviceProvider.GetRequiredService<ImportOrchestrator>();

            // Demonstrate different fluent API upload methods

            // Method 1: Using the fluent builder with multiple upload methods
            var (documentId, logs) = await orchestrator.ProcessUploadAsync(
                index: pipelineConfig.DefaultIndex,
                builder: orchestrator.NewDocumentUpload()
                    // Add a file from byte array with automatic MIME type detection
                    .WithFile("sample.txt", Encoding.UTF8.GetBytes("This is a sample text file for testing."))
                    // Add a file from byte array with custom MIME type
                    .WithFile("custom.data", Encoding.UTF8.GetBytes("Custom data content"), "text/plain")
                    // Add a file from memory stream
                    .WithFile("stream-file.md", new MemoryStream(Encoding.UTF8.GetBytes("# Stream File\n\nThis file was uploaded from a stream.")))
                    // Add tags to categorize the upload
                    .WithTag("demo", "fluent-api")
                    .WithTag("method", "multiple-files")
                    .WithTag("source", "memory")
                    // Add context data
                    .WithContext("upload_reason", "demonstration")
                    .WithContext("batch_size", 3),
                context: new NoopContext(),
                ct);

            return (documentId, logs);
        }
        finally
        {
            serviceProvider.Dispose();
        }
    }

    /// <summary>
    /// Demonstrates uploading files from file paths using the fluent API.
    /// This method is integrated into the main pipeline demo.
    /// </summary>
    /// <param name="configuration">The configuration instance.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A tuple containing the document ID and pipeline logs.</returns>
    public static async Task<(string DocumentId, IReadOnlyList<PipelineLogEntry> Logs)> RunFluentApiFilePathDemo(IConfiguration configuration, CancellationToken ct = default)
    {
        // Configure services
        var services = new ServiceCollection();

        // Add configuration
        services.AddSingleton(configuration);
        services.Configure<AzureOpenAIOptions>(configuration.GetSection(AzureOpenAIOptions.SectionName));
        services.Configure<MarkitDownOptions>(configuration.GetSection(MarkitDownOptions.SectionName));
        services.Configure<TextChunkingConfig>(configuration.GetSection(TextChunkingConfig.SectionName));
        services.Configure<PipelineOptions>(configuration.GetSection(PipelineOptions.SectionName));

        // Add logging
        services.AddLogging(builder =>
        {
            builder.AddConfiguration(configuration.GetSection("Logging"));
            builder.AddConsole();
        });

        // Configure Azure OpenAI embedding generator
        ConfigureAzureOpenAIEmbeddings(services);

        // Get configuration options
        var chunkingConfig = configuration.GetSection(TextChunkingConfig.SectionName).Get<TextChunkingConfig>() ?? new TextChunkingConfig();
        var markitDownConfig = configuration.GetSection(MarkitDownOptions.SectionName).Get<MarkitDownOptions>() ?? new MarkitDownOptions();
        var pipelineConfig = configuration.GetSection(PipelineOptions.SectionName).Get<PipelineOptions>() ?? new PipelineOptions();

        // Configure memory ingestion pipeline
        services.ConfigureMemoryIngestion(options =>
        {
            options
                .WithMarkitDownTextExtraction(markitDownConfig.ServiceUrl)
                .WithSimpleTextChunking(() => new TextChunkingOptions
                {
                    MaxChunkSize = chunkingConfig.Simple.MaxChunkSize,
                    TextOverlap = chunkingConfig.Simple.TextOverlap
                })
                .WithDefaultEmbeddingsGeneration()
                .WithSaveRecords(new InMemoryVectorStore());
        });

        var serviceProvider = services.BuildServiceProvider();

        try
        {
            // Get the orchestrator from the service provider
            var orchestrator = serviceProvider.GetRequiredService<ImportOrchestrator>();

            // Create temporary files for demonstration
            var tempDir = Path.GetTempPath();
            var tempFile1 = Path.Combine(tempDir, "demo1.txt");
            var tempFile2 = Path.Combine(tempDir, "demo2.md");

            await File.WriteAllTextAsync(tempFile1, "This is demo file 1 content.", ct);
            await File.WriteAllTextAsync(tempFile2, "# Demo File 2\n\nThis is demo file 2 content in markdown.", ct);

            try
            {
                // Method 2: Simple single file upload by path
                var documentId1 = await orchestrator.UploadFileAsync(
                    index: pipelineConfig.DefaultIndex,
                    filePath: tempFile1,
                    context: new NoopContext(),
                    cancellationToken: ct);

                // Method 3: Multiple files upload by paths
                var documentId2 = await orchestrator.UploadFilesAsync(
                    index: pipelineConfig.DefaultIndex,
                    filePaths: new[] { tempFile1, tempFile2 },
                    context: new NoopContext(),
                    cancellationToken: ct);

                // Method 4: Using fluent builder with file paths
                var (documentId3, logs) = await orchestrator.ProcessUploadAsync(
                    index: pipelineConfig.DefaultIndex,
                    builder: orchestrator.NewDocumentUpload()
                        .WithFile(tempFile1, "renamed-demo1.txt") // Upload with custom name
                        .WithFile(tempFile2) // Upload with original name
                        .WithTag("demo", "file-paths")
                        .WithTag("files", "2"),
                    context: new NoopContext(),
                    ct);

                return (documentId3, logs);
            }
            finally
            {
                // Clean up temporary files
                try { File.Delete(tempFile1); } catch { }
                try { File.Delete(tempFile2); } catch { }
            }
        }
        finally
        {
            serviceProvider.Dispose();
        }
    }
}
