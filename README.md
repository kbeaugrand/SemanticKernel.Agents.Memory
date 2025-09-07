[![Build Status](https://github.com/kbeaugrand/SemanticKernel.Agents.Memory/actions/workflows/ci.yml/badge.svg)](https://github.com/kbeaugrand/SemanticKernel.Agents.Memory/actions/workflows/ci.yml)
[![CodeQL](https://github.com/kbeaugrand/SemanticKernel.Agents.Memory/actions/workflows/codeql-analysis.yml/badge.svg)](https://github.com/kbeaugrand/SemanticKernel.Agents.Memory/actions/workflows/codeql-analysis.yml)
[![Code Quality](https://github.com/kbeaugrand/SemanticKernel.Agents.Memory/actions/workflows/code-quality.yml/badge.svg)](https://github.com/kbeaugrand/SemanticKernel.Agents.Memory/actions/workflows/code-quality.yml)
[![License](https://img.shields.io/github/license/kbeaugrand/SemanticKernel.Agents.Memory.svg)](https://github.com/kbeaugrand/SemanticKernel.Agents.Memory/blob/main/LICENSE.txt)


[![Issues](https://img.shields.io/github/issues/kbeaugrand/SemanticKernel.Agents.Memory.svg)](https://github.com/kbeaugrand/SemanticKernel.Agents.Memory/issues)
[![Pull Requests](https://img.shields.io/github/issues-pr/kbeaugrand/SemanticKernel.Agents.Memory.svg)](https://github.com/kbeaugrand/SemanticKernel.Agents.Memory/pulls)

# SemanticKernel Memory pipeline for Agent Frameworks

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
services.AddAzureOpenAITextEmbeddingGeneration(
    deploymentName: "NAME_OF_YOUR_DEPLOYMENT", // Name of deployment, e.g. "text-embedding-ada-002".
    endpoint: "YOUR_AZURE_ENDPOINT",           // Name of Azure OpenAI service endpoint, e.g. https://myaiservice.openai.azure.com.
    apiKey: "YOUR_API_KEY",
    modelId: "MODEL_ID",          // Optional name of the underlying model if the deployment name doesn't match the model name, e.g. text-embedding-ada-002.
    serviceId: "YOUR_SERVICE_ID", // Optional; for targeting specific services within Semantic Kernel.
    dimensions: 1536              // Optional number of dimensions to generate embeddings with.
);

services.AddAzureOpenAIChatCompletion(
    deploymentName: "NAME_OF_YOUR_DEPLOYMENT",
    apiKey: "YOUR_API_KEY",
    endpoint: "YOUR_AZURE_ENDPOINT",
    modelId: "gpt-4", // Optional name of the underlying model if the deployment name doesn't match the model name
    serviceId: "YOUR_SERVICE_ID" // Optional; for targeting specific services within Semantic Kernel
);

var memoryStore = new InMemoryVectorStore(); // or your vector store implementation

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
        .WithSaveRecords(memoryStore);
});

services.AddMemorySearchClient(vectorStore, new SearchClientOptions
{
    MaxMatchesCount = 10,        // Max search results to retrieve
    AnswerTokens = 300,          // Max tokens for AI-generated answers
    Temperature = 0.7,           // LLM creativity (0.0 = deterministic, 1.0 = creative)
    MinRelevance = 0.6          // Minimum relevance score for results
});
...

// Get the orchestrator from the service provider
var orchestrator = serviceProvider.GetRequiredService<ImportOrchestrator>();

// Get the search client from the service provider
ISearchClient searchClient = serviceProvider.GetRequiredService<ISearchClient>();

// Create a file upload request using the fluent builder API
var request = orchestrator.NewDocumentUpload()
    .WithFile("path/to/document.pdf")
    .WithTag("document-type", "technical")
    .WithTag("priority", "high")
    .WithContext("source", "user-upload")
    .Build();

var pipeline = orchestrator.PrepareNewDocumentUpload(index: "default", request);
await orchestrator.RunPipelineAsync(pipeline, ct);

var searchResult = await searchClient.SearchAsync(
    index: "default",
    query: query,
    minRelevance: 0.7,
    limit: 5
);

Console.WriteLine($"Found {searchResult.Results.Count} results for: {query}");

foreach (var result in searchResult.Results)
{
    Console.WriteLine($"• {result.Source} (Score: {result.RelevanceScore:F3})");
    Console.WriteLine($"  Content: {result.Content.Substring(0, Math.Min(150, result.ContentLength))}...");
}
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

## License

This project is licensed under the MIT License - see the [LICENSE.txt](LICENSE.txt) file for details.
