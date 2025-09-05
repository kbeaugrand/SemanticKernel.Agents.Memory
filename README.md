[![Build Status](https://github.com/kbeaugrand/SemanticKernel.Agents.Memory/actions/workflows/ci.yml/badge.svg)](https://github.com/kbeaugrand/SemanticKernel.Agents.Memory/actions)
[![License](https://img.shields.io/github/license/kbeaugrand/SemanticKernel.Agents.Memory.svg)](https://github.com/kbeaugrand/SemanticKernel.Agents.Memory/blob/main/LICENSE)
[![Issues](https://img.shields.io/github/issues/kbeaugrand/SemanticKernel.Agents.Memory.svg)](https://github.com/kbeaugrand/SemanticKernel.Agents.Memory/issues)
[![Pull Requests](https://img.shields.io/github/issues-pr/kbeaugrand/SemanticKernel.Agents.Memory.svg)](https://github.com/kbeaugrand/SemanticKernel.Agents.Memory/pulls)

# SemanticKernel.Agents.Memory

This repository contains an advanced **Memory Pipeline** designed to enhance the context management and information retrieval capabilities of **Semantic Kernel** agents. The pipeline integrates various memory storage and retrieval strategies to enable agents to maintain, update, and utilize long-term and short-term memory effectively across interactions.

## Features

- **Advanced Text Chunking**: Supports both simple size-based chunking and intelligent semantic chunking based on document structure
- **Semantic Chunking**: Creates meaningful chunks by detecting document headings and structure (Markdown, underlined, numbered headings)
- **Dependency Injection**: Full integration with Microsoft.Extensions.DependencyInjection for easy configuration and testing
- **Fluent Configuration API**: Intuitive configuration syntax with `services.ConfigureMemoryIngestion(options => {...})`
- Modular memory components supporting vector stores, databases, and custom memory handlers  
- Efficient embedding and semantic search integration for context-aware retrieval  
- Support for both short-term conversation memory and long-term knowledge persistence  
- Scalable design to accommodate multi-agent systems and complex workflows  
- Easy integration with Semantic Kernel SDK and extensible architecture for custom memory logic  

## Getting Started

### Demo setup

The demo code in `samples/SemanticKernel.Agents.Memory.Samples/PipelineDemo.cs` registers configuration and composes the pipeline using the fluent API. The example below shows how to configure the pipeline:

```csharp
// Configure the memory ingestion pipeline using the fluent API
services.ConfigureMemoryIngestion(options =>
{
    options
        // Use MarkitDown extraction service running locally
        .WithMarkitDownTextExtraction("http://localhost:5000")
        // Semantic (structure-aware) chunking with lambda configuration
        .WithSemanticChunking(() => new SemanticChunkingOptions
        {
            MaxChunkSize = 500,         // Max characters per chunk
            MinChunkSize = 100,         // Minimum characters per chunk for structure-aware splitting
            TitleLevelThreshold = 3,    // Consider headings up to this level as titles
            IncludeTitleContext = true, // Include heading/title text in chunk context
            TextOverlap = 50            // Overlapping characters between adjacent chunks
        })
        // Handler that generates embeddings (uses configured Azure OpenAI or mock generator)
        .WithDefaultEmbeddingsGeneration()
        // Save records using an in-memory vector store instance (samples use this for demos)
        .WithSaveRecords(new InMemoryVectorStore());
});

var serviceProvider = services.BuildServiceProvider();

// Get the orchestrator from the service provider
var orchestrator = serviceProvider.GetRequiredService<ImportOrchestrator>();

// Create a file upload request using the fluent builder API
var request = orchestrator.NewDocumentUpload()
    .WithFile("path/to/document.pdf")
    .WithTag("document-type", "technical")
    .WithTag("priority", "high")
    .WithContext("source", "user-upload")
    .Build();

var pipeline = orchestrator.PrepareNewDocumentUpload(index: "default", request);
await orchestrator.RunPipelineAsync(pipeline, ct);
```

### Running the Sample

To see the memory pipeline in action, run the sample application:

```bash
cd samples/SemanticKernel.Agents.Memory.Samples
dotnet run
```

The sample application demonstrates several demos (see `PipelineDemo.cs`):

- Basic pipeline demo using simple, size-based chunking (`RunAsync`)
- Semantic chunking demo that uses document structure (`RunSemanticChunkingAsync`)
- Custom handler / services demo showing how to register additional services (`RunCustomHandlerAsync`)
- Semantic chunking configuration demo with fine-grained options (`RunSemanticChunkingConfigDemo`)

### Running the MarkitDown extraction service

The samples call a small helper service (MarkitDown) to extract and preprocess documents. You can run it either directly with Python or via Docker. The service listens on port 5000 by default and the samples use the URL `http://localhost:5000`.

Run with Python (recommended for development):

```bash
python3 -m venv .venv
source .venv/bin/activate
pip install -r services/markitdown-service/requirements.txt
python services/markitdown-service/app.py
```

Run with Docker:

```bash
docker build -t markitdown-service services/markitdown-service
docker run --rm -p 5000:5000 markitdown-service
```


#### Semantic Chunking

The semantic chunking approach can be configured using the fluent API by calling `.WithSemanticChunking(...)` instead of `.WithSimpleTextChunking(...)`. You can pass `SemanticChunkingOptions` directly or use a lambda function:

```csharp
// Option 1: Using lambda configuration
services.ConfigureMemoryIngestion(options =>
{
    options
        .WithMarkitDownTextExtraction()
        .WithSemanticChunking(() => new SemanticChunkingOptions
        {
            MaxChunkSize = 500,
            MinChunkSize = 100,
            TitleLevelThreshold = 3,
            IncludeTitleContext = true,
            TextOverlap = 50
        })
        .WithDefaultEmbeddingsGeneration()
        .WithSaveRecords(new InMemoryVectorStore());
});

// Option 2: Using direct options
var semanticOptions = new SemanticChunkingOptions
{
    MaxChunkSize = 500,
    MinChunkSize = 100,
    TitleLevelThreshold = 3,
    IncludeTitleContext = true,
    TextOverlap = 50
};

services.ConfigureMemoryIngestion(options =>
{
    options
        .WithMarkitDownTextExtraction()
        .WithSemanticChunking(semanticOptions)
        .WithDefaultEmbeddingsGeneration()
        .WithSaveRecords(new InMemoryVectorStore());
});
```

The sample `PipelineDemo.cs` shows concrete examples of both simple and semantic chunking configurations.

### Configuration

The fluent API provides several ways to configure the memory ingestion pipeline:

#### Basic Configuration
```csharp
services.ConfigureMemoryIngestion(options =>
{
    options
        .WithMarkitDownTextExtraction("http://localhost:5000")
        .WithSimpleTextChunking()
        .WithDefaultEmbeddingsGeneration()
        .WithSaveRecords(new InMemoryVectorStore());
});
```

#### Advanced Configuration with Custom Options
```csharp
services.ConfigureMemoryIngestion(options =>
{
    options
        .WithMarkitDownTextExtraction("http://localhost:5000")
        .WithSimpleTextChunking(new TextChunkingOptions 
        { 
            MaxChunkSize = 1000,
            TextOverlap = 100
        })
        .WithEmbeddingsGeneration<CustomEmbeddingsHandler>()
        .WithSaveRecords(new CustomVectorStore())
        .WithServices(services => 
        {
            // Add custom services if needed
            services.AddSingleton<ICustomService, CustomService>();
        });
});
```

#### Using Custom Handlers
```csharp
services.ConfigureMemoryIngestion(options =>
{
    options
        .WithHandler<CustomTextExtractionHandler>("text-extraction")
        .WithTextChunking<CustomChunkingHandler>()
        .WithEmbeddingsGeneration<CustomEmbeddingsHandler>()
        .WithHandler<CustomSaveHandler>("save-records");
});
```

#### Fluent Document Upload API

The library provides a fluent builder API for creating document upload requests:

```csharp
// Single file upload with metadata
var request = orchestrator.NewDocumentUpload()
    .WithFile("document.pdf")
    .WithTag("category", "technical")
    .WithTag("priority", "high")
    .WithContext("source", "user-upload")
    .WithContext("timestamp", DateTime.UtcNow)
    .Build();

// Multiple files with different sources
var request = orchestrator.NewDocumentUpload()
    .WithFile("report.pdf")
    .WithFile("summary.docx")
    .WithFile("data.txt", customFileName: "processed-data.txt")
    .WithTags(new Dictionary<string, string>
    {
        ["batch"] = "quarterly-reports",
        ["department"] = "finance"
    })
    .Build();

// From byte arrays or streams
var request = orchestrator.NewDocumentUpload()
    .WithFile("document.pdf", pdfBytes, "application/pdf")
    .WithFile("image.png", imageStream)
    .Build();

// Async file operations for large files
var request = await orchestrator.NewDocumentUpload()
    .WithFileAsync("large-document.pdf")
    .WithFilesAsync(new[] { "file1.txt", "file2.txt" });
```

#### Convenience Methods for Direct Processing

For simpler scenarios, you can use convenience methods that combine building and processing:

```csharp
// Direct file upload and processing
string documentId = await orchestrator.UploadFileAsync(
    index: "documents",
    filePath: "report.pdf",
    context: new MyContext());

// Upload multiple files at once
string documentId = await orchestrator.UploadFilesAsync(
    index: "documents",
    filePaths: new[] { "file1.pdf", "file2.docx" },
    context: new MyContext());

// Upload from byte array with automatic processing
string documentId = await orchestrator.UploadFileAsync(
    index: "documents",
    fileName: "document.pdf",
    bytes: pdfBytes,
    context: new MyContext(),
    customMimeType: "application/pdf");

// Advanced processing with full pipeline result
var (documentId, logs) = await orchestrator.ProcessUploadAsync(
    index: "documents",
    builder: orchestrator.NewDocumentUpload()
        .WithFile("document.pdf")
        .WithTag("processed", "true"),
    context: new MyContext());
```
```

For detailed configuration options and advanced scenarios, see [CONFIGURATION.md](CONFIGURATION.md).

### Using in Your Project

The recommended approach is to use the DI-based fluent API for configuring the memory ingestion pipeline. This provides a clean, declarative way to compose your pipeline:

```csharp
// In your Startup.cs or Program.cs
services.ConfigureMemoryIngestion(options =>
{
    options
        .WithMarkitDownTextExtraction()  // Optional: for document extraction
        .WithSemanticChunking()          // or .WithSimpleTextChunking()
        .WithDefaultEmbeddingsGeneration() // Requires Azure OpenAI configuration
        .WithSaveRecords(vectorStore);      // Your vector store instance
});

// In your application code
public class DocumentProcessor
{
    private readonly ImportOrchestrator _orchestrator;

    public DocumentProcessor(ImportOrchestrator orchestrator)
    {
        _orchestrator = orchestrator;
    }

    public async Task ProcessDocumentAsync(string filePath, string indexName = "default")
    {
        // Use the fluent builder API for document upload
        var request = _orchestrator.NewDocumentUpload()
            .WithFile(filePath)
            .WithTag("source", "api")
            .WithContext("timestamp", DateTime.UtcNow)
            .Build();

        var pipeline = _orchestrator.PrepareNewDocumentUpload(indexName, request);
        await _orchestrator.RunPipelineAsync(pipeline);
    }

    public async Task ProcessMultipleDocumentsAsync(string[] filePaths, string indexName = "default")
    {
        var builder = _orchestrator.NewDocumentUpload();
        
        // Add multiple files with the fluent API
        foreach (var filePath in filePaths)
        {
            builder.WithFile(filePath);
        }

        var request = builder
            .WithTag("batch", "multi-upload")
            .WithContext("fileCount", filePaths.Length)
            .Build();

        var pipeline = _orchestrator.PrepareNewDocumentUpload(indexName, request);
        await _orchestrator.RunPipelineAsync(pipeline);
    }

    public async Task ProcessDocumentFromBytesAsync(byte[] fileBytes, string fileName, string mimeType)
    {
        var request = _orchestrator.NewDocumentUpload()
            .WithFile(fileName, fileBytes, mimeType)
            .WithTag("format", "binary")
            .Build();

        var pipeline = _orchestrator.PrepareNewDocumentUpload("default", request);
        await _orchestrator.RunPipelineAsync(pipeline);
    }

    // Convenience methods for simpler scenarios
    public async Task<string> QuickUploadAsync(string filePath)
    {
        return await _orchestrator.UploadFileAsync("default", filePath, new NoopContext());
    }

    public async Task<string> QuickBatchUploadAsync(string[] filePaths)
    {
        return await _orchestrator.UploadFilesAsync("default", filePaths, new NoopContext());
    }
}
```

The library can also be used by manually composing an `ImportOrchestrator`, but the DI-first approach is recommended for most scenarios. The samples demonstrate both approaches with examples of simple and semantic chunking configurations.

## Use Cases

- Maintaining conversational context over multiple turns  
- Recalling past interactions and user preferences  
- Storing domain-specific knowledge for improved reasoning  
- Enabling agents to learn and adapt based on historical data  

---

## License

This project is licensed under the MIT License - see the [LICENSE.txt](LICENSE.txt) file for details.
