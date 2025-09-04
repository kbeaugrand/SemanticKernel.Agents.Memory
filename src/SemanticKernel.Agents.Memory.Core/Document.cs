using System;
using System.Collections.Generic;
using System.IO;

namespace SemanticKernel.Agents.Memory.Core
{
    /// <summary>
    /// Describes a document being processed in the pipeline, including its content stream, identifier, and audit fields.
    /// </summary>
    public class Document
    {
        /// <summary>
        /// The stream containing the document's content.
        /// </summary>
        public Stream? ContentStream { get; set; }

        /// <summary>
        /// The unique identifier for the document.
        /// </summary>
        public string? DocumentId { get; set; }

        /// <summary>
        /// The source of the document (e.g., file path, URL).
        /// </summary>
        public string? Source { get; set; }

        /// <summary>
        /// The timestamp when the document was imported.
        /// </summary>
        public DateTime ImportedAt { get; set; }
        
        /// <summary>
        /// Optional tags associated with the document import.
        /// </summary>
        public Dictionary<string, string>? Tags { get; set; }

        /// <summary>
        /// The MIME type of the document.
        /// </summary>
        public string MimeType { get; set; } = "application/octet-stream";

        /// <summary>
        /// The size of the document in bytes.
        /// </summary>
        public long Size { get; set; }

        /// <summary>
        /// The name of the document file.
        /// </summary>
        public string FileName { get; set; } = string.Empty;

        /// <summary>
        /// The content of the document as bytes.
        /// </summary>
        public byte[] Content { get; set; } = Array.Empty<byte>();

        /// <summary>
        /// Converts this document to an UploadedFile for pipeline processing.
        /// </summary>
        /// <returns>An UploadedFile representation of this document.</returns>
        public UploadedFile ToUploadedFile()
        {
            return new UploadedFile
            {
                FileName = FileName,
                Bytes = Content,
                MimeType = MimeType
            };
        }
    }
}
