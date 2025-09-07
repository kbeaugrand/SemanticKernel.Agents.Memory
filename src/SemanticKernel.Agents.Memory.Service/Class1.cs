using Microsoft.AspNetCore.Mvc;
using SemanticKernel.Agents.Memory;

namespace SemanticKernel.Agents.Memory.Service;

/// <summary>
/// API controller for memory search operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class MemoryController : ControllerBase
{
    private readonly ISearchClient _searchClient;

    public MemoryController(ISearchClient searchClient)
    {
        _searchClient = searchClient ?? throw new ArgumentNullException(nameof(searchClient));
    }

    /// <summary>
    /// Search for relevant information in memory
    /// </summary>
    /// <param name="request">Search request parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Search results</returns>
    [HttpPost("search")]
    public async Task<ActionResult<SearchResult>> SearchAsync([FromBody] SearchRequest request, CancellationToken cancellationToken = default)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Index) || string.IsNullOrWhiteSpace(request.Query))
        {
            return BadRequest("Index and Query are required");
        }

        var result = await _searchClient.SearchAsync(
            request.Index,
            request.Query,
            request.Filters,
            request.MinRelevance,
            request.Limit,
            cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Ask a question and get an answer based on memory content
    /// </summary>
    /// <param name="request">Ask request parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Answer</returns>
    [HttpPost("ask")]
    public async Task<ActionResult<Answer>> AskAsync([FromBody] AskRequest request, CancellationToken cancellationToken = default)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Index) || string.IsNullOrWhiteSpace(request.Question))
        {
            return BadRequest("Index and Question are required");
        }

        var result = await _searchClient.AskAsync(
            request.Index,
            request.Question,
            request.Filters,
            request.MinRelevance,
            cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// List available memory indexes
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of available indexes</returns>
    [HttpGet("indexes")]
    public async Task<ActionResult<IEnumerable<string>>> ListIndexesAsync(CancellationToken cancellationToken = default)
    {
        var indexes = await _searchClient.ListIndexesAsync(cancellationToken);
        return Ok(indexes);
    }
}

/// <summary>
/// Request model for search operations
/// </summary>
public class SearchRequest
{
    public string Index { get; set; } = string.Empty;
    public string Query { get; set; } = string.Empty;
    public ICollection<MemoryFilter>? Filters { get; set; }
    public double MinRelevance { get; set; } = 0.0;
    public int Limit { get; set; } = 10;
}

/// <summary>
/// Request model for ask operations
/// </summary>
public class AskRequest
{
    public string Index { get; set; } = string.Empty;
    public string Question { get; set; } = string.Empty;
    public ICollection<MemoryFilter>? Filters { get; set; }
    public double MinRelevance { get; set; } = 0.0;
}
