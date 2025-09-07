using System;
using System.Collections.Generic;
using System.Linq;
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

public class SemanticChunkingTests
{
    private readonly Mock<ILogger<SemanticChunking>> _mockLogger;

    public SemanticChunkingTests()
    {
        _mockLogger = new Mock<ILogger<SemanticChunking>>();
    }

    [Fact]
    public void Constructor_WithDefaultOptions_ShouldInitializeCorrectly()
    {
        // Act
        var handler = new SemanticChunking();

        // Assert
        handler.Should().NotBeNull();
        handler.StepName.Should().Be("semantic-chunking");
    }

    [Fact]
    public void Constructor_WithCustomOptions_ShouldInitializeCorrectly()
    {
        // Arrange
        var options = new SemanticChunkingOptions
        {
            MaxChunkSize = 500,
            TitleLevelThreshold = 3,
            IncludeTitleContext = false,
            MinChunkSize = 50
        };

        // Act
        var handler = new SemanticChunking(options, _mockLogger.Object);

        // Assert
        handler.Should().NotBeNull();
        handler.StepName.Should().Be(SemanticChunking.Name);
    }

    [Fact]
    public void StepName_ShouldReturnCorrectValue()
    {
        // Arrange
        var handler = new SemanticChunking();

        // Assert
        handler.StepName.Should().Be("semantic-chunking");
    }

    [Fact]
    public async Task InvokeAsync_WithEmptyFiles_ShouldReturnSuccessWithEmptyChunks()
    {
        // Arrange
        var handler = new SemanticChunking();
        var pipeline = new DataPipelineResult();

        // Act
        var result = await handler.InvokeAsync(pipeline);

        // Assert
        result.Result.Should().Be(ReturnType.Success);
        result.Pipeline.Files.Should().BeEmpty();
    }

