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
        Console.WriteLine("1. Basic Pipeline Demo");
        Console.WriteLine("2. Semantic Chunking Demo");
        Console.WriteLine("3. Custom Configuration Demo");
        Console.WriteLine("4. Semantic Chunking with Custom Options");
        Console.WriteLine();
        
        // Get user choice
        int choice = GetUserChoice();
        
        try
        {
            switch (choice)
            {
                case 1:
                    await RunPipelineDemo();
                    break;
                case 2:
                    await RunSemanticChunkingDemo();
                    break;
                case 3:
                    await RunCustomConfigDemo();
                    break;
                case 4:
                    await RunSemanticChunkingConfigDemo();
                    break;
                default:
                    Console.WriteLine("Invalid choice. Running basic demo...");
                    await RunPipelineDemo();
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
            // No service provider to dispose since each demo manages its own
        }
        
        Console.WriteLine("\nPress any key to exit...");
        Console.ReadKey();
    }

    static int GetUserChoice()
    {
        Console.Write("Enter your choice (1-4): ");
        var input = Console.ReadLine();
        
        if (int.TryParse(input, out int choice) && choice >= 1 && choice <= 4)
        {
            return choice;
        }
        
        Console.WriteLine("Invalid input. Defaulting to option 1.");
        return 1;
    }

    static async Task RunPipelineDemo()
    {
        Console.WriteLine("\n=== Running Pipeline Demo ===");
        Console.WriteLine();
        Console.WriteLine("This demo shows the pipeline configuration approach.");
        Console.WriteLine();
        
        var (documentId, logs) = await PipelineDemo.RunAsync();
        
        Console.WriteLine($"Document processed successfully! Document ID: {documentId}");
        Console.WriteLine("\nPipeline execution logs:");
        Console.WriteLine("------------------------");
        
        foreach (var log in logs)
        {
            Console.WriteLine($"[{log.Time:HH:mm:ss}] {log.Source}: {log.Text}");
        }
    }

    static async Task RunSemanticChunkingDemo()
    {
        Console.WriteLine("\n=== Running Semantic Chunking Demo ===");
        Console.WriteLine();
        Console.WriteLine("This demo shows semantic chunking configuration.");
        Console.WriteLine();
        
        var (documentId, logs) = await PipelineDemo.RunSemanticChunkingAsync();
        
        Console.WriteLine($"Document processed successfully! Document ID: {documentId}");
        Console.WriteLine("\nPipeline execution logs:");
        Console.WriteLine("------------------------");
        
        foreach (var log in logs)
        {
            Console.WriteLine($"[{log.Time:HH:mm:ss}] {log.Source}: {log.Text}");
        }
    }

    static async Task RunCustomConfigDemo()
    {
        Console.WriteLine("\n=== Running Custom Configuration Demo ===");
        Console.WriteLine();
        Console.WriteLine("This demo shows advanced configuration options.");
        Console.WriteLine();
        
        var (documentId, logs) = await PipelineDemo.RunCustomHandlerAsync();
        
        Console.WriteLine($"Document processed successfully! Document ID: {documentId}");
        Console.WriteLine("\nPipeline execution logs:");
        Console.WriteLine("------------------------");
        
        foreach (var log in logs)
        {
            Console.WriteLine($"[{log.Time:HH:mm:ss}] {log.Source}: {log.Text}");
        }
    }

    static async Task RunSemanticChunkingConfigDemo()
    {
        Console.WriteLine("\n=== Running Semantic Chunking with Custom Options Demo ===");
        Console.WriteLine();
        Console.WriteLine("This demo shows semantic chunking with custom configuration options.");
        Console.WriteLine("Using: MaxChunkSize=3000, MinChunkSize=200, TitleLevelThreshold=1, IncludeTitleContext=true");
        Console.WriteLine();
        
        var (documentId, logs) = await PipelineDemo.RunSemanticChunkingConfigDemo();
        
        Console.WriteLine($"Document processed successfully! Document ID: {documentId}");
        Console.WriteLine("\nPipeline execution logs:");
        Console.WriteLine("------------------------");
        
        foreach (var log in logs)
        {
            Console.WriteLine($"[{log.Time:HH:mm:ss}] {log.Source}: {log.Text}");
        }
    }
}
