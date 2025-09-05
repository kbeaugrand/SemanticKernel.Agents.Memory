# SemanticKernel.Agents.Memory.Samples

This project contains sample implementations and demonstrations for the SemanticKernel Agents Memory system with full configuration support.

## Configuration

The application supports multiple configuration sources in the following order of precedence:

1. User Secrets (highest priority)
2. Environment Variables
3. appsettings.{Environment}.json
4. appsettings.json (lowest priority)

### Required Configuration

#### Azure OpenAI Settings

You need to configure Azure OpenAI credentials to use the embedding generation features:

```json
{
  "AzureOpenAI": {
    "Endpoint": "https://your-resource-name.openai.azure.com/",
    "ApiKey": "your-azure-openai-key-here",
    "EmbeddingModel": "text-embedding-ada-002"
  }
}
```

### Configuration Methods

#### 1. Using User Secrets (Recommended for development)

Use the .NET Secret Manager to store sensitive configuration:

```bash
dotnet user-secrets set "AzureOpenAI:Endpoint" "https://your-resource-name.openai.azure.com/"
dotnet user-secrets set "AzureOpenAI:ApiKey" "your-azure-openai-key"
dotnet user-secrets set "AzureOpenAI:EmbeddingModel" "text-embedding-ada-002"
```

#### 2. Using Environment Variables

Set the following environment variables:

```bash
export AzureOpenAI__Endpoint="https://your-resource-name.openai.azure.com/"
export AzureOpenAI__ApiKey="your-azure-openai-key"
export AzureOpenAI__EmbeddingModel="text-embedding-ada-002"
```

#### 3. Using appsettings.json (for development)

Update the `appsettings.json` or `appsettings.Development.json` file with your actual Azure OpenAI credentials.

📖 **For detailed Azure OpenAI setup instructions, see [AZURE_OPENAI_SETUP.md](AZURE_OPENAI_SETUP.md)**

## What's included

### PipelineDemo.cs
Contains utility methods for creating and running pipeline demonstrations with configuration support:
- `RunAsync(IConfiguration)` - Runs a complete pipeline demo with sample files using configuration
- `RunSemanticChunkingAsync(IConfiguration)` - Demonstrates semantic chunking configuration
- `RunCustomHandlerAsync(IConfiguration)` - Shows advanced configuration options
- `RunSemanticChunkingConfigDemo(IConfiguration)` - Shows semantic chunking with custom configuration

All methods now accept an `IConfiguration` parameter and read settings from the configuration system.

### Configuration Classes
- `AzureOpenAIOptions` - Strongly-typed configuration for Azure OpenAI settings
- `MarkitDownOptions` - Configuration for MarkitDown service
- `TextChunkingConfig` - Configuration for text chunking options
- `PipelineOptions` - General pipeline configuration

### Configuration Schema

#### TextChunking Section
- `Simple`: Configuration for simple text chunking
  - `MaxChunkSize`: Maximum size of text chunks (default: 500)
  - `TextOverlap`: Number of characters to overlap between chunks (default: 50)
  - `SplitCharacters`: Array of characters to use for splitting text
- `Semantic`: Configuration for semantic text chunking
  - `MaxChunkSize`: Maximum size of text chunks (default: 3000)
  - `MinChunkSize`: Minimum size of text chunks (default: 200)
  - `TitleLevelThreshold`: Title level threshold for splitting (default: 1)
  - `IncludeTitleContext`: Whether to include title context in chunks (default: true)

#### Pipeline Section
- `DefaultIndex`: Default index name for documents (default: "docs")
- `HttpClientTimeout`: HTTP client timeout duration (default: "00:10:00")

#### MarkitDown Section
- `ServiceUrl`: URL of the MarkitDown service for document processing (default: "http://localhost:5000")

### Pipeline Handlers
The sample demonstrates the use of pipeline handlers from the Core package:
- `TextExtractionHandler` - Simulates text extraction from uploaded files
- `GenerateEmbeddingsHandler` - Generates embeddings using Azure OpenAI
- `SaveRecordsHandler` - Simulates saving records to storage

These handlers are now part of the `SemanticKernel.Agents.Memory.Core` package and can be used in production scenarios.

### Azure OpenAI Integration
The sample now includes Azure OpenAI embedding generation configuration:
- **AzureOpenAIEmbeddingGenerator** - Custom implementation for Azure OpenAI embeddings
- **MockEmbeddingGenerator** - Fallback implementation for testing without Azure OpenAI credentials
- **ConfigureAzureOpenAIEmbeddings** - Helper method to configure embedding services

📖 **For detailed Azure OpenAI setup instructions, see [AZURE_OPENAI_SETUP.md](AZURE_OPENAI_SETUP.md)**

### Program.cs
A console application that demonstrates running the pipeline with sample data using configuration. The program:
- Builds configuration from multiple sources (appsettings.json, environment variables, user secrets)
- Offers multiple demo options:
  1. **Basic Pipeline Demo** - Shows standard text processing pipeline with configuration
  2. **Semantic Chunking Demo** - Demonstrates semantic-based text chunking with configuration
  3. **Custom Configuration Demo** - Shows advanced configuration options
  4. **Semantic Chunking with Custom Options** - Shows semantic chunking with custom parameters from configuration

## Mock Mode

If Azure OpenAI credentials are not configured or are invalid, the application will automatically fall back to a mock embedding generator that creates deterministic pseudo-random embeddings for demonstration purposes. This allows you to test the pipeline without requiring actual Azure OpenAI credentials.

## Security Best Practices

1. **Never commit secrets to source control**: Use user secrets, environment variables, or Azure Key Vault for production
2. **Use different credentials for different environments**: Separate development, staging, and production configurations
3. **Rotate credentials regularly**: Update your Azure OpenAI keys periodically
4. **Limit access**: Use Azure RBAC to restrict access to your Azure OpenAI resources

## Running the sample

- .NET 8.0 or later
- Azure OpenAI resource with deployed embedding model (optional - mock implementation is provided for testing)

```bash
cd samples/SemanticKernel.Agents.Memory.Samples
dotnet run
```

This will process sample text files through the pipeline and display the execution logs.

> **Note**: When running interactively, the application will wait for a keypress at the end. If you're running in an automated environment or with piped input, you can ignore the `Cannot read keys` error message at the end.

For Azure OpenAI integration, either:
1. Configure your Azure OpenAI credentials in `PipelineDemo.cs`
2. Set environment variables (recommended for production)
3. Run without configuration to use the mock embedding generator

## Using the samples in your code

You can reference the sample handlers and demo utilities in your own projects by adding a project reference to this sample project, or by copying the code and adapting it to your needs.

```csharp
// Example usage
var (documentId, logs) = await PipelineDemo.RunAsync();
Console.WriteLine($"Processed document: {documentId}");
```

## Configuration Example

To configure Azure OpenAI embedding generation in your own project:

```csharp
services.AddSingleton<IEmbeddingGenerator<string, Embedding<float>>>(serviceProvider =>
{
    var azureOpenAIClient = new AzureOpenAIClient(
        new Uri("https://your-resource.openai.azure.com/"), 
        new AzureKeyCredential("your-api-key")
    );
    
    var embeddingClient = azureOpenAIClient.GetEmbeddingClient("text-embedding-ada-002");
    return new AzureOpenAIEmbeddingGenerator(embeddingClient, "text-embedding-ada-002");
});
```

## Note

These are sample implementations intended for demonstration purposes. The Azure OpenAI integration provides a foundation for production use, but you should implement proper error handling, retry logic, and security measures for production scenarios.
