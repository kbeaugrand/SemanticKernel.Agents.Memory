using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.VectorData;
using SemanticKernel.Agents.Memory.Core.Extensions;
using SemanticKernel.Agents.Memory.Core.Handlers;
using SemanticKernel.Agents.Memory.Core.Services;

namespace SemanticKernel.Agents.Memory.Core;

/// <summary>
/// Configuration options for memory ingestion pipeline
/// </summary>
public class MemoryIngestionOptions
{
    internal readonly List<HandlerRegistration> HandlerRegistrations = new();
    internal readonly List<Action<IServiceCollection>> ServiceRegistrations = new();

    /// <summary>
    /// Gets the registered pipeline step handlers
    /// </summary>
    public IReadOnlyList<HandlerRegistration> Handlers => HandlerRegistrations.AsReadOnly();

    /// <summary>
    /// Registers a pipeline step handler with the specified service lifetime
    /// </summary>
    /// <typeparam name="THandler">The handler type</typeparam>
    /// <param name="stepName">The pipeline step name</param>
    /// <param name="lifetime">Service lifetime (default: Scoped)</param>
    /// <returns>The options instance for chaining</returns>
    public MemoryIngestionOptions WithHandler<THandler>(string stepName, ServiceLifetime lifetime = ServiceLifetime.Scoped)
        where THandler : class, IPipelineStepHandler
    {
        HandlerRegistrations.Add(new HandlerRegistration(typeof(THandler), stepName, lifetime));
        return this;
    }



    /// <summary>
    /// Registers a text chunking handler
    /// </summary>
    /// <typeparam name="THandler">The text chunking handler type</typeparam>
    /// <param name="lifetime">Service lifetime (default: Scoped)</param>
    /// <returns>The options instance for chaining</returns>
    public MemoryIngestionOptions WithTextChunking<THandler>(ServiceLifetime lifetime = ServiceLifetime.Scoped)
        where THandler : class, IPipelineStepHandler
    {
        return WithHandler<THandler>("text-chunking", lifetime);
    }

    /// <summary>
    /// Registers a simple text chunking handler with options
    /// </summary>
    /// <param name="options">Text chunking options</param>
    /// <param name="lifetime">Service lifetime (default: Scoped)</param>
    /// <returns>The options instance for chaining</returns>
    public MemoryIngestionOptions WithSimpleTextChunking(TextChunkingOptions? options = null, ServiceLifetime lifetime = ServiceLifetime.Scoped)
    {
        if (options != null)
        {
            ServiceRegistrations.Add(services => services.AddSingleton(options));
        }
        return WithTextChunking<SimpleTextChunking>(lifetime);
    }

    /// <summary>
    /// Registers a simple text chunking handler with configuration
    /// </summary>
    /// <param name="configure">Function to create configured text chunking options</param>
    /// <param name="lifetime">Service lifetime (default: Scoped)</param>
    /// <returns>The options instance for chaining</returns>
    public MemoryIngestionOptions WithSimpleTextChunking(Func<TextChunkingOptions> configure, ServiceLifetime lifetime = ServiceLifetime.Scoped)
    {
        if (configure == null)
            throw new ArgumentNullException(nameof(configure));

        ServiceRegistrations.Add(services =>
        {
            var options = configure();
            services.AddSingleton(options);
        });
        return WithTextChunking<SimpleTextChunking>(lifetime);
    }

    /// <summary>
    /// Registers a semantic chunking handler
    /// </summary>
    /// <param name="lifetime">Service lifetime (default: Scoped)</param>
    /// <returns>The options instance for chaining</returns>
    public MemoryIngestionOptions WithSemanticChunking(ServiceLifetime lifetime = ServiceLifetime.Scoped)
    {
        return WithTextChunking<SemanticChunking>(lifetime);
    }

    /// <summary>
    /// Registers a semantic chunking handler with options
    /// </summary>
    /// <param name="options">Semantic chunking options</param>
    /// <param name="lifetime">Service lifetime (default: Scoped)</param>
    /// <returns>The options instance for chaining</returns>
    public MemoryIngestionOptions WithSemanticChunking(SemanticChunkingOptions options, ServiceLifetime lifetime = ServiceLifetime.Scoped)
    {
        if (options == null)
            throw new ArgumentNullException(nameof(options));

        ServiceRegistrations.Add(services => services.AddSingleton(options));
        return WithTextChunking<SemanticChunking>(lifetime);
    }

