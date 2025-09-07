using System.Reflection;
using System.Text;
using Microsoft.Extensions.Logging;

namespace SemanticKernel.Agents.Memory.Core.Services;

/// <summary>
/// A prompt provider that reads prompts from embedded resources
/// </summary>
public class EmbeddedPromptProvider : IPromptProvider
{
    private readonly ILogger<EmbeddedPromptProvider> _logger;
    private readonly Assembly _assembly;
    private readonly string _resourcePrefix;
    private readonly Dictionary<string, string> _promptCache;

    /// <summary>
    /// Initializes a new instance of the EmbeddedPromptProvider
    /// </summary>
    /// <param name="logger">Logger instance</param>
    /// <param name="assembly">Assembly containing embedded resources (defaults to current assembly)</param>
    /// <param name="resourcePrefix">Prefix for embedded resource names (defaults to prompts namespace)</param>
    public EmbeddedPromptProvider(
        ILogger<EmbeddedPromptProvider> logger,
        Assembly? assembly = null,
        string? resourcePrefix = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _assembly = assembly ?? Assembly.GetExecutingAssembly();
        _resourcePrefix = resourcePrefix ?? "SemanticKernel.Agents.Memory.Core.Prompts";
        _promptCache = new Dictionary<string, string>();

        _logger.LogDebug("EmbeddedPromptProvider initialized with assembly: {Assembly}, prefix: {Prefix}",
            _assembly.FullName, _resourcePrefix);
    }

    /// <summary>
    /// Reads a prompt from embedded resources
    /// </summary>
    /// <param name="promptName">Name of the prompt (without .prompt extension)</param>
    /// <returns>The prompt content as a string</returns>
    /// <exception cref="ArgumentException">Thrown when promptName is null or empty</exception>
    /// <exception cref="FileNotFoundException">Thrown when the prompt resource is not found</exception>
    public string ReadPrompt(string promptName)
    {
        if (string.IsNullOrEmpty(promptName))
        {
            throw new ArgumentException("Prompt name cannot be null or empty", nameof(promptName));
        }

        // Check cache first
        if (_promptCache.TryGetValue(promptName, out var cachedPrompt))
        {
            _logger.LogDebug("Retrieved prompt '{PromptName}' from cache", promptName);
            return cachedPrompt;
        }

        // Construct the full resource name
        var resourceName = $"{_resourcePrefix}.{promptName}.prompt";

        _logger.LogDebug("Attempting to load prompt resource: {ResourceName}", resourceName);

        // Get the embedded resource
        using var stream = _assembly.GetManifestResourceStream(resourceName);

        if (stream == null)
        {
            // Log available resources for debugging
            var availableResources = _assembly.GetManifestResourceNames()
                .Where(r => r.StartsWith(_resourcePrefix))
                .ToArray();

            _logger.LogWarning("Prompt resource '{ResourceName}' not found. Available prompt resources: {AvailableResources}",
                resourceName, string.Join(", ", availableResources));

            throw new FileNotFoundException($"Embedded prompt resource '{resourceName}' not found in assembly '{_assembly.FullName}'");
        }

        // Read the content
        using var reader = new StreamReader(stream, Encoding.UTF8);
        var content = reader.ReadToEnd();

        // Cache the prompt for future use
        _promptCache[promptName] = content;

        _logger.LogDebug("Successfully loaded and cached prompt '{PromptName}' ({Length} characters)",
            promptName, content.Length);

        return content;
    }

    /// <summary>
    /// Gets all available prompt names from embedded resources
    /// </summary>
    /// <returns>Collection of available prompt names</returns>
    public IEnumerable<string> GetAvailablePrompts()
    {
        var resourceNames = _assembly.GetManifestResourceNames()
            .Where(r => r.StartsWith(_resourcePrefix) && r.EndsWith(".prompt"))
            .Select(r => r.Substring(_resourcePrefix.Length + 1, r.Length - _resourcePrefix.Length - 8)) // Remove prefix and .prompt extension
            .ToList();

        _logger.LogDebug("Found {Count} embedded prompts: {PromptNames}",
            resourceNames.Count, string.Join(", ", resourceNames));

        return resourceNames;
    }

    /// <summary>
    /// Clears the internal prompt cache
    /// </summary>
    public void ClearCache()
    {
        _promptCache.Clear();
        _logger.LogDebug("Prompt cache cleared");
    }

    /// <summary>
    /// Checks if a prompt with the given name exists
    /// </summary>
    /// <param name="promptName">Name of the prompt to check</param>
    /// <returns>True if the prompt exists, false otherwise</returns>
    public bool PromptExists(string promptName)
    {
        if (string.IsNullOrEmpty(promptName))
            return false;

        var resourceName = $"{_resourcePrefix}.{promptName}.prompt";
        using var stream = _assembly.GetManifestResourceStream(resourceName);
        return stream != null;
    }
}
