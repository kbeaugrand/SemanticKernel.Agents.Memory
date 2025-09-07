using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace SemanticKernel.Agents.Memory.Core.Handlers;

/// <summary>
/// Options for semantic text chunking configuration.
/// </summary>
public sealed class SemanticChunkingOptions
{
    /// <summary>
    /// The heading level that should trigger creation of a new chunk (default: 2 for H2).
    /// Headings at this level or higher (lower numbers) will create new chunks.
    /// </summary>
    public int TitleLevelThreshold { get; init; } = 2;

    /// <summary>
    /// Maximum size of each text chunk in characters.
    /// </summary>
    public int MaxChunkSize { get; init; } = 2000;

    /// <summary>
    /// Whether to include previous title context in chunks.
    /// </summary>
    public bool IncludeTitleContext { get; init; } = true;

    /// <summary>
    /// Minimum chunk size to avoid very small chunks.
    /// </summary>
    public int MinChunkSize { get; init; } = 100;
}

/// <summary>
/// Semantic chunking pipeline step handler.
/// Splits extracted text into chunks based on document structure (titles/headings) and content semantics.
/// Creates new chunks when encountering titles at or above the specified level.
/// </summary>
public sealed class SemanticChunking : IPipelineStepHandler
{
    public const string Name = "semantic-chunking";
    public string StepName => Name;

    private readonly SemanticChunkingOptions _options;
    private readonly ILogger<SemanticChunking>? _logger;

    // Regex patterns for detecting different heading formats
    private static readonly Regex MarkdownHeadingRegex = new(@"^(#{1,6})\s+(.+)$", RegexOptions.Multiline | RegexOptions.Compiled);
    private static readonly Regex UnderlineHeadingRegex = new(@"^(.+)\r?\n(={3,}|-{3,})$", RegexOptions.Multiline | RegexOptions.Compiled);
    private static readonly Regex NumberedHeadingRegex = new(@"^(\d+\.)+\s+(.+)$", RegexOptions.Multiline | RegexOptions.Compiled);

    public SemanticChunking(SemanticChunkingOptions? options = null, ILogger<SemanticChunking>? logger = null)
    {
        _options = options ?? new SemanticChunkingOptions();
        _logger = logger;
    }

