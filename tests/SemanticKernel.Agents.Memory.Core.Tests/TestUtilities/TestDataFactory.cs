using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SemanticKernel.Agents.Memory;
using SemanticKernel.Agents.Memory.Core;

namespace SemanticKernel.Agents.Memory.Core.Tests.TestUtilities;

/// <summary>
/// Test utilities for creating test data and mocks
/// </summary>
public static class TestDataFactory
{
    /// <summary>
    /// Creates a sample DataPipelineResult for testing
    /// </summary>
    public static DataPipelineResult CreateSamplePipeline(string? documentId = null, bool complete = false)
    {
        return new DataPipelineResult
        {
            DocumentId = documentId ?? Guid.NewGuid().ToString("n"),
            Complete = complete,
            Index = "test-index",
            Steps = new List<string> { "step1", "step2" },
            Tags = new TagCollection(),
            ContextArguments = new Dictionary<string, object>()
        };
    }

    /// <summary>
    /// Creates a sample Document for testing
    /// </summary>
    public static Document CreateSampleDocument(
        string fileName = "test.txt",
        string content = "Sample document content",
        Dictionary<string, string>? tags = null)
    {
        var contentBytes = System.Text.Encoding.UTF8.GetBytes(content);
        return new Document
        {
            FileName = fileName,
            Content = contentBytes,
            Size = contentBytes.Length,
            MimeType = "text/plain",
            Tags = tags ?? new Dictionary<string, string>()
        };
    }

    /// <summary>
    /// Creates a sample Chunk for testing
    /// </summary>
    public static Chunk CreateSampleChunk(
        string text = "Sample chunk text",
        int chunkNumber = 0,
        float relevance = 0.5f,
        Dictionary<string, string?>? tags = null)
    {
        return new Chunk
        {
            Text = text,
            ChunkNumber = chunkNumber,
            Relevance = relevance,
            LastUpdate = DateTimeOffset.UtcNow,
            Tags = tags ?? new Dictionary<string, string?>()
        };
    }

    /// <summary>
    /// Creates a sample UploadedFile for testing
    /// </summary>
    public static UploadedFile CreateSampleFileUpload(
        string fileName = "test.txt",
        string content = "Sample file content",
        string? mimeType = "text/plain")
    {
        return new UploadedFile
        {
            FileName = fileName,
            Bytes = System.Text.Encoding.UTF8.GetBytes(content),
            MimeType = mimeType
        };
    }

    /// <summary>
    /// Creates a sample FileDetails for testing
    /// </summary>
    public static FileDetails CreateSampleFileDetails(
        string? id = null,
        string name = "test.txt",
        ArtifactTypes artifactType = ArtifactTypes.TextPartition,
        int size = 100)
    {
        return new FileDetails
        {
            Id = id ?? Guid.NewGuid().ToString("n"),
            Name = name,
            ArtifactType = artifactType,
            Size = size
        };
    }

    /// <summary>
    /// Simple hash computation for test data
    /// </summary>
    private static string ComputeHash(string input)
    {
        return System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(input))
            .Take(8)
            .Aggregate("", (acc, b) => acc + b.ToString("x2"));
    }
}

/// <summary>
/// Mock pipeline step handler for testing
/// </summary>
public class MockPipelineStepHandler : IPipelineStepHandler
{
    public string StepName { get; }
    public ReturnType ReturnValue { get; set; } = ReturnType.Success;
    public bool ThrowException { get; set; } = false;
    public Exception? ExceptionToThrow { get; set; }
    public Action<DataPipelineResult>? ProcessAction { get; set; }

    public int InvokeCount { get; private set; }
    public DataPipelineResult? LastPipeline { get; private set; }

    public MockPipelineStepHandler(string stepName = "mock-step")
    {
        StepName = stepName;
    }

    public Task<(ReturnType Result, DataPipelineResult Pipeline)> InvokeAsync(DataPipelineResult pipeline, CancellationToken ct = default)
    {
        InvokeCount++;
        LastPipeline = pipeline;

        ct.ThrowIfCancellationRequested();

        if (ThrowException)
        {
            throw ExceptionToThrow ?? new Exception("Mock exception");
        }

        ProcessAction?.Invoke(pipeline);

        return Task.FromResult((ReturnValue, pipeline));
    }
}

/// <summary>
/// Test extension methods for assertions
/// </summary>
public static class TestExtensions
{
    /// <summary>
    /// Verifies that a pipeline result contains the expected number of files
    /// </summary>
    public static void ShouldHaveFiles(this DataPipelineResult pipeline, int expectedCount)
    {
        if (pipeline.Files?.Count != expectedCount)
        {
            throw new Xunit.Sdk.XunitException($"Expected {expectedCount} files, but found {pipeline.Files?.Count ?? 0}");
        }
    }

    /// <summary>
    /// Verifies that a pipeline result contains the expected number of uploaded files
    /// </summary>
    public static void ShouldHaveUploadedFiles(this DataPipelineResult pipeline, int expectedCount)
    {
        if (pipeline.FilesToUpload?.Count != expectedCount)
        {
            throw new Xunit.Sdk.XunitException($"Expected {expectedCount} uploaded files, but found {pipeline.FilesToUpload?.Count ?? 0}");
        }
    }
}
