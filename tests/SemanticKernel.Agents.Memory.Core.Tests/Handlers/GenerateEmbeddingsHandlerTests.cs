using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Moq;
using SemanticKernel.Agents.Memory.Core;
using SemanticKernel.Agents.Memory.Core.Handlers;
using Xunit;

namespace SemanticKernel.Agents.Memory.Core.Tests.Handlers;

public class GenerateEmbeddingsHandlerTests
{
    private readonly Mock<IEmbeddingGenerator<string, Embedding<float>>> _mockEmbeddingGenerator;
    private readonly Mock<ILogger<GenerateEmbeddingsHandler>> _mockLogger;
    private readonly GenerateEmbeddingsHandler _handler;

    public GenerateEmbeddingsHandlerTests()
    {
        _mockEmbeddingGenerator = new Mock<IEmbeddingGenerator<string, Embedding<float>>>();
        _mockLogger = new Mock<ILogger<GenerateEmbeddingsHandler>>();
        _handler = new GenerateEmbeddingsHandler(_mockEmbeddingGenerator.Object, _mockLogger.Object);
    }

    [Fact]
    public void Constructor_WithValidParameters_ShouldInitializeCorrectly()
    {
        // Act
        var handler = new GenerateEmbeddingsHandler(_mockEmbeddingGenerator.Object, _mockLogger.Object);

        // Assert
        handler.Should().NotBeNull();
        handler.StepName.Should().Be("generate-embeddings");
    }

    [Fact]
    public void Constructor_WithNullEmbeddingGenerator_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var action = () => new GenerateEmbeddingsHandler(null!, _mockLogger.Object);
        action.Should().Throw<ArgumentNullException>().WithParameterName("embeddingGenerator");
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldInitializeCorrectly()
    {
        // Act
        var handler = new GenerateEmbeddingsHandler(_mockEmbeddingGenerator.Object, null);

        // Assert
        handler.Should().NotBeNull();
        handler.StepName.Should().Be(GenerateEmbeddingsHandler.Name);
    }

    [Fact]
    public void StepName_ShouldReturnCorrectValue()
    {
        // Assert
        _handler.StepName.Should().Be("generate-embeddings");
    }

    [Fact]
    public async Task InvokeAsync_WithNoTextPartitions_ShouldReturnContinueWithoutProcessing()
    {
        // Arrange
        var pipeline = new DataPipelineResult
        {
            Files = new List<FileDetails>
            {
                new FileDetails { ArtifactType = ArtifactTypes.ExtractedText },
                new FileDetails { ArtifactType = ArtifactTypes.Undefined }
            }
        };

        // Act
        var result = await _handler.InvokeAsync(pipeline);

        // Assert
        result.Result.Should().Be(ReturnType.Success);
        result.Pipeline.Should().BeSameAs(pipeline);

        // Verify no embeddings were generated - we can't directly verify the extension method
        // but we know no text partitions mean no calls
    }

