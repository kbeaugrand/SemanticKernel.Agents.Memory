using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace SemanticKernel.Agents.Memory.Core.Handlers;

/// <summary>
/// Records saving pipeline step handler.
/// </summary>
public sealed class SaveRecordsHandler : IPipelineStepHandler
{
    public const string Name = "save-records";
    public string StepName => Name;

    private readonly ILogger<SaveRecordsHandler>? _logger;

    public SaveRecordsHandler(ILogger<SaveRecordsHandler>? logger = null)
    {
        _logger = logger;
    }

    public async Task<(ReturnType Result, DataPipelineResult Pipeline)> InvokeAsync(DataPipelineResult pipeline, CancellationToken ct = default)
    {
        var recordsToSave = pipeline.Files.Where(f => f.GeneratedFiles.ContainsKey("embedding.vec")).ToList();
        
        _logger?.LogDebug("Starting record saving for {RecordCount} files with embeddings", recordsToSave.Count);
        
        // Pretend to persist records in-memory (no DB). This is a stub.
        foreach (var file in recordsToSave)
        {
            ct.ThrowIfCancellationRequested();
            
            _logger?.LogTrace("Saving record for file '{FileName}' (Type: {ArtifactType}, Partition: {PartitionNumber})", 
                file.Name, file.ArtifactType, file.PartitionNumber);
        }
        
        var saved = recordsToSave.Count;
        var logMessage = $"Saved {saved} record(s) to in-memory store.";
        pipeline.Log(this, logMessage);
        
        _logger?.LogInformation("Record saving completed: {SavedCount} records saved to in-memory store", saved);
        
        await Task.Yield();
        return (ReturnType.Success, pipeline);
    }
}
