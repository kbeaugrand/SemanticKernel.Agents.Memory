# SemanticKernel.Agents.Memory.Plugin

[![NuGet](https://img.shields.io/nuget/v/SemanticKernel.Agents.Memory.Plugin.svg)](https://www.nuget.org/packages/SemanticKernel.Agents.Memory.Plugin/)
[![License](https://img.shields.io/github/license/kbeaugrand/SemanticKernel.Agents.Memory.svg)](https://github.com/kbeaugrand/SemanticKernel.Agents.Memory/blob/main/LICENSE.txt)

## Overview

This package provides a Semantic Kernel plugin integration for the SemanticKernel Agents Memory system. It enables seamless integration of memory search and retrieval capabilities directly into Semantic Kernel agents and workflows.

## Key Features

### Plugin Integration
- **Memory Search Plugin**: Native Semantic Kernel plugin for memory operations
- **Agent Integration**: Direct integration with Semantic Kernel agents
- **Function Calling**: Expose memory operations as SK functions
- **Context Management**: Automatic context handling for agent conversations

### Memory Operations
- Search memory for relevant information
- Retrieve documents and chunks
- Answer generation with source citations
- Context-aware memory queries

## Installation

```bash
dotnet add package SemanticKernel.Agents.Memory.Plugin
```

## Quick Start

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using SemanticKernel.Agents.Memory;

// Build your service collection with memory services
var services = new ServiceCollection();

// Configure memory ingestion and search (see Core package docs)
services.ConfigureMemoryIngestion(options => { /* ... */ });
services.AddMemorySearchClient(vectorStore, searchOptions);

var serviceProvider = services.BuildServiceProvider();

// Create kernel with memory plugin
var kernel = Kernel.CreateBuilder()
    .AddAzureOpenAIChatCompletion(
        deploymentName: "gpt-4",
        endpoint: "https://your-resource.openai.azure.com/",
        apiKey: "your-api-key"
    )
    .Build();

// Add memory plugin to kernel
var searchClient = serviceProvider.GetRequiredService<ISearchClient>();
kernel.ImportPluginFromObject(new MemoryPlugin(searchClient), "Memory");

// Use in agent conversations
var response = await kernel.InvokePromptAsync(
    "Search for information about {{$query}} and provide a detailed answer",
    new KernelArguments { ["query"] = "machine learning algorithms" }
);
```

## Plugin Functions

The memory plugin provides the following functions that can be called by agents:

### SearchMemory
Search the memory system for relevant information.

```csharp
[KernelFunction, Description("Search memory for relevant information")]
public async Task<string> SearchMemory(
    [Description("Search query")] string query,
    [Description("Memory index to search")] string index = "default",
    [Description("Maximum number of results")] int maxResults = 5
)
```

### GetAnswer
Get an AI-generated answer with source citations.

```csharp
[KernelFunction, Description("Get an answer with citations from memory")]
public async Task<string> GetAnswer(
    [Description("Question to answer")] string question,
    [Description("Memory index to search")] string index = "default"
)
```

## Advanced Usage

### Custom Plugin Integration

```csharp
// Create custom memory-enabled agent
public class MemoryEnabledAgent
{
    private readonly Kernel _kernel;
    private readonly ISearchClient _searchClient;

    public MemoryEnabledAgent(Kernel kernel, ISearchClient searchClient)
    {
        _kernel = kernel;
        _searchClient = searchClient;

        // Import memory plugin
        _kernel.ImportPluginFromObject(new MemoryPlugin(_searchClient), "Memory");
    }

    public async Task<string> ProcessQueryAsync(string userQuery)
    {
        var prompt = """
            User Query: {{$query}}

            First, search memory for relevant information:
            {{Memory.SearchMemory query=$query}}

            Then provide a comprehensive response based on the memory results and your knowledge.
            """;

        var result = await _kernel.InvokePromptAsync(prompt,
            new KernelArguments { ["query"] = userQuery });

        return result.ToString();
    }
}
```

### Agent Workflows

```csharp
// Memory-enhanced multi-agent workflow
var researchAgent = new Agent()
{
    Instructions = "Use the Memory plugin to search for relevant information before answering",
    Kernel = kernel
};

// Agent can now use memory functions in its reasoning
var response = await researchAgent.InvokeAsync(
    "What are the latest developments in quantum computing? " +
    "Search memory first and then provide a comprehensive analysis."
);
```

## Dependencies

- Microsoft.SemanticKernel.Core
- SemanticKernel.Agents.Memory.Abstractions

## Related Packages

- [SemanticKernel.Agents.Memory.Abstractions](https://www.nuget.org/packages/SemanticKernel.Agents.Memory.Abstractions/) - Core interfaces and models
- [SemanticKernel.Agents.Memory.Core](https://www.nuget.org/packages/SemanticKernel.Agents.Memory.Core/) - Core implementation and orchestration
- [SemanticKernel.Agents.Memory.Service](https://www.nuget.org/packages/SemanticKernel.Agents.Memory.Service/) - Web service implementation
- [SemanticKernel.Agents.Memory.MCP](https://www.nuget.org/packages/SemanticKernel.Agents.Memory.MCP/) - Model Context Protocol integration

## Documentation

For complete documentation, examples, and samples, visit the [main repository](https://github.com/kbeaugrand/SemanticKernel.Agents.Memory).

## License

This project is licensed under the MIT License - see the [LICENSE.txt](https://github.com/kbeaugrand/SemanticKernel.Agents.Memory/blob/main/LICENSE.txt) file for details.
