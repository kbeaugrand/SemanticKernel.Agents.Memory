using System.Threading;
using System.Threading.Tasks;

namespace SemanticKernel.Agents.Memory.Core.Handlers;

/// <summary>
/// Records saving pipeline step handler.
/// </summary>
public sealed class SaveRecordsHandler : IPipelineStepHandler
{
    public const string Name = "save-records";
    public string StepName => Name;

    public async Task<(ReturnType Result, DataPipelineResult Pipeline)> InvokeAsync(DataPipelineResult pipeline, CancellationToken ct = default)
    {
        // Pretend to persist records in-memory (no DB). This is a stub.
        var saved = pipeline.Files.Count;
        pipeline.Log(this, $"Saved {saved} record(s) to in-memory store.");
        await Task.Yield();
        return (ReturnType.Success, pipeline);
    }
}