    public Task<(ReturnType Result, DataPipelineResult Pipeline)> InvokeAsync(DataPipelineResult pipeline, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        _logger?.LogDebug("Starting semantic chunking for {FileCount} files with options: MaxChunkSize={MaxChunkSize}, MinChunkSize={MinChunkSize}, TitleLevelThreshold={TitleLevelThreshold}",
            pipeline.Files.Count(f => f.ArtifactType == ArtifactTypes.ExtractedText),
            _options.MaxChunkSize, _options.MinChunkSize, _options.TitleLevelThreshold);

        var newFiles = new List<FileDetails>();
        var totalChunks = 0;

        // Process files with extracted text
        foreach (var file in pipeline.Files.Where(f => f.ArtifactType == ArtifactTypes.ExtractedText))
        {
            ct.ThrowIfCancellationRequested();

            _logger?.LogTrace("Processing file '{FileName}' (ID: {FileId}) for semantic chunking", file.Name, file.Id);

            // Get the actual extracted text from the context
            var extractedTextKey = $"extracted_text_{file.Id}";
            string extractedText;

            if (pipeline.ContextArguments.TryGetValue(extractedTextKey, out var textValue) && textValue is string text)
            {
                extractedText = text;
                _logger?.LogTrace("Retrieved extracted text for file '{FileName}': {CharacterCount} characters",
                    file.Name, extractedText.Length);
            }
            else
            {
                // Fallback to generating sample text if no extracted text is available
                extractedText = GenerateSampleText(file.Name);
                _logger?.LogWarning("No extracted text found for file '{FileName}', using generated sample text", file.Name);
            }

            _logger?.LogDebug("Chunking text for file '{FileName}' ({CharacterCount} characters)",
                file.Name, extractedText.Length);

            var chunks = ChunkTextSemantically(extractedText);
            totalChunks += chunks.Count;

            _logger?.LogInformation("Created {ChunkCount} semantic chunks for file '{FileName}'",
                chunks.Count, file.Name);

            // Create chunked files
            for (int i = 0; i < chunks.Count; i++)
            {
                var chunkFile = new FileDetails
                {
                    Name = $"{System.IO.Path.GetFileNameWithoutExtension(file.Name)}.semantic-chunk{i:000}.txt",
                    Size = Encoding.UTF8.GetByteCount(chunks[i].Content),
                    MimeType = "text/plain",
                    ArtifactType = ArtifactTypes.TextPartition,
                    PartitionNumber = i,
                    SectionNumber = file.SectionNumber
                };

                _logger?.LogTrace("Created chunk {ChunkIndex}/{TotalChunks} for file '{FileName}': {ChunkSize} characters, Title: '{ChunkTitle}'",
                    i + 1, chunks.Count, file.Name, chunks[i].Content.Length, chunks[i].Title);

                // Add generated file reference with chunk metadata
                chunkFile.GeneratedFiles["chunk.txt"] = new GeneratedFileDetails
                {
                    ParentId = file.Id,
                    SourcePartitionId = file.Id,
                    ContentSHA256 = ComputeSHA256(Encoding.UTF8.GetBytes(chunks[i].Content))
                };

                // Store chunk metadata in context
                var chunkMetadataKey = $"chunk_metadata_{chunkFile.Id}";
                pipeline.ContextArguments[chunkMetadataKey] = new
                {
                    Title = chunks[i].Title,
                    TitleLevel = chunks[i].TitleLevel,
                    TitleHierarchy = chunks[i].TitleHierarchy
                };

                // Store chunk text content in context for embedding generation
                var chunkTextKey = $"chunk_text_{chunkFile.Id}";
                pipeline.ContextArguments[chunkTextKey] = chunks[i].Content;

                newFiles.Add(chunkFile);
            }
        }

        // Add chunked files to pipeline
        pipeline.Files.AddRange(newFiles);

        var logMessage = $"Created {totalChunks} semantic chunks from {pipeline.Files.Count(f => f.ArtifactType == ArtifactTypes.ExtractedText)} extracted text file(s).";
        pipeline.Log(this, logMessage);

        _logger?.LogInformation("Semantic chunking completed: {TotalChunks} chunks created from {SourceFileCount} files",
            totalChunks, pipeline.Files.Count(f => f.ArtifactType == ArtifactTypes.ExtractedText));

        ct.ThrowIfCancellationRequested();
        return Task.FromResult((ReturnType.Success, pipeline));
    }

    /// <summary>
    /// Represents a semantic chunk with title context.
    /// </summary>
    private class SemanticChunk
    {
        public string Content { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public int TitleLevel { get; set; }
        public List<string> TitleHierarchy { get; set; } = new();
    }

    /// <summary>
    /// Represents a detected heading in the text.
    /// </summary>
    private class DetectedHeading
    {
        public string Text { get; set; } = string.Empty;
        public int Level { get; set; }
        public int Position { get; set; }
        public int Length { get; set; }
    }

    /// <summary>
    /// Splits text into semantic chunks based on headings and content structure.
    /// </summary>
    /// <param name="text">The text to chunk.</param>
    /// <returns>A list of semantic chunks.</returns>
    private List<SemanticChunk> ChunkTextSemantically(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            _logger?.LogTrace("Input text is null or whitespace, returning empty chunk list");
            return new List<SemanticChunk>();
        }

        _logger?.LogTrace("Starting semantic chunking for text of length {TextLength}", text.Length);

        var chunks = new List<SemanticChunk>();
        var headings = DetectHeadings(text);
        var titleHierarchy = new List<string>();

        _logger?.LogTrace("Detected {HeadingCount} headings in text", headings.Count);

        // If no headings found, fall back to paragraph-based chunking
        if (!headings.Any())
        {
            _logger?.LogTrace("No headings detected, falling back to paragraph-based chunking");
            return ChunkByParagraphs(text, titleHierarchy);
        }

        var currentPosition = 0;

