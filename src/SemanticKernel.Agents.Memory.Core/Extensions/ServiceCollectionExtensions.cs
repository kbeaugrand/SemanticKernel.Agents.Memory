using System;
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
}
