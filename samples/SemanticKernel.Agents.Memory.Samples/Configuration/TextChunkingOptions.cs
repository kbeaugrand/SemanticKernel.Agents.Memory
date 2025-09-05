namespace SemanticKernel.Agents.Memory.Samples.Configuration;

/// <summary>
/// Configuration options for text chunking.
/// </summary>
public class TextChunkingConfig
{
    public const string SectionName = "TextChunking";

    /// <summary>
    /// Simple text chunking options.
    /// </summary>
    public SimpleChunkingConfig Simple { get; set; } = new();

    /// <summary>
    /// Semantic text chunking options.
    /// </summary>
    public SemanticChunkingConfig Semantic { get; set; } = new();
}

/// <summary>
/// Options for simple text chunking.
/// </summary>
public class SimpleChunkingConfig
{
    /// <summary>
    /// Maximum size of text chunks.
    /// </summary>
    public int MaxChunkSize { get; set; } = 500;

    /// <summary>
    /// Number of characters to overlap between chunks.
    /// </summary>
    public int TextOverlap { get; set; } = 50;

    /// <summary>
    /// Characters to use for splitting text.
    /// </summary>
    public string[] SplitCharacters { get; set; } = { "\n\n", "\n", ". " };
}

/// <summary>
/// Options for semantic text chunking.
/// </summary>
public class SemanticChunkingConfig
{
    /// <summary>
    /// Maximum size of text chunks.
    /// </summary>
    public int MaxChunkSize { get; set; } = 3000;

    /// <summary>
    /// Minimum size of text chunks.
    /// </summary>
    public int MinChunkSize { get; set; } = 200;

    /// <summary>
    /// Title level threshold for splitting.
    /// </summary>
    public int TitleLevelThreshold { get; set; } = 1;

    /// <summary>
    /// Whether to include title context in chunks.
    /// </summary>
    public bool IncludeTitleContext { get; set; } = true;
}
