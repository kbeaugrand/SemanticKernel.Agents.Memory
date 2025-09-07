using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SemanticKernel.Rankers.Abstractions;
using SemanticKernel.Rankers.BM25;

namespace SemanticKernel.Agents.Memory.Samples;

/// <summary>
/// Simple Ranking demonstration: Shows how to use BM25 rankers conceptually
/// This demo focuses on the ranking APIs and concepts without requiring full infrastructure.
/// </summary>
public static class SimpleRankingDemo
{
    /// <summary>
    /// Runs the simple ranking demonstration
    /// </summary>
    /// <param name="configuration">Application configuration</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Task representing the asynchronous operation</returns>
    public static async Task RunAsync(IConfiguration configuration, CancellationToken ct = default)
    {
        Console.WriteLine("=== Simple Ranking Demo: BM25 Ranker Concepts ===");
        Console.WriteLine();

        try
        {
            // Step 1: Create BM25 Ranker
            Console.WriteLine("🎯 Step 1: Creating BM25 Ranker");
            Console.WriteLine("─────────────────────────────────");
            var bm25Ranker = new BM25Reranker();
            Console.WriteLine($"✅ Created BM25 Ranker: {bm25Ranker.GetType().Name}");
            Console.WriteLine();

            // Step 2: Demonstrate Ranking Concepts
            Console.WriteLine("📚 Step 2: BM25 Ranking Concepts");
            Console.WriteLine("──────────────────────────────────");
            await DemonstrateRankingConcepts(ct);
            Console.WriteLine();

            // Step 3: Show Code Example
            Console.WriteLine("💡 Step 3: BM25 Usage Example");
            Console.WriteLine("────────────────────────────────");
            ShowBM25UsageExample();
            Console.WriteLine();

            // Step 4: Interactive Query Analysis
            Console.WriteLine("🔍 Step 4: Interactive Query Analysis");
            Console.WriteLine("────────────────────────────────────");
            await InteractiveQueryAnalysis(ct);

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
    /// Demonstrates core BM25 ranking concepts
    /// </summary>
    private static async Task DemonstrateRankingConcepts(CancellationToken ct)
    {
        Console.WriteLine("🧠 BM25 Algorithm Overview:");
        Console.WriteLine("   BM25 (Best Matching 25) is a ranking function used for text search");
        Console.WriteLine("   It improves upon TF-IDF by addressing some of its limitations");
        Console.WriteLine();

        Console.WriteLine("📊 Key Components:");
        Console.WriteLine("   1. Term Frequency (TF): How often a term appears in a document");
        Console.WriteLine("   2. Inverse Document Frequency (IDF): How rare/common a term is in the collection");
        Console.WriteLine("   3. Document Length Normalization: Adjusts for document length variations");
        Console.WriteLine("   4. Parameter Tuning: k1 (term frequency saturation) and b (length normalization)");
        Console.WriteLine();

        Console.WriteLine("🔗 BM25 Formula Components:");
        Console.WriteLine("   Score(D,Q) = Σ IDF(qi) · (f(qi,D) · (k1 + 1)) / (f(qi,D) + k1 · (1 - b + b · |D| / avgdl))");
        Console.WriteLine("   Where:");
        Console.WriteLine("   • qi = query term i");
        Console.WriteLine("   • f(qi,D) = frequency of qi in document D");
        Console.WriteLine("   • |D| = length of document D");
        Console.WriteLine("   • avgdl = average document length");
        Console.WriteLine("   • k1, b = tuning parameters");
        Console.WriteLine();

        await Task.Delay(100, ct); // Small delay for readability
    }

    /// <summary>
    /// Shows a concrete code example of using BM25
    /// </summary>
    private static void ShowBM25UsageExample()
    {
        Console.WriteLine("📝 Code Example - Using BM25 with SemanticKernel:");
        Console.WriteLine();
        Console.WriteLine("   ```csharp");
        Console.WriteLine("   using SemanticKernel.Rankers.BM25;");
        Console.WriteLine("   using SemanticKernel.Rankers.Pipelines;");
        Console.WriteLine("   ");
        Console.WriteLine("   // Create BM25 ranker");
        Console.WriteLine("   var bm25 = new BM25Reranker();");
        Console.WriteLine("   ");
        Console.WriteLine("   // Create specialized pipeline (your requested example)");
        Console.WriteLine("   var lmRanker = new LMRanker();");
        Console.WriteLine("   var config = new BM25ThenLMRankerPipelineConfig { TopK = 20, TopM = 5 };");
        Console.WriteLine("   var pipeline = new BM25ThenLMRankerPipeline(bm25, lmRanker, config);");
        Console.WriteLine("   ");
        Console.WriteLine("   // Retrieve and rank with observability");
        Console.WriteLine("   var result = await pipeline.RetrieveAndRankAsync(query, corpus);");
        Console.WriteLine("   Console.WriteLine($\"Retrieved {result.TopMResults.Count} results in {result.BM25Time + result.LMTime}ms\");");
        Console.WriteLine("   ```");
        Console.WriteLine();

        Console.WriteLine("🏗️  Integration with SearchClient:");
        Console.WriteLine("   ```csharp");
        Console.WriteLine("   // Add ranker to SearchClient");
        Console.WriteLine("   services.AddSingleton<IRanker, BM25Reranker>();");
        Console.WriteLine("   ");
        Console.WriteLine("   // SearchClient will automatically use the ranker to rerank results");
        Console.WriteLine("   var searchClient = new SearchClient<VectorStore>(");
        Console.WriteLine("       vectorStore, embeddingGenerator, promptProvider, ");
        Console.WriteLine("       chatService, options, ranker);");
        Console.WriteLine("   ```");
        Console.WriteLine();
    }

    /// <summary>
    /// Interactive session for analyzing different query types
    /// </summary>
    private static async Task InteractiveQueryAnalysis(CancellationToken ct)
    {
        Console.WriteLine("🎮 Interactive Query Analysis");
        Console.WriteLine("Enter queries to see how BM25 would analyze them.");
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
                AnalyzeQueryForBM25(query);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error analyzing query: {ex.Message}\n");
            }
        }

        Console.WriteLine("👋 Interactive session ended.");
    }

