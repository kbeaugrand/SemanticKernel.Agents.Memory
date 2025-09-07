<!--
  Purpose: concise, repo-specific guidance for AI coding agents (GitHub Copilot / assistants).
  Keep this short (20-50 lines) and focused on patterns, commands, and key integration points.
-->

# Copilot instructions for SemanticKernel.Agents.Memory

Quick intent: help contributors and AI agents be productive here — understand the pipeline architecture, fluent DI patterns, where to change behavior, and how to run samples.

- Big picture
  - This repo implements a memory ingestion pipeline for Semantic Kernel agents. Core pieces live under `src/*Core` and `src/*Abstractions`.
  - The orchestrator pipeline composes small handlers: TextExtraction -> Chunking -> Embeddings -> Save. See `ImportOrchestrator`, handlers in `src/SemanticKernel.Agents.Memory.Core/Handlers`, and models in `src/SemanticKernel.Agents.Memory.Abstractions`.
  - DI-first design: prefer fluent configuration via `services.ConfigureMemoryIngestion(options => {...})` in `ServiceCollectionExtensions.cs` rather than instantiating handlers manually.
  - Multi-layered architecture: Abstractions (interfaces/models) -> Core (orchestration/handlers) -> Samples/Plugin/Service/MCP (higher-level integrations).

- Key files to reference when changing behavior
  - `src/SemanticKernel.Agents.Memory.Core/ImportOrchestrator.cs` (pipeline orchestration with DI handler resolution)
  - `src/SemanticKernel.Agents.Memory.Core/MemoryIngestionOptions.cs` (fluent configuration API with `WithSemanticChunking()`, `WithDefaultEmbeddingsGeneration()`)
  - `src/SemanticKernel.Agents.Memory.Core/Handlers/*` (5 core handlers: `TextExtractionHandler`, `SimpleTextChunking`, `SemanticChunking`, `GenerateEmbeddingsHandler`, `SaveRecordsHandler`)
  - `src/SemanticKernel.Agents.Memory.Abstractions/*` (shared models: `Document`, `Chunk`, `ISearchClient`, `SearchResult`, `Answer`)
  - `samples/SemanticKernel.Agents.Memory.Samples/PipelineDemo.cs` (complete fluent API examples with multiple chunking strategies)
  - `services/markitdown-service/app.py` — external Flask service for document extraction (default: http://localhost:5000)

- Project-specific conventions
  - Pipeline handlers follow strict single-responsibility: each transforms `DataPipelineResult` state and passes to next handler. Keep handlers stateless and testable.
  - Fluent configuration pattern: `options.WithMarkitDownTextExtraction(url).WithSemanticChunking(() => new SemanticChunkingOptions {...}).WithDefaultEmbeddingsGeneration().WithSaveRecords(vectorStore)`
  - Configuration precedence in samples: User Secrets -> Environment -> appsettings.{Env}.json -> appsettings.json. Use `dotnet user-secrets set` for local Azure OpenAI keys.
  - Embedding fallback: MockEmbeddingGenerator activates when Azure OpenAI config missing — essential for tests and local demos.

- Common developer workflows
  - Build & run samples: `cd samples/SemanticKernel.Agents.Memory.Samples && dotnet run` (requires .NET 8+). Interactive menu with 5 demo modes.
  - Start MarkitDown service: `python services/markitdown-service/app.py` or `docker-compose up markitdown-service`
  - Configure Azure OpenAI: `dotnet user-secrets set "AzureOpenAI:Endpoint" "https://your-resource.openai.azure.com/"` (see samples/README)
  - Solution structure: No tests directory exists yet — handlers are tested via sample integration demos.

- Editing tips for AI patches
  - Adding new handlers: implement in `Core/Handlers`, extend `MemoryIngestionOptions` with `With{HandlerName}()` method, register via handler options pattern
  - Chunking strategies: extend `TextChunkingOptions` or `SemanticChunkingOptions` for behavior changes rather than hard-coding values
  - Pipeline steps: handlers registered by step name ("text-extraction", "text-chunking", "generate-embeddings", "save-records") — order matters
  - Keep backwards compatibility: new overloads for fluent methods preferred over breaking changes to `ConfigureMemoryIngestion`

- Examples (concrete patterns from codebase)
  - Fluent registration: `options.WithSemanticChunking(() => new SemanticChunkingOptions { MaxChunkSize = 500, TitleLevelThreshold = 3 })`
  - Pipeline execution: `orchestrator.NewDocumentUpload().WithFile(path).WithTag("type", "pdf").Build()` -> `PrepareNewDocumentUpload()` -> `RunPipelineAsync()`
  - Service resolution: `ImportOrchestrator` resolves handlers from DI container at runtime using `HandlerRegistration.HandlerType`

If anything here is unclear or you want more examples (debugging recipes, adding vector stores, or extending chunking strategies), tell me which area to expand.