        for (int i = 0; i < headings.Count; i++)
        {
            var currentHeading = headings[i];
            var nextHeading = i + 1 < headings.Count ? headings[i + 1] : null;

            _logger?.LogTrace("Processing heading {HeadingIndex}/{TotalHeadings}: '{HeadingText}' (Level {Level})",
                i + 1, headings.Count, currentHeading.Text, currentHeading.Level);

            // Update title hierarchy based on heading level
            UpdateTitleHierarchy(titleHierarchy, currentHeading);

            // Determine if this heading should start a new chunk
            bool shouldStartNewChunk = ShouldStartNewChunk(currentHeading, titleHierarchy);

            _logger?.LogTrace("Heading '{HeadingText}' should start new chunk: {ShouldStartNewChunk}",
                currentHeading.Text, shouldStartNewChunk);

            // Extract content between current heading and next heading (or end of text)
            var startPos = currentHeading.Position;
            var endPos = nextHeading?.Position ?? text.Length;
            var sectionContent = text.Substring(startPos, endPos - startPos);

            _logger?.LogTrace("Section content for heading '{HeadingText}': {ContentLength} characters",
                currentHeading.Text, sectionContent.Length);

            // If we should start a new chunk or this is the first heading
            if (shouldStartNewChunk || chunks.Count == 0)
            {
                // Add any content before the first heading to the previous chunk or create a new one
                if (currentPosition < currentHeading.Position && chunks.Count > 0)
                {
                    var precedingContent = text.Substring(currentPosition, currentHeading.Position - currentPosition).Trim();
                    if (!string.IsNullOrEmpty(precedingContent))
                    {
                        _logger?.LogTrace("Adding preceding content ({ContentLength} characters) to existing chunk",
                            precedingContent.Length);
                        AppendOrCreateChunk(chunks, precedingContent, titleHierarchy);
                    }
                }

                // Create new chunk for this section
                var newChunks = ProcessSectionContent(sectionContent, currentHeading, titleHierarchy);
                chunks.AddRange(newChunks);

                _logger?.LogTrace("Created {NewChunkCount} new chunks for section '{HeadingText}'",
                    newChunks.Count, currentHeading.Text);
            }
            else
            {
                // Append to existing chunk
                _logger?.LogTrace("Appending section content to existing chunk");
                AppendOrCreateChunk(chunks, sectionContent, titleHierarchy);
            }

            currentPosition = endPos;
        }

        // Handle any remaining content after the last heading
        if (currentPosition < text.Length)
        {
            var remainingContent = text.Substring(currentPosition).Trim();
            if (!string.IsNullOrEmpty(remainingContent))
            {
                _logger?.LogTrace("Adding remaining content ({ContentLength} characters) after last heading",
                    remainingContent.Length);
                AppendOrCreateChunk(chunks, remainingContent, titleHierarchy);
            }
        }

        var finalChunks = chunks.Where(c => c.Content.Length >= _options.MinChunkSize).ToList();

        // If no chunks meet the minimum size but we have content, keep at least one chunk
        if (!finalChunks.Any() && chunks.Any())
        {
            finalChunks.Add(chunks.OrderByDescending(c => c.Content.Length).First());
        }

        _logger?.LogTrace("Semantic chunking completed: {InitialChunkCount} chunks created, {FinalChunkCount} chunks after minimum size filter (min size: {MinChunkSize})",
            chunks.Count, finalChunks.Count, _options.MinChunkSize);

        return finalChunks;
    }

    /// <summary>
    /// Detects headings in the text using various patterns.
    /// </summary>
    /// <param name="text">The text to analyze.</param>
    /// <returns>List of detected headings.</returns>
    private List<DetectedHeading> DetectHeadings(string text)
    {
        var headings = new List<DetectedHeading>();

        // Detect Markdown-style headings (# ## ### etc.)
        foreach (Match match in MarkdownHeadingRegex.Matches(text))
        {
            headings.Add(new DetectedHeading
            {
                Text = match.Groups[2].Value.Trim(),
                Level = match.Groups[1].Value.Length,
                Position = match.Index,
                Length = match.Length
            });
        }

        // Detect underlined headings (text followed by === or ---)
        foreach (Match match in UnderlineHeadingRegex.Matches(text))
        {
            var underlineChar = match.Groups[2].Value[0];
            var level = underlineChar == '=' ? 1 : 2; // = for H1, - for H2

            headings.Add(new DetectedHeading
            {
                Text = match.Groups[1].Value.Trim(),
                Level = level,
                Position = match.Index,
                Length = match.Groups[1].Length // Only the title part
            });
        }

        // Detect numbered headings (1. 1.1. 1.1.1. etc.)
        foreach (Match match in NumberedHeadingRegex.Matches(text))
        {
            var numberPart = match.Groups[1].Value;
            var level = numberPart.Count(c => c == '.');

            headings.Add(new DetectedHeading
            {
                Text = match.Groups[2].Value.Trim(),
                Level = level,
                Position = match.Index,
                Length = match.Length
            });
        }

        return headings.OrderBy(h => h.Position).ToList();
    }

