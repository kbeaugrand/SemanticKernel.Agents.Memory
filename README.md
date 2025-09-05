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

## Project Structure

- **`src/SemanticKernel.Agents.Memory.Abstractions`** - Core interfaces and models
- **`src/SemanticKernel.Agents.Memory.Core`** - Core pipeline implementation and orchestration
- **`src/SemanticKernel.Agents.Memory.Plugin`** - Semantic Kernel plugin integration
- **`src/SemanticKernel.Agents.Memory.Service`** - Service layer implementations
- **`src/SemanticKernel.Agents.Memory.MCP`** - Model Context Protocol integration
- **`samples/SemanticKernel.Agents.Memory.Samples`** - Sample implementations and demos

## Getting Started

### New Dependency Injection Configuration (Recommended)

Configure the memory ingestion pipeline using the fluent API:

```csharp
using Microsoft.Extensions.DependencyInjection;
using SemanticKernel.Agents.Memory.Core.Extensions;
using SemanticKernel.Agents.Memory.Core.Handlers;

var services = new ServiceCollection();
services.AddLogging();

// Configure memory ingestion pipeline
services.ConfigureMemoryIngestion(options =>
{
    options
        .WithMarkitDownTextExtraction("http://localhost:5000")
        .WithTextExtraction<TextExtractionHandler>()
        .WithSimpleTextChunking(() => new TextChunkingOptions
        {
            MaxChunkSize = 500,
            TextOverlap = 50
        })
        .WithEmbeddingsGeneration<GenerateEmbeddingsHandler>()
        .WithSaveRecords<SaveRecordsHandler>();
});

var serviceProvider = services.BuildServiceProvider();
var orchestrator = serviceProvider.GetRequiredService<ServiceImportOrchestrator>();
```

### Alternative: Semantic Chunking Configuration

```csharp
services.ConfigureMemoryIngestion(options =>
{
    options
        .WithMarkitDownTextExtraction()
        .WithTextExtraction<TextExtractionHandler>()
        .WithSemanticChunking()  // Uses document structure for intelligent chunking
        .WithEmbeddingsGeneration<GenerateEmbeddingsHandler>()
        .WithSaveRecords<SaveRecordsHandler>();
});
```

### Running the Sample

To see the memory pipeline in action, run the sample application:

```bash
cd samples/SemanticKernel.Agents.Memory.Samples
dotnet run
```

The sample application provides six demo options:

1. **Basic Pipeline Demo** - Original implementation with simple text chunking
2. **Semantic Chunking Demo** - Advanced chunking based on document structure  
3. **Chunking Strategy Comparison** - Side-by-side comparison of different chunking approaches
4. **New DI-based Pipeline Demo** - Demonstrates the new dependency injection configuration
5. **DI-based Semantic Chunking Demo** - Shows semantic chunking with DI configuration
6. **DI-based Custom Configuration Demo** - Advanced configuration options with DI

#### Semantic Chunking Features

The semantic chunking handler provides intelligent document processing:
- **Structure-aware**: Detects headings and creates chunks based on document organization
- **Configurable**: Adjust title level thresholds and chunk sizes
- **Multiple formats**: Supports Markdown (`# ## ###`), underlined headings, and numbered sections
- **Fallback handling**: Gracefully handles unstructured content with paragraph-based chunking

Example configuration:
```csharp
var semanticOptions = new SemanticChunkingOptions
{
    TitleLevelThreshold = 2,  // Split on H2 and above
    MaxChunkSize = 1500,      // Maximum characters per chunk
    MinChunkSize = 100        // Minimum characters per chunk
};
orchestrator.AddHandler(new SemanticChunking(semanticOptions));
```

## Configuration

For detailed configuration options and advanced scenarios, see [CONFIGURATION.md](CONFIGURATION.md).

### Using in Your Project

1. Add references to the core packages:
   ```xml
   <PackageReference Include="SemanticKernel.Agents.Memory.Core" Version="1.0.0" />
   <PackageReference Include="SemanticKernel.Agents.Memory.Abstractions" Version="1.0.0" />
   ```

2. Create a pipeline orchestrator and add handlers:
   ```csharp
   var orchestrator = new ImportOrchestrator();
   orchestrator.AddHandler(new TextExtractionHandler());
   
   // Choose your chunking strategy:
   // Simple chunking (size-based)
   orchestrator.AddHandler(new SimpleTextChunking(new TextChunkingOptions 
   { 
       MaxChunkSize = 1000, 
       TextOverlap = 100 
   }));
   
   // OR Semantic chunking (structure-based) - Recommended
   orchestrator.AddHandler(new SemanticChunking(new SemanticChunkingOptions
   {
       TitleLevelThreshold = 2,  // Split on H1, H2 headings
       MaxChunkSize = 1500,
       MinChunkSize = 100
   }));
   
   orchestrator.AddHandler(new GenerateEmbeddingsHandler());
   orchestrator.AddHandler(new SaveRecordsHandler());
   ```

3. Process documents through the pipeline:
   ```csharp
   var request = new DocumentUploadRequest { /* files */ };
   var pipeline = orchestrator.PrepareNewDocumentUpload("index", request, context);
   await orchestrator.RunPipelineAsync(pipeline);
   ```  

## Use Cases

- Maintaining conversational context over multiple turns  
- Recalling past interactions and user preferences  
- Storing domain-specific knowledge for improved reasoning  
- Enabling agents to learn and adapt based on historical data  

---

## License

This project is licensed under the MIT License - see the [LICENSE.txt](LICENSE.txt) file for details.