    [Fact]
    public async Task InvokeAsync_WithCancellationToken_ShouldRespectCancellation()
    {
        // Arrange
        var handler = new SemanticChunking();
        var pipeline = new DataPipelineResult();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => handler.InvokeAsync(pipeline, cts.Token));
    }

    [Fact]
    public async Task InvokeAsync_WithNonExtractedTextFiles_ShouldIgnoreThem()
    {
        // Arrange
        var handler = new SemanticChunking();
        var pipeline = new DataPipelineResult();
        
        var fileDetails = new FileDetails
        {
            Name = "test.txt"
        };
        
        pipeline.Files.Add(fileDetails);

        // Act
        var result = await handler.InvokeAsync(pipeline);

        // Assert
        result.Result.Should().Be(ReturnType.Success);
        result.Pipeline.Files.Should().HaveCount(1);
        fileDetails.GeneratedFiles.Should().NotContainKey("text.partition");
    }

    [Fact]
    public async Task InvokeAsync_WithSimpleText_ShouldCreateSingleChunk()
    {
        // Arrange
        var handler = new SemanticChunking();
        var extractedText = "This is a simple text without any headings. It contains enough content to meet the minimum chunk size requirement. The text should be processed and turned into a single semantic chunk since there are no heading markers to split it into multiple parts. This ensures that the basic chunking functionality works correctly for plain text documents.";
        
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
        
        // Check that chunk files were created
        var chunkFiles = result.Pipeline.Files.Where(f => f.ArtifactType == ArtifactTypes.TextPartition).ToList();
        chunkFiles.Should().HaveCount(1);
        
        // Check that chunk text was stored in context
        var chunkFile = chunkFiles[0];
        var chunkTextKey = $"chunk_text_{chunkFile.Id}";
        result.Pipeline.ContextArguments.Should().ContainKey(chunkTextKey);
        result.Pipeline.ContextArguments[chunkTextKey].Should().Be(extractedText);
    }

    [Fact]
    public async Task InvokeAsync_WithMarkdownHeadings_ShouldCreateMultipleChunks()
    {
        // Arrange
        var options = new SemanticChunkingOptions
        {
            TitleLevelThreshold = 2,
            MaxChunkSize = 1000,
            MinChunkSize = 10  // Reduce min size for testing
        };
        var handler = new SemanticChunking(options);
        var pipeline = new DataPipelineResult();
        
        var fileDetails = TestDataFactory.CreateSampleFileDetails(
            name: "test.md",
            artifactType: ArtifactTypes.ExtractedText);
        
        var text = @"# Main Title
This is the introduction.

## First Section
Content of the first section.

## Second Section
Content of the second section.

### Subsection
Content of the subsection.";
        
        pipeline.Files.Add(fileDetails);
        pipeline.ContextArguments[$"extracted_text_{fileDetails.Id}"] = text;

        // Act
        var result = await handler.InvokeAsync(pipeline);

        // Assert
        result.Result.Should().Be(ReturnType.Success);
        
        // Should have the original file plus multiple chunk files
        var chunkFiles = result.Pipeline.Files.Where(f => f.ArtifactType == ArtifactTypes.TextPartition).ToList();
        chunkFiles.Should().HaveCountGreaterThan(1);

        // Verify chunk files are stored in context
        foreach (var chunkFile in chunkFiles)
        {
            var chunkTextKey = $"chunk_text_{chunkFile.Id}";
            result.Pipeline.ContextArguments.Should().ContainKey(chunkTextKey);
        }
    }

    [Fact]
    public async Task InvokeAsync_WithTitleLevelThreshold_ShouldRespectThreshold()
    {
        // Arrange
        var options = new SemanticChunkingOptions
        {
            TitleLevelThreshold = 3, // Only H3 and higher (H1, H2, H3) should create new chunks
            MaxChunkSize = 1000,
            MinChunkSize = 10  // Reduce min size for testing
        };
        var handler = new SemanticChunking(options);
        var pipeline = new DataPipelineResult();
        
        var fileDetails = TestDataFactory.CreateSampleFileDetails(
            name: "test.md",
            artifactType: ArtifactTypes.ExtractedText);
        
        var text = @"# H1 Title
Content after H1.

## H2 Title
Content after H2.

### H3 Title
Content after H3.

#### H4 Title
Content after H4 - should not create new chunk.";
        
        pipeline.Files.Add(fileDetails);
        pipeline.ContextArguments[$"extracted_text_{fileDetails.Id}"] = text;

        // Act
        var result = await handler.InvokeAsync(pipeline);

        // Assert
        result.Result.Should().Be(ReturnType.Success);
        
        // Should have chunk files for H1, H2, and H3, but H4 should be included with H3's content
        var chunkFiles = result.Pipeline.Files.Where(f => f.ArtifactType == ArtifactTypes.TextPartition).ToList();
        chunkFiles.Should().HaveCount(3);
    }

    [Fact]
    public async Task InvokeAsync_WithMaxChunkSize_ShouldRespectSizeLimit()
    {
        // Arrange
        var options = new SemanticChunkingOptions
        {
            MaxChunkSize = 50, // Very small chunk size
            MinChunkSize = 10, // Allow small chunks for this test
            TitleLevelThreshold = 2
        };
        var handler = new SemanticChunking(options);
        var pipeline = new DataPipelineResult();
        
        var fileDetails = TestDataFactory.CreateSampleFileDetails(
            name: "test.md",
            artifactType: ArtifactTypes.ExtractedText);
        
        var longText = @"## Section 1
" + string.Join(" ", Enumerable.Repeat("This is a very long text that should be split into multiple chunks due to size constraints.", 10));
        
        pipeline.Files.Add(fileDetails);
        pipeline.ContextArguments[$"extracted_text_{fileDetails.Id}"] = longText;

        // Act
        var result = await handler.InvokeAsync(pipeline);

        // Assert
        result.Result.Should().Be(ReturnType.Success);
        
        var chunkFiles = result.Pipeline.Files.Where(f => f.ArtifactType == ArtifactTypes.TextPartition).ToList();
        chunkFiles.Should().HaveCountGreaterThan(1);

        // Check chunk sizes in context
        foreach (var chunkFile in chunkFiles.Take(chunkFiles.Count - 1)) // Allow last chunk to be flexible
        {
            var chunkTextKey = $"chunk_text_{chunkFile.Id}";
            result.Pipeline.ContextArguments.Should().ContainKey(chunkTextKey);
            var chunkText = result.Pipeline.ContextArguments[chunkTextKey] as string;
            chunkText.Should().NotBeNull();
            // Allow some flexibility for word boundaries
            chunkText!.Length.Should().BeLessThan((int)(options.MaxChunkSize * 1.5));
        }
    }

    [Fact]
    public async Task InvokeAsync_WithIncludeTitleContextDisabled_ShouldNotIncludePreviousTitles()
    {
        // Arrange
        var options = new SemanticChunkingOptions
        {
            IncludeTitleContext = false,
            TitleLevelThreshold = 2,
            MinChunkSize = 10  // Reduce min size for testing
        };
        var handler = new SemanticChunking(options);
        var pipeline = new DataPipelineResult();
        
        var fileDetails = TestDataFactory.CreateSampleFileDetails(
            name: "test.md",
            artifactType: ArtifactTypes.ExtractedText);
        
        var text = @"# Main Title
Introduction content.

## Section Title
Section content.";
        
        pipeline.Files.Add(fileDetails);
        pipeline.ContextArguments[$"extracted_text_{fileDetails.Id}"] = text;

        // Act
        var result = await handler.InvokeAsync(pipeline);

        // Assert
        result.Result.Should().Be(ReturnType.Success);
        
        var chunkFiles = result.Pipeline.Files.Where(f => f.ArtifactType == ArtifactTypes.TextPartition).ToList();
        chunkFiles.Should().HaveCountGreaterThan(1);

        // With IncludeTitleContext disabled, chunks should not contain previous title context
        var secondChunkFile = chunkFiles.Skip(1).FirstOrDefault();
        secondChunkFile.Should().NotBeNull();
        
        var secondChunkTextKey = $"chunk_text_{secondChunkFile!.Id}";
        result.Pipeline.ContextArguments.Should().ContainKey(secondChunkTextKey);
        var secondChunkText = result.Pipeline.ContextArguments[secondChunkTextKey] as string;
        secondChunkText.Should().NotBeNull();
        secondChunkText.Should().NotContain("Main Title");
    }

    [Fact]
    public async Task InvokeAsync_WithMinChunkSize_ShouldFilterSmallChunks()
    {
        // Arrange
        var options = new SemanticChunkingOptions
        {
            MinChunkSize = 20,
            TitleLevelThreshold = 2
        };
        var handler = new SemanticChunking(options);
        var pipeline = new DataPipelineResult();
        
        var fileDetails = TestDataFactory.CreateSampleFileDetails(
            name: "test.md",
            artifactType: ArtifactTypes.ExtractedText);
        
        var text = @"## Section 1
Short.

## Section 2
This is a longer section with more content that should definitely exceed the minimum chunk size requirement.";
        
        pipeline.Files.Add(fileDetails);
        pipeline.ContextArguments[$"extracted_text_{fileDetails.Id}"] = text;

        // Act
        var result = await handler.InvokeAsync(pipeline);

        // Assert
        result.Result.Should().Be(ReturnType.Success);
        
        var chunkFiles = result.Pipeline.Files.Where(f => f.ArtifactType == ArtifactTypes.TextPartition).ToList();
        chunkFiles.Should().NotBeEmpty();

        // All chunks should meet the minimum size requirement
        foreach (var chunkFile in chunkFiles)
        {
            var chunkTextKey = $"chunk_text_{chunkFile.Id}";
            result.Pipeline.ContextArguments.Should().ContainKey(chunkTextKey);
            var chunkText = result.Pipeline.ContextArguments[chunkTextKey] as string;
            chunkText.Should().NotBeNull();
            chunkText!.Length.Should().BeGreaterOrEqualTo(options.MinChunkSize);
        }
    }

    [Fact]
    public async Task InvokeAsync_WithMultipleFiles_ShouldProcessAll()
    {
        // Arrange
        var handler = new SemanticChunking();
        var pipeline = new DataPipelineResult();
        
        var file1 = TestDataFactory.CreateSampleFileDetails(
            name: "test1.md",
            artifactType: ArtifactTypes.ExtractedText);
        var file2 = TestDataFactory.CreateSampleFileDetails(
            name: "test2.md",
            artifactType: ArtifactTypes.ExtractedText);
        
        pipeline.Files.Add(file1);
        pipeline.Files.Add(file2);
        pipeline.ContextArguments[$"extracted_text_{file1.Id}"] = "## First Document\nContent of first document.";
        pipeline.ContextArguments[$"extracted_text_{file2.Id}"] = "## Second Document\nContent of second document.";

        // Act
        var result = await handler.InvokeAsync(pipeline);

        // Assert
        result.Result.Should().Be(ReturnType.Success);
        
        // Should have the original files plus chunk files
        result.Pipeline.Files.Should().HaveCountGreaterThan(2);
        
        var chunkFiles = result.Pipeline.Files.Where(f => f.ArtifactType == ArtifactTypes.TextPartition).ToList();
        chunkFiles.Should().HaveCountGreaterOrEqualTo(2);

        // Verify chunks from both files exist in context
        foreach (var chunkFile in chunkFiles)
        {
            var chunkTextKey = $"chunk_text_{chunkFile.Id}";
            result.Pipeline.ContextArguments.Should().ContainKey(chunkTextKey);
        }
    }
}
