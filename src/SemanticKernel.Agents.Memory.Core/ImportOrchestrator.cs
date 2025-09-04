using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SemanticKernel.Agents.Memory.Core;

/// <summary>
/// In-process pipeline orchestrator for document import.
/// </summary>
public sealed class ImportOrchestrator : BaseOrchestrator
{
    private readonly ConcurrentDictionary<string, IPipelineStepHandler> _handlers = new(StringComparer.OrdinalIgnoreCase);
    private bool _disposed;

    public override IReadOnlyList<string> HandlerNames => _handlers.Keys.OrderBy(k => k).ToArray();

    public override Task AddHandlerAsync(IPipelineStepHandler handler, CancellationToken ct = default)
    {
        _handlers[handler.StepName] = handler;
        return Task.CompletedTask;
    }

    public override Task<bool> TryAddHandlerAsync(IPipelineStepHandler handler, CancellationToken ct = default)
    {
        var added = _handlers.TryAdd(handler.StepName, handler);
        return Task.FromResult(added);
    }

    public void AddHandler<T>(T handler) where T : IPipelineStepHandler => _handlers[handler.StepName] = handler;
    public void AddHandler(IPipelineStepHandler handler) => _handlers[handler.StepName] = handler;

    public override async Task RunPipelineAsync(DataPipelineResult pipeline, CancellationToken ct = default)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(ImportOrchestrator));

        // Simple in-memory execution: iterate through RemainingSteps and invoke handlers.
        var maxRetriesPerStep = 2;

        while (pipeline.RemainingSteps.Count > 0)
        {
            ct.ThrowIfCancellationRequested();
            var stepName = pipeline.RemainingSteps[0];

            if (!_handlers.TryGetValue(stepName, out var handler))
                throw new InvalidOperationException($"No handler registered for step '{stepName}'. Registered: {string.Join(", ", HandlerNames)}");

            var attempt = 0;
            while (true)
            {
                attempt++;
                try
                {
                    var (result, updated) = await handler.InvokeAsync(pipeline, ct).ConfigureAwait(false);
                    pipeline = updated; // allow handler to mutate/replace pipeline instance if desired

                    if (result == ReturnType.Success)
                    {
                        pipeline.CompletedSteps.Add(stepName);
                        pipeline.RemainingSteps.RemoveAt(0);
                        pipeline.Log(handler, $"Step completed successfully (attempt {attempt}).");
                        break; // next step
                    }
                    else if (result == ReturnType.TransientError && attempt <= maxRetriesPerStep)
                    {
                        pipeline.Log(handler, $"Transient error; retrying (attempt {attempt}).");
                        await Task.Delay(TimeSpan.FromMilliseconds(200 * attempt), ct).ConfigureAwait(false);
                        continue; // retry same step
                    }
                    else
                    {
                        pipeline.Log(handler, result == ReturnType.TransientError ? "Transient error; retries exhausted." : "Fatal error.");
                        throw new PipelineStepFailedException(stepName, result);
                    }
                }
                catch (Exception ex) when (attempt <= maxRetriesPerStep)
                {
                    pipeline.Log(handler, $"Exception: {ex.GetType().Name} {ex.Message}; retrying (attempt {attempt}).");
                    await Task.Delay(TimeSpan.FromMilliseconds(200 * attempt), ct).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    pipeline.Log(handler, $"Unhandled exception; aborting pipeline. {ex}");
                    throw;
                }
            }
        }

        pipeline.Complete = true;
        pipeline.UploadComplete = true; // in this minimal sample, upload happens inline
        pipeline.Touch();
    }

    /// <summary>
    /// Legacy method for backward compatibility with the queue-based approach.
    /// </summary>
    /// <param name="context">The import context to process.</param>
    /// <returns>A task that completes when the import is finished, with the result indicating success.</returns>
    public async Task<bool> QueueImportAsync(ImportContext context)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(ImportOrchestrator));

        if (context.UploadRequest == null)
            return false;

        try
        {
            var documentId = await ImportDocumentAsync(context.Index, context.UploadRequest, context);
            return !string.IsNullOrEmpty(documentId);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Legacy method for backward compatibility.
    /// </summary>
    public async Task<bool> ExecuteAsync(ImportContext context)
    {
        return await QueueImportAsync(context);
    }

    /// <summary>
    /// Stops accepting new imports and waits for existing ones to complete.
    /// </summary>
    public async Task StopAsync()
    {
        await Task.CompletedTask; // No background processing in this implementation
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing && !_disposed)
        {
            _disposed = true;
            CancellationTokenSource.Cancel();
        }
        base.Dispose(disposing);
    }
}
