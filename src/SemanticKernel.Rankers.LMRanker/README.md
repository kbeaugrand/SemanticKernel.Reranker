# LMRanker - Language Model Based Document Reranker

LMRanker is a sophisticated document reranking system that leverages Language Models through Microsoft's Semantic Kernel to assess document relevance. Unlike traditional keyword-based ranking algorithms like BM25, LMRanker uses the understanding capabilities of large language models to evaluate how well documents answer specific queries.

## Features

- **Language Model Integration**: Uses Semantic Kernel to orchestrate language model calls
- **Structured Output**: Employs JSON-based structured output for reliable score parsing
- **Flexible Model Support**: Works with Azure OpenAI, OpenAI, local models, and other Semantic Kernel-supported services
- **Streaming Support**: Provides async enumerable interfaces for efficient processing of large document sets
- **Vector Search Integration**: Supports both plain text documents and vector search results
- **Robust Error Handling**: Gracefully handles model errors and provides fallback scores

## Installation

Add the necessary NuGet packages to your project:

```xml
<PackageReference Include="Microsoft.SemanticKernel" Version="1.64.0" />
<PackageReference Include="Microsoft.SemanticKernel.Abstractions" Version="1.64.0" />
```

## Quick Start

### 1. Set up Semantic Kernel

First, configure Semantic Kernel with your preferred language model service:

```csharp
using Microsoft.SemanticKernel;
using SemanticKernel.Rankers.LMRanker;

// Azure OpenAI (recommended for production)
var builder = Kernel.CreateBuilder();
builder.AddAzureOpenAIChatCompletion(
    deploymentName: "your-deployment-name",
    endpoint: "https://your-resource.openai.azure.com/",
    apiKey: "your-api-key"
);
var kernel = builder.Build();

// Or OpenAI
var builder = Kernel.CreateBuilder();
builder.AddOpenAIChatCompletion(
    modelId: "gpt-4",
    apiKey: "your-openai-api-key"
);
var kernel = builder.Build();
```

### 2. Create and Use LMRanker

```csharp
// Create the ranker
var ranker = new LMRanker(kernel);

// Sample documents
var documents = new[]
{
    "Machine learning is a subset of artificial intelligence.",
    "Deep learning uses neural networks with multiple layers.",
    "Natural language processing helps computers understand text.",
    "The quick brown fox jumps over the lazy dog."
};

// Score all documents
var query = "artificial intelligence and machine learning";
await foreach (var (document, score) in ranker.ScoreAsync(query, documents.ToAsyncEnumerable()))
{
    Console.WriteLine($"Score: {score:F3} | Document: {document}");
}

// Get top 3 most relevant documents
await foreach (var (document, score) in ranker.RankAsync(query, documents.ToAsyncEnumerable(), topN: 3))
{
    Console.WriteLine($"Top result - Score: {score:F3} | Document: {document}");
}
```

## API Reference

### Constructor

```csharp
public LMRanker(Kernel kernel)
```

- `kernel`: A configured Semantic Kernel instance

### Methods

#### ScoreAsync

```csharp
public async IAsyncEnumerable<(string DocumentText, double Score)> ScoreAsync(
    string query, 
    IAsyncEnumerable<string> documents)
```

Scores all documents against the query without reordering. Returns scores between 0.0 and 1.0.

#### RankAsync

```csharp
public async IAsyncEnumerable<(string DocumentText, double Score)> RankAsync(
    string query, 
    IAsyncEnumerable<string> documents, 
    int topN = 5)
```

Ranks documents by relevance and returns the top N results in descending order of relevance.

#### Vector Search Support

Both `ScoreAsync` and `RankAsync` have overloads that work with `VectorSearchResult<T>`:

```csharp
public async IAsyncEnumerable<(VectorSearchResult<T> Result, double Score)> ScoreAsync<T>(
    string query, 
    IAsyncEnumerable<VectorSearchResult<T>> searchResults, 
    Expression<Func<T, string>> textProperty)
```

## Scoring System

LMRanker uses a language model to evaluate document relevance on a scale from 0.0 to 1.0:

- **0.0-0.2**: Not relevant or completely off-topic
- **0.2-0.4**: Somewhat relevant but lacks specificity  
- **0.4-0.6**: Moderately relevant with some useful information
- **0.6-0.8**: Highly relevant with good information
- **0.8-1.0**: Extremely relevant and directly answers the query

The scoring is based on:
1. How directly the document answers the query
2. The quality and specificity of the information provided
3. The semantic relationship between query and document content

## Advanced Usage

### Custom Prompts

The ranker uses an internal prompt optimized for relevance scoring. For specialized use cases, you can extend the `LMRanker` class and override the scoring logic.

### Error Handling

The ranker includes robust error handling:
- Returns 0.0 for empty or null documents
- Returns 0.0 if the language model response cannot be parsed
- Gracefully handles model timeouts and errors

### Performance Considerations

- **Model Choice**: Faster models (like GPT-3.5) provide quicker responses but may be less accurate than larger models (like GPT-4)
- **Batch Processing**: Consider processing documents in batches for better throughput
- **Caching**: For repeated queries, consider caching results
- **Async Processing**: The async enumerable design allows for efficient streaming processing

## Comparison with BM25

| Feature | LMRanker | BM25Reranker |
|---------|----------|--------------|
| **Understanding** | Semantic understanding | Keyword matching |
| **Speed** | Slower (requires LM calls) | Faster (local computation) |
| **Accuracy** | Higher for complex queries | Good for keyword-based queries |
| **Cost** | Higher (API calls) | Lower (local processing) |
| **Offline** | Requires model access | Works offline |
| **Languages** | Supports LM languages | Limited by stop word lists |

## Best Practices

1. **Choose the Right Model**: Balance speed vs. accuracy based on your needs
2. **Handle Rate Limits**: Implement retry logic for API-based models
3. **Monitor Costs**: Track usage when using paid API services
4. **Combine Approaches**: Consider using BM25 for initial filtering, then LMRanker for final ranking
5. **Test Thoroughly**: Evaluate performance on your specific domain and query types

## License

This project is licensed under the MIT License - see the LICENSE file for details.
