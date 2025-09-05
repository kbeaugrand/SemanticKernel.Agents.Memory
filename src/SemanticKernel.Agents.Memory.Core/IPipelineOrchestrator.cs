using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace SemanticKernel.Agents.Memory.Core;

/// <summary>
/// Interface for pipeline orchestrators.
/// </summary>
public interface IPipelineOrchestrator
{
    IReadOnlyList<string> HandlerNames { get; }
    Task AddHandlerAsync(IPipelineStepHandler handler, CancellationToken ct = default);
    Task<bool> TryAddHandlerAsync(IPipelineStepHandler handler, CancellationToken ct = default);
    Task<string> ImportDocumentAsync(
        string index,
        DocumentUploadRequest upload,
        IContext context,
        CancellationToken ct = default);
    DataPipelineResult PrepareNewDocumentUpload(
        string index,
        DocumentUploadRequest upload,
        IContext context);
    Task RunPipelineAsync(DataPipelineResult pipeline, CancellationToken ct = default);
}

/// <summary>
/// Base orchestrator with common functionality.
/// </summary>
public abstract class BaseOrchestrator : IPipelineOrchestrator
{
    protected readonly CancellationTokenSource CancellationTokenSource = new();
    protected readonly ILogger? _logger;

    protected readonly List<string> _defaultIngestionSteps = new()
    {
        "text-extraction",
        "text-chunking",
        "generate-embeddings",
        "save-records"
    };

    protected BaseOrchestrator(ILogger? logger = null)
    {
        _logger = logger;
    }

    public abstract IReadOnlyList<string> HandlerNames { get; }
    public abstract Task AddHandlerAsync(IPipelineStepHandler handler, CancellationToken ct = default);
    public abstract Task<bool> TryAddHandlerAsync(IPipelineStepHandler handler, CancellationToken ct = default);
    public abstract Task RunPipelineAsync(DataPipelineResult pipeline, CancellationToken ct = default);

    public virtual DataPipelineResult PrepareNewDocumentUpload(string index, DocumentUploadRequest upload, IContext context)
    {
        _logger?.LogDebug("Preparing new document upload for index '{Index}' with {FileCount} files", index, upload.Files.Count);
        
        var pipeline = new DataPipelineResult
        {
            Index = string.IsNullOrWhiteSpace(index) ? "default" : index,
            FilesToUpload = upload.Files,
            Tags = upload.Tags,
            ContextArguments = upload.Context,
        };

        foreach (var step in _defaultIngestionSteps)
        {
            pipeline.Then(step);
            _logger?.LogTrace("Added pipeline step '{StepName}' for document {DocumentId}", step, pipeline.DocumentId);
        }

        _logger?.LogInformation("Prepared pipeline for document {DocumentId} with {StepCount} steps: [{Steps}]", 
            pipeline.DocumentId, pipeline.Steps.Count, string.Join(", ", pipeline.Steps));

        return pipeline;
    }

    public virtual async Task<string> ImportDocumentAsync(string index, DocumentUploadRequest upload, IContext context, CancellationToken ct = default)
    {
        _logger?.LogInformation("Starting document import for index '{Index}' with {FileCount} files", index, upload.Files.Count);
        
        var startTime = DateTimeOffset.UtcNow;
        var pipeline = PrepareNewDocumentUpload(index, upload, context);
        
        _logger?.LogDebug("Running pipeline for document {DocumentId}", pipeline.DocumentId);
        await RunPipelineAsync(pipeline, ct).ConfigureAwait(false);
        
        var duration = DateTimeOffset.UtcNow - startTime;
        _logger?.LogInformation("Document import completed for {DocumentId} in {Duration:F1}ms", 
            pipeline.DocumentId, duration.TotalMilliseconds);
            
        return pipeline.DocumentId;
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            CancellationTokenSource?.Dispose();
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
