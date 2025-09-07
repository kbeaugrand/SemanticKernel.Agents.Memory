using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.AI.OpenAI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;
using Microsoft.SemanticKernel.Connectors.InMemory;
using SemanticKernel.Agents.Memory.Core;
using SemanticKernel.Agents.Memory.Core.Extensions;
using SemanticKernel.Agents.Memory.Core.Handlers;
using SemanticKernel.Agents.Memory.Core.Services;
using SemanticKernel.Agents.Memory.Samples.Configuration;
using SemanticKernel.Rankers.Abstractions;
using SemanticKernel.Rankers.BM25;

namespace SemanticKernel.Agents.Memory.Samples;

/// <summary>
/// Simple context implementation for demo purposes.
/// </summary>
internal sealed class NoopRankingContext : IContext { }

/// <summary>
/// Ranking demonstration: Shows how to use BM25 rankers with the SearchClient
/// to improve search result quality through sophisticated ranking algorithms.
/// </summary>
public static class RankingDemo
{
    /// <summary>
    /// Runs the ranking demonstration
    /// </summary>
    /// <param name="configuration">Application configuration</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Task representing the asynchronous operation</returns>
    public static async Task RunAsync(IConfiguration configuration, CancellationToken ct = default)
    {
        Console.WriteLine("=== Ranking Demo: BM25 Ranker with SearchClient ===");
        Console.WriteLine();

        // Configure services for ranking demo
        var services = new ServiceCollection();
        var vectorStore = new InMemoryVectorStore();
        ConfigureRankingServices(services, configuration, vectorStore);

        using var serviceProvider = services.BuildServiceProvider();

        try
        {
            // Step 1: Document Ingestion
            Console.WriteLine("🚀 Step 1: Document Ingestion for Ranking Demo");
            Console.WriteLine("─────────────────────────────────────────────────");
            var documentId = await PerformIngestion(serviceProvider, ct);
            Console.WriteLine();

            // Step 2: Standard Search (without ranking)
            Console.WriteLine("🔍 Step 2: Standard Search (Baseline)");
            Console.WriteLine("────────────────────────────────────");
            await PerformStandardSearch(serviceProvider, documentId, ct);
            Console.WriteLine();

            // Step 3: BM25 Ranking Concept Demo
            Console.WriteLine("🎯 Step 3: BM25 Ranking Concept Demo");
            Console.WriteLine("──────────────────────────────────────────");
            await PerformRankingConceptDemo(serviceProvider, documentId, ct);
            Console.WriteLine();

            // Step 4: Interactive Search Demo
            Console.WriteLine("💬 Step 4: Interactive Search Demo");
            Console.WriteLine("─────────────────────────────────");
            await InteractiveSearchSession(serviceProvider, ct);

        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error during ranking demo: {ex.Message}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"   Inner exception: {ex.InnerException.Message}");
            }
        }
    }

    /// <summary>
    /// Configures all services needed for the ranking demo
    /// </summary>
    private static void ConfigureRankingServices(
        IServiceCollection services,
        IConfiguration configuration,
        InMemoryVectorStore vectorStore)
    {
        // Add configuration
        services.AddSingleton(configuration);
        services.Configure<AzureOpenAIOptions>(configuration.GetSection(AzureOpenAIOptions.SectionName));
        services.Configure<MarkitDownOptions>(configuration.GetSection(MarkitDownOptions.SectionName));

        // Add logging
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Warning);
        });

        // Register the vector store
        services.AddSingleton(vectorStore);

        // Configure Azure OpenAI or Mock embedding generator (copied from CompleteFlowDemo)
        ConfigureEmbeddingGenerator(services, configuration);

        // Configure ChatCompletion service for SearchClient (copied from CompleteFlowDemo)
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
                .WithSemanticChunking(() => new SemanticKernel.Agents.Memory.Core.Handlers.SemanticChunkingOptions
                {
                    MaxChunkSize = 800,
                    TitleLevelThreshold = 3
                })
                .WithDefaultEmbeddingsGeneration()
                .WithSaveRecords(vectorStore);
        });

        // Add SearchClient using the established pattern
        services.AddMemorySearchClient(vectorStore);

        // Add BM25 ranker for demonstration
        services.AddSingleton<IRanker, BM25Reranker>();
    }

    /// <summary>
    /// Configures embedding generator (copied from CompleteFlowDemo)
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
    /// Configures chat completion service (copied from CompleteFlowDemo)
    /// </summary>
    private static void ConfigureChatCompletionService(IServiceCollection services, IConfiguration configuration)
    {
        var azureOpenAIOptions = configuration.GetSection(AzureOpenAIOptions.SectionName).Get<AzureOpenAIOptions>();

        Console.WriteLine("🧪 Using Azure OpenAI chat completion service");

        // Configure the Azure OpenAI chat completion service
        services.AddSingleton(serviceProvider =>
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
                var chatCompletionService = new AzureOpenAIChatCompletionService(
                    deploymentName: azureOpenAIOptions.CompletionModel,
                    apiKey: azureOpenAIOptions.ApiKey,
                    endpoint: azureOpenAIOptions.Endpoint);

                logger?.LogInformation("Azure OpenAI chat completion configured successfully with model: {ModelName}", azureOpenAIOptions.CompletionModel);

                return chatCompletionService;
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Failed to configure Azure OpenAI chat completion service. Please ensure your Azure OpenAI credentials are correct.");

                throw new InvalidOperationException("Failed to configure Azure OpenAI chat completion service. See inner exception for details.", ex);
            }
        });
    }

    /// <summary>
    /// Performs document ingestion for the ranking demo
    /// </summary>
    private static async Task<string> PerformIngestion(IServiceProvider serviceProvider, CancellationToken ct)
    {
        var orchestrator = serviceProvider.GetRequiredService<ImportOrchestrator>();

        // Create sample content for ranking demonstration and write it to a file
        var sampleContent = @"
# Artificial Intelligence and Machine Learning

## Introduction to AI
Artificial Intelligence (AI) represents one of the most significant technological advances of our time.
It encompasses machine learning, deep learning, neural networks, and natural language processing.

## Machine Learning Fundamentals
Machine learning is a subset of artificial intelligence that enables computers to learn without being explicitly programmed.
It includes supervised learning, unsupervised learning, and reinforcement learning approaches.

## Deep Learning and Neural Networks
Deep learning uses artificial neural networks with multiple layers to model and understand complex patterns.
These networks can process vast amounts of data and identify intricate relationships.

## Natural Language Processing
Natural Language Processing (NLP) allows computers to understand, interpret, and generate human language.
It powers applications like chatbots, translation services, and text analysis tools.

## Applications in Industry
AI applications span across healthcare, finance, automotive, retail, and entertainment industries.
From medical diagnosis to autonomous vehicles, AI is transforming how we work and live.

## Future of AI
The future of AI holds promise for even more sophisticated applications including artificial general intelligence,
quantum machine learning, and enhanced human-AI collaboration.
";

        // Write content to a temporary file in the working directory
        var tempFileName = "ranking-demo-content.txt";
        var tempFilePath = Path.Combine(Directory.GetCurrentDirectory(), tempFileName);
        await File.WriteAllTextAsync(tempFilePath, sampleContent, ct);

        try
        {
            Console.WriteLine($"📄 Ingesting sample AI content");
            Console.WriteLine("📊 Content includes multiple sections about AI, ML, and related topics");

            // Create document upload request
            var request = orchestrator.NewDocumentUpload()
                                .WithFile(tempFileName);

            // Start ingestion
            Console.WriteLine("⚙️  Starting document ingestion pipeline...");
            const string indexName = "default"; // Default index name
            var context = new NoopRankingContext(); // Simple context implementation

            (var documentId, _) = await orchestrator.ProcessUploadAsync(indexName, request, context, ct);

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
        finally
        {
            // Clean up temporary file
            if (File.Exists(tempFilePath))
            {
                File.Delete(tempFilePath);
            }
        }
    }

    /// <summary>
    /// Performs standard search without ranking
    /// </summary>
    private static async Task PerformStandardSearch(IServiceProvider serviceProvider, string documentId, CancellationToken ct)
    {
        var searchClient = serviceProvider.GetRequiredService<ISearchClient>();
        const string indexName = "default";

        var queries = new[]
        {
            "machine learning algorithms",
            "deep neural networks",
            "AI applications in healthcare"
        };

        Console.WriteLine("Running standard vector similarity search...");
        Console.WriteLine();

        foreach (var query in queries)
        {
            Console.WriteLine($"🔍 Query: \"{query}\"");

            var searchResults = await searchClient.SearchAsync(indexName, query, limit: 10, cancellationToken: ct);

            Console.WriteLine($"📊 Found {searchResults.Results.Count} results using vector similarity:");

            for (int i = 0; i < Math.Min(3, searchResults.Results.Count); i++)
            {
                var result = searchResults.Results.ElementAt(i);
                Console.WriteLine($"   {i + 1}. Score: {result.RelevanceScore:F4} | {TruncateText(result.Content, 80)}");
            }
            Console.WriteLine();
        }
    }

    /// <summary>
    /// Demonstrates BM25 ranking concepts and shows how it would work
    /// </summary>
    private static Task PerformRankingConceptDemo(IServiceProvider serviceProvider, string documentId, CancellationToken ct)
    {
        var ranker = serviceProvider.GetRequiredService<IRanker>();

        Console.WriteLine("🎯 BM25 Ranking Concept Demonstration");
        Console.WriteLine();

        // Show that we have a BM25 ranker available
        Console.WriteLine($"✅ BM25 Ranker Type: {ranker.GetType().Name}");
        Console.WriteLine();

        // Demonstrate the concept of how BM25 ranking works
        Console.WriteLine("📈 How BM25 Ranking Works:");
        Console.WriteLine("   1. Term Frequency (TF): How often query terms appear in documents");
        Console.WriteLine("   2. Inverse Document Frequency (IDF): Rarity of terms across the collection");
        Console.WriteLine("   3. Document Length Normalization: Adjusts for document length variations");
        Console.WriteLine("   4. Parameter Tuning: k1 (term frequency saturation) and b (length normalization)");
        Console.WriteLine();

        // Example usage pattern for BM25 ranking
        Console.WriteLine("🔧 Example BM25 Usage Pattern:");
        Console.WriteLine("   ```csharp");
        Console.WriteLine("   using SemanticKernel.Rankers.BM25;");
        Console.WriteLine("   ");
        Console.WriteLine("   // Create BM25 ranker");
        Console.WriteLine("   var bm25 = new BM25Reranker();");
        Console.WriteLine("   ");
        Console.WriteLine("   // Rank search results");
        Console.WriteLine("   var rankedResults = bm25.RankAsync(query, searchResults, getText);");
        Console.WriteLine("   ```");
        Console.WriteLine();

        // Show conceptual benefit over vector similarity
        Console.WriteLine("� BM25 vs Vector Similarity:");
        Console.WriteLine("   Vector Similarity: Based on semantic meaning in high-dimensional space");
        Console.WriteLine("   BM25 Ranking: Based on exact term matching with statistical weighting");
        Console.WriteLine("   Combined Approach: Vector search for recall + BM25 for precision ranking");
        Console.WriteLine();

        // Demonstrate with sample queries
        var sampleQueries = new[]
        {
            "machine learning",
            "neural networks deep learning",
            "AI healthcare applications"
        };

        Console.WriteLine("🧪 BM25 Analysis for Sample Queries:");
        foreach (var query in sampleQueries)
        {
            Console.WriteLine($"   Query: \"{query}\"");
            var terms = query.Split(' ');
            Console.WriteLine($"   Terms: [{string.Join(", ", terms)}]");
            Console.WriteLine($"   BM25 would analyze term frequency, document frequency, and length for ranking");
            Console.WriteLine();
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Interactive session for testing search concepts
    /// </summary>
    private static async Task InteractiveSearchSession(IServiceProvider serviceProvider, CancellationToken ct)
    {
        var searchClient = serviceProvider.GetRequiredService<ISearchClient>();
        const string indexName = "default";

        Console.WriteLine("🎮 Interactive Search Session");
        Console.WriteLine("Enter your search queries to see search results.");
        Console.WriteLine("(This shows vector search; with BM25 integration, results would be reranked)");
        Console.WriteLine("Type 'exit' to quit.\n");

        while (!ct.IsCancellationRequested)
        {
            Console.Write("Enter query: ");
            var query = Console.ReadLine()?.Trim();

            if (string.IsNullOrEmpty(query) || query.ToLowerInvariant() == "exit")
            {
                break;
            }

            try
            {
                // Get search results
                var results = await searchClient.SearchAsync(indexName, query, limit: 5, cancellationToken: ct);

                if (results.Results.Count == 0)
                {
                    Console.WriteLine("❌ No results found for this query.\n");
                    continue;
                }

                Console.WriteLine($"\n📊 Found {results.Results.Count} results");

                // Show vector search results
                Console.WriteLine("\n🔍 Vector Search Results:");
                for (int i = 0; i < results.Results.Count; i++)
                {
                    var result = results.Results.ElementAt(i);
                    Console.WriteLine($"   {i + 1}. {result.RelevanceScore:F4} | {TruncateText(result.Content, 60)}");
                }

                // Show what BM25 ranking would consider
                Console.WriteLine($"\n🎯 BM25 Ranking Analysis:");
                var queryTerms = query.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                Console.WriteLine($"   Query terms: [{string.Join(", ", queryTerms)}]");
                Console.WriteLine($"   BM25 would analyze:");
                Console.WriteLine($"   • Term frequency in each result");
                Console.WriteLine($"   • Inverse document frequency for each term");
                Console.WriteLine($"   • Document length normalization");
                Console.WriteLine($"   • Results would be reranked based on these statistical measures");

                Console.WriteLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error processing query: {ex.Message}\n");
            }
        }

        Console.WriteLine("👋 Interactive session ended.");
    }

    /// <summary>
    /// Truncates text to specified length with ellipsis
    /// </summary>
    private static string TruncateText(string text, int maxLength)
    {
        if (string.IsNullOrEmpty(text) || text.Length <= maxLength)
            return text;

        return text.Substring(0, maxLength) + "...";
    }
}
