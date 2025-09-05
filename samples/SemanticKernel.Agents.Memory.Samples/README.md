# SemanticKernel.Agents.Memory.Samples

This project contains sample implementations and demonstrations for the SemanticKernel Agents Memory system.

## What's included

### PipelineDemo.cs
Contains utility methods for creating and running pipeline demonstrations:
- `RunAsync()` - Runs a complete pipeline demo with sample files
- `RunSemanticChunkingAsync()` - Demonstrates semantic chunking configuration
- `RunCustomHandlerAsync()` - Shows advanced configuration options

### Pipeline Handlers
The sample demonstrates the use of pipeline handlers from the Core package:
- `TextExtractionHandler` - Simulates text extraction from uploaded files
- `GenerateEmbeddingsHandler` - Simulates embedding generation
- `SaveRecordsHandler` - Simulates saving records to storage

These handlers are now part of the `SemanticKernel.Agents.Memory.Core` package and can be used in production scenarios.

### Program.cs
A console application that demonstrates running the pipeline with sample data. The program offers multiple demo options:
1. **Basic Pipeline Demo** - Shows standard text processing pipeline
2. **Semantic Chunking Demo** - Demonstrates semantic-based text chunking
3. **Custom Configuration Demo** - Shows advanced configuration options

## Running the sample

```bash
cd samples/SemanticKernel.Agents.Memory.Samples
dotnet run
```

This will process two sample text files through the pipeline and display the execution logs.

## Using the samples in your code

You can reference the sample handlers and demo utilities in your own projects by adding a project reference to this sample project, or by copying the code and adapting it to your needs.

```csharp
// Example usage
var (documentId, logs) = await PipelineDemo.RunAsync();
Console.WriteLine($"Processed document: {documentId}");
```

## Note

These are simplified sample implementations intended for demonstration purposes. For production use, you would implement actual text extraction, embedding generation, and persistent storage logic.
