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
using SemanticKernel.Agents.Memory.Core.Services;
using SemanticKernel.Agents.Memory.Core.Tests.TestUtilities;
using Xunit;

namespace SemanticKernel.Agents.Memory.Core.Tests.Handlers;

public class TextExtractionHandlerTests
{
    private readonly Mock<IMarkitDownService> _mockMarkitDownService;
    private readonly Mock<ILogger<TextExtractionHandler>> _mockLogger;
    private readonly TextExtractionHandler _handler;

    public TextExtractionHandlerTests()
    {
        _mockMarkitDownService = new Mock<IMarkitDownService>();
        _mockLogger = new Mock<ILogger<TextExtractionHandler>>();
        _handler = new TextExtractionHandler(_mockMarkitDownService.Object, _mockLogger.Object);
    }

    [Fact]
    public void Constructor_WithValidParameters_ShouldInitializeCorrectly()
    {
        // Act
        var handler = new TextExtractionHandler(_mockMarkitDownService.Object, _mockLogger.Object);

        // Assert
        handler.Should().NotBeNull();
        handler.StepName.Should().Be("text-extraction");
    }

    [Fact]
    public void Constructor_WithNullMarkitDownService_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var action = () => new TextExtractionHandler(null!, _mockLogger.Object);
        action.Should().Throw<ArgumentNullException>().WithParameterName("markitDownService");
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var action = () => new TextExtractionHandler(_mockMarkitDownService.Object, null!);
        action.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public void StepName_ShouldReturnCorrectValue()
    {
        // Assert
        _handler.StepName.Should().Be(TextExtractionHandler.Name);
        _handler.StepName.Should().Be("text-extraction");
    }

