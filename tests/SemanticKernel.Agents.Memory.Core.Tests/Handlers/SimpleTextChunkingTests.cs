using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SemanticKernel.Agents.Memory.Core;
using SemanticKernel.Agents.Memory.Core.Handlers;
using SemanticKernel.Agents.Memory.Core.Tests.TestUtilities;
using Xunit;

namespace SemanticKernel.Agents.Memory.Core.Tests.Handlers;

public class SimpleTextChunkingTests
{
    private readonly Mock<ILogger<SimpleTextChunking>> _mockLogger;

    public SimpleTextChunkingTests()
    {
        _mockLogger = new Mock<ILogger<SimpleTextChunking>>();
    }

    [Fact]
    public void Constructor_WithDefaultOptions_ShouldInitializeCorrectly()
    {
        // Act
        var handler = new SimpleTextChunking();

        // Assert
        handler.Should().NotBeNull();
        handler.StepName.Should().Be("text-chunking");
    }

    [Fact]
    public void Constructor_WithCustomOptions_ShouldInitializeCorrectly()
    {
        // Arrange
        var options = new TextChunkingOptions
        {
            MaxChunkSize = 500,
            TextOverlap = 50
        };

        // Act
        var handler = new SimpleTextChunking(options, _mockLogger.Object);

        // Assert
        handler.Should().NotBeNull();
        handler.StepName.Should().Be(SimpleTextChunking.Name);
    }

    [Fact]
    public void StepName_ShouldReturnCorrectValue()
    {
        // Arrange
        var handler = new SimpleTextChunking();

        // Assert
        handler.StepName.Should().Be("text-chunking");
    }

    [Fact]
    public async Task InvokeAsync_WithEmptyFiles_ShouldReturnSuccessWithEmptyChunks()
    {
        // Arrange
        var handler = new SimpleTextChunking();
        var pipeline = new DataPipelineResult();

        // Act
        var result = await handler.InvokeAsync(pipeline);

        // Assert
        result.Result.Should().Be(ReturnType.Success);
        result.Pipeline.Files.Should().BeEmpty();
    }

    [Fact]
    public async Task InvokeAsync_WithShortText_ShouldCreateSingleChunk()
    {
        // Arrange
        var handler = new SimpleTextChunking();
        var extractedText = "This is a short text that fits in one chunk.";

        var fileDetails = TestDataFactory.CreateSampleFileDetails(
            name: "test.txt",
            artifactType: ArtifactTypes.ExtractedText);

        var pipeline = new DataPipelineResult();
        pipeline.Files.Add(fileDetails);
        pipeline.ContextArguments[$"extracted_text_{fileDetails.Id}"] = extractedText;

        // Act
        var result = await handler.InvokeAsync(pipeline);

        // Assert
        result.Result.Should().Be(ReturnType.Success);

        // Should have the original file plus chunk files
        result.Pipeline.Files.Should().HaveCountGreaterThan(1);

        // Check that chunk text was stored in context
        var chunkFiles = result.Pipeline.Files.Where(f => f.ArtifactType == ArtifactTypes.TextPartition).ToList();
        chunkFiles.Should().HaveCountGreaterThan(0);

        var chunkFile = chunkFiles[0];
        var chunkTextKey = $"chunk_text_{chunkFile.Id}";
        result.Pipeline.ContextArguments.Should().ContainKey(chunkTextKey);
        result.Pipeline.ContextArguments[chunkTextKey].Should().Be(extractedText);
    }

