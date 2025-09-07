using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;
using SemanticKernel.Agents.Memory;

namespace SemanticKernel.Agents.Memory.Plugin;

/// <summary>
/// Semantic Kernel plugin for memory search operations
/// </summary>
public class MemoryPlugin
{
    private readonly ISearchClient _searchClient;

    public MemoryPlugin(ISearchClient searchClient)
    {
        _searchClient = searchClient ?? throw new ArgumentNullException(nameof(searchClient));
    }

    /// <summary>
    /// Search for relevant information in memory
    /// </summary>
    /// <param name="index">The memory index to search</param>
    /// <param name="query">The search query</param>
    /// <param name="limit">Maximum number of results to return</param>
    /// <param name="minRelevance">Minimum relevance score for results</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Search results as JSON</returns>
    [KernelFunction, Description("Search for relevant information in memory")]
    public async Task<string> SearchAsync(
        [Description("The memory index to search")] string index,
        [Description("The search query")] string query,
        [Description("Maximum number of results to return")] int limit = 5,
        [Description("Minimum relevance score for results")] double minRelevance = 0.0,
        CancellationToken cancellationToken = default)
    {
        var searchResult = await _searchClient.SearchAsync(index, query, limit: limit, minRelevance: minRelevance, cancellationToken: cancellationToken);
        return searchResult.ToJson(indented: true);
    }

    /// <summary>
    /// Ask a question and get an answer based on memory content
    /// </summary>
    /// <param name="index">The memory index to search</param>
    /// <param name="question">The question to ask</param>
    /// <param name="minRelevance">Minimum relevance score for results</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Answer as JSON</returns>
    [KernelFunction, Description("Ask a question and get an answer based on memory content")]
    public async Task<string> AskAsync(
        [Description("The memory index to search")] string index,
        [Description("The question to ask")] string question,
        [Description("Minimum relevance score for results")] double minRelevance = 0.0,
        CancellationToken cancellationToken = default)
    {
        var answer = await _searchClient.AskAsync(index, question, minRelevance: minRelevance, cancellationToken: cancellationToken);
        return System.Text.Json.JsonSerializer.Serialize(answer, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
    }

    /// <summary>
    /// List available memory indexes
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of available indexes as JSON array</returns>
    [KernelFunction, Description("List available memory indexes")]
    public async Task<string> ListIndexesAsync(CancellationToken cancellationToken = default)
    {
        var indexes = await _searchClient.ListIndexesAsync(cancellationToken);
        return System.Text.Json.JsonSerializer.Serialize(indexes, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
    }
}
