using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SemanticKernel.Agents.Memory.Core;
using SemanticKernel.Agents.Memory.Core.Handlers;

namespace SemanticKernel.Agents.Memory.Test;

/// <summary>
/// Simple test to verify semantic chunking functionality
/// </summary>
public static class SemanticChunkingTest
{
    public static async Task RunTestAsync()
    {
        Console.WriteLine("=== Semantic Chunking Test ===");
        Console.WriteLine();

        var testDocument = @"# Machine Learning Guide

This is an introduction to machine learning concepts.

## Supervised Learning

Supervised learning uses labeled data to train models. Common algorithms include:
- Linear regression
- Decision trees
- Neural networks

The key is having input-output pairs for training.

## Unsupervised Learning

Unsupervised learning finds patterns in data without labels. Examples include:
- Clustering
- Dimensionality reduction
- Association rules

This approach is useful when you don't have labeled training data.

### K-Means Clustering

K-means is a popular clustering algorithm that groups data into k clusters.

### Principal Component Analysis

PCA reduces dimensionality while preserving variance in the data.

## Deep Learning

Deep learning uses neural networks with multiple layers. It has revolutionized many fields including:
- Computer vision
- Natural language processing
- Speech recognition

### Convolutional Neural Networks

CNNs are particularly effective for image processing tasks.

### Recurrent Neural Networks

RNNs are designed for sequential data processing.
";

        // Test different configurations
        await TestConfiguration("H2 Threshold", new SemanticChunkingOptions 
        { 
            TitleLevelThreshold = 2, 
            MaxChunkSize = 1000 
        }, testDocument);

        Console.WriteLine();

        await TestConfiguration("H3 Threshold", new SemanticChunkingOptions 
        { 
            TitleLevelThreshold = 3, 
            MaxChunkSize = 1000 
        }, testDocument);

        Console.WriteLine();

        await TestConfiguration("Small Max Size", new SemanticChunkingOptions 
        { 
            TitleLevelThreshold = 2, 
            MaxChunkSize = 300 
        }, testDocument);
    }

    private static async Task TestConfiguration(string configName, SemanticChunkingOptions options, string testDocument)
    {
        Console.WriteLine($"Testing Configuration: {configName}");
        Console.WriteLine($"  - Title Level Threshold: {options.TitleLevelThreshold}");
        Console.WriteLine($"  - Max Chunk Size: {options.MaxChunkSize}");
        Console.WriteLine();

        var handler = new SemanticChunking(options);

        // Create a mock pipeline
        var pipeline = new DataPipelineResult
        {
            Index = "test",
            DocumentId = Guid.NewGuid().ToString("n"),
            ExecutionId = Guid.NewGuid().ToString("n")
        };

        // Add a file with extracted text
        var fileDetails = new FileDetails
        {
            Name = "test-doc.md",
            Size = testDocument.Length,
            MimeType = "text/markdown",
            ArtifactType = ArtifactTypes.ExtractedText
        };

        pipeline.Files.Add(fileDetails);
        pipeline.ContextArguments[$"extracted_text_{fileDetails.Id}"] = testDocument;

        // Process with semantic chunking
        var (result, processedPipeline) = await handler.InvokeAsync(pipeline, CancellationToken.None);

        if (result == ReturnType.Success)
        {
            var chunks = processedPipeline.Files.FindAll(f => f.ArtifactType == ArtifactTypes.TextPartition);
            Console.WriteLine($"Results: {chunks.Count} chunks created");
            Console.WriteLine($"Average chunk size: {(chunks.Count > 0 ? chunks.Sum(c => c.Size) / chunks.Count : 0)} characters");
            
            // Show first few chunks as examples
            for (int i = 0; i < Math.Min(3, chunks.Count); i++)
            {
                var chunkKey = $"extracted_text_{chunks[i].Id}";
                if (processedPipeline.ContextArguments.ContainsKey(chunkKey))
                {
                    var content = processedPipeline.ContextArguments[chunkKey].ToString();
                    var contentLength = content?.Length ?? 0;
                    var previewLength = Math.Min(100, contentLength);
                    Console.WriteLine($"  Chunk {i + 1} ({contentLength} chars): {content?.Substring(0, previewLength)}...");
                }
            }
        }
        else
        {
            Console.WriteLine($"Processing failed with result: {result}");
        }

        Console.WriteLine(new string('-', 50));
    }
}