    [Fact]
    public async Task InvokeAsync_WithLongText_ShouldCreateMultipleChunks()
    {
        // Arrange
        var handler = new SimpleTextChunking();
        var longText = string.Join(" ", Enumerable.Repeat("This is a long sentence that will be repeated many times to create a text that exceeds the default chunk size.", 20));

        var fileDetails = TestDataFactory.CreateSampleFileDetails(
            name: "longtext.txt",
            artifactType: ArtifactTypes.ExtractedText);

        var pipeline = new DataPipelineResult();
        pipeline.Files.Add(fileDetails);
        pipeline.ContextArguments[$"extracted_text_{fileDetails.Id}"] = longText;

        // Act
        var result = await handler.InvokeAsync(pipeline);

        // Assert
        result.Result.Should().Be(ReturnType.Success);

        // Should have the original file plus multiple chunk files
        var chunkFiles = result.Pipeline.Files.Where(f => f.ArtifactType == ArtifactTypes.TextPartition).ToList();
        chunkFiles.Should().HaveCountGreaterThan(1);

        // Verify each chunk is stored in context and within size limits
        foreach (var chunkFile in chunkFiles)
        {
            var chunkTextKey = $"chunk_text_{chunkFile.Id}";
            result.Pipeline.ContextArguments.Should().ContainKey(chunkTextKey);
            var chunkText = result.Pipeline.ContextArguments[chunkTextKey] as string;
            chunkText.Should().NotBeNull();
            chunkText!.Length.Should().BeLessOrEqualTo(1000); // Default MaxChunkSize
        }
    }

    [Fact]
    public async Task InvokeAsync_WithMultipleFiles_ShouldCreateChunksForEach()
    {
        // Arrange
        var handler = new SimpleTextChunking();

        var file1 = TestDataFactory.CreateSampleFileDetails(
            name: "doc1.txt",
            artifactType: ArtifactTypes.ExtractedText);
        var file2 = TestDataFactory.CreateSampleFileDetails(
            name: "doc2.txt",
            artifactType: ArtifactTypes.ExtractedText);

        var pipeline = new DataPipelineResult();
        pipeline.Files.AddRange(new[] { file1, file2 });
        pipeline.ContextArguments[$"extracted_text_{file1.Id}"] = "First document text content.";
        pipeline.ContextArguments[$"extracted_text_{file2.Id}"] = "Second document text content.";

        // Act
        var result = await handler.InvokeAsync(pipeline);

        // Assert
        result.Result.Should().Be(ReturnType.Success);

        // Should have 2 original files plus chunk files
        result.Pipeline.Files.Should().HaveCountGreaterThan(2);

        var chunkFiles = result.Pipeline.Files.Where(f => f.ArtifactType == ArtifactTypes.TextPartition).ToList();
        chunkFiles.Should().HaveCountGreaterThan(0);

        // Should have chunks from both files
        chunkFiles.Should().Contain(f => f.Name.StartsWith("doc1."));
        chunkFiles.Should().Contain(f => f.Name.StartsWith("doc2."));

        // Verify each chunk has corresponding text in context
        foreach (var chunkFile in chunkFiles)
        {
            var chunkTextKey = $"chunk_text_{chunkFile.Id}";
            result.Pipeline.ContextArguments.Should().ContainKey(chunkTextKey);
        }
    }

    [Fact]
    public async Task InvokeAsync_WithCustomChunkSize_ShouldRespectSizeLimit()
    {
        // Arrange
        var options = new TextChunkingOptions
        {
            MaxChunkSize = 50,
            TextOverlap = 10
        };
        var handler = new SimpleTextChunking(options);

        var longText = string.Join(" ", Enumerable.Repeat("Word", 50));
        var fileDetails = TestDataFactory.CreateSampleFileDetails(
            name: "test.txt",
            artifactType: ArtifactTypes.ExtractedText);

        var pipeline = new DataPipelineResult();
        pipeline.Files.Add(fileDetails);
        pipeline.ContextArguments[$"extracted_text_{fileDetails.Id}"] = longText;

        // Act
        var result = await handler.InvokeAsync(pipeline);

        // Assert
        result.Result.Should().Be(ReturnType.Success);

        var chunkFiles = result.Pipeline.Files.Where(f => f.ArtifactType == ArtifactTypes.TextPartition).ToList();
        chunkFiles.Should().HaveCountGreaterThan(1);

        // Verify each chunk respects the size limit
        foreach (var chunkFile in chunkFiles)
        {
            var chunkTextKey = $"chunk_text_{chunkFile.Id}";
            var chunkText = result.Pipeline.ContextArguments[chunkTextKey] as string;
            chunkText.Should().NotBeNull();
            chunkText!.Length.Should().BeLessOrEqualTo(50);
        }
    }

