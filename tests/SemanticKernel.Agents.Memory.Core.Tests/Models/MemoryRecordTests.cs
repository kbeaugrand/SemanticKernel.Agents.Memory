using System;
using System.Collections.Generic;
using FluentAssertions;
using SemanticKernel.Agents.Memory.Core.Models;
using Xunit;

namespace SemanticKernel.Agents.Memory.Core.Tests.Models;

public class MemoryRecordTests
{
    [Fact]
    public void Constructor_ShouldInitializeWithDefaultValues()
    {
        // Act
        var record = new MemoryRecord();

        // Assert
        record.Id.Should().Be(string.Empty);
        record.DocumentId.Should().Be(string.Empty);
        record.ExecutionId.Should().Be(string.Empty);
        record.Index.Should().Be(string.Empty);
        record.FileName.Should().Be(string.Empty);
        record.Text.Should().Be(string.Empty);
        record.ArtifactType.Should().Be(string.Empty);
        record.PartitionNumber.Should().Be(0);
        record.SectionNumber.Should().Be(0);
        record.Tags.Should().NotBeNull().And.BeEmpty();
        record.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
        record.Embedding.IsEmpty.Should().BeTrue();
    }

    [Fact]
    public void Properties_ShouldBeSettableAndGettable()
    {
        // Arrange
        var testTime = DateTimeOffset.UtcNow.AddHours(-1);
        var testTags = new Dictionary<string, string> { { "key", "value" } };
        var testEmbedding = new float[] { 0.1f, 0.2f, 0.3f }.AsMemory();

        // Act
        var record = new MemoryRecord
        {
            Id = "test-id-123",
            DocumentId = "doc-456",
            ExecutionId = "exec-789",
            Index = "test-index",
            FileName = "test-file.txt",
            Text = "Test content",
            ArtifactType = "document",
            PartitionNumber = 5,
            SectionNumber = 3,
            Tags = testTags,
            CreatedAt = testTime,
            Embedding = testEmbedding
        };

        // Assert
        record.Id.Should().Be("test-id-123");
        record.DocumentId.Should().Be("doc-456");
        record.ExecutionId.Should().Be("exec-789");
        record.Index.Should().Be("test-index");
        record.FileName.Should().Be("test-file.txt");
        record.Text.Should().Be("Test content");
        record.ArtifactType.Should().Be("document");
        record.PartitionNumber.Should().Be(5);
        record.SectionNumber.Should().Be(3);
        record.Tags.Should().BeSameAs(testTags);
        record.CreatedAt.Should().Be(testTime);
        record.Embedding.ToArray().Should().Equal(0.1f, 0.2f, 0.3f);
    }

    [Fact]
    public void Tags_ShouldBeModifiable()
    {
        // Arrange
        var record = new MemoryRecord();

        // Act
        record.Tags["category"] = "test";
        record.Tags["priority"] = "high";

        // Assert
        record.Tags.Should().HaveCount(2);
        record.Tags["category"].Should().Be("test");
        record.Tags["priority"].Should().Be("high");
    }

    [Fact]
    public void Embedding_ShouldSupportEmptyMemory()
    {
        // Arrange
        var record = new MemoryRecord();

        // Act
        record.Embedding = ReadOnlyMemory<float>.Empty;

        // Assert
        record.Embedding.IsEmpty.Should().BeTrue();
        record.Embedding.Length.Should().Be(0);
    }

    [Fact]
    public void Embedding_ShouldSupportLargeVectors()
    {
        // Arrange
        var record = new MemoryRecord();
        var largeVector = new float[1536]; // Common embedding size
        for (int i = 0; i < largeVector.Length; i++)
        {
            largeVector[i] = i * 0.001f;
        }

        // Act
        record.Embedding = largeVector.AsMemory();

        // Assert
        record.Embedding.Length.Should().Be(1536);
        record.Embedding.Span[0].Should().Be(0f);
        record.Embedding.Span[1535].Should().BeApproximately(1.535f, 0.0001f);
    }

    [Fact]
    public void PartitionNumber_ShouldSupportNegativeValues()
    {
        // Arrange
        var record = new MemoryRecord();

        // Act
        record.PartitionNumber = -1;

        // Assert
        record.PartitionNumber.Should().Be(-1);
    }

    [Fact]
    public void SectionNumber_ShouldSupportNegativeValues()
    {
        // Arrange
        var record = new MemoryRecord();

        // Act
        record.SectionNumber = -1;

        // Assert
        record.SectionNumber.Should().Be(-1);
    }

