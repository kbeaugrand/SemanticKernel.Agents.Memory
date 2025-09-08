using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using SemanticKernel.Agents.Memory;
using SemanticKernel.Rankers.Abstractions;
using SemanticKernel.Agents.Memory.Core.Models;

namespace SemanticKernel.Agents.Memory.Core.Services;

public class SearchClient<TVectorStore> : ISearchClient
    where TVectorStore : VectorStore
{
    private readonly TVectorStore _vectorStore;

    private readonly IEmbeddingGenerator<string, Embedding<float>> _embeddingGenerator;

    private readonly IChatCompletionService _chatCompletionService;

    private readonly IPromptProvider _promptProvider;

    private readonly SearchClientOptions _options;

    private readonly IRanker? _ranker;

    public SearchClient(TVectorStore vectorStore,
                        IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator,
                        IPromptProvider promptProvider,
                        IChatCompletionService chatCompletionService,
                        SearchClientOptions? options = null,
                        IRanker? ranker = null)
    {
        _vectorStore = vectorStore;
        _embeddingGenerator = embeddingGenerator ?? throw new ArgumentNullException(nameof(embeddingGenerator));
        _promptProvider = promptProvider ?? throw new ArgumentNullException(nameof(promptProvider));
        _chatCompletionService = chatCompletionService ?? throw new ArgumentNullException(nameof(chatCompletionService));
        _options = options ?? new SearchClientOptions();
        _ranker = ranker;
    }

    public async Task<SearchResult> SearchAsync(
        string index,
        string query,
        ICollection<MemoryFilter>? filters = null,
        double minRelevance = 0,
        int limit = -1,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNullOrWhiteSpace(index);
        ArgumentNullException.ThrowIfNullOrWhiteSpace(query);

        try
        {
            // Generate embedding for the query
            var queryEmbedding = await _embeddingGenerator.GenerateAsync([query], cancellationToken: cancellationToken).ConfigureAwait(false);
            var embedding = queryEmbedding.FirstOrDefault();

            if (embedding == null)
            {
                return new SearchResult { Query = query, Results = new List<Citation>() };
            }

            var collection = _vectorStore.GetCollection<string, MemoryRecord>(index, MemoryRecordStoreDefinitionProvider.GetMemoryRecordStoreDefinition(embedding.Dimensions));

            await collection.EnsureCollectionExistsAsync(cancellationToken).ConfigureAwait(false);

            // Determine the search limit - use provided limit, or default to MaxMatchesCount from options
            var searchLimit = limit > 0 ? limit : _options.MaxMatchesCount;

            var searchResult = collection.SearchAsync(embedding, top: searchLimit, new VectorSearchOptions<MemoryRecord>
            {
                Filter = MapFiltersToVectorStoreFilter(filters)
            });

            IAsyncEnumerable<(VectorSearchResult<MemoryRecord> Result, double Score)> rankedResults = null!;

            rankedResults = MapSearchResultsToRankedResultsAsync(query, searchResult);

            var citations = new List<Citation>();

            // Process search results and convert to Citations
            await foreach ((var result, double rankedScore) in rankedResults.WithCancellation(cancellationToken).ConfigureAwait(false))
            {
                var citation = new Citation
                {
                    Id = result.Record.Id,
                    Content = result.Record.Text,
                    Source = !string.IsNullOrEmpty(result.Record.FileName) ? result.Record.FileName : result.Record.DocumentId,
                    RelevanceScore = result.Score ?? rankedScore
                };

                citations.Add(citation);
            }


            return new SearchResult
            {
                Query = query,
                Results = citations
            };
        }
        catch (Exception)
        {
            // Log error and return empty result
            return new SearchResult
            {
                Query = query,
                Results = new List<Citation>()
            };
        }
    }

    public async Task<Answer> AskAsync(
        string index,
        string question,
        ICollection<MemoryFilter>? filters = null,
        double minRelevance = 0,
        CancellationToken cancellationToken = default)
    {
        Answer answer = null!;
        IEnumerable<SourceReference> sourceReferences = [];

        // Call AskStreamingAsync and return the last result only once the streaming is complete.
        await foreach (var answerChunk in AskStreamingAsync(index, question, filters, minRelevance, cancellationToken))
        {
            if (answer is null)
            {
                // Capture source references from the first chunk that has them
                sourceReferences = answerChunk.RelevantSources;
            }

            answer = answerChunk;
        }

        if (answer is not null)
        {
            // Ensure the final answer includes all relevant sources
            answer.RelevantSources.AddRange(sourceReferences);
            return answer;
        }

        // Fallback in case streaming returns no results
        return new Answer
        {
            Question = question,
            HasResult = false,
            Result = "No results from streaming implementation.",
            RelevantSources = new List<SourceReference>()
        };
    }

    public async IAsyncEnumerable<Answer> AskStreamingAsync(
        string index,
        string question,
        ICollection<MemoryFilter>? filters = null,
        double minRelevance = 0,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(index);
        ArgumentException.ThrowIfNullOrWhiteSpace(question);

        // Search for relevant content
        SearchResult? searchResult = null;
        string? errorMessage = null;

        try
        {
            searchResult = await SearchAsync(index, question, filters, minRelevance, _options.MaxMatchesCount, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            errorMessage = $"An error occurred during search: {ex.Message}";
        }

        if (errorMessage != null)
        {
            yield return new Answer
            {
                Question = question,
                HasResult = false,
                Result = errorMessage,
                RelevantSources = new List<SourceReference>()
            };
            yield break;
        }

        if (searchResult!.NoResult)
        {
            yield return new Answer
            {
                Question = question,
                HasResult = false,
                Result = _options.EmptyAnswer,
                RelevantSources = new List<SourceReference>()
            };
            yield break;
        }

        // Build context from search results using the fact template
        var factsBuilder = new System.Text.StringBuilder();
        var citationCount = 0;
        foreach (var result in searchResult.Results)
        {
            if (citationCount > 0)
                factsBuilder.Append("\n\n");

            // Apply the fact template with available placeholders
            var factText = _options.FactTemplate
                .Replace("{{$content}}", result.Content)
                .Replace("{{$source}}", result.Source)
                .Replace("{{$relevance}}", result.RelevanceScore.ToString("F3"))
                .Replace("{{$memoryId}}", result.Id);

            factsBuilder.Append(factText);
            citationCount++;
        }
        var facts = factsBuilder.ToString();

        // Pre-build relevant sources list
        var relevantSources = searchResult.Results.Select(r => new SourceReference
        {
            DocumentId = r.Id,
            SourceName = r.Source,
            Chunks = new List<Chunk>
            {
                new Chunk
                {
                    Text = r.Content,
                    Relevance = (float)r.RelevanceScore
                }
            }
        }).ToList();

        // Get the prompt template and build the prompt
        string? prompt = null;
        var notFoundMessage = _options.EmptyAnswer;

        try
        {
            var promptTemplate = _promptProvider.ReadPrompt("AskWithFacts");
            prompt = promptTemplate
                .Replace("{{$facts}}", facts)
                .Replace("{{$input}}", question)
                .Replace("{{$notFound}}", notFoundMessage);
        }
        catch (Exception ex)
        {
            errorMessage = $"An error occurred while preparing the prompt: {ex.Message}";
        }

        if (errorMessage != null)
        {
            yield return new Answer
            {
                Question = question,
                HasResult = false,
                Result = errorMessage,
                RelevantSources = new List<SourceReference>()
            };
            yield break;
        }

        // Create chat history with the prompt
        var chatHistory = new ChatHistory();
        chatHistory.AddUserMessage(prompt!);

        // Create execution settings using the options
        var executionSettings = new PromptExecutionSettings
        {
            ExtensionData = new Dictionary<string, object>
            {
                ["temperature"] = _options.Temperature,
                ["top_p"] = _options.TopP,
                ["presence_penalty"] = _options.PresencePenalty,
                ["frequency_penalty"] = _options.FrequencyPenalty
            }
        };

        // Add stop sequences if provided
        if (_options.StopSequences.Count > 0)
        {
            executionSettings.ExtensionData["stop"] = _options.StopSequences;
        }

        // Add max tokens if specified
        if (_options.MaxAskPromptSize > 0)
        {
            executionSettings.ExtensionData["max_tokens"] = _options.AnswerTokens;
        }

        // Track token usage and accumulate response
        var tokenUsageList = new List<TokenUsage>();
        var responseBuilder = new System.Text.StringBuilder();
        var isFirstChunk = true;
        var chunkCount = 0;

        // Generate response using chat completion service and stream results
        await foreach (var chunk in _chatCompletionService.GetStreamingChatMessageContentsAsync(
            chatHistory,
            executionSettings,
            cancellationToken: cancellationToken)
            .ConfigureAwait(false))
        {
            chunkCount++;

            if (chunk.Content != null)
            {
                responseBuilder.Append(chunk.Content);
                var responseText = responseBuilder.ToString();

                // For streaming, yield partial results as they come in
                var hasResult = !string.IsNullOrWhiteSpace(responseText) &&
                               !responseText.Trim().Equals(notFoundMessage, StringComparison.OrdinalIgnoreCase);

                // Include relevant sources only in first chunk to avoid duplication
                var sourcesToInclude = isFirstChunk ? relevantSources : new List<SourceReference>();

                yield return new Answer
                {
                    Question = question,
                    HasResult = hasResult,
                    Result = responseText,
                    TokenUsage = tokenUsageList.Count > 0 ? new List<TokenUsage>(tokenUsageList) : null,
                    RelevantSources = sourcesToInclude
                };

                isFirstChunk = false;
            }

            // Track token usage if available
            if (chunk.Metadata?.TryGetValue("Usage", out var usage) == true && usage != null)
            {
                var tokenUsage = ExtractTokenUsage(usage, chunk.ModelId);
                if (tokenUsage != null)
                {
                    tokenUsageList.Add(tokenUsage);
                }
            }
        }

        // If no content was streamed, yield a final result
        if (isFirstChunk)
        {
            var responseText = responseBuilder.ToString();
            var hasResult = !string.IsNullOrWhiteSpace(responseText) &&
                           !responseText.Trim().Equals(notFoundMessage, StringComparison.OrdinalIgnoreCase);

            yield return new Answer
            {
                Question = question,
                HasResult = hasResult,
                Result = string.IsNullOrEmpty(responseText) ? "No response received from chat completion service." : responseText,
                TokenUsage = tokenUsageList.Count > 0 ? tokenUsageList : null,
                RelevantSources = relevantSources
            };
        }
    }

    /// <summary>
    /// Extracts token usage information from the chat completion metadata.
    /// </summary>
    /// <param name="usage">The usage metadata object.</param>
    /// <param name="modelId">The model ID from the chat completion.</param>
    /// <returns>A TokenUsage object if extraction is successful, null otherwise.</returns>
    private static TokenUsage? ExtractTokenUsage(object usage, string? modelId)
    {
        try
        {
            // Try to handle as a generic object with reflection for different usage types
            var usageType = usage.GetType();
            var inputTokensProperty = usageType.GetProperty("InputTokens") ??
                                     usageType.GetProperty("PromptTokens") ??
                                     usageType.GetProperty("InputTokenCount");
            var outputTokensProperty = usageType.GetProperty("OutputTokens") ??
                                      usageType.GetProperty("CompletionTokens") ??
                                      usageType.GetProperty("OutputTokenCount");
            var totalTokensProperty = usageType.GetProperty("TotalTokens") ??
                                     usageType.GetProperty("TotalTokenCount");

            var inputTokens = inputTokensProperty?.GetValue(usage) as int? ?? 0;
            var outputTokens = outputTokensProperty?.GetValue(usage) as int? ?? 0;
            var totalTokens = totalTokensProperty?.GetValue(usage) as int? ?? (inputTokens + outputTokens);

            return new TokenUsage
            {
                Model = modelId ?? "unknown",
                InputTokens = inputTokens,
                OutputTokens = outputTokens,
                TotalTokens = totalTokens
            };
        }
        catch
        {
            // If we can't extract token usage, return null
            return null;
        }
    }

    public async Task<IEnumerable<string>> ListIndexesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Get collection names from the vector store
            var collectionNames = new List<string>();
            await foreach (var name in _vectorStore.ListCollectionNamesAsync(cancellationToken).ConfigureAwait(false))
            {
                collectionNames.Add(name);
            }
            return collectionNames;
        }
        catch (Exception)
        {
            // Return empty list if we can't get the collection names
            return new List<string>();
        }
    }

    /// <summary>
    /// Maps memory filters to vector store filters.
    /// This is a basic implementation that can be extended for more complex filtering scenarios.
    /// </summary>
    /// <param name="filters">The memory filters to map.</param>
    /// <returns>A vector store filter expression, or null if no filters are provided.</returns>
    private static Expression<Func<MemoryRecord, bool>>? MapFiltersToVectorStoreFilter(ICollection<MemoryFilter>? filters)
    {
        if (filters == null || filters.Count == 0)
        {
            return null;
        }

        var parameter = Expression.Parameter(typeof(MemoryRecord), "record");
        Expression? combinedExpression = null;

        foreach (var filter in filters)
        {
            if (string.IsNullOrEmpty(filter.Field) || filter.Value == null)
            {
                continue;
            }

            // Map common field names to MemoryRecord properties
            var propertyName = filter.Field switch
            {
                "documentId" or "DocumentId" => nameof(MemoryRecord.DocumentId),
                "executionId" or "ExecutionId" => nameof(MemoryRecord.ExecutionId),
                "index" or "Index" => nameof(MemoryRecord.Index),
                "fileName" or "FileName" => nameof(MemoryRecord.FileName),
                _ => filter.Field
            };

            // Get the property from MemoryRecord
            var property = typeof(MemoryRecord).GetProperty(propertyName);
            if (property == null)
            {
                continue; // Skip if property doesn't exist
            }

            var propertyAccess = Expression.Property(parameter, property);
            var constantValue = Expression.Constant(filter.Value);

            Expression? condition = null;

            // For now, only support equality operations
            if (filter.Operator.Equals("equals", StringComparison.OrdinalIgnoreCase))
            {
                // Handle type conversions if needed
                if (property.PropertyType == typeof(string) && filter.Value is string)
                {
                    condition = Expression.Equal(propertyAccess, constantValue);
                }
                else if (property.PropertyType == constantValue.Type)
                {
                    condition = Expression.Equal(propertyAccess, constantValue);
                }
                else
                {
                    // Try to convert the value to the property type
                    try
                    {
                        var convertedValue = Convert.ChangeType(filter.Value, property.PropertyType);
                        var convertedConstant = Expression.Constant(convertedValue, property.PropertyType);
                        condition = Expression.Equal(propertyAccess, convertedConstant);
                    }
                    catch
                    {
                        // Skip this filter if conversion fails
                        continue;
                    }
                }
            }

            if (condition != null)
            {
                combinedExpression = combinedExpression == null
                    ? condition
                    : Expression.AndAlso(combinedExpression, condition);
            }
        }

        return combinedExpression == null
            ? null
            : Expression.Lambda<Func<MemoryRecord, bool>>(combinedExpression, parameter);
    }

    /// <summary>
    /// Maps search results to ranked results format for streaming processing.
    /// This helper function converts the vector search results to the expected format
    /// used by the ranking system, preserving the original scores.
    /// </summary>
    /// <param name="query">The original search query</param>
    /// <param name="searchResults">The search results from the vector store</param>
    /// <returns>An async enumerable of ranked results with preserved scores</returns>
    private async IAsyncEnumerable<(VectorSearchResult<MemoryRecord> Result, double Score)> MapSearchResultsToRankedResultsAsync(
        string query,
        IAsyncEnumerable<VectorSearchResult<MemoryRecord>> searchResults)
    {
        if (_ranker is not null)
        {
            await foreach ((VectorSearchResult<MemoryRecord> result, double score) in _ranker.RankAsync(
                query,
                searchResults,
                r => r.Text,
                _options.MaxMatchesCount).ConfigureAwait(false))
            {
                yield return (result, score);
            }

            yield break;
        }

        await foreach (var result in searchResults.ConfigureAwait(false))
        {
            // Use the original search score, defaulting to 0.0 if not available
            var score = result.Score ?? 0.0;
            yield return (result, score);
        }
    }
}
