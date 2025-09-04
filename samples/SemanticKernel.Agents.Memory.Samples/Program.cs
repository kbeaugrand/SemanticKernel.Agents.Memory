using System;
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
        Console.WriteLine("SemanticKernel.Agents.Memory Pipeline Demo with MarkitDown");
        Console.WriteLine("==========================================================");
        Console.WriteLine();
        
        // Configure services
        var services = new ServiceCollection();
        
        // Add logging
        services.AddLogging(builder => 
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
        });
        
        // Add MarkitDown text extraction services
        // Note: This assumes the MarkitDown service is running on localhost:5000
        // You can start it with: docker-compose up markitdown-service
        services.AddMarkitDownTextExtraction("http://localhost:5000");
        
        var serviceProvider = services.BuildServiceProvider();
        
        try
        {
            var (documentId, logs) = await PipelineDemo.RunAsync(serviceProvider);
            
            Console.WriteLine($"Document processed successfully! Document ID: {documentId}");
            Console.WriteLine("\nPipeline execution logs:");
            Console.WriteLine("------------------------");
            
            foreach (var log in logs)
            {
                Console.WriteLine($"[{log.Time:HH:mm:ss}] {log.Source}: {log.Text}");
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
}
