<!--
  Purpose: concise, repo-specific guidance for AI coding agents (GitHub Copilot / assistants).
  Keep this short (20-50 lines) and focused on patterns, commands, and key integration points.
-->

# Copilot instructions for SemanticKernel.Agents.Memory

Quick intent: help contributors and AI agents be productive here — understand the pipeline architecture, common DI patterns, where to change behavior, and how to run samples.

- Big picture
  - This repo implements a memory ingestion pipeline for Semantic Kernel agents. Core pieces live under `src/*Core` and `src/*Abstractions`.
  - The orchestrator pipeline composes small handlers: TextExtraction -> Chunking -> Embeddings -> Save. See `ImportOrchestrator`, handlers in `src/SemanticKernel.Agents.Memory.Core/Handlers`, and models in `src/SemanticKernel.Agents.Memory.Abstractions`.
  - DI-first design: prefer wiring via `services.ConfigureMemoryIngestion(...)` in `src/.../Extensions/ServiceCollectionExtensions.cs` rather than instantiating handlers manually.

- Key files to reference when changing behavior
  - `src/SemanticKernel.Agents.Memory.Core/ImportOrchestrator.cs` (pipeline orchestration)
  - `src/SemanticKernel.Agents.Memory.Core/Handlers/*` (individual handler implementations: `TextExtractionHandler`, `SimpleTextChunking`, `SemanticChunking`, `GenerateEmbeddingsHandler`, `SaveRecordsHandler`)
  - `src/SemanticKernel.Agents.Memory.Abstractions/*` (models and interfaces: `Document`, `Chunk`, `ISearchClient`)
  - `samples/SemanticKernel.Agents.Memory.Samples/Program.cs` and `PipelineDemo.cs` (sample wiring and runnable entrypoint)
  - `services/markitdown-service/app.py` — external helper service used for document extraction (default URL: http://localhost:5000)

- Project-specific conventions
  - Handlers follow a small-step pattern: each transforms the pipeline state and passes it on. Keep handlers small, pure, and testable.
  - Configuration precedence in samples: User Secrets -> Environment -> appsettings.{Env}.json -> appsettings.json. See samples README and `Program.cs`.
  - Embedding generator abstraction: code falls back to `MockEmbeddingGenerator` when Azure OpenAI config is missing — tests and samples rely on this deterministic fallback.

- Common developer workflows
  - Build & run samples: `cd samples/SemanticKernel.Agents.Memory.Samples && dotnet run` (requires .NET 8+). The app uses configuration and may prompt for user input when run interactively.
  - To test embedding integration locally, either set Azure OpenAI settings via user-secrets or env vars (see samples/README) or rely on the mock generator.
  - The MarkitDown extraction service is a small Flask app in `services/markitdown-service` and listens on `http://localhost:5000` by default for richer extraction.

- Editing tips for AI patches
  - When adding new handlers: implement the handler in `Core/Handlers`, add any models to `Abstractions` only if shared, and register via `ConfigureMemoryIngestion` or tests by creating `ImportOrchestrator` directly.
  - Prefer altering options objects (e.g., `TextChunkingOptions`, `SemanticChunkingOptions`) for behavior changes rather than hard-coding values.
  - Keep public APIs stable: new overloads for `ConfigureMemoryIngestion` are preferred over breaking changes.

- Examples (short snippets / pointers)
  - Registering DI pipeline: see `samples/.../Program.cs` and `ServiceCollectionExtensions.cs` for `WithSemanticChunking()` and `WithEmbeddingsGeneration<GenerateEmbeddingsHandler>()` usage.
  - Pipeline flow: `ImportOrchestrator.PrepareNewDocumentUpload(...)` -> `RunPipelineAsync(...)` — inspect logs for each handler stage.

If anything here is unclear or you want more examples (unit tests, debugging recipes, or a checklist for adding handlers), tell me which area to expand. 