    /// <summary>
    /// Updates the title hierarchy based on the current heading level.
    /// </summary>
    /// <param name="hierarchy">The current title hierarchy.</param>
    /// <param name="heading">The new heading to process.</param>
    private void UpdateTitleHierarchy(List<string> hierarchy, DetectedHeading heading)
    {
        // Remove titles at the same level or deeper
        while (hierarchy.Count >= heading.Level)
        {
            hierarchy.RemoveAt(hierarchy.Count - 1);
        }

        // Add the current heading
        if (hierarchy.Count == heading.Level - 1)
        {
            hierarchy.Add(heading.Text);
        }
        else
        {
            // Fill in missing levels if necessary
            while (hierarchy.Count < heading.Level - 1)
            {
                hierarchy.Add("Untitled Section");
            }
            hierarchy.Add(heading.Text);
        }
    }

    /// <summary>
    /// Determines if a heading should start a new chunk.
    /// </summary>
    /// <param name="heading">The heading to evaluate.</param>
    /// <param name="titleHierarchy">Current title hierarchy.</param>
    /// <returns>True if a new chunk should be started.</returns>
    private bool ShouldStartNewChunk(DetectedHeading heading, List<string> titleHierarchy)
    {
        return heading.Level <= _options.TitleLevelThreshold;
    }

    /// <summary>
    /// Processes content for a section, potentially splitting it if too large.
    /// </summary>
    /// <param name="content">The section content.</param>
    /// <param name="heading">The section heading.</param>
    /// <param name="titleHierarchy">Current title hierarchy.</param>
    /// <returns>List of chunks for this section.</returns>
    private List<SemanticChunk> ProcessSectionContent(string content, DetectedHeading heading, List<string> titleHierarchy)
    {
        var chunks = new List<SemanticChunk>();
        var trimmedContent = content.Trim();

        if (string.IsNullOrEmpty(trimmedContent))
        {
            return chunks;
        }

        if (trimmedContent.Length <= _options.MaxChunkSize)
        {
            // Content fits in one chunk
            chunks.Add(new SemanticChunk
            {
                Content = trimmedContent,
                Title = heading.Text,
                TitleLevel = heading.Level,
                TitleHierarchy = new List<string>(titleHierarchy)
            });
        }
        else
        {
            // Split large content into multiple chunks while preserving the heading context
            var splitChunks = SplitContentIntoChunks(trimmedContent, titleHierarchy);

            // Set the title and level for the first chunk
            if (splitChunks.Any())
            {
                splitChunks[0].Title = heading.Text;
                splitChunks[0].TitleLevel = heading.Level;
            }

            chunks.AddRange(splitChunks);
        }

        return chunks;
    }

