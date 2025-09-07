# SemanticKernel.Agents.Memory.Abstractions

[![NuGet](https://img.shields.io/nuget/v/SemanticKernel.Agents.Memory.Abstractions.svg)](https://www.nuget.org/packages/SemanticKernel.Agents.Memory.Abstractions/)
[![License](https://img.shields.io/github/license/kbeaugrand/SemanticKernel.Agents.Memory.svg)](https://github.com/kbeaugrand/SemanticKernel.Agents.Memory/blob/main/LICENSE.txt)

## Overview

This package provides the core abstractions and interfaces for the SemanticKernel Agents Memory system. It defines the fundamental contracts that enable memory ingestion, search, and retrieval functionality across the entire memory pipeline.

## Key Components

### Core Models
- **`Document`**: Represents a document in the memory system with metadata and content
- **`Chunk`**: Represents a processed text chunk with embeddings and metadata
- **`SearchResult`**: Contains search results with relevance scores and source references
- **`Answer`**: Structured response containing AI-generated answers with citations
- **`SourceReference`**: Tracks the origin and location of content within documents

### Core Interfaces
- **`ISearchClient`**: Defines search operations for querying the memory system
- **`IPromptProvider`**: Provides prompts for various memory operations

## Installation

```bash
dotnet add package SemanticKernel.Agents.Memory.Abstractions
```

## Basic Usage

```csharp
using SemanticKernel.Agents.Memory;

// Use the abstractions to implement custom memory components
public class CustomSearchClient : ISearchClient
{
    public async Task<SearchResult[]> SearchAsync(
        string index, 
        string query, 
        CancellationToken cancellationToken = default)
    {
        // Your custom search implementation
    }
    
    // ... other interface members
}
```

## Dependencies

- Microsoft.SemanticKernel.Abstractions

## Related Packages

- [SemanticKernel.Agents.Memory.Core](https://www.nuget.org/packages/SemanticKernel.Agents.Memory.Core/) - Core implementation and orchestration
- [SemanticKernel.Agents.Memory.Plugin](https://www.nuget.org/packages/SemanticKernel.Agents.Memory.Plugin/) - Semantic Kernel plugin integration
- [SemanticKernel.Agents.Memory.Service](https://www.nuget.org/packages/SemanticKernel.Agents.Memory.Service/) - Web service implementation
- [SemanticKernel.Agents.Memory.MCP](https://www.nuget.org/packages/SemanticKernel.Agents.Memory.MCP/) - Model Context Protocol integration

## Documentation

For complete documentation and examples, visit the [main repository](https://github.com/kbeaugrand/SemanticKernel.Agents.Memory).

## License

This project is licensed under the MIT License - see the [LICENSE.txt](https://github.com/kbeaugrand/SemanticKernel.Agents.Memory/blob/main/LICENSE.txt) file for details.
