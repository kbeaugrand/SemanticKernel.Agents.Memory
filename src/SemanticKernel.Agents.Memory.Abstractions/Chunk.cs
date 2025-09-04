using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace SemanticKernel.Agents.Memory;

public class Chunk
{
    [JsonPropertyName("text")]
    [JsonPropertyOrder(1)]
    public string Text { get; set; } = string.Empty;

    [JsonPropertyName("relevance")]
    [JsonPropertyOrder(2)]
    public float Relevance { get; set; } = 0;

    [JsonPropertyName("chunkNumber")]
    [JsonPropertyOrder(3)]
    public int ChunkNumber { get; set; } = 0;

    [JsonPropertyName("lastUpdate")]
    [JsonPropertyOrder(10)]
    public DateTimeOffset LastUpdate { get; set; } = DateTimeOffset.MinValue;

    [JsonPropertyName("tags")]
    [JsonPropertyOrder(100)]
    public Dictionary<string, string?>? Tags { get; set; } = [];
}
