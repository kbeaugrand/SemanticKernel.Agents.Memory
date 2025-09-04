using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

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

    protected readonly List<string> _defaultIngestionSteps = new()
    {
        "text-extraction",
        "text-chunking",
        "generate-embeddings",
        "save-records"
    };

    public abstract IReadOnlyList<string> HandlerNames { get; }
    public abstract Task AddHandlerAsync(IPipelineStepHandler handler, CancellationToken ct = default);
    public abstract Task<bool> TryAddHandlerAsync(IPipelineStepHandler handler, CancellationToken ct = default);
    public abstract Task RunPipelineAsync(DataPipelineResult pipeline, CancellationToken ct = default);

    public virtual DataPipelineResult PrepareNewDocumentUpload(string index, DocumentUploadRequest upload, IContext context)
    {
        var pipeline = new DataPipelineResult
        {
            Index = string.IsNullOrWhiteSpace(index) ? "default" : index,
            FilesToUpload = upload.Files,
            Tags = upload.Tags,
            ContextArguments = upload.Context,
        };

        foreach (var step in _defaultIngestionSteps)
            pipeline.Then(step);

        return pipeline;
    }

    public virtual async Task<string> ImportDocumentAsync(string index, DocumentUploadRequest upload, IContext context, CancellationToken ct = default)
    {
        var pipeline = PrepareNewDocumentUpload(index, upload, context);
        await RunPipelineAsync(pipeline, ct).ConfigureAwait(false);
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
