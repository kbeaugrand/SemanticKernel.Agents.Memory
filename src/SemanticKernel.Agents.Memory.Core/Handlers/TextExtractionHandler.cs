using System;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SemanticKernel.Agents.Memory.Core.Services;

namespace SemanticKernel.Agents.Memory.Core.Handlers;

/// <summary>
/// Text extraction pipeline step handler that uses MarkitDown service to convert files to markdown.
/// </summary>
public sealed class TextExtractionHandler : IPipelineStepHandler
{
    public const string Name = "text-extraction";
    public string StepName => Name;

    private readonly IMarkitDownService _markitDownService;
    private readonly ILogger<TextExtractionHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the TextExtractionHandler
    /// </summary>
    /// <param name="markitDownService">MarkitDown service for text extraction</param>
    /// <param name="logger">Logger instance</param>
    public TextExtractionHandler(IMarkitDownService markitDownService, ILogger<TextExtractionHandler> logger)
    {
        _markitDownService = markitDownService ?? throw new ArgumentNullException(nameof(markitDownService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<(ReturnType Result, DataPipelineResult Pipeline)> InvokeAsync(DataPipelineResult pipeline, CancellationToken ct = default)
    {
        _logger.LogInformation("Starting text extraction for {FileCount} file(s)", pipeline.FilesToUpload.Count);

        // Check if MarkitDown service is healthy before processing
        var isHealthy = await _markitDownService.IsHealthyAsync(ct);
        if (!isHealthy)
        {
            _logger.LogWarning("MarkitDown service is not healthy, falling back to basic extraction");
        }

        // Process uploaded files through MarkitDown service
        foreach (var upload in pipeline.FilesToUpload)
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                var details = new FileDetails
                {
                    Name = upload.FileName,
                    Size = upload.Bytes.LongLength,
                    MimeType = upload.MimeType ?? "application/octet-stream",
                    ArtifactType = ArtifactTypes.ExtractedText,
                    PartitionNumber = 0,
                    SectionNumber = 0,
                };

                string extractedText;

                if (isHealthy)
                {
                    try
                    {
                        // Use MarkitDown service to extract text as markdown
                        _logger.LogDebug("Converting {FileName} using MarkitDown service", upload.FileName);
                        extractedText = await _markitDownService.ConvertToMarkdownAsync(
                            upload.Bytes,
                            upload.FileName,
                            upload.MimeType ?? "application/octet-stream",
                            ct);

                        _logger.LogInformation("Successfully extracted {CharCount} characters from {FileName}",
                            extractedText.Length, upload.FileName);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "MarkitDown service failed for {FileName}, falling back to basic extraction", upload.FileName);
                        extractedText = GetFallbackText(upload);
                    }
                }
                else
                {
                    extractedText = GetFallbackText(upload);
                }

                // Store extracted text
                details.GeneratedFiles["extracted.txt"] = new GeneratedFileDetails
                {
                    ParentId = details.Id,
                    SourcePartitionId = details.Id,
                    ContentSHA256 = ComputeSHA256(System.Text.Encoding.UTF8.GetBytes(extractedText))
                };

                // Store the extracted text in context for next pipeline steps
                if (!pipeline.ContextArguments.ContainsKey($"extracted_text_{details.Id}"))
                {
                    pipeline.ContextArguments[$"extracted_text_{details.Id}"] = extractedText;
                }

                pipeline.Files.Add(details);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process file {FileName}", upload.FileName);
                return (ReturnType.TransientError, pipeline);
            }
        }

        // Mark uploads as consumed
        pipeline.FilesToUpload.Clear();
        pipeline.UploadComplete = true;
        pipeline.Log(this, $"Extracted text from {pipeline.Files.Count} file(s) using MarkitDown service.");

        return (ReturnType.Success, pipeline);
    }

    /// <summary>
    /// Provides fallback text extraction for unsupported files or when MarkitDown service is unavailable
    /// </summary>
    private string GetFallbackText(UploadedFile upload)
    {
        try
        {
            // Try to extract as UTF-8 text for text-based files
            if (IsTextFile(upload.MimeType))
            {
                return System.Text.Encoding.UTF8.GetString(upload.Bytes);
            }

            // For binary files, return basic metadata
            return $"""
                # {upload.FileName}
                
                **File Type:** {upload.MimeType ?? "Unknown"}
                **File Size:** {upload.Bytes.Length:N0} bytes
                **Note:** Binary content could not be extracted. Consider using MarkitDown service for better extraction.
                """;
        }
        catch
        {
            return $"""
                # {upload.FileName}
                
                **File Type:** {upload.MimeType ?? "Unknown"}
                **File Size:** {upload.Bytes.Length:N0} bytes
                **Note:** Content extraction failed. Raw binary content detected.
                """;
        }
    }

    /// <summary>
    /// Determines if a MIME type represents a text-based file
    /// </summary>
    private static bool IsTextFile(string? mimeType)
    {
        if (string.IsNullOrEmpty(mimeType))
            return false;

        return mimeType.StartsWith("text/", StringComparison.OrdinalIgnoreCase) ||
               mimeType.Equals("application/json", StringComparison.OrdinalIgnoreCase) ||
               mimeType.Equals("application/xml", StringComparison.OrdinalIgnoreCase) ||
               mimeType.Equals("application/javascript", StringComparison.OrdinalIgnoreCase) ||
               mimeType.Contains("xml", StringComparison.OrdinalIgnoreCase);
    }

    private static string ComputeSHA256(byte[] bytes)
    {
        using var sha = SHA256.Create();
        var hash = sha.ComputeHash(bytes);
        return string.Concat(hash.Select(b => b.ToString("x2")));
    }
}
