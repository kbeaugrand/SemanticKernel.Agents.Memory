using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace SemanticKernel.Agents.Memory.Core
{
    #region Enums
    public enum ReturnType
    {
        Success,
        TransientError,
        FatalError
    }

    public enum ArtifactTypes
    {
        Undefined,
        TextPartition,
        ExtractedText,
        TextEmbeddingVector,
        SyntheticData,
        ExtractedContent
    }
    #endregion

    /// <summary>
    /// Represents the result of a data pipeline step.
    /// </summary>
    public class DataPipelineResult
    {
        public string Index { get; init; } = string.Empty;
        public string DocumentId { get; init; } = Guid.NewGuid().ToString("n");
        public string ExecutionId { get; init; } = Guid.NewGuid().ToString("n");
        public List<string> Steps { get; init; } = new();
        public List<string> RemainingSteps { get; init; } = new();
        public List<string> CompletedSteps { get; init; } = new();
        public TagCollection Tags { get; init; } = new();
        public DateTimeOffset Creation { get; init; } = DateTimeOffset.UtcNow;
        public DateTimeOffset LastUpdate { get; private set; } = DateTimeOffset.UtcNow;
        public List<FileDetails> Files { get; init; } = new();
        public IDictionary<string, object> ContextArguments { get; init; } = new Dictionary<string, object>();
        public List<DataPipelineResult> PreviousExecutionsToPurge { get; init; } = new();
        public bool Complete { get; set; }
        public List<UploadedFile> FilesToUpload { get; init; } = new();
        public bool UploadComplete { get; set; }
        public List<PipelineLogEntry> Logs { get; } = new();

        public DataPipelineResult Then(string step)
        {
            Steps.Add(step);
            RemainingSteps.Add(step);
            Touch();
            return this;
        }

        public void Log(IPipelineStepHandler source, string text)
        {
            Logs.Add(new PipelineLogEntry
            {
                Time = DateTimeOffset.UtcNow,
                Source = source?.StepName ?? "orchestrator",
                Text = text
            });
            Touch();
        }

        public void Touch() => LastUpdate = DateTimeOffset.UtcNow;
    }

    public sealed class DataPipelinePointer
    {
        public string Index { get; init; } = string.Empty;
        public string DocumentId { get; init; } = string.Empty;
        public string ExecutionId { get; init; } = string.Empty;
        public List<string> Steps { get; init; } = new();
    }

    public sealed class FileDetails
    {
        public string Id { get; init; } = Guid.NewGuid().ToString("n");
        public string Name { get; init; } = string.Empty;
        public long Size { get; init; }
        public string MimeType { get; init; } = "application/octet-stream";
        public ArtifactTypes ArtifactType { get; init; } = ArtifactTypes.Undefined;
        public int PartitionNumber { get; init; }
        public int SectionNumber { get; init; }
        public Dictionary<string, GeneratedFileDetails> GeneratedFiles { get; init; } = new();

        public string GetPartitionFileName(int partition) => $"{System.IO.Path.GetFileNameWithoutExtension(Name)}.part{partition:000}{System.IO.Path.GetExtension(Name)}";
    }

    public sealed class GeneratedFileDetails
    {
        public string ParentId { get; init; } = string.Empty;
        public string SourcePartitionId { get; init; } = string.Empty;
        public string ContentSHA256 { get; init; } = string.Empty;
    }

    public sealed class PipelineLogEntry
    {
        public DateTimeOffset Time { get; init; }
        public string Source { get; init; } = string.Empty;
        public string Text { get; init; } = string.Empty;
        public override string ToString() => $"[{Time:u}] {Source}: {Text}";
    }

    public sealed class TagCollection : Dictionary<string, string> { }

    public sealed class UploadedFile
    {
        public string FileName { get; init; } = string.Empty;
        public byte[] Bytes { get; init; } = Array.Empty<byte>();
        public string? MimeType { get; init; }
    }

    public sealed class DocumentUploadRequest
    {
        public List<UploadedFile> Files { get; init; } = new();
        public TagCollection Tags { get; init; } = new();
        public IDictionary<string, object> Context { get; init; } = new Dictionary<string, object>();
    }

    public sealed class PipelineStepFailedException : Exception
    {
        public string StepName { get; }
        public ReturnType Outcome { get; }
        public PipelineStepFailedException(string stepName, ReturnType outcome)
            : base($"Pipeline step '{stepName}' failed with outcome '{outcome}'.")
        {
            StepName = stepName;
            Outcome = outcome;
        }
    }
}
