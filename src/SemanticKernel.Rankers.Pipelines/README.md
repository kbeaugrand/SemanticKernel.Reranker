# SemanticKernel.Rankers.Pipelines

This project provides reusable pipeline implementations for RAG systems with support for chaining multiple rankers in cascade.

## Components

### CascadeRerankPipeline

A flexible pipeline implementation that chains multiple rankers in cascade. Each stage filters the top-K results for the next stage until the final top-M results are produced.

**Features:**

- **IRanker Implementation**: Fully implements the `IRanker` interface with support for both string documents and `VectorSearchResult<T>` objects
- **Cascade Filtering**: Each ranker stage filters results for the next stage based on configurable parameters
- **Configurable Thresholds**: Support for score thresholds, top-K, and top-M filtering
- **Async Streaming**: Efficient async enumerable processing for large document sets
- **Type Safety**: Full generic support for different document types

### BM25ThenLMRankerPipeline

A specialized two-stage retrieval pipeline for RAG systems:

1. **BM25 Retrieval**: High-recall lexical retrieval to fetch top-K candidates.
2. **LM Re-ranking**: LLM-based scoring, sorting, and filtering to select top-M high-precision passages.

## API Methods

The `CascadeRerankPipeline` implements all `IRanker` methods:

### RankAsync (String Documents)

```csharp
IAsyncEnumerable<(string DocumentText, double Score)> RankAsync(
    string query, 
    IAsyncEnumerable<string> documents, 
    int topN = 5)
```

### RankAsync (VectorSearchResult Documents)

```csharp
IAsyncEnumerable<(VectorSearchResult<T> Result, double Score)> RankAsync<T>(
    string query, 
    IAsyncEnumerable<VectorSearchResult<T>> documents, 
    Expression<Func<T, string>> textProperty, 
    int topN = 5)
```

### ScoreAsync (String Documents)

```csharp
IAsyncEnumerable<(string DocumentText, double Score)> ScoreAsync(
    string query, 
    IAsyncEnumerable<string> documents)
```

### ScoreAsync (VectorSearchResult Documents)

```csharp
IAsyncEnumerable<(VectorSearchResult<T> Result, double Score)> ScoreAsync<T>(
    string query, 
    IAsyncEnumerable<VectorSearchResult<T>> searchResults, 
    Expression<Func<T, string>> textProperty)
```

## Usage

### Basic String Document Ranking

```csharp
using SemanticKernel.Rankers.Pipelines;
using SemanticKernel.Rankers.BM25;
using SemanticKernel.Rankers.LMRanker;

// Create rankers
var bm25Ranker = new BM25Reranker();
var lmRanker = new LMRanker(/* configuration */);

// Configure pipeline
var config = new CascadeRerankPipelineConfig
{
    TopK = 20,           // Pass top 20 from each stage to next
    TopM = 5,            // Final output: top 5 results
    ScoreThreshold = 0.1 // Minimum score threshold
};

// Create pipeline
var pipeline = new CascadeRerankPipeline(
    new List<IRanker> { bm25Ranker, lmRanker }, 
    config);

// Use for ranking
var documents = new[] { "doc1", "doc2", "doc3" };
var query = "search query";

await foreach (var (docText, score) in pipeline.RankAsync(query, ToAsyncEnumerable(documents), topN: 3))
{
    Console.WriteLine($"Document: {docText}, Score: {score}");
}
```

### VectorSearchResult Ranking

```csharp
// For custom document types
public class MyDocument
{
    public string Content { get; set; }
    public string Title { get; set; }
}

// Rank using content property
await foreach (var (result, score) in pipeline.RankAsync(
    query, 
    ToAsyncEnumerable(searchResults), 
    doc => doc.Content,  // Extract text using this property
    topN: 5))
{
    Console.WriteLine($"Document: {result.Record.Title}, Score: {score}");
}
```

### Configuration

- **TopK**: Number of top results to pass from each intermediate stage (default: 20)
- **TopM**: Number of final results to output from the last stage (default: 5)  
- **ScoreThreshold**: Minimum score threshold for results (default: 0.0)

Configure K, M, batching, model, thresholds, and prompts via pipeline-specific configuration classes.

## BM25ThenLMRankerPipeline Implementation Details

The `BM25ThenLMRankerPipeline` is a specialized implementation using the cascade pipeline with BM25 and LM rankers.

### Example Usage

```csharp
var bm25 = new BM25Reranker(...);
var lmRanker = new LMRanker(...);
var config = new BM25ThenLMRankerPipelineConfig { TopK = 20, TopM = 5 };
var pipeline = new BM25ThenLMRankerPipeline(bm25, lmRanker, config);
var result = await pipeline.RetrieveAndRankAsync(query, corpus);
Console.WriteLine($"Top {result.TopMResults.Count} results retrieved in {result.BM25Time + result.LMTime}");
```

### Features

- Use `BM25ThenLMRankerPipeline.RetrieveAndRankAsync(query, corpus)` to get top-M results.
- Observability: timings, token usage, scores, and selection decisions are returned in `PipelineResult`.
- Improve answerability and reduce hallucinations by prioritizing relevant passages.
- Balance recall (BM25) and precision (LM re-ranking) with configurable trade-offs.
- Keep latency and cost within budget.
