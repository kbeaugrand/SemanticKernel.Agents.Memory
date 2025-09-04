[![Build Status](https://github.com/kbeaugrand/SemanticKernel.Agents.Memory/actions/workflows/ci.yml/badge.svg)](https://github.com/kbeaugrand/SemanticKernel.Agents.Memory/actions)
[![License](https://img.shields.io/github/license/kbeaugrand/SemanticKernel.Agents.Memory.svg)](https://github.com/kbeaugrand/SemanticKernel.Agents.Memory/blob/main/LICENSE)
[![Issues](https://img.shields.io/github/issues/kbeaugrand/SemanticKernel.Agents.Memory.svg)](https://github.com/kbeaugrand/SemanticKernel.Agents.Memory/issues)
[![Pull Requests](https://img.shields.io/github/issues-pr/kbeaugrand/SemanticKernel.Agents.Memory.svg)](https://github.com/kbeaugrand/SemanticKernel.Agents.Memory/pulls)

# SemanticKernel.Agents.Memory

This repository contains an advanced **Memory Pipeline** designed to enhance the context management and information retrieval capabilities of **Semantic Kernel** agents. The pipeline integrates various memory storage and retrieval strategies to enable agents to maintain, update, and utilize long-term and short-term memory effectively across interactions.

## Features

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

### Running the Sample

To see the memory pipeline in action, run the sample application:

```bash
cd samples/SemanticKernel.Agents.Memory.Samples
dotnet run
```

This will demonstrate a complete pipeline processing sample documents through text extraction, embedding generation, and storage steps.

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
