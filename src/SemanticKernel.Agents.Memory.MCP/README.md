# SemanticKernel.Agents.Memory.MCP

[![NuGet](https://img.shields.io/nuget/v/SemanticKernel.Agents.Memory.MCP.svg)](https://www.nuget.org/packages/SemanticKernel.Agents.Memory.MCP/)
[![License](https://img.shields.io/github/license/kbeaugrand/SemanticKernel.Agents.Memory.svg)](https://github.com/kbeaugrand/SemanticKernel.Agents.Memory/blob/main/LICENSE.txt)

## Overview

This package provides Model Context Protocol (MCP) integration for the SemanticKernel Agents Memory system. It enables memory capabilities to be exposed as MCP tools and resources, allowing AI assistants and agents to interact with the memory system through the standardized MCP interface.

## Key Features

### MCP Server Integration
- **MCP Tools**: Expose memory operations as MCP tools
- **MCP Resources**: Provide memory content as MCP resources
- **Protocol Compliance**: Full compatibility with MCP specification
- **Standard Interface**: Consistent interface for AI assistants

### Memory Operations as MCP Tools
- **Search Tool**: Search memory for relevant information
- **Upload Tool**: Add documents to memory system
- **Answer Tool**: Generate answers with source citations
- **Index Management**: Create and manage memory indexes

### Resource Management
- **Document Resources**: Access stored documents through MCP
- **Chunk Resources**: Retrieve processed text chunks
- **Metadata Access**: Access document and chunk metadata
- **Dynamic Resources**: Real-time resource discovery

## Installation

```bash
dotnet add package SemanticKernel.Agents.Memory.MCP
```

## Quick Start

### MCP Server Setup

```csharp
using SemanticKernel.Agents.Memory;
using SemanticKernel.Agents.Memory.MCP;

// Configure services with memory system
var services = new ServiceCollection();

// Add memory services (see Core package docs)
services.ConfigureMemoryIngestion(options => { /* ... */ });
services.AddMemorySearchClient(vectorStore, searchOptions);

// Add MCP server
services.AddMcpServer(options =>
{
    options.ServerName = "memory-server";
    options.ServerVersion = "1.0.0";
});

var serviceProvider = services.BuildServiceProvider();

// Start MCP server
var mcpServer = serviceProvider.GetRequiredService<IMcpServer>();
await mcpServer.StartAsync();
```

### MCP Client Configuration

```json
{
  "mcpServers": {
    "memory": {
      "command": "dotnet",
      "args": ["run", "--project", "path/to/memory-mcp-server"],
      "env": {
        "AZURE_OPENAI_ENDPOINT": "https://your-resource.openai.azure.com/",
        "AZURE_OPENAI_API_KEY": "your-api-key"
      }
    }
  }
}
```

## MCP Tools

### memory_search
Search the memory system for relevant information.

```json
{
  "name": "memory_search",
  "description": "Search memory for relevant information",
  "inputSchema": {
    "type": "object",
    "properties": {
      "query": {
        "type": "string",
        "description": "Search query"
      },
      "index": {
        "type": "string",
        "description": "Memory index to search",
        "default": "default"
      },
      "maxResults": {
        "type": "integer",
        "description": "Maximum number of results",
        "default": 5
      }
    },
    "required": ["query"]
  }
}
```

### memory_upload
Upload a document to the memory system.

```json
{
  "name": "memory_upload",
  "description": "Upload a document to memory",
  "inputSchema": {
    "type": "object",
    "properties": {
      "filePath": {
        "type": "string",
        "description": "Path to the document file"
      },
      "index": {
        "type": "string",
        "description": "Memory index for storage",
        "default": "default"
      },
      "tags": {
        "type": "object",
        "description": "Metadata tags for the document"
      }
    },
    "required": ["filePath"]
  }
}
```

### memory_answer
Generate an answer with citations from memory.

```json
{
  "name": "memory_answer",
  "description": "Get an answer with citations from memory",
  "inputSchema": {
    "type": "object",
    "properties": {
      "question": {
        "type": "string",
        "description": "Question to answer"
      },
      "index": {
        "type": "string",
        "description": "Memory index to search",
        "default": "default"
      }
    },
    "required": ["question"]
  }
}
```

## MCP Resources

### Documents
Access stored documents through MCP resources.

```
memory://documents/{index}/{documentId}
```

### Chunks
Access processed text chunks.

```
memory://chunks/{index}/{chunkId}
```

### Search Results
Access search results as resources.

```
memory://search/{index}?query={searchQuery}
```

## Usage Examples

### AI Assistant Integration

```typescript
// Claude Desktop or similar MCP-enabled assistant
const memorySearch = await use_mcp_tool("memory_search", {
  query: "machine learning algorithms",
  maxResults: 3
});

const answer = await use_mcp_tool("memory_answer", {
  question: "What are the key differences between supervised and unsupervised learning?"
});
```

### Custom MCP Client

```csharp
public class MemoryMcpClient
{
    private readonly McpClient _client;

    public async Task<string[]> SearchMemoryAsync(string query, string index = "default")
    {
        var result = await _client.CallToolAsync("memory_search", new
        {
            query = query,
            index = index,
            maxResults = 5
        });

        return ParseSearchResults(result);
    }

    public async Task UploadDocumentAsync(string filePath, string index = "default")
    {
        await _client.CallToolAsync("memory_upload", new
        {
            filePath = filePath,
            index = index
        });
    }
}
```

## Advanced Configuration

### Custom MCP Tools

```csharp
services.AddMcpServer(options =>
{
    options.ServerName = "memory-server";
    
    // Add custom tools
    options.Tools.Add(new McpTool
    {
        Name = "memory_summarize",
        Description = "Summarize documents in memory",
        InputSchema = new { /* ... */ },
        Handler = async (args) => { /* custom logic */ }
    });
});
```

### Resource Providers

```csharp
services.AddMcpResourceProvider<DocumentResourceProvider>();
services.AddMcpResourceProvider<ChunkResourceProvider>();
```

## Dependencies

- SemanticKernel.Agents.Memory.Abstractions

## Related Packages

- [SemanticKernel.Agents.Memory.Abstractions](https://www.nuget.org/packages/SemanticKernel.Agents.Memory.Abstractions/) - Core interfaces and models
- [SemanticKernel.Agents.Memory.Core](https://www.nuget.org/packages/SemanticKernel.Agents.Memory.Core/) - Core implementation and orchestration
- [SemanticKernel.Agents.Memory.Plugin](https://www.nuget.org/packages/SemanticKernel.Agents.Memory.Plugin/) - Semantic Kernel plugin integration
- [SemanticKernel.Agents.Memory.Service](https://www.nuget.org/packages/SemanticKernel.Agents.Memory.Service/) - Web service implementation

## Documentation

For complete documentation, examples, and MCP protocol details, visit the [main repository](https://github.com/kbeaugrand/SemanticKernel.Agents.Memory).

## License

This project is licensed under the MIT License - see the [LICENSE.txt](https://github.com/kbeaugrand/SemanticKernel.Agents.Memory/blob/main/LICENSE.txt) file for details.
