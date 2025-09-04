using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SemanticKernel.Agents.Memory.Core.Handlers;

/// <summary>
/// Options for text chunking configuration.
/// </summary>
public sealed class TextChunkingOptions
{
    /// <summary>
    /// Maximum size of each text chunk in characters.
    /// </summary>
    public int MaxChunkSize { get; init; } = 1000;

    /// <summary>
    /// Number of characters to overlap between consecutive chunks.
    /// </summary>
    public int TextOverlap { get; init; } = 100;

    /// <summary>
    /// Characters to use for splitting text into sentences/paragraphs.
    /// </summary>
    public string[] SplitCharacters { get; init; } = new[] { "\n\n", "\n", ". ", "! ", "? " };
}

/// <summary>
/// Text chunking pipeline step handler.
/// Splits extracted text into smaller, overlapping chunks for better processing.
/// </summary>
public sealed class TextChunkingHandler : IPipelineStepHandler
{
    public const string Name = "text-chunking";
    public string StepName => Name;

    private readonly TextChunkingOptions _options;

    public TextChunkingHandler(TextChunkingOptions? options = null)
    {
        _options = options ?? new TextChunkingOptions();
    }

    public async Task<(ReturnType Result, DataPipelineResult Pipeline)> InvokeAsync(DataPipelineResult pipeline, CancellationToken ct = default)
    {
        var newFiles = new List<FileDetails>();

        // Process files with extracted text
        foreach (var file in pipeline.Files.Where(f => f.ArtifactType == ArtifactTypes.ExtractedText))
        {
            ct.ThrowIfCancellationRequested();

            // Get the actual extracted text from the context
            var extractedTextKey = $"extracted_text_{file.Id}";
            string extractedText;
            
            if (pipeline.ContextArguments.TryGetValue(extractedTextKey, out var textValue) && textValue is string text)
            {
                extractedText = text;
            }
            else
            {
                // Fallback to generating sample text if no extracted text is available
                extractedText = GenerateSampleText(file.Name);
            }
            
            var chunks = ChunkText(extractedText);

            // Create chunked files
            for (int i = 0; i < chunks.Count; i++)
            {
                var chunkFile = new FileDetails
                {
                    Name = $"{System.IO.Path.GetFileNameWithoutExtension(file.Name)}.chunk{i:000}.txt",
                    Size = Encoding.UTF8.GetByteCount(chunks[i]),
                    MimeType = "text/plain",
                    ArtifactType = ArtifactTypes.TextPartition,
                    PartitionNumber = i,
                    SectionNumber = file.SectionNumber
                };

                // Add generated file reference
                chunkFile.GeneratedFiles["chunk.txt"] = new GeneratedFileDetails
                {
                    ParentId = file.Id,
                    SourcePartitionId = file.Id,
                    ContentSHA256 = ComputeSHA256(Encoding.UTF8.GetBytes(chunks[i]))
                };

                newFiles.Add(chunkFile);
            }
        }

        // Add chunked files to pipeline
        pipeline.Files.AddRange(newFiles);
        pipeline.Log(this, $"Created {newFiles.Count} text chunks from {pipeline.Files.Count(f => f.ArtifactType == ArtifactTypes.ExtractedText)} extracted text file(s).");
        
        await Task.Yield();
        return (ReturnType.Success, pipeline);
    }

    /// <summary>
    /// Splits text into overlapping chunks based on the configured options.
    /// </summary>
    /// <param name="text">The text to chunk.</param>
    /// <returns>A list of text chunks.</returns>
    private List<string> ChunkText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return new List<string>();

        var chunks = new List<string>();
        var currentPosition = 0;

        while (currentPosition < text.Length)
        {
            var chunkSize = Math.Min(_options.MaxChunkSize, text.Length - currentPosition);
            var endPosition = currentPosition + chunkSize;

            // Try to find a natural break point near the end of the chunk
            if (endPosition < text.Length)
            {
                var bestBreakPoint = FindBestBreakPoint(text, currentPosition, endPosition);
                if (bestBreakPoint > currentPosition)
                {
                    endPosition = bestBreakPoint;
                }
            }

            var chunk = text.Substring(currentPosition, endPosition - currentPosition).Trim();
            if (!string.IsNullOrWhiteSpace(chunk))
            {
                chunks.Add(chunk);
            }

            // Move to next position with overlap
            currentPosition = Math.Max(currentPosition + 1, endPosition - _options.TextOverlap);
        }

        return chunks;
    }

    /// <summary>
    /// Finds the best break point near the target position using configured split characters.
    /// </summary>
    /// <param name="text">The text to search in.</param>
    /// <param name="startPosition">The start position of the current chunk.</param>
    /// <param name="targetPosition">The target end position.</param>
    /// <returns>The best break point position, or the target position if no good break point is found.</returns>
    private int FindBestBreakPoint(string text, int startPosition, int targetPosition)
    {
        var searchStart = Math.Max(startPosition, targetPosition - 200); // Look back up to 200 chars
        var searchText = text.Substring(searchStart, targetPosition - searchStart);

        // Try each split character in order of preference
        foreach (var splitChar in _options.SplitCharacters)
        {
            var lastIndex = searchText.LastIndexOf(splitChar, StringComparison.Ordinal);
            if (lastIndex >= 0)
            {
                return searchStart + lastIndex + splitChar.Length;
            }
        }

        // If no good break point found, return the target position
        return targetPosition;
    }

    /// <summary>
    /// Generates sample text for demonstration purposes.
    /// In a real implementation, this would read the actual extracted text.
    /// </summary>
    /// <param name="fileName">The source file name.</param>
    /// <returns>Sample text content.</returns>
    private static string GenerateSampleText(string fileName)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"This is extracted text from {fileName}.");
        sb.AppendLine();
        
        for (int i = 1; i <= 5; i++)
        {
            sb.AppendLine($"This is paragraph {i} of the extracted content. It contains multiple sentences to demonstrate text chunking. ");
            sb.AppendLine($"The chunking handler will split this text into smaller, manageable pieces while preserving context through overlapping segments. ");
            sb.AppendLine($"This approach ensures that important information is not lost at chunk boundaries. ");
            sb.AppendLine();
        }

        return sb.ToString();
    }

    /// <summary>
    /// Computes SHA256 hash of the given bytes.
    /// </summary>
    /// <param name="bytes">The bytes to hash.</param>
    /// <returns>The SHA256 hash as a hexadecimal string.</returns>
    private static string ComputeSHA256(byte[] bytes)
    {
        using var sha = SHA256.Create();
        var hash = sha.ComputeHash(bytes);
        return string.Concat(hash.Select(b => b.ToString("x2")));
    }
}
