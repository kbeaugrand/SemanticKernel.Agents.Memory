namespace SemanticKernel.Agents.Memory.Samples.Configuration;

/// <summary>
/// Configuration options for the pipeline.
/// </summary>
public class PipelineOptions
{
    public const string SectionName = "Pipeline";

    /// <summary>
    /// Default index name for documents.
    /// </summary>
    public string DefaultIndex { get; set; } = "docs";

    /// <summary>
    /// HTTP client timeout duration.
    /// </summary>
    public TimeSpan HttpClientTimeout { get; set; } = TimeSpan.FromMinutes(10);
}
