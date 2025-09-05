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

The demo code in `samples/SemanticKernel.Agents.Memory.Samples/PipelineDemo.cs` registers configuration and composes the pipeline using the fluent API. The example below mirrors the demo:

```csharp

// configure the memory ingestion pipeline
services.ConfigureMemoryIngestion(options =>
{
    options
        // use MarkitDown extraction service running locally
        .WithMarkitDownTextExtraction("http://localhost:5000")
        // semantic (structure-aware) chunking
        .WithSemanticChunking(() => new SemanticChunkingOptions
        {
            MaxChunkSize = 500,         // max characters per chunk
            MinChunkSize = 100,         // minimum characters per chunk for structure-aware splitting
            TitleLevelThreshold = 3,    // consider headings up to this level as titles
            IncludeTitleContext = true, // include heading/title text in chunk context
            TextOverlap = 50            // overlapping characters between adjacent chunks
        })
        // handler that generates embeddings
        .WithDefaultEmbeddingsGeneration(new AzureOpenAIClient(new Uri("https://<your-custom-endpoint>"), 
                                                new DefaultAzureCredential())
                                .GetEmbeddingClient("text-embedding-ada-002")
                                .AsIEmbeddingGenerator())
        // save records using an in-memory vector store instance (sample uses this for demos)
        .WithSaveRecords(new InMemoryVectorStore());
});

var serviceProvider = services.BuildServiceProvider();

 // Get the orchestrator from the service provider
var orchestrator = serviceProvider.GetRequiredService<ImportOrchestrator>();

// Create a file upload request with a large text file
var request = new DocumentUploadRequest
{
    Files =
    {
        new UploadedFile{ 
            FileName = "large-document.pdf", 
            Bytes = file.GetBytes(),
            MimeType = "application/pdf"
        }
    }
};

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

The DI example above can be configured to use semantic (structure-aware) chunking by replacing the `.WithSimpleTextChunking(...)` call with `.WithSemanticChunking(...)` and passing `SemanticChunkingOptions` (MaxChunkSize, MinChunkSize, TitleLevelThreshold, IncludeTitleContext). The sample `PipelineDemo.cs` shows concrete values and a runnable demo.

### Configuration

For detailed configuration options and advanced scenarios, see [CONFIGURATION.md](CONFIGURATION.md).

### Using in Your Project

The library can be used by composing an `ImportOrchestrator` or by wiring the DI-based pipeline as shown above. The samples demonstrate the DI-first approach and include examples of both simple and semantic chunking.

## Use Cases

- Maintaining conversational context over multiple turns  
- Recalling past interactions and user preferences  
- Storing domain-specific knowledge for improved reasoning  
- Enabling agents to learn and adapt based on historical data  

---

## License

This project is licensed under the MIT License - see the [LICENSE.txt](LICENSE.txt) file for details.
