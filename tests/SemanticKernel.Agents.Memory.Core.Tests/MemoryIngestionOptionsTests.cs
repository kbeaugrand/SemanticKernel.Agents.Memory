using System;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using SemanticKernel.Agents.Memory.Core;
using SemanticKernel.Agents.Memory.Core.Handlers;
using Xunit;

namespace SemanticKernel.Agents.Memory.Core.Tests;

public class MemoryIngestionOptionsTests
{
    [Fact]
    public void Constructor_ShouldInitializeWithEmptyCollections()
    {
        // Act
        var options = new MemoryIngestionOptions();

        // Assert
        options.Handlers.Should().BeEmpty();
    }

    [Fact]
    public void WithHandler_ShouldAddHandlerRegistration()
    {
        // Arrange
        var options = new MemoryIngestionOptions();

        // Act
        var result = options.WithHandler<MockHandler>("test-step");

        // Assert
        result.Should().BeSameAs(options); // Fluent interface
        options.Handlers.Should().HaveCount(1);

        var handler = options.Handlers[0];
        handler.HandlerType.Should().Be(typeof(MockHandler));
        handler.StepName.Should().Be("test-step");
        handler.Lifetime.Should().Be(ServiceLifetime.Scoped); // Default
    }

    [Fact]
    public void WithHandler_WithCustomLifetime_ShouldSetCorrectLifetime()
    {
        // Arrange
        var options = new MemoryIngestionOptions();

        // Act
        options.WithHandler<MockHandler>("test-step", ServiceLifetime.Singleton);

        // Assert
        var handler = options.Handlers[0];
        handler.Lifetime.Should().Be(ServiceLifetime.Singleton);
    }

    [Fact]
    public void WithTextChunking_ShouldAddChunkingHandler()
    {
        // Arrange
        var options = new MemoryIngestionOptions();

        // Act
        var result = options.WithTextChunking<SimpleTextChunking>();

        // Assert
        result.Should().BeSameAs(options);
        options.Handlers.Should().HaveCount(1);

        var handler = options.Handlers[0];
        handler.HandlerType.Should().Be(typeof(SimpleTextChunking));
        handler.StepName.Should().Be("text-chunking");
    }

    [Fact]
    public void WithSimpleTextChunking_WithoutOptions_ShouldAddHandler()
    {
        // Arrange
        var options = new MemoryIngestionOptions();

        // Act
        var result = options.WithSimpleTextChunking();

        // Assert
        result.Should().BeSameAs(options);
        options.Handlers.Should().HaveCount(1);

        var handler = options.Handlers[0];
        handler.HandlerType.Should().Be(typeof(SimpleTextChunking));
        handler.StepName.Should().Be("text-chunking");
    }

    [Fact]
    public void WithSimpleTextChunking_WithOptions_ShouldAddHandlerAndConfigureOptions()
    {
        // Arrange
        var options = new MemoryIngestionOptions();
        var chunkingOptions = new TextChunkingOptions
        {
            MaxChunkSize = 500,
            TextOverlap = 50
        };

        // Act
        var result = options.WithSimpleTextChunking(chunkingOptions);

        // Assert
        result.Should().BeSameAs(options);
        options.Handlers.Should().HaveCount(1);

        var handler = options.Handlers[0];
        handler.HandlerType.Should().Be(typeof(SimpleTextChunking));
        handler.StepName.Should().Be("text-chunking");
    }

    [Fact]
    public void WithSimpleTextChunking_WithConfigureFunction_ShouldAddHandlerAndConfigureOptions()
    {
        // Arrange
        var options = new MemoryIngestionOptions();

        // Act
        var result = options.WithSimpleTextChunking(() => new TextChunkingOptions
        {
            MaxChunkSize = 750,
            TextOverlap = 75
        });

        // Assert
        result.Should().BeSameAs(options);
        options.Handlers.Should().HaveCount(1);

        var handler = options.Handlers[0];
        handler.HandlerType.Should().Be(typeof(SimpleTextChunking));
        handler.StepName.Should().Be("text-chunking");
    }

    [Fact]
    public void WithSimpleTextChunking_WithNullConfigureFunction_ShouldThrowArgumentNullException()
    {
        // Arrange
        var options = new MemoryIngestionOptions();

        // Act & Assert
        var action = () => options.WithSimpleTextChunking((Func<TextChunkingOptions>)null!);
        action.Should().Throw<ArgumentNullException>().WithParameterName("configure");
    }

    [Fact]
    public void WithSemanticChunking_ShouldAddSemanticChunkingHandler()
    {
        // Arrange
        var options = new MemoryIngestionOptions();

        // Act
        var result = options.WithSemanticChunking();

        // Assert
        result.Should().BeSameAs(options);
        options.Handlers.Should().HaveCount(1);

        var handler = options.Handlers[0];
        handler.HandlerType.Should().Be(typeof(SemanticChunking));
        handler.StepName.Should().Be("text-chunking");
    }

    [Fact]
    public void ChainedConfiguration_ShouldAddMultipleHandlers()
    {
        // Arrange
        var options = new MemoryIngestionOptions();

        // Act
        var result = options
            .WithHandler<MockHandler>("step1")
            .WithTextChunking<SimpleTextChunking>()
            .WithHandler<MockHandler>("step3", ServiceLifetime.Singleton);

        // Assert
        result.Should().BeSameAs(options);
        options.Handlers.Should().HaveCount(3);

        options.Handlers[0].StepName.Should().Be("step1");
        options.Handlers[0].Lifetime.Should().Be(ServiceLifetime.Scoped);

        options.Handlers[1].StepName.Should().Be("text-chunking");
        options.Handlers[1].HandlerType.Should().Be(typeof(SimpleTextChunking));

        options.Handlers[2].StepName.Should().Be("step3");
        options.Handlers[2].Lifetime.Should().Be(ServiceLifetime.Singleton);
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

public class HandlerRegistrationTests
{
    [Fact]
    public void Constructor_WithValidParameters_ShouldInitializeCorrectly()
    {
        // Arrange
        var handlerType = typeof(MockHandler);
        var stepName = "test-step";
        var lifetime = ServiceLifetime.Transient;

        // Act
        var registration = new HandlerRegistration(handlerType, stepName, lifetime);

        // Assert
        registration.HandlerType.Should().Be(handlerType);
        registration.StepName.Should().Be(stepName);
        registration.Lifetime.Should().Be(lifetime);
    }

    [Fact]
    public void Constructor_WithNullHandlerType_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var action = () => new HandlerRegistration(null!, "test-step", ServiceLifetime.Scoped);
        action.Should().Throw<ArgumentNullException>().WithParameterName("handlerType");
    }

    [Fact]
    public void Constructor_WithNullStepName_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var action = () => new HandlerRegistration(typeof(MockHandler), null!, ServiceLifetime.Scoped);
        action.Should().Throw<ArgumentNullException>().WithParameterName("stepName");
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
