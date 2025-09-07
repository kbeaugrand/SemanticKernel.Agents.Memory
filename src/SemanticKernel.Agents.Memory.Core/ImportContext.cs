using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SemanticKernel.Agents.Memory.Core
{
    /// <summary>
    /// Context interface for pipeline operations.
    /// </summary>
    public interface IContext { }

    /// <summary>
    /// Context for import operations.
    /// </summary>
    public class ImportContext : IContext
    {
        public string Index { get; set; } = string.Empty;
        public DocumentUploadRequest? UploadRequest { get; set; }
        public Dictionary<string, object> Arguments { get; set; } = new();
        public TagCollection Tags { get; set; } = new();
    }
}
