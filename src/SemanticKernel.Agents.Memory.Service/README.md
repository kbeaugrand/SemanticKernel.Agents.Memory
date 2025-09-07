# SemanticKernel.Agents.Memory.Service

[![NuGet](https://img.shields.io/nuget/v/SemanticKernel.Agents.Memory.Service.svg)](https://www.nuget.org/packages/SemanticKernel.Agents.Memory.Service/)
[![License](https://img.shields.io/github/license/kbeaugrand/SemanticKernel.Agents.Memory.svg)](https://github.com/kbeaugrand/SemanticKernel.Agents.Memory/blob/main/LICENSE.txt)

## Overview

This package provides ASP.NET Core web service implementation for the SemanticKernel Agents Memory system. It enables hosting memory ingestion and search capabilities as a web service with REST API endpoints.

## Key Features

### Web API Endpoints
- **Document Upload**: REST endpoints for document ingestion
- **Memory Search**: HTTP-based search operations
- **Answer Generation**: API endpoints for AI-generated answers with citations
- **Index Management**: Create and manage memory indexes

### Service Integration
- **ASP.NET Core Integration**: Native integration with ASP.NET Core
- **Dependency Injection**: Full DI container integration
- **Configuration**: Supports ASP.NET Core configuration patterns
- **Middleware Support**: Custom middleware for memory operations

### Scalability Features
- **Background Processing**: Async document processing
- **API Rate Limiting**: Built-in rate limiting support
- **Health Checks**: Service health monitoring
- **Logging**: Comprehensive logging integration

## Installation

```bash
dotnet add package SemanticKernel.Agents.Memory.Service
```

## Quick Start

### Basic Web Service Setup

```csharp
using SemanticKernel.Agents.Memory;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();

// Add Azure OpenAI services
builder.Services.AddAzureOpenAITextEmbeddingGeneration(
    deploymentName: builder.Configuration["AzureOpenAI:EmbeddingDeployment"]!,
    endpoint: builder.Configuration["AzureOpenAI:Endpoint"]!,
    apiKey: builder.Configuration["AzureOpenAI:ApiKey"]!
);

// Configure memory ingestion pipeline
builder.Services.ConfigureMemoryIngestion(options =>
{
    options
        .WithMarkitDownTextExtraction("http://localhost:5000")
        .WithSemanticChunking(() => new SemanticChunkingOptions
        {
            MaxChunkSize = 500,
            MinChunkSize = 100
        })
        .WithDefaultEmbeddingsGeneration()
        .WithSaveRecords(vectorStore);
});

// Add memory search client
builder.Services.AddMemorySearchClient(vectorStore, new SearchClientOptions
{
    MaxMatchesCount = 10,
    AnswerTokens = 300
});

var app = builder.Build();

// Configure the HTTP request pipeline
app.UseRouting();
app.MapControllers();

app.Run();
```

### Memory Controller

```csharp
[ApiController]
[Route("api/[controller]")]
public class MemoryController : ControllerBase
{
    private readonly ImportOrchestrator _orchestrator;
    private readonly ISearchClient _searchClient;

    public MemoryController(ImportOrchestrator orchestrator, ISearchClient searchClient)
    {
        _orchestrator = orchestrator;
        _searchClient = searchClient;
    }

    [HttpPost("upload")]
    public async Task<IActionResult> UploadDocument(
        [FromForm] IFormFile file,
        [FromForm] string index = "default")
    {
        var tempPath = Path.GetTempFileName();
        await using (var stream = new FileStream(tempPath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        var request = _orchestrator.NewDocumentUpload()
            .WithFile(tempPath)
            .WithTag("filename", file.FileName)
            .Build();

        var pipeline = _orchestrator.PrepareNewDocumentUpload(index, request);
        await _orchestrator.RunPipelineAsync(pipeline, HttpContext.RequestAborted);

        return Ok(new { Message = "Document uploaded successfully" });
    }

    [HttpGet("search")]
    public async Task<IActionResult> Search(
        [FromQuery] string query,
        [FromQuery] string index = "default",
        [FromQuery] int maxResults = 5)
    {
        var results = await _searchClient.SearchAsync(
            index, query, HttpContext.RequestAborted);

        return Ok(results.Take(maxResults));
    }

    [HttpPost("answer")]
    public async Task<IActionResult> GetAnswer(
        [FromBody] AnswerRequest request)
    {
        var answer = await _searchClient.GetAnswerAsync(
            request.Index ?? "default",
            request.Question,
            HttpContext.RequestAborted);

        return Ok(answer);
    }
}

public record AnswerRequest(string Question, string? Index = null);
```

## Configuration

### appsettings.json

```json
{
  "AzureOpenAI": {
    "Endpoint": "https://your-resource.openai.azure.com/",
    "ApiKey": "your-api-key",
    "EmbeddingDeployment": "text-embedding-ada-002",
    "ChatDeployment": "gpt-4"
  },
  "MemoryService": {
    "MarkitDownUrl": "http://localhost:5000",
    "DefaultIndex": "default",
    "MaxUploadSize": "50MB"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "SemanticKernel.Agents.Memory": "Debug"
    }
  }
}
```

## API Reference

### Upload Document
```http
POST /api/memory/upload
Content-Type: multipart/form-data

file: [document file]
index: "documents" (optional)
```

### Search Memory
```http
GET /api/memory/search?query=machine%20learning&index=default&maxResults=5
```

### Get Answer
```http
POST /api/memory/answer
Content-Type: application/json

{
  "question": "What is machine learning?",
  "index": "default"
}
```

## Docker Support

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY . .
EXPOSE 80
ENTRYPOINT ["dotnet", "YourMemoryService.dll"]
```

## Dependencies

- Microsoft.AspNetCore.App (Framework Reference)
- SemanticKernel.Agents.Memory.Abstractions

## Related Packages

- [SemanticKernel.Agents.Memory.Abstractions](https://www.nuget.org/packages/SemanticKernel.Agents.Memory.Abstractions/) - Core interfaces and models
- [SemanticKernel.Agents.Memory.Core](https://www.nuget.org/packages/SemanticKernel.Agents.Memory.Core/) - Core implementation and orchestration
- [SemanticKernel.Agents.Memory.Plugin](https://www.nuget.org/packages/SemanticKernel.Agents.Memory.Plugin/) - Semantic Kernel plugin integration
- [SemanticKernel.Agents.Memory.MCP](https://www.nuget.org/packages/SemanticKernel.Agents.Memory.MCP/) - Model Context Protocol integration

## Documentation

For complete documentation, examples, and samples, visit the [main repository](https://github.com/kbeaugrand/SemanticKernel.Agents.Memory).

## License

This project is licensed under the MIT License - see the [LICENSE.txt](https://github.com/kbeaugrand/SemanticKernel.Agents.Memory/blob/main/LICENSE.txt) file for details.
