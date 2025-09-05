using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SemanticKernel.Agents.Memory.Core.Extensions;
using SemanticKernel.Agents.Memory.Samples;

namespace SemanticKernel.Agents.Memory.Samples;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("SemanticKernel.Agents.Memory Pipeline Demo");
        Console.WriteLine("==========================================");
        Console.WriteLine();
        Console.WriteLine("Available demos:");
        Console.WriteLine("1. Basic Pipeline Demo (original)");
        Console.WriteLine("2. Semantic Chunking Demo");
        Console.WriteLine("3. Chunking Strategy Comparison");
        Console.WriteLine();
        
        // Get user choice
        int choice = GetUserChoice();
        
        // Configure services with different logging levels based on demo choice
        var services = new ServiceCollection();
        
        // Add logging with appropriate level
        services.AddLogging(builder => 
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Debug);
        });
        
        // Add MarkitDown text extraction services
        // Note: This assumes the MarkitDown service is running on localhost:5000
        // You can start it with: docker-compose up markitdown-service
        services.AddMarkitDownTextExtraction("http://localhost:5000");
        
        var serviceProvider = services.BuildServiceProvider();
        
        try
        {
            switch (choice)
            {
                case 1:
                    await RunBasicPipelineDemo(serviceProvider);
                    break;
                case 2:
                    await RunSemanticChunkingDemo(serviceProvider);
                    break;
                case 3:
                    await RunChunkingComparisonDemo(serviceProvider);
                    break;
                default:
                    Console.WriteLine("Invalid choice. Running basic demo...");
                    await RunBasicPipelineDemo(serviceProvider);
                    break;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            
            if (ex.InnerException != null)
            {
                Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
            }
        }
        finally
        {
            serviceProvider.Dispose();
        }
        
        Console.WriteLine("\nPress any key to exit...");
        Console.ReadKey();
    }

    static int GetUserChoice()
    {
        Console.Write("Enter your choice (1-3): ");
        var input = Console.ReadLine();
        
        if (int.TryParse(input, out int choice) && choice >= 1 && choice <= 3)
        {
            return choice;
        }
        
        Console.WriteLine("Invalid input. Defaulting to option 1.");
        return 1;
    }

    static async Task RunBasicPipelineDemo(IServiceProvider serviceProvider)
    {
        Console.WriteLine("\n=== Running Basic Pipeline Demo ===");
        Console.WriteLine();
        
        var (documentId, logs) = await PipelineDemo.RunAsync(serviceProvider);
        
        Console.WriteLine($"Document processed successfully! Document ID: {documentId}");
        Console.WriteLine("\nPipeline execution logs:");
        Console.WriteLine("------------------------");
        
        foreach (var log in logs)
        {
            Console.WriteLine($"[{log.Time:HH:mm:ss}] {log.Source}: {log.Text}");
        }
    }

    static async Task RunSemanticChunkingDemo(IServiceProvider serviceProvider)
    {
        Console.WriteLine("\n=== Running Semantic Chunking Demo ===");
        Console.WriteLine();
        Console.WriteLine("This demo shows how the Semantic Chunking handler processes structured documents");
        Console.WriteLine("by creating chunks based on document headings and content organization.");
        Console.WriteLine();
        
        var (documentId, logs) = await SemanticChunkingDemo.RunAsync(serviceProvider);
        
        Console.WriteLine($"Document processed successfully! Document ID: {documentId}");
        Console.WriteLine("\nPipeline execution logs:");
        Console.WriteLine("------------------------");
        
        foreach (var log in logs)
        {
            Console.WriteLine($"[{log.Time:HH:mm:ss}] {log.Source}: {log.Text}");
        }
    }

    static async Task RunChunkingComparisonDemo(IServiceProvider serviceProvider)
    {
        Console.WriteLine("\n=== Running Chunking Strategy Comparison ===");
        Console.WriteLine();
        Console.WriteLine("This demo compares different chunking strategies on the same document:");
        Console.WriteLine("- Simple Text Chunking (size-based)");
        Console.WriteLine("- Semantic Chunking with H2 threshold");
        Console.WriteLine("- Semantic Chunking with H3 threshold");
        Console.WriteLine();
        
        var results = await SemanticChunkingDemo.CompareChunkingStrategiesAsync(serviceProvider);
        
        Console.WriteLine("Comparison Results:");
        Console.WriteLine("===================");
        Console.WriteLine();
        
        Console.WriteLine($"Simple Chunking:");
        Console.WriteLine($"  - Chunks created: {results.SimpleChunking.ChunkCount}");
        Console.WriteLine($"  - Average chunk size: {results.SimpleChunking.AverageChunkSize} characters");
        Console.WriteLine();
        
        Console.WriteLine($"Semantic Chunking (H2 threshold):");
        Console.WriteLine($"  - Chunks created: {results.SemanticH2.ChunkCount}");
        Console.WriteLine($"  - Average chunk size: {results.SemanticH2.AverageChunkSize} characters");
        Console.WriteLine();
        
        Console.WriteLine($"Semantic Chunking (H3 threshold):");
        Console.WriteLine($"  - Chunks created: {results.SemanticH3.ChunkCount}");
        Console.WriteLine($"  - Average chunk size: {results.SemanticH3.AverageChunkSize} characters");
        Console.WriteLine();
        
        Console.WriteLine("Analysis:");
        Console.WriteLine("---------");
        Console.WriteLine("Semantic chunking typically creates more meaningful chunks that respect");
        Console.WriteLine("document structure, while simple chunking may break content mid-sentence");
        Console.WriteLine("or split related concepts across chunks.");
    }
}