    /// <summary>
    /// Registers a semantic chunking handler with configuration
    /// </summary>
    /// <param name="configure">Function to create configured semantic chunking options</param>
    /// <param name="lifetime">Service lifetime (default: Scoped)</param>
    /// <returns>The options instance for chaining</returns>
    public MemoryIngestionOptions WithSemanticChunking(Func<SemanticChunkingOptions> configure, ServiceLifetime lifetime = ServiceLifetime.Scoped)
    {
        if (configure == null)
            throw new ArgumentNullException(nameof(configure));

        ServiceRegistrations.Add(services =>
        {
            var options = configure();
            services.AddSingleton(options);
        });
        return WithTextChunking<SemanticChunking>(lifetime);
    }

    /// <summary>
    /// Registers an embeddings generation handler
    /// </summary>
    /// <typeparam name="THandler">The embeddings handler type</typeparam>
    /// <param name="lifetime">Service lifetime (default: Scoped)</param>
    /// <returns>The options instance for chaining</returns>
    public MemoryIngestionOptions WithEmbeddingsGeneration<THandler>(ServiceLifetime lifetime = ServiceLifetime.Scoped)
        where THandler : class, IPipelineStepHandler
    {
        return WithHandler<THandler>("generate-embeddings", lifetime);
    }

    /// <summary>
    /// Registers the save records handler with vector store support.
    /// </summary>
    /// <param name="vectorStore">The vector store instance</param>
    /// <param name="lifetime">Service lifetime (default: Scoped)</param>
    /// <returns>The options instance for chaining</returns>
    public MemoryIngestionOptions WithSaveRecords<TVectorStore>(TVectorStore vectorStore, ServiceLifetime lifetime = ServiceLifetime.Scoped)
        where TVectorStore : VectorStore
    {
        if (vectorStore == null)
            throw new ArgumentNullException(nameof(vectorStore));

        // Register the vector store instance
        ServiceRegistrations.Add(services => services.AddSingleton(vectorStore));
        // Register the vector store save records handler
        ServiceRegistrations.Add(services => services.AddVectorStoreSaveRecords<TVectorStore>());
        return WithHandler<SaveRecordsHandler<TVectorStore>>("save-records", lifetime);
    }

    /// <summary>
    /// Adds custom service registrations to the DI container
    /// </summary>
    /// <param name="configureServices">Action to configure services</param>
    /// <returns>The options instance for chaining</returns>
    public MemoryIngestionOptions WithServices(Action<IServiceCollection> configureServices)
    {
        ServiceRegistrations.Add(configureServices);
        return this;
    }

    /// <summary>
    /// Configures MarkitDown text extraction with default settings
    /// </summary>
    /// <param name="serviceUrl">MarkitDown service URL (default: http://localhost:5000)</param>
    /// <returns>The options instance for chaining</returns>
    public MemoryIngestionOptions WithMarkitDownTextExtraction(string serviceUrl = "http://localhost:5000")
    {
        ServiceRegistrations.Add(services => services.AddMarkitDownTextExtraction(serviceUrl));
        // Also register the text extraction handler in the pipeline
        WithHandler<TextExtractionHandler>("text-extraction");
        return this;
    }

    /// <summary>
    /// Configures MarkitDown text extraction with custom HTTP client configuration
    /// </summary>
    /// <param name="configureHttpClient">Action to configure the HTTP client</param>
    /// <param name="serviceUrl">MarkitDown service URL (default: http://localhost:5000)</param>
    /// <returns>The options instance for chaining</returns>
    public MemoryIngestionOptions WithMarkitDownTextExtraction(
        Action<System.Net.Http.HttpClient> configureHttpClient,
        string serviceUrl = "http://localhost:5000")
    {
        ServiceRegistrations.Add(services => services.AddMarkitDownTextExtraction(configureHttpClient, serviceUrl));
        // Also register the text extraction handler in the pipeline
        WithHandler<TextExtractionHandler>("text-extraction");
        return this;
    }
}

/// <summary>
/// Represents a handler registration for the pipeline
/// </summary>
public class HandlerRegistration
{
    /// <summary>
    /// Gets the handler type
    /// </summary>
    public Type HandlerType { get; }

    /// <summary>
    /// Gets the pipeline step name
    /// </summary>
    public string StepName { get; }

    /// <summary>
    /// Gets the service lifetime
    /// </summary>
    public ServiceLifetime Lifetime { get; }

    /// <summary>
    /// Initializes a new handler registration
    /// </summary>
    /// <param name="handlerType">The handler type</param>
    /// <param name="stepName">The pipeline step name</param>
    /// <param name="lifetime">The service lifetime</param>
    public HandlerRegistration(Type handlerType, string stepName, ServiceLifetime lifetime)
    {
        HandlerType = handlerType ?? throw new ArgumentNullException(nameof(handlerType));
        StepName = stepName ?? throw new ArgumentNullException(nameof(stepName));
        Lifetime = lifetime;
    }
}
