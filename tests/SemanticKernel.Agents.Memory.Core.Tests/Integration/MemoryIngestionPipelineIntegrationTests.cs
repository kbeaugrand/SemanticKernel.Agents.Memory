using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using SemanticKernel.Agents.Memory.Core;
using SemanticKernel.Agents.Memory.Core.Handlers;
using SemanticKernel.Agents.Memory.Core.Services;
using SemanticKernel.Agents.Memory.Core.Tests.TestUtilities;
using Xunit;

namespace SemanticKernel.Agents.Memory.Core.Tests.Integration;

public class MemoryIngestionPipelineIntegrationTests
{
    [Fact]
    public async Task FullPipeline_WithTextExtraction_ShouldProcessDocumentEndToEnd()
    {
        // Arrange
        var services = new ServiceCollection();
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Debug));
        
        // Setup mocks
        var mockMarkitDownService = new Mock<IMarkitDownService>();
        var mockEmbeddingGenerator = new Mock<IEmbeddingGenerator<string, Embedding<float>>>();

        mockMarkitDownService.Setup(x => x.IsHealthyAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        
        mockMarkitDownService.Setup(x => x.ConvertToMarkdownAsync(
                It.IsAny<byte[]>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("# Extracted Document\n\nThis is the extracted text from the document.");

        mockEmbeddingGenerator.Setup(x => x.GenerateAsync(
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<EmbeddingGenerationOptions?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([new Embedding<float>(new float[] { 0.1f, 0.2f, 0.3f, 0.4f })]);

        // Configure services
        services.AddSingleton(loggerFactory);
        services.AddLogging();
        services.AddSingleton<IMarkitDownService>(mockMarkitDownService.Object);
        services.AddSingleton<IEmbeddingGenerator<string, Embedding<float>>>(mockEmbeddingGenerator.Object);
        services.AddScoped<TextExtractionHandler>();
        services.AddScoped<SimpleTextChunking>();
        services.AddScoped<GenerateEmbeddingsHandler>();

        // Configure memory ingestion options
        var options = new MemoryIngestionOptions()
            .WithHandler<TextExtractionHandler>("text-extraction")
            .WithSimpleTextChunking(new TextChunkingOptions { MaxChunkSize = 100, TextOverlap = 20 })
            .WithHandler<GenerateEmbeddingsHandler>("generate-embeddings");

        services.AddSingleton(options);
        services.AddScoped<ImportOrchestrator>();

        var serviceProvider = services.BuildServiceProvider();

        // Create test data
        var fileUpload = TestDataFactory.CreateSampleFileUpload(
            "test-document.pdf",
            "This is a test document that will be processed through the pipeline.",
            "application/pdf");

        var pipeline = new DataPipelineResult
        {
            FilesToUpload = new List<UploadedFile> { fileUpload }
        };

        // Act
        var orchestrator = serviceProvider.GetRequiredService<ImportOrchestrator>();
        
        // Execute text extraction
        var textExtractionHandler = serviceProvider.GetRequiredService<TextExtractionHandler>();
        var textResult = await textExtractionHandler.InvokeAsync(pipeline);
        
        // Execute text chunking
        var chunkingHandler = serviceProvider.GetRequiredService<SimpleTextChunking>();
        var chunkResult = await chunkingHandler.InvokeAsync(textResult.Pipeline);
        
        // Execute embedding generation
        var embeddingHandler = serviceProvider.GetRequiredService<GenerateEmbeddingsHandler>();
        var embeddingResult = await embeddingHandler.InvokeAsync(chunkResult.Pipeline);

        // Assert
        textResult.Result.Should().Be(ReturnType.Success);
        chunkResult.Result.Should().Be(ReturnType.Success);
        embeddingResult.Result.Should().Be(ReturnType.Success);

        var finalPipeline = embeddingResult.Pipeline;

        // Verify files were processed
        finalPipeline.Files.Should().HaveCountGreaterThan(0);
        var files = finalPipeline.Files;
        
        // Verify extracted text files exist
        var extractedTextFiles = files.Where(f => f.ArtifactType == ArtifactTypes.ExtractedText).ToList();
        extractedTextFiles.Should().HaveCountGreaterThan(0);
        
        // Verify chunked files exist  
        var chunkFiles = files.Where(f => f.ArtifactType == ArtifactTypes.TextPartition).ToList();
        chunkFiles.Should().HaveCountGreaterThan(0);
        
        // Verify content is stored in context arguments
        foreach (var chunkFile in chunkFiles)
        {
            var chunkTextKey = $"chunk_text_{chunkFile.Id}";
            finalPipeline.ContextArguments.Should().ContainKey(chunkTextKey);
            var chunkText = finalPipeline.ContextArguments[chunkTextKey] as string;
            chunkText.Should().NotBeNullOrEmpty();
        }

        // Verify service calls
        mockMarkitDownService.Verify(x => x.ConvertToMarkdownAsync(
            It.IsAny<byte[]>(),
            "test-document.pdf",
            "application/pdf",
            It.IsAny<CancellationToken>()), Times.Once);

        // Note: Embedding generation verification would depend on the actual implementation
        // of how chunks are converted to FileDetails with ArtifactTypes.TextPartition
    }

    [Fact]
    public async Task Pipeline_WithFailedTextExtraction_ShouldHandleGracefully()
    {
        // Arrange
        var services = new ServiceCollection();
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Debug));
        
        var mockMarkitDownService = new Mock<IMarkitDownService>();
        mockMarkitDownService.Setup(x => x.IsHealthyAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        
        mockMarkitDownService.Setup(x => x.ConvertToMarkdownAsync(
                It.IsAny<byte[]>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Service unavailable"));

        services.AddSingleton(loggerFactory);
        services.AddLogging();
        services.AddSingleton<IMarkitDownService>(mockMarkitDownService.Object);
        services.AddScoped<TextExtractionHandler>();

        var serviceProvider = services.BuildServiceProvider();

        var fileUpload = TestDataFactory.CreateSampleFileUpload();
        var pipeline = new DataPipelineResult
        {
            FilesToUpload = new List<UploadedFile> { fileUpload }
        };

        // Act
        var handler = serviceProvider.GetRequiredService<TextExtractionHandler>();
        var result = await handler.InvokeAsync(pipeline);

        // Assert
        result.Result.Should().Be(ReturnType.Success); // Pipeline continues despite error
        result.Pipeline.Files.Should().HaveCount(1); // File was processed using fallback text extraction
        result.Pipeline.Files[0].ArtifactType.Should().Be(ArtifactTypes.ExtractedText);
        result.Pipeline.Files[0].GeneratedFiles.Should().ContainKey("extracted.txt");
    }

    [Fact]
    public void MemoryIngestionOptions_FluentConfiguration_ShouldConfigureHandlersCorrectly()
    {
        // Act
        var options = new MemoryIngestionOptions()
            .WithHandler<TextExtractionHandler>("text-extraction")
            .WithSimpleTextChunking(() => new TextChunkingOptions { MaxChunkSize = 500 })
            .WithSemanticChunking()
            .WithHandler<GenerateEmbeddingsHandler>("generate-embeddings");

        // Assert
        options.Handlers.Should().HaveCount(4);
        
        options.Handlers[0].StepName.Should().Be("text-extraction");
        options.Handlers[0].HandlerType.Should().Be(typeof(TextExtractionHandler));
        
        options.Handlers[1].StepName.Should().Be("text-chunking");
        options.Handlers[1].HandlerType.Should().Be(typeof(SimpleTextChunking));
        
        options.Handlers[2].StepName.Should().Be("text-chunking");
        options.Handlers[2].HandlerType.Should().Be(typeof(SemanticChunking));
        
        options.Handlers[3].StepName.Should().Be("generate-embeddings");
        options.Handlers[3].HandlerType.Should().Be(typeof(GenerateEmbeddingsHandler));
    }

    [Fact]
    public void ImportOrchestrator_HandlerNames_ShouldReturnConfiguredHandlers()
    {
        // Arrange
        var services = new ServiceCollection();
        var options = new MemoryIngestionOptions()
            .WithHandler<TextExtractionHandler>("text-extraction")
            .WithSimpleTextChunking()
            .WithHandler<GenerateEmbeddingsHandler>("generate-embeddings");

        services.AddSingleton(options);
        var serviceProvider = services.BuildServiceProvider();

        // Act
        var orchestrator = new ImportOrchestrator(serviceProvider, options);

        // Assert
        orchestrator.HandlerNames.Should().BeEquivalentTo(new[]
        {
            "text-extraction",
            "text-chunking", 
            "generate-embeddings"
        });
    }
}
