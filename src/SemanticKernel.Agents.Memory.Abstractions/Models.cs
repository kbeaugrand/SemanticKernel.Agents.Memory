using System;
using System.Text.Json.Serialization;

namespace SemanticKernel.Agents.Memory;

/// <summary>
/// Represents token usage information.
/// </summary>
public class TokenUsage
{
    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;

    [JsonPropertyName("inputTokens")]
    public int InputTokens { get; set; }

    [JsonPropertyName("outputTokens")]
    public int OutputTokens { get; set; }

    [JsonPropertyName("totalTokens")]
    public int TotalTokens { get; set; }
}

/// <summary>
/// Represents a memory filter for search operations.
/// </summary>
public class MemoryFilter
{
    [JsonPropertyName("field")]
    public string Field { get; set; } = string.Empty;

    [JsonPropertyName("value")]
    public object? Value { get; set; }

    [JsonPropertyName("operator")]
    public string Operator { get; set; } = "equals";
}

/// <summary>
/// Represents a citation in search results.
/// </summary>
public class Citation
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;

    [JsonPropertyName("source")]
    public string Source { get; set; } = string.Empty;

    [JsonPropertyName("relevanceScore")]
    public double RelevanceScore { get; set; }
}
