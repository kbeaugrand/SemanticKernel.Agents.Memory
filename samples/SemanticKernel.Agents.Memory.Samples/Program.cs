using System;
using System.Threading.Tasks;
using SemanticKernel.Agents.Memory.Samples;

namespace SemanticKernel.Agents.Memory.Samples;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("SemanticKernel.Agents.Memory Pipeline Demo");
        Console.WriteLine("==========================================");
        
        try
        {
            var (documentId, logs) = await PipelineDemo.RunAsync();
            
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
        }
        
        Console.WriteLine("\nPress any key to exit...");
        Console.ReadKey();
    }
}
