using System.Threading;
using System.Threading.Tasks;

namespace SemanticKernel.Agents.Memory.Core.Services;

/// <summary>
/// Interface for MarkitDown text extraction service
/// </summary>
public interface IMarkitDownService
{
    /// <summary>
    /// Converts a file to markdown text
    /// </summary>
    /// <param name="fileBytes">File content as bytes</param>
    /// <param name="fileName">Original file name</param>
    /// <param name="mimeType">MIME type of the file</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Extracted markdown text</returns>
    Task<string> ConvertToMarkdownAsync(byte[] fileBytes, string fileName, string mimeType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Converts a file from URL to markdown text
    /// </summary>
    /// <param name="url">URL to the file</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Extracted markdown text</returns>
    Task<string> ConvertUrlToMarkdownAsync(string url, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if the MarkitDown service is healthy
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if service is healthy</returns>
    Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default);
}
