using System;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace SemanticKernel.Agents.Memory.Core.Handlers;

/// <summary>
/// Text extraction pipeline step handler.
/// </summary>
public sealed class TextExtractionHandler : IPipelineStepHandler
{
    public const string Name = "text-extraction";
    public string StepName => Name;

    public async Task<(ReturnType Result, DataPipelineResult Pipeline)> InvokeAsync(DataPipelineResult pipeline, CancellationToken ct = default)
    {
        // Simulate work: convert uploaded files into FileDetails with ExtractedText artifacts
        foreach (var upload in pipeline.FilesToUpload)
        {
            ct.ThrowIfCancellationRequested();

            var details = new FileDetails
            {
                Name = upload.FileName,
                Size = upload.Bytes.LongLength,
                MimeType = upload.MimeType ?? "application/octet-stream",
                ArtifactType = ArtifactTypes.ExtractedText,
                PartitionNumber = 0,
                SectionNumber = 0,
            };

            // In a real system: detect mime, run decoders or scraper, split partitions, etc.
            details.GeneratedFiles["extracted.txt"] = new GeneratedFileDetails
            {
                ParentId = details.Id,
                SourcePartitionId = details.Id,
                ContentSHA256 = ComputeSHA256(upload.Bytes)
            };

            pipeline.Files.Add(details);
        }

        // Mark uploads as consumed
        pipeline.FilesToUpload.Clear();
        pipeline.UploadComplete = true;
        pipeline.Log(this, $"Extracted text from {pipeline.Files.Count} file(s).");
        await Task.Yield();
        return (ReturnType.Success, pipeline);
    }

    private static string ComputeSHA256(byte[] bytes)
    {
        using var sha = SHA256.Create();
        var hash = sha.ComputeHash(bytes);
        return string.Concat(hash.Select(b => b.ToString("x2")));
    }
}
