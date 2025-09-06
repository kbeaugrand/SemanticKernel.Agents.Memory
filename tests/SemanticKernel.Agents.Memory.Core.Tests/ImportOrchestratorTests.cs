using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using SemanticKernel.Agents.Memory.Core;
using Xunit;

namespace SemanticKernel.Agents.Memory.Core.Tests;

public class ImportOrchestratorTests
{
    private readonly Mock<IServiceProvider> _mockServiceProvider;
    private readonly Mock<ILogger<ImportOrchestrator>> _mockLogger;
    private readonly MemoryIngestionOptions _options;

    public ImportOrchestratorTests()
    {
        _mockServiceProvider = new Mock<IServiceProvider>();
        _mockLogger = new Mock<ILogger<ImportOrchestrator>>();
        _options = new MemoryIngestionOptions();
    }

    [Fact]
    public void Constructor_WithValidParameters_ShouldInitializeCorrectly()
    {
        // Act
        var orchestrator = new ImportOrchestrator(_mockServiceProvider.Object, _options, _mockLogger.Object);

        // Assert
        orchestrator.Should().NotBeNull();
        orchestrator.HandlerNames.Should().BeEmpty(); // Since no handlers are configured
    }

    [Fact]
    public void Constructor_WithNullServiceProvider_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var action = () => new ImportOrchestrator(null!, _options, _mockLogger.Object);
        action.Should().Throw<ArgumentNullException>().WithParameterName("serviceProvider");
    }

    [Fact]
    public void Constructor_WithNullOptions_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var action = () => new ImportOrchestrator(_mockServiceProvider.Object, null!, _mockLogger.Object);
        action.Should().Throw<ArgumentNullException>().WithParameterName("options");
    }

    [Fact]
    public void HandlerNames_WithConfiguredHandlers_ShouldReturnCorrectNames()
    {
        // Arrange
        _options.WithHandler<MockHandler>("handler1");
        _options.WithHandler<MockHandler>("handler2");

        var orchestrator = new ImportOrchestrator(_mockServiceProvider.Object, _options, _mockLogger.Object);

        // Act
        var handlerNames = orchestrator.HandlerNames;

        // Assert
        handlerNames.Should().HaveCount(2);
        handlerNames.Should().Contain("handler1");
        handlerNames.Should().Contain("handler2");
    }

    [Fact]
    public async Task AddHandlerAsync_ShouldThrowNotSupportedException()
    {
        // Arrange
        var orchestrator = new ImportOrchestrator(_mockServiceProvider.Object, _options, _mockLogger.Object);
        var mockHandler = new Mock<IPipelineStepHandler>();

        // Act & Assert
        var action = () => orchestrator.AddHandlerAsync(mockHandler.Object);
        await action.Should().ThrowAsync<NotSupportedException>()
            .WithMessage("*dependency injection*");
    }

    [Fact]
    public async Task TryAddHandlerAsync_ShouldThrowNotSupportedException()
    {
        // Arrange
        var orchestrator = new ImportOrchestrator(_mockServiceProvider.Object, _options, _mockLogger.Object);
        var mockHandler = new Mock<IPipelineStepHandler>();

        // Act & Assert
        var action = () => orchestrator.TryAddHandlerAsync(mockHandler.Object);
        await action.Should().ThrowAsync<NotSupportedException>()
            .WithMessage("*dependency injection*");
    }

    [Fact]
    public void Dispose_ShouldNotThrow()
    {
        // Arrange
        var orchestrator = new ImportOrchestrator(_mockServiceProvider.Object, _options, _mockLogger.Object);

        // Act & Assert
        var action = () => orchestrator.Dispose();
        action.Should().NotThrow();
    }

    // Mock handler for testing
    private class MockHandler : IPipelineStepHandler
    {
        public string StepName => "mock";

        public Task<(ReturnType Result, DataPipelineResult Pipeline)> InvokeAsync(DataPipelineResult pipeline, CancellationToken ct = default)
        {
            return Task.FromResult((ReturnType.Success, pipeline));
        }
    }
}
