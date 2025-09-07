using System;
using FluentAssertions;
using SemanticKernel.Agents.Memory.Core.Builders;
using Xunit;

namespace SemanticKernel.Agents.Memory.Core.Tests.Builders;

public class MimeTypeDetectorTests
{
    [Theory]
    [InlineData("document.txt", "text/plain")]
    [InlineData("readme.md", "text/markdown")]
    [InlineData("page.html", "text/html")]
    [InlineData("data.json", "application/json")]
    [InlineData("document.pdf", "application/pdf")]
    [InlineData("document.docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document")]
    [InlineData("spreadsheet.xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
    [InlineData("image.jpg", "image/jpeg")]
    [InlineData("image.png", "image/png")]
    [InlineData("file.unknown", "application/octet-stream")]
    public void GetMimeType_WithKnownExtensions_ShouldReturnCorrectMimeType(string fileName, string expectedMimeType)
    {
        // Arrange
        var detector = new MimeTypeDetector();

        // Act
        var result = detector.GetMimeType(fileName);

        // Assert
        result.Should().Be(expectedMimeType);
    }

    [Theory]
    [InlineData("FILE.TXT", "text/plain")]
    [InlineData("Document.PDF", "application/pdf")]
    [InlineData("IMAGE.PNG", "image/png")]
    public void GetMimeType_WithDifferentCasing_ShouldBeCaseInsensitive(string fileName, string expectedMimeType)
    {
        // Arrange
        var detector = new MimeTypeDetector();

        // Act
        var result = detector.GetMimeType(fileName);

        // Assert
        result.Should().Be(expectedMimeType);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void GetMimeType_WithNullOrEmptyFileName_ShouldReturnDefaultMimeType(string? fileName)
    {
        // Arrange
        var detector = new MimeTypeDetector();

        // Act
        var result = detector.GetMimeType(fileName);

        // Assert
        result.Should().Be("application/octet-stream");
    }

    [Fact]
    public void GetMimeType_WithFileNameWithoutExtension_ShouldReturnDefaultMimeType()
    {
        // Arrange
        var detector = new MimeTypeDetector();

        // Act
        var result = detector.GetMimeType("filename_without_extension");

        // Assert
        result.Should().Be("application/octet-stream");
    }

    [Fact]
    public void GetMimeType_WithFullPath_ShouldExtractExtensionCorrectly()
    {
        // Arrange
        var detector = new MimeTypeDetector();

        // Act
        var result = detector.GetMimeType("/path/to/document.pdf");

        // Assert
        result.Should().Be("application/pdf");
    }

    [Fact]
    public void GetMimeType_WithWindowsPath_ShouldExtractExtensionCorrectly()
    {
        // Arrange
        var detector = new MimeTypeDetector();

        // Act
        var result = detector.GetMimeType(@"C:\Documents\file.docx");

        // Assert
        result.Should().Be("application/vnd.openxmlformats-officedocument.wordprocessingml.document");
    }

    [Theory]
    [InlineData(".txt", "text/plain")]
    [InlineData(".md", "text/markdown")]
    [InlineData(".markdown", "text/markdown")]
    [InlineData(".html", "text/html")]
    [InlineData(".css", "text/css")]
    [InlineData(".js", "application/javascript")]
    [InlineData(".json", "application/json")]
    [InlineData(".xml", "application/xml")]
    [InlineData(".csv", "text/csv")]
    public void GetMimeType_WithTextFormats_ShouldReturnCorrectMimeTypes(string extension, string expectedMimeType)
    {
        // Arrange
        var detector = new MimeTypeDetector();
        var fileName = $"test{extension}";

        // Act
        var result = detector.GetMimeType(fileName);

        // Assert
        result.Should().Be(expectedMimeType);
    }

    [Theory]
    [InlineData(".cs", "text/x-csharp")]
    [InlineData(".java", "text/x-java-source")]
    [InlineData(".py", "text/x-python")]
    [InlineData(".rb", "text/x-ruby")]
    [InlineData(".php", "text/x-php")]
    [InlineData(".sql", "text/x-sql")]
    [InlineData(".yaml", "text/x-yaml")]
    [InlineData(".yml", "text/x-yaml")]
    public void GetMimeType_WithCodeFormats_ShouldReturnCorrectMimeTypes(string extension, string expectedMimeType)
    {
        // Arrange
        var detector = new MimeTypeDetector();
        var fileName = $"test{extension}";

        // Act
        var result = detector.GetMimeType(fileName);

        // Assert
        result.Should().Be(expectedMimeType);
    }

    [Theory]
    [InlineData(".doc", "application/msword")]
    [InlineData(".docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document")]
    [InlineData(".xls", "application/vnd.ms-excel")]
    [InlineData(".xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
    [InlineData(".ppt", "application/vnd.ms-powerpoint")]
    [InlineData(".pptx", "application/vnd.openxmlformats-officedocument.presentationml.presentation")]
    public void GetMimeType_WithOfficeFormats_ShouldReturnCorrectMimeTypes(string extension, string expectedMimeType)
    {
        // Arrange
        var detector = new MimeTypeDetector();
        var fileName = $"test{extension}";

        // Act
        var result = detector.GetMimeType(fileName);

        // Assert
        result.Should().Be(expectedMimeType);
    }

    [Theory]
    [InlineData(".jpg", "image/jpeg")]
    [InlineData(".jpeg", "image/jpeg")]
    [InlineData(".png", "image/png")]
    [InlineData(".gif", "image/gif")]
    [InlineData(".bmp", "image/bmp")]
    [InlineData(".svg", "image/svg+xml")]
    [InlineData(".webp", "image/webp")]
    public void GetMimeType_WithImageFormats_ShouldReturnCorrectMimeTypes(string extension, string expectedMimeType)
    {
        // Arrange
        var detector = new MimeTypeDetector();
        var fileName = $"test{extension}";

        // Act
        var result = detector.GetMimeType(fileName);

        // Assert
        result.Should().Be(expectedMimeType);
    }

    [Fact]
    public void GetMimeType_WithMultipleDots_ShouldUseLastExtension()
    {
        // Arrange
        var detector = new MimeTypeDetector();

        // Act
        var result = detector.GetMimeType("archive.tar.gz");

        // Assert
        result.Should().Be("application/gzip");
    }

    [Fact]
    public void GetMimeType_WithDotAtEnd_ShouldReturnDefaultMimeType()
    {
        // Arrange
        var detector = new MimeTypeDetector();

        // Act
        var result = detector.GetMimeType("filename.");

        // Assert
        result.Should().Be("application/octet-stream");
    }

    [Theory]
    [InlineData(".mp3", "audio/mpeg")]
    [InlineData(".wav", "audio/wav")]
    [InlineData(".mp4", "video/mp4")]
    [InlineData(".avi", "video/x-msvideo")]
    public void GetMimeType_WithMediaFormats_ShouldReturnCorrectMimeTypes(string extension, string expectedMimeType)
    {
        // Arrange
        var detector = new MimeTypeDetector();
        var fileName = $"test{extension}";

        // Act
        var result = detector.GetMimeType(fileName);

        // Assert
        result.Should().Be(expectedMimeType);
    }

    [Theory]
    [InlineData(".zip", "application/zip")]
    [InlineData(".rar", "application/x-rar-compressed")]
    [InlineData(".7z", "application/x-7z-compressed")]
    [InlineData(".tar", "application/x-tar")]
    [InlineData(".gz", "application/gzip")]
    public void GetMimeType_WithArchiveFormats_ShouldReturnCorrectMimeTypes(string extension, string expectedMimeType)
    {
        // Arrange
        var detector = new MimeTypeDetector();
        var fileName = $"test{extension}";

        // Act
        var result = detector.GetMimeType(fileName);

        // Assert
        result.Should().Be(expectedMimeType);
    }

    [Fact]
    public void GetMimeType_WithVeryLongFileName_ShouldHandleCorrectly()
    {
        // Arrange
        var detector = new MimeTypeDetector();
        var longFileName = new string('a', 1000) + ".txt";

        // Act
        var result = detector.GetMimeType(longFileName);

        // Assert
        result.Should().Be("text/plain");
    }

    [Fact]
    public void GetMimeType_WithSpecialCharactersInFileName_ShouldExtractExtensionCorrectly()
    {
        // Arrange
        var detector = new MimeTypeDetector();

        // Act
        var result = detector.GetMimeType("file with spaces & symbols!@#.pdf");

        // Assert
        result.Should().Be("application/pdf");
    }
}
