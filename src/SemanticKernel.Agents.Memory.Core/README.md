# SemanticKernel.Agents.Memory.Core

[![NuGet](https://img.shields.io/nuget/v/SemanticKernel.Agents.Memory.Core.svg)](https://www.nuget.org/packages/SemanticKernel.Agents.Memory.Core/)
[![License](https://img.shields.io/github/license/kbeaugrand/SemanticKernel.Agents.Memory.svg)](https://github.com/kbeaugrand/SemanticKernel.Agents.Memory/blob/main/LICENSE.txt)

## Overview

This package contains the core implementation of the SemanticKernel Agents Memory pipeline. It provides the orchestration engine, pipeline handlers, and fluent configuration API for building sophisticated memory ingestion and retrieval systems.

## Key Features

### Pipeline Orchestration
- **`ImportOrchestrator`**: Central pipeline orchestrator with dependency injection support
- **Fluent Configuration API**: Intuitive configuration using `services.ConfigureMemoryIngestion()`
- **Handler Pipeline**: Modular text extraction → chunking → embeddings → storage pipeline

### Advanced Text Processing
- **Semantic Chunking**: Structure-aware chunking based on document headings and layout
- **Simple Text Chunking**: Size-based chunking with configurable overlap
- **Text Extraction**: Integration with MarkitDown service for document processing

### Pipeline Handlers
- **`TextExtractionHandler`**: Handles document text extraction
- **`SimpleTextChunking`**: Basic size-based text chunking
- **`SemanticChunking`**: Intelligent structure-aware chunking
- **`GenerateEmbeddingsHandler`**: Embedding generation for text chunks
- **`SaveRecordsHandler`**: Persistent storage of processed chunks

## Installation

```bash
dotnet add package SemanticKernel.Agents.Memory.Core
```

## Quick Start

```csharp
using Microsoft.Extensions.DependencyInjection;
using SemanticKernel.Agents.Memory;

// Configure services
var services = new ServiceCollection();

// Add Azure OpenAI services
services.AddAzureOpenAITextEmbeddingGeneration(
    deploymentName: "text-embedding-ada-002",
    endpoint: "https://your-resource.openai.azure.com/",
    apiKey: "your-api-key"
);

// Configure memory ingestion pipeline
services.ConfigureMemoryIngestion(options =>
{
    options
        .WithMarkitDownTextExtraction("http://localhost:5000")
        .WithSemanticChunking(() => new SemanticChunkingOptions
        {
            MaxChunkSize = 500,
            MinChunkSize = 100,
            TitleLevelThreshold = 3,
            IncludeTitleContext = true,
            TextOverlap = 50
        })
        .WithDefaultEmbeddingsGeneration()
        .WithSaveRecords(vectorStore);
});

// Build service provider
var serviceProvider = services.BuildServiceProvider();

// Use the orchestrator
var orchestrator = serviceProvider.GetRequiredService<ImportOrchestrator>();

var request = orchestrator.NewDocumentUpload()
    .WithFile("document.pdf")
    .WithTag("type", "technical")
    .Build();

var pipeline = orchestrator.PrepareNewDocumentUpload("index", request);
await orchestrator.RunPipelineAsync(pipeline, cancellationToken);
```

## Configuration Options

### Semantic Chunking
```csharp
.WithSemanticChunking(() => new SemanticChunkingOptions
{
    MaxChunkSize = 500,         // Maximum characters per chunk
    MinChunkSize = 100,         // Minimum chunk size threshold
    TitleLevelThreshold = 3,    // Heading levels to consider as titles
    IncludeTitleContext = true, // Include heading context in chunks
    TextOverlap = 50           // Character overlap between chunks
})
```

### Search Client Configuration
```csharp
services.AddMemorySearchClient(vectorStore, new SearchClientOptions
{
    MaxMatchesCount = 10,    // Maximum search results
    AnswerTokens = 300,      // Tokens for AI answers
    Temperature = 0.7,       // LLM creativity level
    MinRelevance = 0.6      // Minimum relevance threshold
});
```

## Dependencies

- Microsoft.Extensions.VectorData.Abstractions
- Microsoft.SemanticKernel.Core
- Microsoft.Extensions.Http
- Microsoft.Extensions.DependencyInjection.Abstractions
- Microsoft.Extensions.Logging.Abstractions
- System.Text.Json
- SemanticKernel.Agents.Memory.Abstractions

## Related Packages

- [SemanticKernel.Agents.Memory.Abstractions](https://www.nuget.org/packages/SemanticKernel.Agents.Memory.Abstractions/) - Core interfaces and models
- [SemanticKernel.Agents.Memory.Plugin](https://www.nuget.org/packages/SemanticKernel.Agents.Memory.Plugin/) - Semantic Kernel plugin integration
- [SemanticKernel.Agents.Memory.Service](https://www.nuget.org/packages/SemanticKernel.Agents.Memory.Service/) - Web service implementation
- [SemanticKernel.Agents.Memory.MCP](https://www.nuget.org/packages/SemanticKernel.Agents.Memory.MCP/) - Model Context Protocol integration

## Documentation

For complete documentation, examples, and samples, visit the [main repository](https://github.com/kbeaugrand/SemanticKernel.Agents.Memory).

## License

This project is licensed under the MIT License - see the [LICENSE.txt](https://github.com/kbeaugrand/SemanticKernel.Agents.Memory/blob/main/LICENSE.txt) file for details.