    /// <summary>
    /// Splits content into multiple chunks respecting max chunk size while trying to preserve paragraph boundaries.
    /// </summary>
    /// <param name="content">The content to split.</param>
    /// <param name="titleHierarchy">Current title hierarchy.</param>
    /// <returns>List of chunks.</returns>
    private List<SemanticChunk> SplitContentIntoChunks(string content, List<string> titleHierarchy)
    {
        var chunks = new List<SemanticChunk>();
        var paragraphs = content.Split(new[] { "\n\n", "\r\n\r\n" }, StringSplitOptions.RemoveEmptyEntries);

        // Track if the original content has paragraph breaks
        var hasOriginalParagraphBreaks = paragraphs.Length > 1;

        // If no paragraph breaks found, split by sentences
        if (paragraphs.Length == 1)
        {
            paragraphs = content.Split(new[] { ". ", ".\n", ".\r\n" }, StringSplitOptions.RemoveEmptyEntries)
                               .Select(s => s.Trim() + (s.EndsWith('.') ? "" : "."))
                               .ToArray();
        }

        var currentChunk = new StringBuilder();
        var currentTitle = titleHierarchy.LastOrDefault() ?? "Content";

        foreach (var paragraph in paragraphs)
        {
            var trimmedParagraph = paragraph.Trim();
            if (string.IsNullOrEmpty(trimmedParagraph)) continue;

            // If this single paragraph is too large, force split it by words
            if (trimmedParagraph.Length > _options.MaxChunkSize)
            {
                // First, save any current chunk content
                if (currentChunk.Length > 0)
                {
                    var chunkContent = currentChunk.ToString().Trim();
                    if (!string.IsNullOrEmpty(chunkContent))
                    {
                        chunks.Add(new SemanticChunk
                        {
                            Content = chunkContent,
                            Title = currentTitle,
                            TitleLevel = 0,
                            TitleHierarchy = new List<string>(titleHierarchy)
                        });
                    }
                    currentChunk.Clear();
                }

                // Force split the large paragraph
                var wordChunks = ForceWordSplit(trimmedParagraph, titleHierarchy);
                chunks.AddRange(wordChunks);
                continue;
            }

            // Check if adding this paragraph would exceed the limit
            var separator = hasOriginalParagraphBreaks ? "\n\n" : " ";
            var separatorLength = currentChunk.Length > 0 ? separator.Length : 0;
            var wouldExceedLimit = currentChunk.Length + trimmedParagraph.Length + separatorLength > _options.MaxChunkSize;

            if (wouldExceedLimit && currentChunk.Length > 0)
            {
                // Create chunk with current content
                var chunkContent = currentChunk.ToString().Trim();
                if (!string.IsNullOrEmpty(chunkContent))
                {
                    chunks.Add(new SemanticChunk
                    {
                        Content = chunkContent,
                        Title = currentTitle,
                        TitleLevel = 0, // No specific level for split chunks
                        TitleHierarchy = new List<string>(titleHierarchy)
                    });
                }

                // Start new chunk
                currentChunk.Clear();
            }

            // Add paragraph to current chunk
            if (currentChunk.Length > 0)
            {
                if (hasOriginalParagraphBreaks)
                {
                    currentChunk.AppendLine();
                    currentChunk.AppendLine();
                }
                else
                {
                    currentChunk.Append(" ");
                }
            }
            currentChunk.Append(trimmedParagraph);
        }

        // Add final chunk if it has content
        if (currentChunk.Length > 0)
        {
            var chunkContent = currentChunk.ToString().Trim();
            if (!string.IsNullOrEmpty(chunkContent))
            {
                chunks.Add(new SemanticChunk
                {
                    Content = chunkContent,
                    Title = currentTitle,
                    TitleLevel = 0,
                    TitleHierarchy = new List<string>(titleHierarchy)
                });
            }
        }

        return chunks;
    }

    /// <summary>
    /// Force splits content at word boundaries when other methods fail.
    /// </summary>
    /// <param name="content">The content to split.</param>
    /// <param name="titleHierarchy">Current title hierarchy.</param>
    /// <returns>List of chunks split at word boundaries.</returns>
    private List<SemanticChunk> ForceWordSplit(string content, List<string> titleHierarchy)
    {
        var chunks = new List<SemanticChunk>();
        var words = content.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var currentChunk = new StringBuilder();
        var currentTitle = titleHierarchy.LastOrDefault() ?? "Content";

        foreach (var word in words)
        {
            // Check if adding this word would exceed the limit
            var wouldExceedLimit = currentChunk.Length + word.Length + 1 > _options.MaxChunkSize;

            if (wouldExceedLimit && currentChunk.Length > 0)
            {
                // Create chunk with current content
                var chunkContent = currentChunk.ToString().Trim();
                if (!string.IsNullOrEmpty(chunkContent))
                {
                    chunks.Add(new SemanticChunk
                    {
                        Content = chunkContent,
                        Title = currentTitle,
                        TitleLevel = 0,
                        TitleHierarchy = new List<string>(titleHierarchy)
                    });
                }

                // Start new chunk
                currentChunk.Clear();
            }

            // Add word to current chunk
            if (currentChunk.Length > 0)
            {
                currentChunk.Append(" ");
            }
            currentChunk.Append(word);
        }

        // Add final chunk if it has content
        if (currentChunk.Length > 0)
        {
            var chunkContent = currentChunk.ToString().Trim();
            if (!string.IsNullOrEmpty(chunkContent))
            {
                chunks.Add(new SemanticChunk
                {
                    Content = chunkContent,
                    Title = currentTitle,
                    TitleLevel = 0,
                    TitleHierarchy = new List<string>(titleHierarchy)
                });
            }
        }

        return chunks;
    }

