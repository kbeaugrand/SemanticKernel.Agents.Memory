using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace SemanticKernel.Agents.Memory.Core;

/// <summary>
/// In-process pipeline orchestrator for document import.
/// </summary>
public sealed class ImportOrchestrator : BaseOrchestrator
{
    private readonly ConcurrentDictionary<string, IPipelineStepHandler> _handlers = new(StringComparer.OrdinalIgnoreCase);
    private bool _disposed;

    public ImportOrchestrator(ILogger<ImportOrchestrator>? logger = null) : base(logger)
    {
    }

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

        _logger?.LogInformation("Starting pipeline execution for document {DocumentId} with {StepCount} steps: [{Steps}]", 
            pipeline.DocumentId, pipeline.RemainingSteps.Count, string.Join(", ", pipeline.RemainingSteps));

        // Simple in-memory execution: iterate through RemainingSteps and invoke handlers.
        var maxRetriesPerStep = 2;
        var totalSteps = pipeline.RemainingSteps.Count;
        var completedSteps = 0;

        while (pipeline.RemainingSteps.Count > 0)
        {
            ct.ThrowIfCancellationRequested();
            var stepName = pipeline.RemainingSteps[0];
            completedSteps++;

            _logger?.LogDebug("Executing pipeline step {CurrentStep}/{TotalSteps}: '{StepName}' for document {DocumentId}", 
                completedSteps, totalSteps, stepName, pipeline.DocumentId);

            if (!_handlers.TryGetValue(stepName, out var handler))
            {
                var errorMessage = $"No handler registered for step '{stepName}'. Registered: {string.Join(", ", HandlerNames)}";
                _logger?.LogError("Pipeline execution failed: {ErrorMessage}", errorMessage);
                throw new InvalidOperationException(errorMessage);
            }

            var stepStartTime = DateTimeOffset.UtcNow;
            var attempt = 0;
            while (true)
            {
                attempt++;
                try
                {
                    _logger?.LogTrace("Invoking handler '{HandlerName}' (attempt {Attempt}) for document {DocumentId}", 
                        handler.StepName, attempt, pipeline.DocumentId);

                    var (result, updated) = await handler.InvokeAsync(pipeline, ct).ConfigureAwait(false);
                    pipeline = updated; // allow handler to mutate/replace pipeline instance if desired

                    var stepDuration = DateTimeOffset.UtcNow - stepStartTime;
                    
                    if (result == ReturnType.Success)
                    {
                        pipeline.CompletedSteps.Add(stepName);
                        pipeline.RemainingSteps.RemoveAt(0);
                        var logMessage = $"Step completed successfully (attempt {attempt}) in {stepDuration.TotalMilliseconds:F1}ms.";
                        pipeline.Log(handler, logMessage);
                        
                        _logger?.LogInformation("Pipeline step '{StepName}' completed successfully for document {DocumentId} in {Duration:F1}ms (attempt {Attempt})", 
                            stepName, pipeline.DocumentId, stepDuration.TotalMilliseconds, attempt);
                        break; // next step
                    }
                    else if (result == ReturnType.TransientError && attempt <= maxRetriesPerStep)
                    {
                        var logMessage = $"Transient error; retrying (attempt {attempt}).";
                        pipeline.Log(handler, logMessage);
                        
                        _logger?.LogWarning("Pipeline step '{StepName}' failed with transient error for document {DocumentId}, retrying (attempt {Attempt}/{MaxRetries})", 
                            stepName, pipeline.DocumentId, attempt, maxRetriesPerStep);
                        
                        await Task.Delay(TimeSpan.FromMilliseconds(200 * attempt), ct).ConfigureAwait(false);
                        continue; // retry same step
                    }
                    else
                    {
                        var logMessage = result == ReturnType.TransientError ? "Transient error; retries exhausted." : "Fatal error.";
                        pipeline.Log(handler, logMessage);
                        
                        _logger?.LogError("Pipeline step '{StepName}' failed for document {DocumentId} with result {Result} after {Attempts} attempts in {Duration:F1}ms", 
                            stepName, pipeline.DocumentId, result, attempt, stepDuration.TotalMilliseconds);
                        
                        throw new PipelineStepFailedException(stepName, result);
                    }
                }
                catch (Exception ex) when (attempt <= maxRetriesPerStep)
                {
                    var logMessage = $"Exception: {ex.GetType().Name} {ex.Message}; retrying (attempt {attempt}).";
                    pipeline.Log(handler, logMessage);
                    
                    _logger?.LogWarning(ex, "Pipeline step '{StepName}' threw exception for document {DocumentId}, retrying (attempt {Attempt}/{MaxRetries})", 
                        stepName, pipeline.DocumentId, attempt, maxRetriesPerStep);
                    
                    await Task.Delay(TimeSpan.FromMilliseconds(200 * attempt), ct).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    var logMessage = $"Unhandled exception; aborting pipeline. {ex}";
                    pipeline.Log(handler, logMessage);
                    
                    _logger?.LogError(ex, "Pipeline step '{StepName}' failed with unhandled exception for document {DocumentId}, aborting pipeline", 
                        stepName, pipeline.DocumentId);
                    throw;
                }
            }
        }

        pipeline.Complete = true;
        pipeline.UploadComplete = true; // in this minimal sample, upload happens inline
        pipeline.Touch();
        
        var totalDuration = DateTimeOffset.UtcNow - pipeline.Creation;
        _logger?.LogInformation("Pipeline execution completed successfully for document {DocumentId} in {Duration:F1}ms. Processed {TotalSteps} steps", 
            pipeline.DocumentId, totalDuration.TotalMilliseconds, totalSteps);
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
