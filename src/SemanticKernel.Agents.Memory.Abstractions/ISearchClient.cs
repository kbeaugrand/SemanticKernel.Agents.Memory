using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SemanticKernel.Agents.Memory;

/// <summary>
/// Interface for search client operations.
/// </summary>
public interface ISearchClient
{
    Task<SearchResult> SearchAsync(
        string index,
        string query,
        ICollection<MemoryFilter>? filters = null,
        double minRelevance = 0,
        int limit = -1,
        CancellationToken cancellationToken = default);

    Task<Answer> AskAsync(
        string index,
        string question,
        ICollection<MemoryFilter>? filters = null,
        double minRelevance = 0,
        CancellationToken cancellationToken = default);

    IAsyncEnumerable<Answer> AskStreamingAsync(
        string index,
        string question,
        ICollection<MemoryFilter>? filters = null,
        double minRelevance = 0,
        CancellationToken cancellationToken = default);

    Task<IEnumerable<string>> ListIndexesAsync(CancellationToken cancellationToken = default);
}
