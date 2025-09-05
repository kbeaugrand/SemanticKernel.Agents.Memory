using System;
using System.Collections.Generic;
using System.IO;

namespace SemanticKernel.Agents.Memory.Core.Builders;

/// <summary>
/// Simple MIME type detector based on file extensions.
/// </summary>
internal sealed class MimeTypeDetector
{
    private static readonly Dictionary<string, string> s_mimeTypeMappings = new(StringComparer.OrdinalIgnoreCase)
    {
        // Text formats
        { ".txt", "text/plain" },
        { ".md", "text/markdown" },
        { ".markdown", "text/markdown" },
        { ".html", "text/html" },
        { ".htm", "text/html" },
        { ".css", "text/css" },
        { ".js", "application/javascript" },
        { ".json", "application/json" },
        { ".xml", "application/xml" },
        { ".csv", "text/csv" },
        { ".tsv", "text/tab-separated-values" },
        { ".rtf", "application/rtf" },
        
        // Code files
        { ".cs", "text/x-csharp" },
        { ".vb", "text/x-vbasic" },
        { ".cpp", "text/x-c++src" },
        { ".c", "text/x-csrc" },
        { ".h", "text/x-chdr" },
        { ".java", "text/x-java-source" },
        { ".py", "text/x-python" },
        { ".rb", "text/x-ruby" },
        { ".php", "text/x-php" },
        { ".pl", "text/x-perl" },
        { ".sh", "text/x-shellscript" },
        { ".bat", "text/x-batch" },
        { ".ps1", "text/x-powershell" },
        { ".sql", "text/x-sql" },
        { ".yaml", "text/x-yaml" },
        { ".yml", "text/x-yaml" },
        { ".ini", "text/plain" },
        { ".cfg", "text/plain" },
        { ".conf", "text/plain" },
        { ".log", "text/plain" },
        
        // Microsoft Office formats
        { ".doc", "application/msword" },
        { ".docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document" },
        { ".xls", "application/vnd.ms-excel" },
        { ".xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" },
        { ".ppt", "application/vnd.ms-powerpoint" },
        { ".pptx", "application/vnd.openxmlformats-officedocument.presentationml.presentation" },
        
        // OpenDocument formats
        { ".odt", "application/vnd.oasis.opendocument.text" },
        { ".ods", "application/vnd.oasis.opendocument.spreadsheet" },
        { ".odp", "application/vnd.oasis.opendocument.presentation" },
        
        // PDF
        { ".pdf", "application/pdf" },
        
        // Images
        { ".jpg", "image/jpeg" },
        { ".jpeg", "image/jpeg" },
        { ".png", "image/png" },
        { ".gif", "image/gif" },
        { ".bmp", "image/bmp" },
        { ".svg", "image/svg+xml" },
        { ".tiff", "image/tiff" },
        { ".tif", "image/tiff" },
        { ".webp", "image/webp" },
        { ".ico", "image/x-icon" },
        
        // Audio
        { ".mp3", "audio/mpeg" },
        { ".wav", "audio/wav" },
        { ".ogg", "audio/ogg" },
        { ".m4a", "audio/mp4" },
        { ".aac", "audio/aac" },
        { ".flac", "audio/flac" },
        
        // Video
        { ".mp4", "video/mp4" },
        { ".avi", "video/x-msvideo" },
        { ".mov", "video/quicktime" },
        { ".wmv", "video/x-ms-wmv" },
        { ".mkv", "video/x-matroska" },
        { ".webm", "video/webm" },
        
        // Archives
        { ".zip", "application/zip" },
        { ".rar", "application/vnd.rar" },
        { ".7z", "application/x-7z-compressed" },
        { ".tar", "application/x-tar" },
        { ".gz", "application/gzip" },
        { ".bz2", "application/x-bzip2" },
        
        // eBooks
        { ".epub", "application/epub+zip" },
        { ".mobi", "application/x-mobipocket-ebook" },
        
        // Other common formats
        { ".exe", "application/vnd.microsoft.portable-executable" },
        { ".dll", "application/vnd.microsoft.portable-executable" },
        { ".dmg", "application/x-apple-diskimage" },
        { ".iso", "application/x-iso9660-image" },
        { ".deb", "application/vnd.debian.binary-package" },
        { ".rpm", "application/x-rpm" },
        { ".msi", "application/x-msi" },
    };

    /// <summary>
    /// Gets the MIME type for a file based on its extension.
    /// </summary>
    /// <param name="fileName">The file name or path.</param>
    /// <returns>The MIME type string, or "application/octet-stream" if the extension is not recognized.</returns>
    public string GetMimeType(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return "application/octet-stream";

        var extension = Path.GetExtension(fileName);
        if (string.IsNullOrEmpty(extension))
            return "application/octet-stream";

        return s_mimeTypeMappings.TryGetValue(extension, out var mimeType) 
            ? mimeType 
            : "application/octet-stream";
    }

    /// <summary>
    /// Checks if a file extension represents a text-based file.
    /// </summary>
    /// <param name="fileName">The file name or path.</param>
    /// <returns>True if the file is likely text-based, false otherwise.</returns>
    public bool IsTextFile(string fileName)
    {
        var mimeType = GetMimeType(fileName);
        return IsTextMimeType(mimeType);
    }

    /// <summary>
    /// Checks if a MIME type represents text-based content.
    /// </summary>
    /// <param name="mimeType">The MIME type to check.</param>
    /// <returns>True if the MIME type represents text content, false otherwise.</returns>
    public static bool IsTextMimeType(string? mimeType)
    {
        if (string.IsNullOrEmpty(mimeType))
            return false;

        return mimeType.StartsWith("text/", StringComparison.OrdinalIgnoreCase) ||
               mimeType.Equals("application/json", StringComparison.OrdinalIgnoreCase) ||
               mimeType.Equals("application/xml", StringComparison.OrdinalIgnoreCase) ||
               mimeType.Equals("application/javascript", StringComparison.OrdinalIgnoreCase) ||
               mimeType.Contains("xml", StringComparison.OrdinalIgnoreCase) ||
               mimeType.StartsWith("text/x-", StringComparison.OrdinalIgnoreCase);
    }
}
