using System.Threading;
using System.Threading.Tasks;

namespace SemanticKernel.Agents.Memory.Core;

/// <summary>
/// Interface for a pipeline step handler.
/// </summary>
public interface IPipelineStepHandler
{
    string StepName { get; }
    Task<(ReturnType Result, DataPipelineResult Pipeline)> InvokeAsync(DataPipelineResult pipeline, CancellationToken ct = default);
}
