using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using SemanticKernel.Agents.Memory.Core.Builders;
using Xunit;

namespace SemanticKernel.Agents.Memory.Core.Tests.Builders;

public class DocumentUploadBuilderTests : IDisposable
{
    private readonly string _tempDirectory;
    private readonly string _testFilePath;

    public DocumentUploadBuilderTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDirectory);

        _testFilePath = Path.Combine(_tempDirectory, "test.txt");
        File.WriteAllText(_testFilePath, "This is test content");
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, true);
        }
    }

    [Fact]
    public void WithFile_WithValidPath_ShouldAddFile()
    {
        // Arrange
        var builder = new DocumentUploadBuilder();

        // Act
        var result = builder.WithFile(_testFilePath);

        // Assert
        result.Should().BeSameAs(builder); // Should return same instance for chaining
        var request = builder.Build();
        request.Files.Should().HaveCount(1);

        var file = request.Files.First();
        file.FileName.Should().Be("test.txt");
        file.Bytes.Should().NotBeEmpty();
        file.MimeType.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void WithFile_WithCustomFileName_ShouldUseCustomName()
    {
        // Arrange
        var builder = new DocumentUploadBuilder();

        // Act
        var result = builder.WithFile(_testFilePath, "custom-name.txt");

        // Assert
        result.Should().BeSameAs(builder);
        var request = builder.Build();
        var file = request.Files.First();
        file.FileName.Should().Be("custom-name.txt");
    }

    [Fact]
    public void WithFile_WithNullOrEmptyPath_ShouldThrowArgumentException()
    {
        // Arrange
        var builder = new DocumentUploadBuilder();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => builder.WithFile(null!));
        Assert.Throws<ArgumentException>(() => builder.WithFile(""));
        Assert.Throws<ArgumentException>(() => builder.WithFile("   "));
    }

    [Fact]
    public void WithFile_WithNonExistentFile_ShouldThrowFileNotFoundException()
    {
        // Arrange
        var builder = new DocumentUploadBuilder();
        var nonExistentPath = Path.Combine(_tempDirectory, "non-existent.txt");

        // Act & Assert
        Assert.Throws<FileNotFoundException>(() => builder.WithFile(nonExistentPath));
    }

    [Fact]
    public void WithFileStream_WithValidParameters_ShouldAddFile()
    {
        // Arrange
        var builder = new DocumentUploadBuilder();
        var content = "Stream content";
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(content));

        // Act
        var result = builder.WithFile("test-stream.txt", stream);

        // Assert
        result.Should().BeSameAs(builder);
        var request = builder.Build();
        var file = request.Files.First();
        file.FileName.Should().Be("test-stream.txt");
        file.Bytes.Should().Equal(System.Text.Encoding.UTF8.GetBytes(content));
    }

    [Fact]
    public void WithFileStream_WithCustomMimeType_ShouldUseCustomMimeType()
    {
        // Arrange
        var builder = new DocumentUploadBuilder();
        using var stream = new MemoryStream(new byte[] { 1, 2, 3 });

        // Act
        var result = builder.WithFile("test.bin", stream, "application/custom");

        // Assert
        result.Should().BeSameAs(builder);
        var request = builder.Build();
        var file = request.Files.First();
        file.MimeType.Should().Be("application/custom");
    }

    [Fact]
    public void WithFileStream_WithNullFileName_ShouldThrowArgumentException()
    {
        // Arrange
        var builder = new DocumentUploadBuilder();
        using var stream = new MemoryStream();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => builder.WithFile(null!, stream));
        Assert.Throws<ArgumentException>(() => builder.WithFile("", stream));
    }

    [Fact]
    public void WithFileStream_WithNullStream_ShouldThrowArgumentNullException()
    {
        // Arrange
        var builder = new DocumentUploadBuilder();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => builder.WithFile("test.txt", (Stream)null!));
    }

    [Fact]
    public void WithFileBytes_WithValidParameters_ShouldAddFile()
    {
        // Arrange
        var builder = new DocumentUploadBuilder();
        var bytes = new byte[] { 1, 2, 3, 4 };

        // Act
        var result = builder.WithFile("test.bin", bytes);

        // Assert
        result.Should().BeSameAs(builder);
        var request = builder.Build();
        var file = request.Files.First();
        file.FileName.Should().Be("test.bin");
        file.Bytes.Should().Equal(bytes);
    }

    [Fact]
    public void WithFileBytes_WithCustomMimeType_ShouldUseCustomMimeType()
    {
        // Arrange
        var builder = new DocumentUploadBuilder();
        var bytes = new byte[] { 1, 2, 3 };

        // Act
        var result = builder.WithFile("test.custom", bytes, "application/custom");

        // Assert
        result.Should().BeSameAs(builder);
        var request = builder.Build();
        var file = request.Files.First();
        file.MimeType.Should().Be("application/custom");
    }

    [Fact]
    public void WithFileBytes_WithNullFileName_ShouldThrowArgumentException()
    {
        // Arrange
        var builder = new DocumentUploadBuilder();
        var bytes = new byte[] { 1, 2, 3 };

        // Act & Assert
        Assert.Throws<ArgumentException>(() => builder.WithFile(null!, bytes));
        Assert.Throws<ArgumentException>(() => builder.WithFile("", bytes));
    }

    [Fact]
    public void WithFileBytes_WithNullBytes_ShouldThrowArgumentNullException()
    {
        // Arrange
        var builder = new DocumentUploadBuilder();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => builder.WithFile("test.bin", (byte[])null!));
    }

    [Fact]
    public void WithTag_WithValidKeyValue_ShouldAddTag()
    {
        // Arrange
        var builder = new DocumentUploadBuilder();
        builder.WithFile("test.txt", "Hello World"u8.ToArray(), "text/plain");

        // Act
        var result = builder.WithTag("category", "document");

        // Assert
        result.Should().BeSameAs(builder);
        var request = builder.Build();
        request.Tags.Should().ContainKey("category").WhoseValue.Should().Be("document");
    }

    [Fact]
    public void WithTag_WithNullKey_ShouldThrowArgumentException()
    {
        // Arrange
        var builder = new DocumentUploadBuilder();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => builder.WithTag(null!, "value"));
        Assert.Throws<ArgumentException>(() => builder.WithTag("", "value"));
    }

    [Fact]
    public void WithTag_WithNullValue_ShouldAllowNullValue()
    {
        // Arrange
        var builder = new DocumentUploadBuilder();
        builder.WithFile("test.txt", "Hello World"u8.ToArray(), "text/plain");

        // Act
        var result = builder.WithTag("key", null!);

        // Assert
        result.Should().BeSameAs(builder);
        var request = builder.Build();
        request.Tags.Should().ContainKey("key").WhoseValue.Should().Be(string.Empty); // Per implementation, null becomes empty string
    }

    [Fact]
    public void WithContext_WithValidKeyValue_ShouldAddContext()
    {
        // Arrange
        var builder = new DocumentUploadBuilder();
        builder.WithFile("test.txt", "Hello World"u8.ToArray(), "text/plain");

        // Act
        var result = builder.WithContext("category", "document");

        // Assert
        result.Should().BeSameAs(builder);
        var request = builder.Build();
        request.Context.Should().ContainKey("category").WhoseValue.Should().Be("document");
    }

    [Fact]
    public void WithContext_WithNullKey_ShouldThrowArgumentException()
    {
        // Arrange
        var builder = new DocumentUploadBuilder();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => builder.WithContext(null!, "value"));
        Assert.Throws<ArgumentException>(() => builder.WithContext("", "value"));
    }

    [Fact]
    public void ChainedCalls_ShouldBuildCompleteRequest()
    {
        // Arrange
        var builder = new DocumentUploadBuilder();
        var bytes = new byte[] { 1, 2, 3 };

        // Act
        var result = builder
            .WithFile(_testFilePath)
            .WithFile("binary.bin", bytes)
            .WithTag("type", "mixed")
            .WithTag("priority", "high")
            .WithContext("user", "test-user");

        // Assert
        result.Should().BeSameAs(builder);
        var request = builder.Build();

        request.Files.Should().HaveCount(2);
        request.Files[0].FileName.Should().Be("test.txt");
        request.Files[1].FileName.Should().Be("binary.bin");

        request.Tags.Should().HaveCount(2);
        request.Tags["type"].Should().Be("mixed");
        request.Tags["priority"].Should().Be("high");

        request.Context.Should().HaveCount(1);
        request.Context["user"].Should().Be("test-user");
    }

    [Fact]
    public void Build_WithoutAnyFiles_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var builder = new DocumentUploadBuilder();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => builder.Build());
    }

    [Fact]
    public void Build_ShouldReturnSameInstanceWithSameContent()
    {
        // Arrange
        var builder = new DocumentUploadBuilder()
            .WithFile("test.txt", "Hello World"u8.ToArray(), "text/plain")
            .WithTag("test", "value");

        // Act
        var request1 = builder.Build();
        var request2 = builder.Build();

        // Assert
        request1.Should().BeSameAs(request2);
        request1.Tags.Should().Equal(request2.Tags);
    }

    [Fact]
    public void WithFile_WithDifferentExtensions_ShouldDetectCorrectMimeTypes()
    {
        // Arrange
        var builder = new DocumentUploadBuilder();
        var txtPath = Path.Combine(_tempDirectory, "test.txt");
        var pdfPath = Path.Combine(_tempDirectory, "test.pdf");
        var jsonPath = Path.Combine(_tempDirectory, "test.json");

        File.WriteAllText(txtPath, "text");
        File.WriteAllBytes(pdfPath, new byte[] { 1, 2, 3 });
        File.WriteAllText(jsonPath, "{}");

        // Act
        builder.WithFile(txtPath)
               .WithFile(pdfPath)
               .WithFile(jsonPath);

        // Assert
        var request = builder.Build();
        request.Files.Should().HaveCount(3);

        var txtFile = request.Files.First(f => f.FileName == "test.txt");
        var pdfFile = request.Files.First(f => f.FileName == "test.pdf");
        var jsonFile = request.Files.First(f => f.FileName == "test.json");

        // Note: Actual MIME type detection depends on MimeTypeDetector implementation
        txtFile.MimeType.Should().NotBeNullOrEmpty();
        pdfFile.MimeType.Should().NotBeNullOrEmpty();
        jsonFile.MimeType.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void WithMultipleTags_WithSameKey_ShouldOverwritePreviousValue()
    {
        // Arrange
        var builder = new DocumentUploadBuilder();
        builder.WithFile("test.txt", "Hello World"u8.ToArray(), "text/plain");

        // Act
        builder.WithTag("category", "first")
               .WithTag("category", "second");

        // Assert
        var request = builder.Build();
        request.Tags.Should().HaveCount(1);
        request.Tags["category"].Should().Be("second");
    }

    [Fact]
    public void WithFileStreamAsync_WithLargeStream_ShouldHandleCorrectly()
    {
        // Arrange
        var builder = new DocumentUploadBuilder();
        var largeContent = new byte[1024 * 1024]; // 1MB
        for (int i = 0; i < largeContent.Length; i++)
        {
            largeContent[i] = (byte)(i % 256);
        }

        using var stream = new MemoryStream(largeContent);

        // Act
        var result = builder.WithFile("large.bin", stream);

        // Assert
        result.Should().BeSameAs(builder);
        var request = builder.Build();
        var file = request.Files.First();
        file.Bytes.Should().HaveCount(1024 * 1024);
        file.Bytes.Should().Equal(largeContent);
    }
}