    /// <summary>
    /// Chunks text by paragraphs when content is too large.
    /// </summary>
    /// <param name="text">The text to chunk.</param>
    /// <param name="titleHierarchy">Current title hierarchy.</param>
    /// <returns>List of paragraph-based chunks.</returns>
    private List<SemanticChunk> ChunkByParagraphs(string text, List<string> titleHierarchy)
    {
        return SplitContentIntoChunks(text, titleHierarchy);
    }

    /// <summary>
    /// Appends content to the last chunk or creates a new one if necessary.
    /// </summary>
    /// <param name="chunks">The list of existing chunks.</param>
    /// <param name="content">The content to append.</param>
    /// <param name="titleHierarchy">Current title hierarchy.</param>
    private void AppendOrCreateChunk(List<SemanticChunk> chunks, string content, List<string> titleHierarchy)
    {
        var trimmedContent = content.Trim();
        if (string.IsNullOrEmpty(trimmedContent))
        {
            return;
        }

        if (!chunks.Any())
        {
            // Create first chunk
            var newChunks = SplitContentIntoChunks(trimmedContent, titleHierarchy);
            if (newChunks.Any())
            {
                chunks.AddRange(newChunks);
            }
            return;
        }

        var lastChunk = chunks.Last();

        // Check if we can append to the last chunk
        if (lastChunk.Content.Length + trimmedContent.Length + 2 <= _options.MaxChunkSize)
        {
            lastChunk.Content += "\n\n" + trimmedContent;
        }
        else
        {
            // Create new chunks for the content
            var newChunks = SplitContentIntoChunks(trimmedContent, titleHierarchy);
            chunks.AddRange(newChunks);
        }
    }

    /// <summary>
    /// Generates sample text with headings for demonstration purposes.
    /// </summary>
    /// <param name="fileName">The source file name.</param>
    /// <returns>Sample text content with headings.</returns>
    private static string GenerateSampleText(string fileName)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"# Document: {fileName}");
        sb.AppendLine();
        sb.AppendLine("This is the introduction to the document.");
        sb.AppendLine();

        sb.AppendLine("## Section 1: Overview");
        sb.AppendLine();
        sb.AppendLine("This section provides an overview of the main concepts. It contains multiple paragraphs that demonstrate how the semantic chunking works.");
        sb.AppendLine();
        sb.AppendLine("The semantic chunker analyzes the structure of the document and creates meaningful chunks based on headings and content organization.");
        sb.AppendLine();

        sb.AppendLine("### Subsection 1.1: Details");
        sb.AppendLine();
        sb.AppendLine("This is a subsection that provides more detailed information. Since it's at level 3 and the default threshold is 2, this will not create a new chunk unless the parent section becomes too large.");
        sb.AppendLine();

        sb.AppendLine("## Section 2: Implementation");
        sb.AppendLine();
        sb.AppendLine("This section describes the implementation details. Because it's a level 2 heading, it will create a new chunk.");
        sb.AppendLine();

        for (int i = 1; i <= 3; i++)
        {
            sb.AppendLine($"Implementation paragraph {i}: This contains detailed technical information about how the semantic chunking algorithm works. ");
            sb.AppendLine($"It processes the document structure and maintains context about headings and their hierarchy. ");
            sb.AppendLine();
        }

        sb.AppendLine("## Section 3: Examples");
        sb.AppendLine();
        sb.AppendLine("This final section provides examples of how the chunking works in practice.");
        sb.AppendLine();

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