    [Fact]
    public async Task InvokeAsync_WithEmptyFilesToUpload_ShouldReturnSuccessWithEmptyFiles()
    {
        // Arrange
        var pipeline = new DataPipelineResult
        {
            FilesToUpload = new List<UploadedFile>()
        };

        _mockMarkitDownService.Setup(x => x.IsHealthyAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _handler.InvokeAsync(pipeline);

        // Assert
        result.Result.Should().Be(ReturnType.Success);
        result.Pipeline.Files.Should().BeEmpty();
    }

    [Fact]
    public async Task InvokeAsync_WithValidFile_ShouldProcessSuccessfully()
    {
        // Arrange
        var fileContent = "Test file content"u8.ToArray();
        var fileUpload = new UploadedFile
        {
            FileName = "test.txt",
            Bytes = fileContent,
            MimeType = "text/plain"
        };

        var pipeline = new DataPipelineResult
        {
            FilesToUpload = new List<UploadedFile> { fileUpload }
        };

        var extractedText = "# Extracted Text\nThis is the extracted markdown content.";

        _mockMarkitDownService.Setup(x => x.IsHealthyAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _mockMarkitDownService.Setup(x => x.ConvertToMarkdownAsync(
                fileContent,
                "test.txt",
                "text/plain",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(extractedText);

        // Act
        var result = await _handler.InvokeAsync(pipeline);

        // Assert
        result.Result.Should().Be(ReturnType.Success);

        // Should have one file with extracted text
        result.Pipeline.Files.Should().HaveCount(1);
        var file = result.Pipeline.Files[0];
        file.Name.Should().Be("test.txt");
        file.ArtifactType.Should().Be(ArtifactTypes.ExtractedText);

        // Extracted text should be stored in context
        var extractedTextKey = $"extracted_text_{file.Id}";
        result.Pipeline.ContextArguments.Should().ContainKey(extractedTextKey);
        result.Pipeline.ContextArguments[extractedTextKey].Should().Be(extractedText);
    }

    [Fact]
    public async Task InvokeAsync_WithUnhealthyService_ShouldStillProcess()
    {
        // Arrange
        var fileContent = "Test file content"u8.ToArray();
        var fileUpload = new UploadedFile
        {
            FileName = "test.txt",
            Bytes = fileContent,
            MimeType = "text/plain"
        };

        var pipeline = new DataPipelineResult
        {
            FilesToUpload = new List<UploadedFile> { fileUpload }
        };

        _mockMarkitDownService.Setup(x => x.IsHealthyAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _handler.InvokeAsync(pipeline);

        // Assert
        result.Result.Should().Be(ReturnType.Success);

        // Should have one file with fallback text
        result.Pipeline.Files.Should().HaveCount(1);
        var file = result.Pipeline.Files[0];
        file.Name.Should().Be("test.txt");
        file.ArtifactType.Should().Be(ArtifactTypes.ExtractedText);

        // Should contain fallback text in context
        var extractedTextKey = $"extracted_text_{file.Id}";
        result.Pipeline.ContextArguments.Should().ContainKey(extractedTextKey);
        var extractedText = result.Pipeline.ContextArguments[extractedTextKey] as string;
        extractedText.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task InvokeAsync_WithServiceFailure_ShouldUseFallbackText()
    {
        // Arrange
        var fileContent = "Test file content"u8.ToArray();
        var fileUpload = new UploadedFile
        {
            FileName = "test.txt",
            Bytes = fileContent,
            MimeType = "text/plain"
        };

        var pipeline = new DataPipelineResult
        {
            FilesToUpload = new List<UploadedFile> { fileUpload }
        };

        _mockMarkitDownService.Setup(x => x.IsHealthyAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _mockMarkitDownService.Setup(x => x.ConvertToMarkdownAsync(
                It.IsAny<byte[]>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Service error"));

        // Act
        var result = await _handler.InvokeAsync(pipeline);

        // Assert
        result.Result.Should().Be(ReturnType.Success);

        // Should have one file with fallback text
        result.Pipeline.Files.Should().HaveCount(1);
        var file = result.Pipeline.Files[0];
        file.Name.Should().Be("test.txt");
        file.ArtifactType.Should().Be(ArtifactTypes.ExtractedText);

        // Should contain fallback text in context
        var extractedTextKey = $"extracted_text_{file.Id}";
        result.Pipeline.ContextArguments.Should().ContainKey(extractedTextKey);
        var extractedText = result.Pipeline.ContextArguments[extractedTextKey] as string;
        extractedText.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task InvokeAsync_WithMultipleFiles_ShouldProcessAll()
    {
        // Arrange
        var pipeline = new DataPipelineResult
        {
            FilesToUpload = new List<UploadedFile>
            {
                new UploadedFile
                {
                    FileName = "file1.txt",
                    Bytes = "Content 1"u8.ToArray(),
                    MimeType = "text/plain"
                },
                new UploadedFile
                {
                    FileName = "file2.txt",
                    Bytes = "Content 2"u8.ToArray(),
                    MimeType = "text/plain"
                }
            }
        };

        _mockMarkitDownService.Setup(x => x.IsHealthyAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _mockMarkitDownService.Setup(x => x.ConvertToMarkdownAsync(
                It.IsAny<byte[]>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("Extracted content");

        // Act
        var result = await _handler.InvokeAsync(pipeline);

        // Assert
        result.Result.Should().Be(ReturnType.Success);
        result.Pipeline.Files.Should().HaveCount(2);

        var file1 = result.Pipeline.Files.First(f => f.Name == "file1.txt");
        var file2 = result.Pipeline.Files.First(f => f.Name == "file2.txt");

        // Both files should have extracted text in context
        result.Pipeline.ContextArguments.Should().ContainKey($"extracted_text_{file1.Id}");
        result.Pipeline.ContextArguments.Should().ContainKey($"extracted_text_{file2.Id}");
    }

    [Fact]
    public async Task InvokeAsync_WithCancellationToken_ShouldRespectCancellation()
    {
        // Arrange
        var fileUpload = new UploadedFile
        {
            FileName = "test.txt",
            Bytes = "Test content"u8.ToArray(),
            MimeType = "text/plain"
        };

        var pipeline = new DataPipelineResult
        {
            FilesToUpload = new List<UploadedFile> { fileUpload }
        };

        _mockMarkitDownService.Setup(x => x.IsHealthyAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            _handler.InvokeAsync(pipeline, cts.Token));
    }
}