    /// <summary>
    /// Analyzes a query from a BM25 perspective
    /// </summary>
    private static void AnalyzeQueryForBM25(string query)
    {
        Console.WriteLine($"\n🔍 BM25 Analysis for: \"{query}\"");
        Console.WriteLine(new string('─', 50));

        // Tokenize the query
        var terms = query.ToLowerInvariant()
            .Split(new char[] { ' ', ',', '.', '!', '?', ';', ':', '-', '(', ')', '[', ']' },
                   StringSplitOptions.RemoveEmptyEntries)
            .Where(t => t.Length > 2) // Filter very short terms
            .ToArray();

        Console.WriteLine($"📋 Query Terms: [{string.Join(", ", terms)}]");
        Console.WriteLine($"📊 Term Count: {terms.Length}");
        Console.WriteLine();

        Console.WriteLine("🔄 BM25 Processing Steps:");
        Console.WriteLine("1. Term Frequency Analysis:");
        var termFreq = terms.GroupBy(t => t).ToDictionary(g => g.Key, g => g.Count());
        foreach (var (term, freq) in termFreq)
        {
            Console.WriteLine($"   • '{term}': appears {freq} time(s) in query");
        }
        Console.WriteLine();

        Console.WriteLine("2. For each document, BM25 would calculate:");
        Console.WriteLine("   • Term frequency in document (TF)");
        Console.WriteLine("   • Inverse document frequency (IDF) for each term");
        Console.WriteLine("   • Document length normalization factor");
        Console.WriteLine("   • Combined score using BM25 formula");
        Console.WriteLine();

        Console.WriteLine("3. Ranking Considerations:");
        if (terms.Length == 1)
        {
            Console.WriteLine("   • Single term query: Focus on exact matches and term frequency");
        }
        else if (terms.Length <= 3)
        {
            Console.WriteLine("   • Short query: Balance between term frequency and term rarity");
        }
        else
        {
            Console.WriteLine("   • Long query: Document length normalization becomes important");
        }

        // Check for common/stop words
        var commonWords = new HashSet<string> { "the", "and", "for", "are", "but", "not", "you", "all", "can", "had", "her", "was", "one", "our", "out", "day", "get", "has", "him", "his", "how", "its", "may", "new", "now", "old", "see", "two", "who", "boy", "did", "man", "men", "run", "she", "try", "use", "way", "who", "yes", "yet" };
        var significantTerms = terms.Where(t => !commonWords.Contains(t)).ToArray();

        if (significantTerms.Length != terms.Length)
        {
            Console.WriteLine($"   • Significant terms (excluding common words): [{string.Join(", ", significantTerms)}]");
        }

        Console.WriteLine();
    }
}
