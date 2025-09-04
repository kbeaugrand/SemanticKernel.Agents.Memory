# SemanticKernel.Agents.Memory.Samples

This project contains sample implementations and demonstrations for the SemanticKernel Agents Memory system.

## What's included

### PipelineDemo.cs
Contains utility methods for creating and running pipeline demonstrations:
- `RunAsync()` - Runs a complete pipeline demo with sample files
- `CreateTestContext()` - Creates a test import context
- `CreateSampleDocument()` - Creates sample documents for testing

### Pipeline Handlers
The sample demonstrates the use of pipeline handlers from the Core package:
- `TextExtractionHandler` - Simulates text extraction from uploaded files
- `GenerateEmbeddingsHandler` - Simulates embedding generation
- `SaveRecordsHandler` - Simulates saving records to storage

These handlers are now part of the `SemanticKernel.Agents.Memory.Core` package and can be used in production scenarios.

### Program.cs
A console application that demonstrates running the pipeline with sample data.

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
