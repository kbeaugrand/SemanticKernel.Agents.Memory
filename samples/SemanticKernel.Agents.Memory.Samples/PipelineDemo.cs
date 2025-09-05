using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Azure.AI.OpenAI;
using Azure;
using OpenAI.Embeddings;
using SemanticKernel.Agents.Memory.Core;
using SemanticKernel.Agents.Memory.Core.Extensions;
using SemanticKernel.Agents.Memory.Core.Handlers;
using SemanticKernel.Agents.Memory.Samples.Configuration;
using Microsoft.SemanticKernel.Connectors.InMemory;

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
                .WithSimpleTextChunking(() => new SemanticKernel.Agents.Memory.Core.Handlers.TextChunkingOptions
                {
                    MaxChunkSize = chunkingConfig.Simple.MaxChunkSize,
                    TextOverlap = chunkingConfig.Simple.TextOverlap
                })
                .WithEmbeddingsGeneration<GenerateEmbeddingsHandler>()
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

            var pipeline = orchestrator.PrepareNewDocumentUpload(index: pipelineConfig.DefaultIndex, request, context: new NoopContext());
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
    /// Configures Azure OpenAI embedding generation services.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    private static void ConfigureAzureOpenAIEmbeddings(IServiceCollection services)
    {
        // Configure the Azure OpenAI embedding generator
        services.AddSingleton<IEmbeddingGenerator<string, Embedding<float>>>(serviceProvider =>
        {
            var logger = serviceProvider.GetService<ILogger<AzureOpenAIEmbeddingGenerator>>();
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
                
                // Get embedding client and wrap it
                var embeddingClient = azureOpenAIClient.GetEmbeddingClient(azureOpenAIOptions.EmbeddingModel);
                var embeddingGenerator = new AzureOpenAIEmbeddingGenerator(embeddingClient, azureOpenAIOptions.EmbeddingModel, logger);

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
    /// Azure OpenAI embedding generator implementation.
    /// </summary>
    private sealed class AzureOpenAIEmbeddingGenerator : IEmbeddingGenerator<string, Embedding<float>>
    {
        private readonly EmbeddingClient _embeddingClient;
        private readonly string _modelName;
        private readonly ILogger? _logger;

        public AzureOpenAIEmbeddingGenerator(EmbeddingClient embeddingClient, string modelName, ILogger? logger = null)
        {
            _embeddingClient = embeddingClient ?? throw new ArgumentNullException(nameof(embeddingClient));
            _modelName = modelName ?? throw new ArgumentNullException(nameof(modelName));
            _logger = logger;
        }

        public EmbeddingGeneratorMetadata Metadata { get; } = new("azure-openai-embeddings");

        public async Task<GeneratedEmbeddings<Embedding<float>>> GenerateAsync(
            IEnumerable<string> values,
            Microsoft.Extensions.AI.EmbeddingGenerationOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            var inputTexts = values.ToArray();
            _logger?.LogDebug("Generating embeddings for {Count} texts using Azure OpenAI model: {ModelName}", inputTexts.Length, _modelName);

            try
            {
                var response = await _embeddingClient.GenerateEmbeddingsAsync(inputTexts, new OpenAI.Embeddings.EmbeddingGenerationOptions(), cancellationToken);
                
                var embeddings = response.Value.Select(embeddingItem =>
                {
                    var vector = embeddingItem.ToFloats().ToArray();
                    return new Embedding<float>(vector);
                }).ToArray();

                _logger?.LogDebug("Successfully generated {Count} embeddings", embeddings.Length);
                return new GeneratedEmbeddings<Embedding<float>>(embeddings);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to generate embeddings using Azure OpenAI");
                throw;
            }
        }

        public object? GetService(Type serviceType, object? serviceKey = null) => null;

        public TService? GetService<TService>(object? serviceKey = null) => default;

        public void Dispose() { }
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
}