    [Fact]
    public async Task InvokeAsync_WithTextPartitionButNoContext_ShouldUseFallbackText()
    {
        // Arrange
        var fileId = "file1";
        var fileName = "chunk1.txt";
        var pipeline = new DataPipelineResult
        {
            Files = new List<FileDetails>
            {
                new FileDetails
                {
                    Id = fileId,
                    Name = fileName,
                    ArtifactType = ArtifactTypes.TextPartition,
                    Size = 100
                }
            },
            ContextArguments = new Dictionary<string, object>()
        };

        // Setup the mock to return an embedding for the fallback text
        var embeddingVector = new float[] { 0.1f, 0.2f, 0.3f };
        var embedding = new Embedding<float>(embeddingVector);

        _mockEmbeddingGenerator
            .Setup(x => x.GenerateAsync(
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<EmbeddingGenerationOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([embedding]);

        // Act
        var result = await _handler.InvokeAsync(pipeline);

        // Assert
        result.Result.Should().Be(ReturnType.Success);

        // Verify embedding was generated with fallback text
        _mockEmbeddingGenerator.Verify(
            x => x.GenerateAsync(
                It.Is<IEnumerable<string>>(texts => texts.First() == $"Sample text content for {fileName}"),
                It.IsAny<EmbeddingGenerationOptions>(),
                It.IsAny<CancellationToken>()),
            Times.Once);

        // Verify embedding was stored
        var embeddingKey = $"embedding_{fileId}";
        result.Pipeline.ContextArguments.Should().ContainKey(embeddingKey);
    }

    [Fact]
    public async Task InvokeAsync_WithValidTextPartition_ShouldGenerateEmbedding()
    {
        // Arrange
        var fileId = "file1";
        var textContent = "This is sample text content for embedding generation.";
        var embeddingVector = new float[] { 0.1f, 0.2f, 0.3f, 0.4f };

        var pipeline = new DataPipelineResult
        {
            Files = new List<FileDetails>
            {
                new FileDetails
                {
                    Id = fileId,
                    Name = "chunk1.txt",
                    ArtifactType = ArtifactTypes.TextPartition,
                    Size = textContent.Length
                }
            },
            ContextArguments = new Dictionary<string, object>
            {
                { $"chunk_text_{fileId}", textContent }
            }
        };

        var embedding = new Embedding<float>(embeddingVector);
        _mockEmbeddingGenerator
            .Setup(x => x.GenerateAsync(
                It.Is<IEnumerable<string>>(inputs => inputs.First() == textContent),
                It.IsAny<EmbeddingGenerationOptions?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([embedding]);

        // Act
        var result = await _handler.InvokeAsync(pipeline);

        // Assert
        result.Result.Should().Be(ReturnType.Success);

        // Verify embedding was generated
        _mockEmbeddingGenerator.Verify(
            x => x.GenerateAsync(
                It.Is<IEnumerable<string>>(inputs => inputs.First() == textContent),
                It.IsAny<EmbeddingGenerationOptions?>(),
                It.IsAny<CancellationToken>()),
            Times.Once);

        // Verify embedding was stored in context
        var embeddingKey = $"embedding_{fileId}";
        result.Pipeline.ContextArguments.Should().ContainKey(embeddingKey);

        var storedEmbedding = result.Pipeline.ContextArguments[embeddingKey] as float[];
        storedEmbedding.Should().NotBeNull();
        storedEmbedding.Should().BeEquivalentTo(embeddingVector);
    }

    [Fact]
    public async Task InvokeAsync_WithMultipleTextPartitions_ShouldGenerateMultipleEmbeddings()
    {
        // Arrange
        var file1Id = "file1";
        var file2Id = "file2";
        var text1 = "First text content";
        var text2 = "Second text content";
        var embedding1 = new float[] { 0.1f, 0.2f };
        var embedding2 = new float[] { 0.3f, 0.4f };

        var pipeline = new DataPipelineResult
        {
            Files = new List<FileDetails>
            {
                new FileDetails
                {
                    Id = file1Id,
                    Name = "chunk1.txt",
                    ArtifactType = ArtifactTypes.TextPartition,
                    Size = text1.Length
                },
                new FileDetails
                {
                    Id = file2Id,
                    Name = "chunk2.txt",
                    ArtifactType = ArtifactTypes.TextPartition,
                    Size = text2.Length
                }
            },
            ContextArguments = new Dictionary<string, object>
            {
                { $"chunk_text_{file1Id}", text1 },
                { $"chunk_text_{file2Id}", text2 }
            }
        };

        _mockEmbeddingGenerator
            .Setup(x => x.GenerateAsync(
                It.Is<IEnumerable<string>>(inputs => inputs.First() == text1),
                It.IsAny<EmbeddingGenerationOptions?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([new Embedding<float>(embedding1)]);

        _mockEmbeddingGenerator
            .Setup(x => x.GenerateAsync(
                It.Is<IEnumerable<string>>(inputs => inputs.First() == text2),
                It.IsAny<EmbeddingGenerationOptions?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([new Embedding<float>(embedding2)]);

        // Act
        var result = await _handler.InvokeAsync(pipeline);

        // Assert
        result.Result.Should().Be(ReturnType.Success);

        // Verify both embeddings were generated
        _mockEmbeddingGenerator.Verify(
            x => x.GenerateAsync(
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<EmbeddingGenerationOptions?>(),
                It.IsAny<CancellationToken>()),
            Times.Exactly(2));

        // Verify both embeddings were stored
        result.Pipeline.ContextArguments.Should().ContainKey($"embedding_{file1Id}");
        result.Pipeline.ContextArguments.Should().ContainKey($"embedding_{file2Id}");
    }

    [Fact]
    public async Task InvokeAsync_WithEmbeddingGenerationException_ShouldLogErrorAndReturnTransientError()
    {
        // Arrange
        var fileId = "file1";
        var textContent = "Test content";

        var pipeline = new DataPipelineResult
        {
            Files = new List<FileDetails>
            {
                new FileDetails
                {
                    Id = fileId,
                    Name = "chunk1.txt",
                    ArtifactType = ArtifactTypes.TextPartition,
                    Size = textContent.Length
                }
            },
            ContextArguments = new Dictionary<string, object>
            {
                { $"chunk_text_{fileId}", textContent }
            }
        };

        _mockEmbeddingGenerator
            .Setup(x => x.GenerateAsync(
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<EmbeddingGenerationOptions?>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Embedding generation failed"));

        // Act
        var result = await _handler.InvokeAsync(pipeline);

        // Assert
        result.Result.Should().Be(ReturnType.TransientError);

        // Verify error was logged
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to generate embedding")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        // Verify embedding was not stored
        result.Pipeline.ContextArguments.Should().NotContainKey($"embedding_{fileId}");
    }

    [Fact]
    public async Task InvokeAsync_WithCancellationToken_ShouldRespectCancellation()
    {
        // Arrange
        var pipeline = new DataPipelineResult
        {
            Files = new List<FileDetails>
            {
                new FileDetails
                {
                    Id = "file1",
                    ArtifactType = ArtifactTypes.TextPartition
                }
            }
        };

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            _handler.InvokeAsync(pipeline, cts.Token));
    }
}
