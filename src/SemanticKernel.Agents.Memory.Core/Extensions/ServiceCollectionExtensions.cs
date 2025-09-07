using System;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SemanticKernel.Agents.Memory.Core.Handlers;
using SemanticKernel.Agents.Memory.Core.Services;

namespace SemanticKernel.Agents.Memory.Core.Extensions;

/// <summary>
/// Extension methods for dependency injection configuration
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds vector store save records handler for saving records using Microsoft.Extensions.VectorData concepts with a specific vector store type.
    /// </summary>
    /// <typeparam name="TVectorStore">The type of vector store</typeparam>
    /// <param name="services">Service collection</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddVectorStoreSaveRecords<TVectorStore>(this IServiceCollection services)
        where TVectorStore : Microsoft.Extensions.VectorData.VectorStore
    {
        // Register the save records handler
        services.AddScoped<SaveRecordsHandler<TVectorStore>>();
        services.AddScoped<IPipelineStepHandler, SaveRecordsHandler<TVectorStore>>();

        return services;
    }

    /// <summary>
    /// Adds MarkitDown text extraction services to the dependency injection container
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="markitDownServiceUrl">URL of the MarkitDown service (default: http://localhost:5000)</param>
    /// <param name="httpClientName">Name for the HTTP client (default: MarkitDown)</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddMarkitDownTextExtraction(
        this IServiceCollection services,
        string markitDownServiceUrl = "http://localhost:5000",
        string httpClientName = "MarkitDown")
    {
        // Register HTTP client for MarkitDown service
        services.AddHttpClient(httpClientName, client =>
        {
            client.BaseAddress = new Uri(markitDownServiceUrl);
            client.Timeout = TimeSpan.FromMinutes(5); // Allow time for large file processing
            client.DefaultRequestHeaders.Add("User-Agent", "SemanticKernel.Agents.Memory/1.0.0");
        });

        // Register MarkitDown service
        services.AddScoped<IMarkitDownService>(serviceProvider =>
        {
            var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
            var logger = serviceProvider.GetRequiredService<ILogger<MarkitDownService>>();
            var httpClient = httpClientFactory.CreateClient(httpClientName);

            return new MarkitDownService(httpClient, logger, markitDownServiceUrl);
        });

        // Register text extraction handler
        services.AddScoped<TextExtractionHandler>();
        services.AddScoped<IPipelineStepHandler, TextExtractionHandler>();

        return services;
    }

    /// <summary>
    /// Adds MarkitDown text extraction services with custom HTTP client configuration
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="configureHttpClient">Action to configure the HTTP client</param>
    /// <param name="markitDownServiceUrl">URL of the MarkitDown service</param>
    /// <param name="httpClientName">Name for the HTTP client</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddMarkitDownTextExtraction(
        this IServiceCollection services,
        Action<System.Net.Http.HttpClient> configureHttpClient,
        string markitDownServiceUrl = "http://localhost:5000",
        string httpClientName = "MarkitDown")
    {
        // Register HTTP client with custom configuration
        services.AddHttpClient(httpClientName, configureHttpClient);

        // Register MarkitDown service
        services.AddScoped<IMarkitDownService>(serviceProvider =>
        {
            var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
            var logger = serviceProvider.GetRequiredService<ILogger<MarkitDownService>>();
            var httpClient = httpClientFactory.CreateClient(httpClientName);

            return new MarkitDownService(httpClient, logger, markitDownServiceUrl);
        });

        // Register text extraction handler
        services.AddScoped<TextExtractionHandler>();
        services.AddScoped<IPipelineStepHandler, TextExtractionHandler>();

        return services;
    }

    /// <summary>
    /// Configures memory ingestion pipeline with fluent configuration
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="configure">Configuration action for memory ingestion options</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection ConfigureMemoryIngestion(
        this IServiceCollection services,
        Action<MemoryIngestionOptions> configure)
    {
        if (configure == null)
            throw new ArgumentNullException(nameof(configure));

        var options = new MemoryIngestionOptions();
        configure(options);

        // Register the options instance
        services.AddSingleton(options);

        // Apply custom service registrations
        foreach (var serviceRegistration in options.ServiceRegistrations)
        {
            serviceRegistration(services);
        }

        // Register all handlers from options
        foreach (var handlerRegistration in options.Handlers)
        {
            var serviceDescriptor = new ServiceDescriptor(
                handlerRegistration.HandlerType,
                handlerRegistration.HandlerType,
                handlerRegistration.Lifetime);
            services.Add(serviceDescriptor);

            // Also register as IPipelineStepHandler interface
            var interfaceDescriptor = new ServiceDescriptor(
                typeof(IPipelineStepHandler),
                provider => provider.GetRequiredService(handlerRegistration.HandlerType),
                handlerRegistration.Lifetime);
            services.Add(interfaceDescriptor);
        }

        // Register the ImportOrchestrator
        services.AddScoped<ImportOrchestrator>();
        services.AddScoped<IPipelineOrchestrator>(provider =>
            provider.GetRequiredService<ImportOrchestrator>());

        return services;
    }

    /// <summary>
    /// Adds memory search client with a specific vector store instance
    /// </summary>
    /// <typeparam name="TVectorStore">The type of vector store</typeparam>
    /// <param name="services">Service collection</param>
    /// <param name="vectorStore">The vector store instance to use</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddMemorySearchClient<TVectorStore>(this IServiceCollection services, TVectorStore vectorStore)
        where TVectorStore : Microsoft.Extensions.VectorData.VectorStore
    {
        return AddMemorySearchClient(services, vectorStore, null);
    }

    /// <summary>
    /// Adds memory search client with a specific vector store instance and options
    /// </summary>
    /// <typeparam name="TVectorStore">The type of vector store</typeparam>
    /// <param name="services">Service collection</param>
    /// <param name="vectorStore">The vector store instance to use</param>
    /// <param name="options">Search client options</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddMemorySearchClient<TVectorStore>(this IServiceCollection services, TVectorStore vectorStore, SearchClientOptions? options)
        where TVectorStore : Microsoft.Extensions.VectorData.VectorStore
    {
        if (vectorStore == null)
            throw new ArgumentNullException(nameof(vectorStore));

        // Register the vector store instance
        services.AddSingleton(vectorStore);

        services.AddScoped<SearchClient<TVectorStore>>(provider =>
        {
            var embeddingGenerator = provider.GetRequiredService<Microsoft.Extensions.AI.IEmbeddingGenerator<string, Microsoft.Extensions.AI.Embedding<float>>>();
            var promptProvider = provider.GetRequiredService<IPromptProvider>();
            var chatCompletionService = provider.GetRequiredService<Microsoft.SemanticKernel.ChatCompletion.IChatCompletionService>();

            return new SearchClient<TVectorStore>(vectorStore, embeddingGenerator, promptProvider, chatCompletionService, options);
        });

        services.AddScoped<SemanticKernel.Agents.Memory.ISearchClient>(provider =>
            provider.GetRequiredService<SearchClient<TVectorStore>>());

        return services;
    }

    /// <summary>
    /// Adds embedded prompt provider to the dependency injection container
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="assembly">Assembly containing embedded prompt resources (defaults to Core assembly)</param>
    /// <param name="resourcePrefix">Prefix for embedded resource names (defaults to Core prompts namespace)</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddEmbeddedPromptProvider(
        this IServiceCollection services,
        Assembly? assembly = null,
        string? resourcePrefix = null)
    {
        services.AddScoped<IPromptProvider>(serviceProvider =>
        {
            var logger = serviceProvider.GetRequiredService<ILogger<EmbeddedPromptProvider>>();
            return new EmbeddedPromptProvider(logger, assembly, resourcePrefix);
        });

        return services;
    }

    /// <summary>
    /// Adds embedded prompt provider with custom configuration
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="configure">Action to configure the embedded prompt provider</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddEmbeddedPromptProvider(
        this IServiceCollection services,
        Action<EmbeddedPromptProviderOptions> configure)
    {
        if (configure == null)
            throw new ArgumentNullException(nameof(configure));

        var options = new EmbeddedPromptProviderOptions();
        configure(options);

        services.AddScoped<IPromptProvider>(serviceProvider =>
        {
            var logger = serviceProvider.GetRequiredService<ILogger<EmbeddedPromptProvider>>();
            return new EmbeddedPromptProvider(logger, options.Assembly, options.ResourcePrefix);
        });

        return services;
    }

    /// <summary>
    /// Adds default prompt provider with embedded prompts from the Core assembly
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddDefaultPromptProvider(this IServiceCollection services)
    {
        return services.AddEmbeddedPromptProvider();
    }
}

/// <summary>
/// Configuration options for EmbeddedPromptProvider
/// </summary>
public class EmbeddedPromptProviderOptions
{
    /// <summary>
    /// Assembly containing embedded prompt resources
    /// </summary>
    public Assembly? Assembly { get; set; }

    /// <summary>
    /// Prefix for embedded resource names
    /// </summary>
    public string? ResourcePrefix { get; set; }
}