    [Fact]
    public void StringProperties_ShouldAllowNullAssignment()
    {
        // Arrange
        var record = new MemoryRecord();

        // Act
        record.Id = null!;
        record.DocumentId = null!;
        record.ExecutionId = null!;
        record.Index = null!;
        record.FileName = null!;
        record.Text = null!;
        record.ArtifactType = null!;

        // Assert
        record.Id.Should().BeNull();
        record.DocumentId.Should().BeNull();
        record.ExecutionId.Should().BeNull();
        record.Index.Should().BeNull();
        record.FileName.Should().BeNull();
        record.Text.Should().BeNull();
        record.ArtifactType.Should().BeNull();
    }

    [Fact]
    public void StringProperties_ShouldAllowEmptyStrings()
    {
        // Arrange
        var record = new MemoryRecord();

        // Act
        record.Id = "";
        record.DocumentId = "";
        record.ExecutionId = "";
        record.Index = "";
        record.FileName = "";
        record.Text = "";
        record.ArtifactType = "";

        // Assert
        record.Id.Should().Be("");
        record.DocumentId.Should().Be("");
        record.ExecutionId.Should().Be("");
        record.Index.Should().Be("");
        record.FileName.Should().Be("");
        record.Text.Should().Be("");
        record.ArtifactType.Should().Be("");
    }

    [Fact]
    public void StringProperties_ShouldAllowVeryLongStrings()
    {
        // Arrange
        var record = new MemoryRecord();
        var longString = new string('a', 10000);

        // Act
        record.Id = longString;
        record.Text = longString;

        // Assert
        record.Id.Should().Be(longString);
        record.Text.Should().Be(longString);
    }

    [Fact]
    public void Tags_CanBeReassigned()
    {
        // Arrange
        var record = new MemoryRecord();
        var originalTags = new Dictionary<string, string> { { "key1", "value1" } };
        var newTags = new Dictionary<string, string> { { "key2", "value2" } };

        // Act
        record.Tags = originalTags;
        record.Tags = newTags;

        // Assert
        record.Tags.Should().BeSameAs(newTags);
        record.Tags.Should().NotBeSameAs(originalTags);
        record.Tags["key2"].Should().Be("value2");
        record.Tags.Should().NotContainKey("key1");
    }

    [Fact]
    public void CreatedAt_ShouldSupportPastAndFutureDates()
    {
        // Arrange
        var record = new MemoryRecord();
        var pastDate = DateTimeOffset.UtcNow.AddYears(-10);
        var futureDate = DateTimeOffset.UtcNow.AddYears(10);

        // Act & Assert
        record.CreatedAt = pastDate;
        record.CreatedAt.Should().Be(pastDate);

        record.CreatedAt = futureDate;
        record.CreatedAt.Should().Be(futureDate);
    }

    [Fact]
    public void CreatedAt_ShouldSupportDifferentTimeZones()
    {
        // Arrange
        var record = new MemoryRecord();
        var utcTime = DateTimeOffset.UtcNow;
        var localTime = DateTimeOffset.Now;
        var specificTimezone = new DateTimeOffset(2023, 1, 1, 12, 0, 0, TimeSpan.FromHours(5));

        // Act & Assert
        record.CreatedAt = utcTime;
        record.CreatedAt.Should().Be(utcTime);

        record.CreatedAt = localTime;
        record.CreatedAt.Should().Be(localTime);

        record.CreatedAt = specificTimezone;
        record.CreatedAt.Should().Be(specificTimezone);
        record.CreatedAt.Offset.Should().Be(TimeSpan.FromHours(5));
    }

    [Fact]
    public void Embedding_ShouldPreserveFloatPrecision()
    {
        // Arrange
        var record = new MemoryRecord();
        var preciseFloats = new float[] { 0.123456789f, -0.987654321f, float.MaxValue, float.MinValue, 0f };

        // Act
        record.Embedding = preciseFloats.AsMemory();

        // Assert
        var result = record.Embedding.ToArray();
        result.Should().HaveCount(5);
        result[0].Should().BeApproximately(0.123456789f, 0.000001f);
        result[1].Should().BeApproximately(-0.987654321f, 0.000001f);
        result[2].Should().Be(float.MaxValue);
        result[3].Should().Be(float.MinValue);
        result[4].Should().Be(0f);
    }
}
