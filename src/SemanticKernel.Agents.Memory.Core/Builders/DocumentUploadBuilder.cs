using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace SemanticKernel.Agents.Memory.Core.Builders;

/// <summary>
/// Fluent builder for creating document upload requests with convenient file upload methods.
/// </summary>
public sealed class DocumentUploadBuilder
{
    private readonly DocumentUploadRequest _request = new();
    private readonly MimeTypeDetector _mimeTypeDetector = new();

    /// <summary>
    /// Adds a file by its file path. The MIME type will be automatically detected from the file extension.
    /// </summary>
    /// <param name="filePath">The path to the file to upload.</param>
    /// <param name="customFileName">Optional custom file name to use instead of the original file name.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public DocumentUploadBuilder WithFile(string filePath, string? customFileName = null)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));

        if (!File.Exists(filePath))
            throw new FileNotFoundException($"File not found: {filePath}", filePath);

        var fileName = customFileName ?? Path.GetFileName(filePath);
        var bytes = File.ReadAllBytes(filePath);
        var mimeType = _mimeTypeDetector.GetMimeType(filePath);

        _request.Files.Add(new UploadedFile
        {
            FileName = fileName,
            Bytes = bytes,
            MimeType = mimeType
        });

        return this;
    }

    /// <summary>
    /// Adds a file from a stream with a specified file name. The MIME type will be automatically detected from the file extension.
    /// </summary>
    /// <param name="fileName">The name of the file.</param>
    /// <param name="stream">The stream containing the file data.</param>
    /// <param name="customMimeType">Optional custom MIME type. If not provided, it will be detected from the file extension.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public DocumentUploadBuilder WithFile(string fileName, Stream stream, string? customMimeType = null)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("File name cannot be null or empty.", nameof(fileName));

        if (stream == null)
            throw new ArgumentNullException(nameof(stream));

        if (!stream.CanRead)
            throw new ArgumentException("Stream must be readable.", nameof(stream));

        var bytes = ReadStreamToBytes(stream);
        var mimeType = customMimeType ?? _mimeTypeDetector.GetMimeType(fileName);

        _request.Files.Add(new UploadedFile
        {
            FileName = fileName,
            Bytes = bytes,
            MimeType = mimeType
        });

        return this;
    }

    /// <summary>
    /// Adds a file from a byte array with a specified file name. The MIME type will be automatically detected from the file extension.
    /// </summary>
    /// <param name="fileName">The name of the file.</param>
    /// <param name="bytes">The byte array containing the file data.</param>
    /// <param name="customMimeType">Optional custom MIME type. If not provided, it will be detected from the file extension.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public DocumentUploadBuilder WithFile(string fileName, byte[] bytes, string? customMimeType = null)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("File name cannot be null or empty.", nameof(fileName));

        if (bytes == null)
            throw new ArgumentNullException(nameof(bytes));

        var mimeType = customMimeType ?? _mimeTypeDetector.GetMimeType(fileName);

        _request.Files.Add(new UploadedFile
        {
            FileName = fileName,
            Bytes = bytes,
            MimeType = mimeType
        });

        return this;
    }

    /// <summary>
    /// Adds a file asynchronously from a stream with a specified file name. The MIME type will be automatically detected from the file extension.
    /// </summary>
    /// <param name="fileName">The name of the file.</param>
    /// <param name="stream">The stream containing the file data.</param>
    /// <param name="customMimeType">Optional custom MIME type. If not provided, it will be detected from the file extension.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when the file is added, returning the builder instance for method chaining.</returns>
    public async Task<DocumentUploadBuilder> WithFileAsync(string fileName, Stream stream, string? customMimeType = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("File name cannot be null or empty.", nameof(fileName));

        if (stream == null)
            throw new ArgumentNullException(nameof(stream));

        if (!stream.CanRead)
            throw new ArgumentException("Stream must be readable.", nameof(stream));

        var bytes = await ReadStreamToBytesAsync(stream, cancellationToken);
        var mimeType = customMimeType ?? _mimeTypeDetector.GetMimeType(fileName);

        _request.Files.Add(new UploadedFile
        {
            FileName = fileName,
            Bytes = bytes,
            MimeType = mimeType
        });

        return this;
    }

    /// <summary>
    /// Adds a file asynchronously by its file path. The MIME type will be automatically detected from the file extension.
    /// </summary>
    /// <param name="filePath">The path to the file to upload.</param>
    /// <param name="customFileName">Optional custom file name to use instead of the original file name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when the file is added, returning the builder instance for method chaining.</returns>
    public async Task<DocumentUploadBuilder> WithFileAsync(string filePath, string? customFileName = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));

        if (!File.Exists(filePath))
            throw new FileNotFoundException($"File not found: {filePath}", filePath);

        var fileName = customFileName ?? Path.GetFileName(filePath);
        var bytes = await File.ReadAllBytesAsync(filePath, cancellationToken);
        var mimeType = _mimeTypeDetector.GetMimeType(filePath);

        _request.Files.Add(new UploadedFile
        {
            FileName = fileName,
            Bytes = bytes,
            MimeType = mimeType
        });

        return this;
    }

    /// <summary>
    /// Adds multiple files by their file paths. The MIME types will be automatically detected from the file extensions.
    /// </summary>
    /// <param name="filePaths">The paths to the files to upload.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public DocumentUploadBuilder WithFiles(params string[] filePaths)
    {
        if (filePaths == null)
            throw new ArgumentNullException(nameof(filePaths));

        foreach (var filePath in filePaths)
        {
            WithFile(filePath);
        }

        return this;
    }

    /// <summary>
    /// Adds multiple files asynchronously by their file paths. The MIME types will be automatically detected from the file extensions.
    /// </summary>
    /// <param name="filePaths">The paths to the files to upload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when all files are added, returning the builder instance for method chaining.</returns>
    public async Task<DocumentUploadBuilder> WithFilesAsync(string[] filePaths, CancellationToken cancellationToken = default)
    {
        if (filePaths == null)
            throw new ArgumentNullException(nameof(filePaths));

        foreach (var filePath in filePaths)
        {
            await WithFileAsync(filePath, cancellationToken: cancellationToken);
        }

        return this;
    }

    /// <summary>
    /// Adds tags to the document upload request.
    /// </summary>
    /// <param name="key">The tag key.</param>
    /// <param name="value">The tag value.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public DocumentUploadBuilder WithTag(string key, string value)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Tag key cannot be null or empty.", nameof(key));

        _request.Tags[key] = value ?? string.Empty;
        return this;
    }

    /// <summary>
    /// Adds multiple tags to the document upload request.
    /// </summary>
    /// <param name="tags">The tags to add.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public DocumentUploadBuilder WithTags(IDictionary<string, string> tags)
    {
        if (tags == null)
            throw new ArgumentNullException(nameof(tags));

        foreach (var tag in tags)
        {
            _request.Tags[tag.Key] = tag.Value;
        }

        return this;
    }

    /// <summary>
    /// Adds context data to the document upload request.
    /// </summary>
    /// <param name="key">The context key.</param>
    /// <param name="value">The context value.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public DocumentUploadBuilder WithContext(string key, object value)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Context key cannot be null or empty.", nameof(key));

        _request.Context[key] = value;
        return this;
    }

    /// <summary>
    /// Adds multiple context entries to the document upload request.
    /// </summary>
    /// <param name="context">The context entries to add.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public DocumentUploadBuilder WithContext(IDictionary<string, object> context)
    {
        if (context == null)
            throw new ArgumentNullException(nameof(context));

        foreach (var entry in context)
        {
            _request.Context[entry.Key] = entry.Value;
        }

        return this;
    }

    /// <summary>
    /// Builds the document upload request.
    /// </summary>
    /// <returns>The configured document upload request.</returns>
    public DocumentUploadRequest Build()
    {
        if (_request.Files.Count == 0)
            throw new InvalidOperationException("At least one file must be added before building the request.");

        return _request;
    }

    private static byte[] ReadStreamToBytes(Stream stream)
    {
        if (stream is MemoryStream memoryStream)
        {
            return memoryStream.ToArray();
        }

        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        return ms.ToArray();
    }

    private static async Task<byte[]> ReadStreamToBytesAsync(Stream stream, CancellationToken cancellationToken)
    {
        if (stream is MemoryStream memoryStream)
        {
            return memoryStream.ToArray();
        }

        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms, cancellationToken);
        return ms.ToArray();
    }
}
