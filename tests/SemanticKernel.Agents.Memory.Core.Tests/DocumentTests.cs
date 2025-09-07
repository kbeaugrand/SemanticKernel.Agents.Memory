using System;
using System.Collections.Generic;
using System.IO;
using FluentAssertions;
using SemanticKernel.Agents.Memory.Core;
using Xunit;

namespace SemanticKernel.Agents.Memory.Core.Tests;

public class DocumentTests
{
    [Fact]
    public void Constructor_ShouldInitializeWithDefaultValues()
    {
        // Act
        var document = new Document();

        // Assert
        document.ContentStream.Should().BeNull();
        document.DocumentId.Should().BeNull();
        document.Source.Should().BeNull();
        document.ImportedAt.Should().Be(default(DateTime));
        document.Tags.Should().BeNull();
        document.MimeType.Should().Be("application/octet-stream");
        document.Size.Should().Be(0);
        document.FileName.Should().Be(string.Empty);
        document.Content.Should().BeEmpty();
    }

    [Fact]
    public void Properties_ShouldBeSettableAndGettable()
    {
        // Arrange
        var testStream = new MemoryStream();
        var testTime = DateTime.UtcNow;
        var testTags = new Dictionary<string, string> { { "key", "value" } };

        // Act
        var document = new Document
        {
            ContentStream = testStream,
            DocumentId = "test-doc-123",
            Source = "test-source.txt",
            ImportedAt = testTime,
            Tags = testTags,
            MimeType = "text/plain",
            Size = 1024,
            FileName = "test-file.txt"
        };

        // Assert
        document.ContentStream.Should().BeSameAs(testStream);
        document.DocumentId.Should().Be("test-doc-123");
        document.Source.Should().Be("test-source.txt");
        document.ImportedAt.Should().Be(testTime);
        document.Tags.Should().BeSameAs(testTags);
        document.MimeType.Should().Be("text/plain");
        document.Size.Should().Be(1024);
        document.FileName.Should().Be("test-file.txt");
    }

    [Fact]
    public void Content_ShouldBeModifiable()
    {
        // Arrange
        var document = new Document();
        var testContent = new byte[] { 1, 2, 3, 4, 5 };

        // Act
        document.Content = testContent;

        // Assert
        document.Content.Should().BeSameAs(testContent);
        document.Content.Should().Equal(1, 2, 3, 4, 5);
    }

    [Fact]
    public void Tags_WhenNull_ShouldAllowNullAssignment()
    {
        // Arrange
        var document = new Document();

        // Act
        document.Tags = null;

        // Assert
        document.Tags.Should().BeNull();
    }

    [Fact]
    public void Tags_WhenSet_ShouldAllowModification()
    {
        // Arrange
        var document = new Document
        {
            Tags = new Dictionary<string, string>()
        };

        // Act
        document.Tags["category"] = "test";
        document.Tags["priority"] = "high";

        // Assert
        document.Tags.Should().HaveCount(2);
        document.Tags["category"].Should().Be("test");
        document.Tags["priority"].Should().Be("high");
    }

    [Fact]
    public void ContentStream_WhenDisposed_ShouldNotThrow()
    {
        // Arrange
        var document = new Document();
        var stream = new MemoryStream();
        document.ContentStream = stream;

        // Act
        stream.Dispose();

        // Assert
        document.ContentStream.Should().BeSameAs(stream);
        // Should not throw when accessing disposed stream reference
    }

    [Fact]
    public void MimeType_DefaultValue_ShouldBeOctetStream()
    {
        // Arrange & Act
        var document = new Document();

        // Assert
        document.MimeType.Should().Be("application/octet-stream");
    }

    [Fact]
    public void FileName_DefaultValue_ShouldBeEmptyString()
    {
        // Arrange & Act
        var document = new Document();

        // Assert
        document.FileName.Should().Be(string.Empty);
    }

    [Fact]
    public void Size_DefaultValue_ShouldBeZero()
    {
        // Arrange & Act
        var document = new Document();

        // Assert
        document.Size.Should().Be(0);
    }

    [Fact]
    public void ImportedAt_DefaultValue_ShouldBeDefaultDateTime()
    {
        // Arrange & Act
        var document = new Document();

        // Assert
        document.ImportedAt.Should().Be(default(DateTime));
    }

    [Fact]
    public void Document_ShouldAllowNegativeSize()
    {
        // Arrange
        var document = new Document();

        // Act
        document.Size = -1;

        // Assert
        document.Size.Should().Be(-1);
    }

    [Fact]
    public void Document_ShouldAllowEmptyStringsForProperties()
    {
        // Act
        var document = new Document
        {
            DocumentId = "",
            Source = "",
            FileName = "",
            MimeType = ""
        };

        // Assert
        document.DocumentId.Should().Be("");
        document.Source.Should().Be("");
        document.FileName.Should().Be("");
        document.MimeType.Should().Be("");
    }

    [Fact]
    public void Document_ShouldAllowVeryLongStrings()
    {
        // Arrange
        var longString = new string('a', 10000);

        // Act
        var document = new Document
        {
            DocumentId = longString,
            Source = longString,
            FileName = longString,
            MimeType = longString
        };

        // Assert
        document.DocumentId.Should().Be(longString);
        document.Source.Should().Be(longString);
        document.FileName.Should().Be(longString);
        document.MimeType.Should().Be(longString);
    }

    [Fact]
    public void ToUploadedFile_ShouldCreateCorrectUploadedFile()
    {
        // Arrange
        var document = new Document
        {
            FileName = "test.txt",
            Content = new byte[] { 72, 101, 108, 108, 111 }, // "Hello" in bytes
            MimeType = "text/plain"
        };

        // Act
        var uploadedFile = document.ToUploadedFile();

        // Assert
        uploadedFile.FileName.Should().Be("test.txt");
        uploadedFile.Bytes.Should().Equal(72, 101, 108, 108, 111);
        uploadedFile.MimeType.Should().Be("text/plain");
    }

    [Fact]
    public void Content_ShouldSupportLargeArrays()
    {
        // Arrange
        var document = new Document();
        var largeContent = new byte[1024 * 1024]; // 1MB
        for (int i = 0; i < largeContent.Length; i++)
        {
            largeContent[i] = (byte)(i % 256);
        }

        // Act
        document.Content = largeContent;

        // Assert
        document.Content.Should().HaveCount(1024 * 1024);
        document.Content[0].Should().Be(0);
        document.Content[255].Should().Be(255);
        document.Content[256].Should().Be(0); // wraps around
    }
}
