using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace SemanticKernel.Agents.Memory;

public class Answer
{
    /// <summary>
    /// Client question.
    /// </summary>
    [JsonPropertyName("question")]
    [JsonPropertyOrder(1)]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string Question { get; set; } = string.Empty;

    [JsonPropertyName("hasResult")]
    [JsonPropertyOrder(2)]
    public bool HasResult { get; set; } = true;

    [JsonPropertyName("text")]
    [JsonPropertyOrder(10)]
    public string Result { get; set; } = string.Empty;

    [JsonPropertyName("tokenUsage")]
    [JsonPropertyOrder(11)]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<TokenUsage>? TokenUsage { get; set; }

    [JsonPropertyName("relevantSources")]
    [JsonPropertyOrder(20)]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<SourceReference> RelevantSources { get; set; } = [];
}
