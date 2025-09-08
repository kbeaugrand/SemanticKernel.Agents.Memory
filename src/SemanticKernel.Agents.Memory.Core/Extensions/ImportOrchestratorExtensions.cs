using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using SemanticKernel.Agents.Memory.Core.Builders;

namespace SemanticKernel.Agents.Memory.Core.Extensions;

/// <summary>
/// Extension methods for ImportOrchestrator to support fluent file upload API.
/// </summary>
public static class ImportOrchestratorExtensions
{
    /// <summary>
    /// Creates a new document upload builder for fluent file upload configuration.
    /// </summary>
    /// <param name="orchestrator">The import orchestrator instance.</param>
    /// <returns>A new document upload builder.</returns>
    public static DocumentUploadBuilder NewDocumentUpload(this ImportOrchestrator orchestrator)
    {
        return new DocumentUploadBuilder();
    }

    /// <summary>
    /// Uploads a single file by path and processes it through the pipeline.
    /// </summary>
    /// <param name="orchestrator">The import orchestrator instance.</param>
    /// <param name="index">The index name to upload to.</param>
    /// <param name="filePath">The path to the file to upload.</param>
    /// <param name="customFileName">Optional custom file name to use instead of the original file name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when the upload and processing is finished, containing the document ID.</returns>
    public static async Task<string> UploadFileAsync(
        this ImportOrchestrator orchestrator,
        string index,
        string filePath,
        string? customFileName = null,
        CancellationToken cancellationToken = default)
    {
        var request = new DocumentUploadBuilder()
            .WithFile(filePath, customFileName)
            .Build();

        return await orchestrator.ImportDocumentAsync(index, request, cancellationToken);
    }

    /// <summary>
    /// Uploads a single file from a stream and processes it through the pipeline.
    /// </summary>
    /// <param name="orchestrator">The import orchestrator instance.</param>
    /// <param name="index">The index name to upload to.</param>
    /// <param name="fileName">The name of the file.</param>
    /// <param name="stream">The stream containing the file data.</param>
    /// <param name="customMimeType">Optional custom MIME type. If not provided, it will be detected from the file extension.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when the upload and processing is finished, containing the document ID.</returns>
    public static async Task<string> UploadFileAsync(
        this ImportOrchestrator orchestrator,
        string index,
        string fileName,
        Stream stream,
        string? customMimeType = null,
        CancellationToken cancellationToken = default)
    {
        var request = await new DocumentUploadBuilder()
            .WithFileAsync(fileName, stream, customMimeType, cancellationToken);

        return await orchestrator.ImportDocumentAsync(index, request.Build(), cancellationToken);
    }

    /// <summary>
    /// Uploads a single file from a byte array and processes it through the pipeline.
    /// </summary>
    /// <param name="orchestrator">The import orchestrator instance.</param>
    /// <param name="index">The index name to upload to.</param>
    /// <param name="fileName">The name of the file.</param>
    /// <param name="bytes">The byte array containing the file data.</param>
    /// <param name="customMimeType">Optional custom MIME type. If not provided, it will be detected from the file extension.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when the upload and processing is finished, containing the document ID.</returns>
    public static async Task<string> UploadFileAsync(
        this ImportOrchestrator orchestrator,
        string index,
        string fileName,
        byte[] bytes,
        string? customMimeType = null,
        CancellationToken cancellationToken = default)
    {
        var request = new DocumentUploadBuilder()
            .WithFile(fileName, bytes, customMimeType)
            .Build();

        return await orchestrator.ImportDocumentAsync(index, request, cancellationToken);
    }

    /// <summary>
    /// Uploads multiple files by paths and processes them through the pipeline.
    /// </summary>
    /// <param name="orchestrator">The import orchestrator instance.</param>
    /// <param name="index">The index name to upload to.</param>
    /// <param name="filePaths">The paths to the files to upload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when the upload and processing is finished, containing the document ID.</returns>
    public static async Task<string> UploadFilesAsync(
        this ImportOrchestrator orchestrator,
        string index,
        string[] filePaths,
        CancellationToken cancellationToken = default)
    {
        var request = await new DocumentUploadBuilder()
            .WithFilesAsync(filePaths, cancellationToken);

        return await orchestrator.ImportDocumentAsync(index, request.Build(), cancellationToken);
    }

    /// <summary>
    /// Processes a document upload request built with the fluent API and returns both the document ID and pipeline logs.
    /// </summary>
    /// <param name="orchestrator">The import orchestrator instance.</param>
    /// <param name="index">The index name to upload to.</param>
    /// <param name="builder">The configured document upload builder.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when the upload and processing is finished, containing the document ID and pipeline logs.</returns>
    public static async Task<(string DocumentId, System.Collections.Generic.IReadOnlyList<PipelineLogEntry> Logs)> ProcessUploadAsync(
        this ImportOrchestrator orchestrator,
        string index,
        DocumentUploadBuilder builder,
        CancellationToken cancellationToken = default)
    {
        var request = builder.Build();
        var pipeline = orchestrator.PrepareNewDocumentUpload(index, request);
        await orchestrator.RunPipelineAsync(pipeline, cancellationToken);
        return (pipeline.DocumentId, pipeline.Logs);
    }

    /// <summary>
    /// Processes a document upload request built with the fluent API and returns a DataPipelineResult for advanced scenarios.
    /// </summary>
    /// <param name="orchestrator">The import orchestrator instance.</param>
    /// <param name="index">The index name to upload to.</param>
    /// <param name="builder">The configured document upload builder.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when the upload and processing is finished, containing the complete pipeline result.</returns>
    public static async Task<DataPipelineResult> ProcessUploadAdvancedAsync(
        this ImportOrchestrator orchestrator,
        string index,
        DocumentUploadBuilder builder,
        CancellationToken cancellationToken = default)
    {
        var request = builder.Build();
        var pipeline = orchestrator.PrepareNewDocumentUpload(index, request);
        await orchestrator.RunPipelineAsync(pipeline, cancellationToken);
        return pipeline;
    }
}
