using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
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

        // Build configuration
        var configuration = BuildConfiguration();

        Console.WriteLine("Available demos:");
        Console.WriteLine("1. Basic Pipeline Demo");
        Console.WriteLine("2. Semantic Chunking Demo");
        Console.WriteLine("3. Custom Configuration Demo");
        Console.WriteLine("4. Semantic Chunking with Custom Options");
        Console.WriteLine("5. Complete Flow Demo (Ingestion + Q&A)");
        Console.WriteLine();

        // Get user choice
        int choice = GetUserChoice();

        try
        {
            switch (choice)
            {
                case 1:
                    await RunPipelineDemo(configuration);
                    break;
                case 2:
                    await RunSemanticChunkingDemo(configuration);
                    break;
                case 3:
                    await RunCustomConfigDemo(configuration);
                    break;
                case 4:
                    await RunSemanticChunkingConfigDemo(configuration);
                    break;
                case 5:
                    await CompleteFlowDemo.RunAsync(configuration);
                    break;
                default:
                    Console.WriteLine("Invalid choice. Running basic demo...");
                    await RunPipelineDemo(configuration);
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

    static IConfiguration BuildConfiguration()
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development"}.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables();

        return builder.Build();
    }

    static int GetUserChoice()
    {
        Console.Write("Enter your choice (1-5): ");
        var input = Console.ReadLine();

        if (int.TryParse(input, out int choice) && choice >= 1 && choice <= 5)
        {
            return choice;
        }

        Console.WriteLine("Invalid input. Defaulting to option 1.");
        return 1;
    }

    static async Task RunPipelineDemo(IConfiguration configuration)
    {
        Console.WriteLine("\n=== Running Pipeline Demo ===");
        Console.WriteLine();
        Console.WriteLine("This demo shows both pipeline configuration approach and fluent API examples.");
        Console.WriteLine();

        // Run the original pipeline demo
        Console.WriteLine("1. Traditional Pipeline Configuration:");
        var (documentId1, logs1) = await PipelineDemo.RunAsync(configuration);

        Console.WriteLine($"Document processed successfully! Document ID: {documentId1}");
        Console.WriteLine("\nPipeline execution logs:");
        Console.WriteLine("------------------------");

        foreach (var log in logs1)
        {
            Console.WriteLine($"[{log.Time:HH:mm:ss}] {log.Source}: {log.Text}");
        }

        Console.WriteLine();
        Console.WriteLine("2. Fluent API Example - Multiple Upload Methods:");
        var (documentId2, logs2) = await PipelineDemo.RunFluentApiDemo(configuration);

        Console.WriteLine($"Document processed successfully! Document ID: {documentId2}");
        Console.WriteLine("\nPipeline execution logs:");
        Console.WriteLine("------------------------");

        foreach (var log in logs2)
        {
            Console.WriteLine($"[{log.Time:HH:mm:ss}] {log.Source}: {log.Text}");
        }

        Console.WriteLine();
        Console.WriteLine("3. Fluent API Example - File Path Uploads:");
        var (documentId3, logs3) = await PipelineDemo.RunFluentApiFilePathDemo(configuration);

        Console.WriteLine($"Document processed successfully! Document ID: {documentId3}");
        Console.WriteLine("\nPipeline execution logs:");
        Console.WriteLine("------------------------");

        foreach (var log in logs3)
        {
            Console.WriteLine($"[{log.Time:HH:mm:ss}] {log.Source}: {log.Text}");
        }
    }

    static async Task RunSemanticChunkingDemo(IConfiguration configuration)
    {
        Console.WriteLine("\n=== Running Semantic Chunking Demo ===");
        Console.WriteLine();
        Console.WriteLine("This demo shows semantic chunking configuration.");
        Console.WriteLine();

        var (documentId, logs) = await PipelineDemo.RunSemanticChunkingAsync(configuration);

        Console.WriteLine($"Document processed successfully! Document ID: {documentId}");
        Console.WriteLine("\nPipeline execution logs:");
        Console.WriteLine("------------------------");

        foreach (var log in logs)
        {
            Console.WriteLine($"[{log.Time:HH:mm:ss}] {log.Source}: {log.Text}");
        }
    }

    static async Task RunCustomConfigDemo(IConfiguration configuration)
    {
        Console.WriteLine("\n=== Running Custom Configuration Demo ===");
        Console.WriteLine();
        Console.WriteLine("This demo shows advanced configuration options.");
        Console.WriteLine();

        var (documentId, logs) = await PipelineDemo.RunCustomHandlerAsync(configuration);

        Console.WriteLine($"Document processed successfully! Document ID: {documentId}");
        Console.WriteLine("\nPipeline execution logs:");
        Console.WriteLine("------------------------");

        foreach (var log in logs)
        {
            Console.WriteLine($"[{log.Time:HH:mm:ss}] {log.Source}: {log.Text}");
        }
    }

    static async Task RunSemanticChunkingConfigDemo(IConfiguration configuration)
    {
        Console.WriteLine("\n=== Running Semantic Chunking with Custom Options Demo ===");
        Console.WriteLine();
        Console.WriteLine("This demo shows semantic chunking with custom configuration options.");

        // Display current configuration values
        var chunkingConfig = configuration.GetSection("TextChunking:Semantic");
        Console.WriteLine($"Using: MaxChunkSize={chunkingConfig["MaxChunkSize"]}, MinChunkSize={chunkingConfig["MinChunkSize"]}, TitleLevelThreshold={chunkingConfig["TitleLevelThreshold"]}, IncludeTitleContext={chunkingConfig["IncludeTitleContext"]}");
        Console.WriteLine();

        var (documentId, logs) = await PipelineDemo.RunSemanticChunkingConfigDemo(configuration);

        Console.WriteLine($"Document processed successfully! Document ID: {documentId}");
        Console.WriteLine("\nPipeline execution logs:");
        Console.WriteLine("------------------------");

        foreach (var log in logs)
        {
            Console.WriteLine($"[{log.Time:HH:mm:ss}] {log.Source}: {log.Text}");
        }
    }
}
