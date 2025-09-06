namespace SemanticKernel.Agents.Memory.Samples.Configuration;

/// <summary>
/// Configuration options for Azure OpenAI service.
/// </summary>
public class AzureOpenAIOptions
{
    public const string SectionName = "AzureOpenAI";

    /// <summary>
    /// The Azure OpenAI endpoint URL.
    /// </summary>
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>
    /// The Azure OpenAI API key.
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// The embedding model deployment name.
    /// </summary>
    public string EmbeddingModel { get; set; } = "text-embedding-ada-002";

    /// <summary>
    /// The chat completion model deployment name.
    /// </summary>
    public string CompletionModel { get; set; } = "gpt-4.1-mini";

    /// <summary>
    /// Validates that the configuration is properly set.
    /// </summary>
    /// <returns>True if configuration is valid, false otherwise.</returns>
    public bool IsValid()
    {
        return !string.IsNullOrWhiteSpace(Endpoint) &&
               !string.IsNullOrWhiteSpace(ApiKey) &&
               !string.IsNullOrWhiteSpace(EmbeddingModel) &&
               !string.IsNullOrWhiteSpace(CompletionModel) &&
               !Endpoint.Contains("your-resource-name") &&
               !ApiKey.StartsWith("your-");
    }
}