    [Fact]
    public async Task InvokeAsync_WithCancellationToken_ShouldRespectCancellation()
    {
        // Arrange
        var handler = new SimpleTextChunking();
        var fileDetails = TestDataFactory.CreateSampleFileDetails(
            name: "test.txt",
            artifactType: ArtifactTypes.ExtractedText);

        var pipeline = new DataPipelineResult();
        pipeline.Files.Add(fileDetails);
        pipeline.ContextArguments[$"extracted_text_{fileDetails.Id}"] = "Some text to chunk";

        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            handler.InvokeAsync(pipeline, cts.Token));
    }

    [Fact]
    public async Task InvokeAsync_WithEmptyText_ShouldUseSampleText()
    {
        // Arrange
        var handler = new SimpleTextChunking();
        var fileDetails = TestDataFactory.CreateSampleFileDetails(
            name: "empty.txt",
            artifactType: ArtifactTypes.ExtractedText);

        var pipeline = new DataPipelineResult();
        pipeline.Files.Add(fileDetails);
        // No extracted text in context - should trigger fallback to sample text

        // Act
        var result = await handler.InvokeAsync(pipeline);

        // Assert
        result.Result.Should().Be(ReturnType.Success);

        var chunkFiles = result.Pipeline.Files.Where(f => f.ArtifactType == ArtifactTypes.TextPartition).ToList();
        chunkFiles.Should().HaveCountGreaterThan(0); // Sample text generates multiple chunks

        var firstChunkFile = chunkFiles[0];
        var chunkTextKey = $"chunk_text_{firstChunkFile.Id}";
        result.Pipeline.ContextArguments.Should().ContainKey(chunkTextKey);

        // Should contain generated sample text
        var chunkText = result.Pipeline.ContextArguments[chunkTextKey] as string;
        chunkText.Should().NotBeNullOrEmpty();
        chunkText.Should().Contain("extracted text"); // The handler generates sample text containing this phrase
    }

    [Fact]
    public async Task InvokeAsync_WithNonExtractedTextFiles_ShouldIgnoreThem()
    {
        // Arrange
        var handler = new SimpleTextChunking();

        var extractedTextFile = TestDataFactory.CreateSampleFileDetails(
            name: "extracted.txt",
            artifactType: ArtifactTypes.ExtractedText);
        var otherFile = TestDataFactory.CreateSampleFileDetails(
            name: "other.txt",
            artifactType: ArtifactTypes.Undefined);

        var pipeline = new DataPipelineResult();
        pipeline.Files.AddRange(new[] { extractedTextFile, otherFile });
        pipeline.ContextArguments[$"extracted_text_{extractedTextFile.Id}"] = "Text to chunk";

        // Act
        var result = await handler.InvokeAsync(pipeline);

        // Assert
        result.Result.Should().Be(ReturnType.Success);

        // Should process the extracted text file
        var chunkFiles = result.Pipeline.Files.Where(f => f.ArtifactType == ArtifactTypes.TextPartition).ToList();
        chunkFiles.Should().HaveCountGreaterThan(0);

        // All chunk files should be related to the extracted text file
        foreach (var chunkFile in chunkFiles)
        {
            chunkFile.Name.Should().StartWith("extracted.");
        }
    }
}

public class TextChunkingOptionsTests
{
    [Fact]
    public void DefaultValues_ShouldBeCorrect()
    {
        // Act
        var options = new TextChunkingOptions();

        // Assert
        options.MaxChunkSize.Should().Be(1000);
        options.TextOverlap.Should().Be(100);
        options.SplitCharacters.Should().BeEquivalentTo(new[] { "\n\n", "\n", ". ", "! ", "? " });
    }

    [Fact]
    public void CustomValues_ShouldBeSetCorrectly()
    {
        // Act
        var options = new TextChunkingOptions
        {
            MaxChunkSize = 500,
            TextOverlap = 50,
            SplitCharacters = new[] { ".", "!", "?" }
        };

        // Assert
        options.MaxChunkSize.Should().Be(500);
        options.TextOverlap.Should().Be(50);
        options.SplitCharacters.Should().BeEquivalentTo(new[] { ".", "!", "?" });
    }
}
