using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel.Connectors.InMemory;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Azure.AI.OpenAI;
using Azure.Identity;
using SemanticKernel.Agents.Memory;
using SemanticKernel.Agents.Memory.Core;
using SemanticKernel.Agents.Memory.Core.Extensions;
using SemanticKernel.Agents.Memory.Core.Services;
using SemanticKernel.Agents.Memory.Core.Handlers;
using SemanticKernel.Agents.Memory.Samples.Configuration;
using Azure;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;

namespace SemanticKernel.Agents.Memory.Samples;

/// <summary>
/// Simple context implementation for demo purposes.
/// </summary>
internal sealed class NoopContext : IContext { }

/// <summary>
/// Complete flow demonstration: Ingestion → Query with SearchClient
/// This sample shows the end-to-end process from document ingestion to querying the memory.
/// </summary>
public static class CompleteFlowDemo
{
    /// <summary>
    /// Runs the complete flow demonstration
    /// </summary>
    /// <param name="configuration">Application configuration</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Task representing the asynchronous operation</returns>
    public static async Task RunAsync(IConfiguration configuration, CancellationToken ct = default)
    {
        Console.WriteLine("=== Complete Flow Demo: Ingestion + SearchClient ===");
        Console.WriteLine();

        // Configure services for the complete flow
        var services = new ServiceCollection();
        var vectorStore = new InMemoryVectorStore();
        ConfigureCompleteFlowServices(services, configuration, vectorStore);
        
        using var serviceProvider = services.BuildServiceProvider();
        
        try
        {
            // Step 1: Document Ingestion
            Console.WriteLine("🚀 Step 1: Document Ingestion");
            Console.WriteLine("─────────────────────────────");
            var documentId = await PerformIngestion(serviceProvider, ct);
            Console.WriteLine();

            // Step 2: Search and Query
            Console.WriteLine("🔍 Step 2: Search and Query with SearchClient");
            Console.WriteLine("───────────────────────────────────────────────");
            await PerformSearchAndQuery(serviceProvider, documentId, ct);
            Console.WriteLine();

            // Step 3: Interactive Q&A Session
            Console.WriteLine("💬 Step 3: Interactive Q&A Session");
            Console.WriteLine("─────────────────────────────────");
            await InteractiveQASession(serviceProvider, ct);

        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error during complete flow demo: {ex.Message}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"   Inner exception: {ex.InnerException.Message}");
            }
        }
    }

    /// <summary>
    /// Configures all services needed for the complete flow
    /// </summary>
    private static void ConfigureCompleteFlowServices(
        IServiceCollection services, 
        IConfiguration configuration, 
        InMemoryVectorStore vectorStore)
    {
        // Add configuration
        services.AddSingleton(configuration);
        services.Configure<AzureOpenAIOptions>(configuration.GetSection(AzureOpenAIOptions.SectionName));
        services.Configure<MarkitDownOptions>(configuration.GetSection(MarkitDownOptions.SectionName));
        services.Configure<TextChunkingConfig>(configuration.GetSection(TextChunkingConfig.SectionName));

        // Add logging
        services.AddLogging(builder => 
        {
            builder.AddConfiguration(configuration.GetSection("Logging"));
            builder.AddConsole();
        });

        // Register the vector store
        services.AddSingleton(vectorStore);

        // Configure Azure OpenAI or Mock embedding generator
        ConfigureEmbeddingGenerator(services, configuration);

        // DEBUG: Log what service is actually registered
        Console.WriteLine("🔍 DEBUG: Checking what IEmbeddingGenerator service is registered...");
        using var tempProvider = services.BuildServiceProvider();
        var embeddingGenerator = tempProvider.GetService<IEmbeddingGenerator<string, Embedding<float>>>();
        if (embeddingGenerator != null)
        {
            Console.WriteLine($"   Registered service type: {embeddingGenerator.GetType().FullName}");
        }
        else
        {
            Console.WriteLine("   No IEmbeddingGenerator service registered!");
        }

        // Configure ChatCompletion service for SearchClient
        ConfigureChatCompletionService(services, configuration);

        // Add prompt provider
        services.AddDefaultPromptProvider();

        // Get configuration options
        var markitDownConfig = configuration.GetSection(MarkitDownOptions.SectionName).Get<MarkitDownOptions>() ?? new MarkitDownOptions();

        // Configure memory ingestion pipeline
        services.ConfigureMemoryIngestion(options =>
        {
            options
                .WithMarkitDownTextExtraction(markitDownConfig.ServiceUrl)
                .WithSemanticChunking()
                .WithDefaultEmbeddingsGeneration()
                .WithSaveRecords(vectorStore);
        });

        // Add SearchClient
        services.AddMemorySearchClient(vectorStore);
    }

    /// <summary>
    /// Performs document ingestion step
    /// </summary>
    private static async Task<string> PerformIngestion(IServiceProvider serviceProvider, CancellationToken ct)
    {
        var orchestrator = serviceProvider.GetRequiredService<ImportOrchestrator>();
        
        // Read the sample content file
        var sampleFilePath = Path.Combine(Directory.GetCurrentDirectory(), "sample-content.txt");
        if (!File.Exists(sampleFilePath))
        {
            throw new FileNotFoundException($"Sample content file not found at: {sampleFilePath}");
        }

        var fileContent = await File.ReadAllBytesAsync(sampleFilePath, ct);
        Console.WriteLine($"📄 Loaded sample file: {Path.GetFileName(sampleFilePath)} ({fileContent.Length} bytes)");

        // Create document upload request
        var request = orchestrator.NewDocumentUpload()
                            .WithFile("sample-content.txt");

        // Start ingestion
        Console.WriteLine("⚙️  Starting document ingestion pipeline...");
        const string indexName = "default"; // Default index name
        var context = new NoopContext(); // Simple context implementation
        
        (var documentId, var logs) = await orchestrator.ProcessUploadAsync(indexName, request, context, ct);
        
        if (!string.IsNullOrEmpty(documentId))
        {
            Console.WriteLine($"✅ Ingestion completed successfully!");
            Console.WriteLine($"   Document ID: {documentId}");
            return documentId;
        }
        else
        {
            throw new InvalidOperationException("Document ingestion failed - no document ID returned");
        }
    }

    /// <summary>
    /// Performs search and query operations using SearchClient
    /// </summary>
    private static async Task PerformSearchAndQuery(IServiceProvider serviceProvider, string documentId, CancellationToken ct)
    {
        var searchClient = serviceProvider.GetRequiredService<ISearchClient>();
        const string indexName = "default"; // Default index name used by the pipeline

        // Test 1: List available indexes
        Console.WriteLine("📋 Available indexes:");
        var indexes = await searchClient.ListIndexesAsync(ct);
        foreach (var index in indexes)
        {
            Console.WriteLine($"   • {index}");
        }
        Console.WriteLine();

        // Test 2: Semantic Search
        Console.WriteLine("🔎 Performing semantic searches...");
        
        var searchQueries = new[]
        {
            "machine learning types",
            "AI applications in healthcare",
            "ethical considerations artificial intelligence"
        };

        foreach (var query in searchQueries)
        {
            Console.WriteLine($"   Query: \"{query}\"");
            var searchResult = await searchClient.SearchAsync(indexName, query, minRelevance: 0.7, limit: 3, cancellationToken: ct);
            
            if (searchResult.NoResult)
            {
                Console.WriteLine("     No results found.");
            }
            else
            {
                Console.WriteLine($"     Found {searchResult.Results.Count} results:");
                foreach (var result in searchResult.Results)
                {
                    Console.WriteLine($"       • Source: {result.Source} (Score: {result.RelevanceScore:F3})");
                    Console.WriteLine($"         Content: {result.Content.Substring(0, Math.Min(100, result.Content.Length))}...");
                }
            }
            Console.WriteLine();
        }

        // Test 3: Question Answering
        Console.WriteLine("❓ Asking questions using SearchClient...");
        
        var questions = new[]
        {
            "What are the three main types of machine learning?",
            "How is AI being used in healthcare?",
            "What are some ethical considerations for AI?",
            "What is the difference between AI and machine learning?"
        };

        foreach (var question in questions)
        {
            Console.WriteLine($"   Question: \"{question}\"");
            try
            {
                var answer = await searchClient.AskAsync(indexName, question, minRelevance: 0.6, cancellationToken: ct);
                
                if (answer.HasResult)
                {
                    Console.WriteLine($"   Answer: {answer.Result}");
                    
                    if (answer.RelevantSources?.Count > 0)
                    {
                        Console.WriteLine($"   Sources used: {answer.RelevantSources.Count}");
                        foreach (var source in answer.RelevantSources)
                        {
                            Console.WriteLine($"     • {source.SourceName} (Document ID: {source.DocumentId})");
                        }
                    }

                    if (answer.TokenUsage?.Count > 0)
                    {
                        var totalInputTokens = answer.TokenUsage.Sum(u => u.InputTokens);
                        var totalOutputTokens = answer.TokenUsage.Sum(u => u.OutputTokens);
                        Console.WriteLine($"   Token usage: {totalInputTokens} input, {totalOutputTokens} output");
                    }
                }
                else
                {
                    Console.WriteLine($"   Answer: {answer.Result}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   Error: {ex.Message}");
            }
            Console.WriteLine();
        }
    }

    /// <summary>
    /// Interactive Q&A session with the user
    /// </summary>
    private static async Task InteractiveQASession(IServiceProvider serviceProvider, CancellationToken ct)
    {
        var searchClient = serviceProvider.GetRequiredService<ISearchClient>();
        const string indexName = "default";

        Console.WriteLine("You can now ask questions about the ingested content!");
        Console.WriteLine("Type 'exit' to quit, 'search:' followed by keywords to search, or ask any question.");
        Console.WriteLine();

        while (!ct.IsCancellationRequested)
        {
            Console.Write("Your question: ");
            var input = Console.ReadLine()?.Trim();
            
            if (string.IsNullOrEmpty(input) || input.Equals("exit", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("👋 Goodbye!");
                break;
            }

            try
            {
                if (input.StartsWith("search:", StringComparison.OrdinalIgnoreCase))
                {
                    // Perform search
                    var searchQuery = input.Substring(7).Trim();
                    Console.WriteLine($"🔍 Searching for: \"{searchQuery}\"");
                    
                    var searchResult = await searchClient.SearchAsync(indexName, searchQuery, minRelevance: 0.5, limit: 5, cancellationToken: ct);
                    
                    if (searchResult.NoResult)
                    {
                        Console.WriteLine("❌ No results found.");
                    }
                    else
                    {
                        Console.WriteLine($"✅ Found {searchResult.Results.Count} results:");
                        foreach (var (result, index) in searchResult.Results.Select((r, i) => (r, i + 1)))
                        {
                            Console.WriteLine($"{index}. Source: {result.Source} (Score: {result.RelevanceScore:F3})");
                            Console.WriteLine($"   {result.Content.Substring(0, Math.Min(200, result.Content.Length))}{(result.Content.Length > 200 ? "..." : "")}");
                            Console.WriteLine();
                        }
                    }
                }
                else
                {
                    // Ask question
                    Console.WriteLine($"🤔 Thinking about: \"{input}\"");
                    
                    var answer = await searchClient.AskAsync(indexName, input, minRelevance: 0.5, cancellationToken: ct);
                    
                    if (answer.HasResult)
                    {
                        Console.WriteLine($"💡 Answer: {answer.Result}");
                        
                        if (answer.RelevantSources?.Count > 0)
                        {
                            Console.WriteLine($"📚 Based on {answer.RelevantSources.Count} source(s)");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"❌ {answer.Result}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error: {ex.Message}");
            }
            
            Console.WriteLine();
        }
    }

    /// <summary>
    /// Configures embedding generator (Azure OpenAI or Mock)
    /// </summary>
    private static void ConfigureEmbeddingGenerator(IServiceCollection services, IConfiguration configuration)
    {
        var azureOpenAIOptions = configuration.GetSection(AzureOpenAIOptions.SectionName).Get<AzureOpenAIOptions>();
        
        Console.WriteLine("🧪 Using Azure OpenAI embedding generator");

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
    /// Configures chat completion service for SearchClient
    /// </summary>
    private static void ConfigureChatCompletionService(IServiceCollection services, IConfiguration configuration)
    {
        var azureOpenAIOptions = configuration.GetSection(AzureOpenAIOptions.SectionName).Get<AzureOpenAIOptions>();
        
        Console.WriteLine("🧪 Using Azure OpenAI chat completion service");
        
        // Configure the Azure OpenAI chat completion
        services.AddSingleton<IChatCompletionService>(serviceProvider =>
        {
            var logger = serviceProvider.GetService<ILogger<IChatCompletionService>>();
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
                var chatCompletionService = new AzureOpenAIChatCompletionService (
                    deploymentName: azureOpenAIOptions.CompletionModel,
                    apiKey: azureOpenAIOptions.ApiKey,
                    endpoint: azureOpenAIOptions.Endpoint);


                logger?.LogInformation("Azure OpenAI chat completion configured successfully with model: {ModelName}", azureOpenAIOptions.CompletionModel);
                
                return chatCompletionService;
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Failed to configure Azure OpenAI chat completion. Please ensure your Azure OpenAI credentials are correct.");

                throw new InvalidOperationException("Failed to configure Azure OpenAI chat completion. See inner exception for details.", ex);
            }
        });
    }
}