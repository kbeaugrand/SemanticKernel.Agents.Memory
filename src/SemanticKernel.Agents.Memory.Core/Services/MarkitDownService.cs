using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace SemanticKernel.Agents.Memory.Core.Services;

/// <summary>
/// HTTP client implementation for MarkitDown service
/// </summary>
public sealed class MarkitDownService : IMarkitDownService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<MarkitDownService> _logger;
    private readonly string _baseUrl;

    /// <summary>
    /// Initializes a new instance of the MarkitDownService
    /// </summary>
    /// <param name="httpClient">HTTP client instance</param>
    /// <param name="logger">Logger instance</param>
    /// <param name="baseUrl">Base URL of the MarkitDown service (default: http://localhost:5000)</param>
    public MarkitDownService(HttpClient httpClient, ILogger<MarkitDownService> logger, string baseUrl = "http://localhost:5000")
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _baseUrl = baseUrl.TrimEnd('/');
    }

    /// <inheritdoc />
    public async Task<string> ConvertToMarkdownAsync(byte[] fileBytes, string fileName, string mimeType, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Converting file {FileName} ({MimeType}, {Size} bytes) to markdown",
                fileName, mimeType, fileBytes.Length);

            using var form = new MultipartFormDataContent();
            using var fileContent = new ByteArrayContent(fileBytes);

            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(mimeType ?? "application/octet-stream");
            form.Add(fileContent, "file", fileName);
            form.Add(new StringContent(fileName), "filename");

            var response = await _httpClient.PostAsync($"{_baseUrl}/convert", form, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("MarkitDown service returned error {StatusCode}: {Error}",
                    response.StatusCode, errorContent);
                throw new HttpRequestException($"MarkitDown service error: {response.StatusCode} - {errorContent}");
            }

            var jsonResponse = await response.Content.ReadAsStringAsync(cancellationToken);
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            var result = JsonSerializer.Deserialize<MarkitDownResponse>(jsonResponse, options);

            if (result?.Success != true)
            {
                var error = result?.Error ?? "Unknown error";
                _logger.LogError("MarkitDown conversion failed: {Error}", error);
                throw new InvalidOperationException($"MarkitDown conversion failed: {error}");
            }

            _logger.LogInformation("Successfully converted {FileName} to markdown ({MarkdownSize} characters)",
                fileName, result.Markdown?.Length ?? 0);

            return result.Markdown ?? string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error converting file {FileName} to markdown", fileName);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<string> ConvertUrlToMarkdownAsync(string url, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Converting URL {Url} to markdown", url);

            var request = new { url };
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{_baseUrl}/convert-url", content, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("MarkitDown service returned error {StatusCode}: {Error}",
                    response.StatusCode, errorContent);
                throw new HttpRequestException($"MarkitDown service error: {response.StatusCode} - {errorContent}");
            }

            var jsonResponse = await response.Content.ReadAsStringAsync(cancellationToken);
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            var result = JsonSerializer.Deserialize<MarkitDownUrlResponse>(jsonResponse, options);

            if (result?.Success != true)
            {
                var error = result?.Error ?? "Unknown error";
                _logger.LogError("MarkitDown URL conversion failed: {Error}", error);
                throw new InvalidOperationException($"MarkitDown URL conversion failed: {error}");
            }

            _logger.LogInformation("Successfully converted URL {Url} to markdown ({MarkdownSize} characters)",
                url, result.Markdown?.Length ?? 0);

            return result.Markdown ?? string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error converting URL {Url} to markdown", url);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/health", cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Health check failed for MarkitDown service");
            return false;
        }
    }

    /// <summary>
    /// Response model for file conversion
    /// </summary>
    private sealed class MarkitDownResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("filename")]
        public string? Filename { get; set; }

        [JsonPropertyName("markdown")]
        public string? Markdown { get; set; }

        [JsonPropertyName("original_size")]
        public long OriginalSize { get; set; }

        [JsonPropertyName("markdown_size")]
        public int MarkdownSize { get; set; }

        [JsonPropertyName("error")]
        public string? Error { get; set; }
    }

    /// <summary>
    /// Response model for URL conversion
    /// </summary>
    private sealed class MarkitDownUrlResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("url")]
        public string? Url { get; set; }

        [JsonPropertyName("markdown")]
        public string? Markdown { get; set; }

        [JsonPropertyName("markdown_size")]
        public int MarkdownSize { get; set; }

        [JsonPropertyName("error")]
        public string? Error { get; set; }
    }
}
