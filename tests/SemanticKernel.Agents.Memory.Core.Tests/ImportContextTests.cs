using System;
using System.Collections.Generic;
using FluentAssertions;
using SemanticKernel.Agents.Memory.Core;
using Xunit;

namespace SemanticKernel.Agents.Memory.Core.Tests;

public class ImportContextTests
{
    [Fact]
    public void Constructor_ShouldInitializeWithDefaultValues()
    {
        // Act
        var context = new ImportContext();

        // Assert
        context.Index.Should().Be(string.Empty);
        context.UploadRequest.Should().BeNull();
        context.Arguments.Should().NotBeNull().And.BeEmpty();
        context.Tags.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void Index_ShouldBeSettableAndGettable()
    {
        // Arrange
        var context = new ImportContext();

        // Act
        context.Index = "test-index";

        // Assert
        context.Index.Should().Be("test-index");
    }

    [Fact]
    public void Index_ShouldAllowEmptyString()
    {
        // Arrange
        var context = new ImportContext();

        // Act
        context.Index = "";

        // Assert
        context.Index.Should().Be("");
    }

    [Fact]
    public void Index_ShouldAllowNull()
    {
        // Arrange
        var context = new ImportContext();

        // Act
        context.Index = null!;

        // Assert
        context.Index.Should().BeNull();
    }

    [Fact]
    public void UploadRequest_ShouldBeSettableAndGettable()
    {
        // Arrange
        var context = new ImportContext();
        var uploadRequest = new DocumentUploadRequest();

        // Act
        context.UploadRequest = uploadRequest;

        // Assert
        context.UploadRequest.Should().BeSameAs(uploadRequest);
    }

    [Fact]
    public void UploadRequest_ShouldAllowNull()
    {
        // Arrange
        var context = new ImportContext();
        var uploadRequest = new DocumentUploadRequest();

        // Act
        context.UploadRequest = uploadRequest;
        context.UploadRequest = null;

        // Assert
        context.UploadRequest.Should().BeNull();
    }

    [Fact]
    public void Arguments_ShouldBeModifiable()
    {
        // Arrange
        var context = new ImportContext();

        // Act
        context.Arguments["key1"] = "value1";
        context.Arguments["key2"] = 123;
        context.Arguments["key3"] = new { Name = "Test" };

        // Assert
        context.Arguments.Should().HaveCount(3);
        context.Arguments["key1"].Should().Be("value1");
        context.Arguments["key2"].Should().Be(123);
        context.Arguments["key3"].Should().BeEquivalentTo(new { Name = "Test" });
    }

    [Fact]
    public void Arguments_ShouldAllowOverwriting()
    {
        // Arrange
        var context = new ImportContext();

        // Act
        context.Arguments["key"] = "original";
        context.Arguments["key"] = "updated";

        // Assert
        context.Arguments.Should().HaveCount(1);
        context.Arguments["key"].Should().Be("updated");
    }

    [Fact]
    public void Arguments_ShouldSupportNullValues()
    {
        // Arrange
        var context = new ImportContext();

        // Act
        context.Arguments["null-key"] = null!;

        // Assert
        context.Arguments.Should().ContainKey("null-key");
        context.Arguments["null-key"].Should().BeNull();
    }

    [Fact]
    public void Arguments_CanBeReassigned()
    {
        // Arrange
        var context = new ImportContext();
        var originalArgs = new Dictionary<string, object> { { "key1", "value1" } };
        var newArgs = new Dictionary<string, object> { { "key2", "value2" } };

        // Act
        context.Arguments = originalArgs;
        context.Arguments = newArgs;

        // Assert
        context.Arguments.Should().BeSameAs(newArgs);
        context.Arguments.Should().NotBeSameAs(originalArgs);
        context.Arguments["key2"].Should().Be("value2");
        context.Arguments.Should().NotContainKey("key1");
    }

    [Fact]
    public void Tags_ShouldBeModifiable()
    {
        // Arrange
        var context = new ImportContext();

        // Act
        context.Tags["category"] = "test";
        context.Tags["priority"] = "high";

        // Assert
        context.Tags.Should().HaveCount(2);
        context.Tags["category"].Should().Be("test");
        context.Tags["priority"].Should().Be("high");
    }

    [Fact]
    public void Tags_ShouldAllowOverwriting()
    {
        // Arrange
        var context = new ImportContext();

        // Act
        context.Tags["key"] = "original";
        context.Tags["key"] = "updated";

        // Assert
        context.Tags.Should().HaveCount(1);
        context.Tags["key"].Should().Be("updated");
    }

    [Fact]
    public void Tags_CanBeReassigned()
    {
        // Arrange
        var context = new ImportContext();
        var originalTags = new TagCollection { { "key1", "value1" } };
        var newTags = new TagCollection { { "key2", "value2" } };

        // Act
        context.Tags = originalTags;
        context.Tags = newTags;

        // Assert
        context.Tags.Should().BeSameAs(newTags);
        context.Tags.Should().NotBeSameAs(originalTags);
        context.Tags["key2"].Should().Be("value2");
        context.Tags.Should().NotContainKey("key1");
    }

    [Fact]
    public void Properties_ShouldAllowChainedConfiguration()
    {
        // Arrange & Act
        var context = new ImportContext
        {
            Index = "my-index",
            UploadRequest = new DocumentUploadRequest(),
            Arguments = new Dictionary<string, object> { { "arg1", "value1" } },
            Tags = new TagCollection { { "tag1", "tagvalue1" } }
        };

        // Assert
        context.Index.Should().Be("my-index");
        context.UploadRequest.Should().NotBeNull();
        context.Arguments.Should().ContainKey("arg1");
        context.Tags.Should().ContainKey("tag1");
    }

    [Fact]
    public void ImportContext_ShouldSupportComplexArguments()
    {
        // Arrange
        var context = new ImportContext();
        var complexObject = new Dictionary<string, List<int>>
        {
            { "numbers", new List<int> { 1, 2, 3, 4, 5 } },
            { "fibonacci", new List<int> { 1, 1, 2, 3, 5, 8 } }
        };

        // Act
        context.Arguments["complex"] = complexObject;

        // Assert
        context.Arguments["complex"].Should().BeSameAs(complexObject);
        var retrieved = context.Arguments["complex"] as Dictionary<string, List<int>>;
        retrieved.Should().NotBeNull();
        retrieved!["numbers"].Should().Equal(1, 2, 3, 4, 5);
        retrieved["fibonacci"].Should().Equal(1, 1, 2, 3, 5, 8);
    }

    [Fact]
    public void ImportContext_ShouldHandleLargeCollections()
    {
        // Arrange
        var context = new ImportContext();

        // Act
        for (int i = 0; i < 1000; i++)
        {
            context.Arguments[$"key{i}"] = $"value{i}";
            context.Tags[$"tag{i}"] = $"tagvalue{i}";
        }

        // Assert
        context.Arguments.Should().HaveCount(1000);
        context.Tags.Should().HaveCount(1000);
        context.Arguments["key999"].Should().Be("value999");
        context.Tags["tag999"].Should().Be("tagvalue999");
    }
}
