namespace SemanticKernel.Agents.Memory.Samples.Configuration;

/// <summary>
/// Configuration options for MarkitDown service.
/// </summary>
public class MarkitDownOptions
{
    public const string SectionName = "MarkitDown";

    /// <summary>
    /// The MarkitDown service URL.
    /// </summary>
    public string ServiceUrl { get; set; } = "http://localhost:5000";
}
