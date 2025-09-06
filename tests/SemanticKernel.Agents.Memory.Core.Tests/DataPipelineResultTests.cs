using System;
using System.Collections.Generic;
using FluentAssertions;
using SemanticKernel.Agents.Memory.Core;
using Xunit;

namespace SemanticKernel.Agents.Memory.Core.Tests;

public class DataPipelineResultTests
{
    [Fact]
    public void Constructor_ShouldInitializeWithDefaultValues()
    {
        // Act
        var result = new DataPipelineResult();

        // Assert
        result.Index.Should().Be(string.Empty);
        result.DocumentId.Should().NotBeNullOrEmpty();
        result.ExecutionId.Should().NotBeNullOrEmpty();
        result.Steps.Should().BeEmpty();
        result.RemainingSteps.Should().BeEmpty();
        result.CompletedSteps.Should().BeEmpty();
        result.Tags.Should().NotBeNull();
        result.Creation.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
        result.LastUpdate.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
        result.Files.Should().BeEmpty();
        result.ContextArguments.Should().BeEmpty();
        result.PreviousExecutionsToPurge.Should().BeEmpty();
        result.Complete.Should().BeFalse();
        result.FilesToUpload.Should().BeEmpty();
        result.UploadComplete.Should().BeFalse();
        result.Logs.Should().BeEmpty();
    }

    [Fact]
    public void DocumentId_ShouldBeUniqueForEachInstance()
    {
        // Act
        var result1 = new DataPipelineResult();
        var result2 = new DataPipelineResult();

        // Assert
        result1.DocumentId.Should().NotBe(result2.DocumentId);
    }

    [Fact]
    public void ExecutionId_ShouldBeUniqueForEachInstance()
    {
        // Act
        var result1 = new DataPipelineResult();
        var result2 = new DataPipelineResult();

        // Assert
        result1.ExecutionId.Should().NotBe(result2.ExecutionId);
    }

    [Fact]
    public void Then_ShouldAddStepToRemainingSteps()
    {
        // Arrange
        var result = new DataPipelineResult();

        // Act
        var returnedResult = result.Then("test-step");

        // Assert
        returnedResult.Should().BeSameAs(result); // Fluent interface
        result.RemainingSteps.Should().Contain("test-step");
    }

    [Fact]
    public void Complete_ShouldBeSettable()
    {
        // Arrange
        var result = new DataPipelineResult();

        // Act
        result.Complete = true;

        // Assert
        result.Complete.Should().BeTrue();
    }

    [Fact]
    public void UploadComplete_ShouldBeSettable()
    {
        // Arrange
        var result = new DataPipelineResult();

        // Act
        result.UploadComplete = true;

        // Assert
        result.UploadComplete.Should().BeTrue();
    }

    [Fact]
    public void Collections_ShouldBeModifiable()
    {
        // Arrange
        var result = new DataPipelineResult();

        // Act
        result.Steps.Add("step1");
        result.RemainingSteps.Add("remaining-step");
        result.CompletedSteps.Add("completed-step");
        result.Files.Add(new FileDetails());
        result.ContextArguments.Add("key", "value");
        result.PreviousExecutionsToPurge.Add(new DataPipelineResult());
        result.FilesToUpload.Add(new UploadedFile());

        // Assert
        result.Steps.Should().HaveCount(1);
        result.RemainingSteps.Should().HaveCount(1);
        result.CompletedSteps.Should().HaveCount(1);
        result.Files.Should().HaveCount(1);
        result.ContextArguments.Should().HaveCount(1);
        result.PreviousExecutionsToPurge.Should().HaveCount(1);
        result.FilesToUpload.Should().HaveCount(1);
    }

    [Fact]
    public void Logs_ShouldBeModifiable()
    {
        // Arrange
        var result = new DataPipelineResult();
        var logEntry = new PipelineLogEntry();

        // Act
        result.Logs.Add(logEntry);

        // Assert
        result.Logs.Should().HaveCount(1);
        result.Logs[0].Should().BeSameAs(logEntry);
    }
}

public class ReturnTypeTests
{
    [Fact]
    public void ReturnType_ShouldHaveExpectedValues()
    {
        // Assert
        Enum.GetValues<ReturnType>().Should().Contain(new[]
        {
            ReturnType.Success,
            ReturnType.TransientError,
            ReturnType.FatalError
        });
    }
}

public class ArtifactTypesTests
{
    [Fact]
    public void ArtifactTypes_ShouldHaveExpectedValues()
    {
        // Assert
        Enum.GetValues<ArtifactTypes>().Should().Contain(new[]
        {
            ArtifactTypes.Undefined,
            ArtifactTypes.TextPartition,
            ArtifactTypes.ExtractedText,
            ArtifactTypes.TextEmbeddingVector,
            ArtifactTypes.SyntheticData,
            ArtifactTypes.ExtractedContent
        });
    }
}
