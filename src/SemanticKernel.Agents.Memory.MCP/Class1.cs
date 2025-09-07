using System.Threading;
using System.Threading.Tasks;
using SemanticKernel.Agents.Memory;

namespace SemanticKernel.Agents.Memory.MCP;

/// <summary>
/// Model Context Protocol (MCP) server implementation for Semantic Kernel Agents Memory
/// </summary>
public class MemoryMcpServer
{
    private readonly ISearchClient _searchClient;

    public MemoryMcpServer(ISearchClient searchClient)
    {
        _searchClient = searchClient ?? throw new ArgumentNullException(nameof(searchClient));
    }

    /// <summary>
    /// Handles MCP search tool requests
    /// </summary>
    /// <param name="index">The memory index to search</param>
    /// <param name="query">The search query</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Search results in MCP format</returns>
    public async Task<string> HandleSearchToolAsync(string index, string query, CancellationToken cancellationToken = default)
    {
        var searchResult = await _searchClient.SearchAsync(index, query, cancellationToken: cancellationToken);
        return searchResult.ToJson(indented: true);
    }

    /// <summary>
    /// Handles MCP ask tool requests
    /// </summary>
    /// <param name="index">The memory index to search</param>
    /// <param name="question">The question to ask</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Answer in MCP format</returns>
    public async Task<string> HandleAskToolAsync(string index, string question, CancellationToken cancellationToken = default)
    {
        var answer = await _searchClient.AskAsync(index, question, cancellationToken: cancellationToken);
        return System.Text.Json.JsonSerializer.Serialize(answer, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
    }

    /// <summary>
    /// Lists available memory indexes
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of available indexes</returns>
    public async Task<string[]> ListIndexesAsync(CancellationToken cancellationToken = default)
    {
        var indexes = await _searchClient.ListIndexesAsync(cancellationToken);
        return indexes.ToArray();
    }
}
