![Build & Test](https://github.com/kbeaugrand/SemanticKernel.Rankers/actions/workflows/build_tests.yml/badge.svg)
![Create Release](https://github.com/kbeaugrand/SemanticKernel.Rankers/actions/workflows/publish.yml/badge.svg)
![Version](https://img.shields.io/github/v/release/kbeaugrand/SemanticKernel.Rankers)
![License](https://img.shields.io/github/license/kbeaugrand/SemanticKernel.Rankers)

# Semantic Kernel Rankers

**A robust C# library for reranking search results using Semantic Kernel**

---

## Table of Contents

- [Introduction](#introduction)
- [Installation](#installation)
- [Ranking Pipelines](#ranking-pipelines)
- [Usage Examples](#usage-examples)
- [License](#license)

---

## Introduction

This project provides a flexible C# implementation of Rankers for Microsoft's Semantic Kernel, including:

- **BM25Ranker**: A classic ranking function widely used in search engines, based on term frequency and document length normalization.
> This ranker supports sophisticated tokenization, lemmatization, stop word removal, and multi-language support through the Catalyst NLP library.
- **LMRanker**: A neural ranker leveraging advanced language models for semantic reranking.
- **Ranking Pipelines**: Powerful pipeline implementations that chain multiple rankers in cascade for optimal retrieval performance.

---

## Installation

You can install the individual packages based on your needs:

```shell
# For BM25 ranking
dotnet add package SemanticKernel.Rankers.BM25

# For LM-based ranking  
dotnet add package SemanticKernel.Rankers.LMRanker

# For ranking pipelines
dotnet add package SemanticKernel.Rankers.Pipelines
```

---

## Ranking Pipelines

For production RAG systems, combining multiple ranking approaches often yields better results than using a single ranker. This library provides powerful pipeline implementations:

### CascadeRerankPipeline

A flexible pipeline that chains multiple rankers in cascade. Each stage filters the top-K results for the next stage until the final top-M results are produced.

**Key Features:**

- **Multi-stage Filtering**: Each ranker stage processes and filters results for the next stage
- **Configurable Parameters**: Support for score thresholds, top-K, and top-M filtering
- **Type Safety**: Full generic support for different document types including `VectorSearchResult<T>`
- **Async Streaming**: Efficient processing for large document sets

### BM25ThenLMRankerPipeline

A specialized two-stage retrieval pipeline optimized for RAG systems:

1. **BM25 Retrieval**: High-recall lexical retrieval to fetch top-K candidates
2. **LM Re-ranking**: LLM-based semantic scoring and filtering to select top-M high-precision passages

This approach balances recall (BM25) and precision (LM re-ranking) while keeping latency and cost within budget.

**Benefits:**

- Improved answerability and reduced hallucinations
- Configurable trade-offs between recall and precision
- Built-in observability with timings, token usage, and scores

---

## Usage Examples

### BM25Ranker

```shell
dotnet add package SemanticKernel.Rankers.BM25
```

```csharp
using SemanticKernel.Rankers.BM25;

// Prepare your documents and query
var documents = new List<string> { "The quick brown fox", "Jumps over the lazy dog" };
var query = "quick fox";

// Create and use the BM25Ranker
var ranker = new BM25Ranker();
var rankedResults = ranker.Rank(query, documents);

// rankedResults is a list of documents ordered by relevance
```

### LMRanker

```shell
dotnet add package SemanticKernel.Rankers.LMRanker
```

```csharp
using SemanticKernel.Rankers.LMRanker;

// Prepare your documents and query
var documents = new List<string> { "The quick brown fox", "Jumps over the lazy dog" };
var query = "quick fox";

// Create and use the LMRanker
var ranker = new LMRanker();
var rankedResults = await ranker.RankAsync(query, documents);

// rankedResults is a list of documents ordered by semantic relevance
```

### Pipeline Usage Examples

```shell
dotnet add package SemanticKernel.Rankers.Pipelines
```

#### Using CascadeRerankPipeline

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
var documents = new[] { "The quick brown fox", "Jumps over the lazy dog", "Another document" };
var query = "quick fox";

await foreach (var (docText, score) in pipeline.RankAsync(query, documents.ToAsyncEnumerable(), topN: 3))
{
    Console.WriteLine($"Document: {docText}, Score: {score}");
}
```

#### Using BM25ThenLMRankerPipeline

```csharp
using SemanticKernel.Rankers.Pipelines;

// Create specialized pipeline
var bm25 = new BM25Reranker();
var lmRanker = new LMRanker();
var config = new BM25ThenLMRankerPipelineConfig { TopK = 20, TopM = 5 };
var pipeline = new BM25ThenLMRankerPipeline(bm25, lmRanker, config);

// Retrieve and rank with observability
var result = await pipeline.RetrieveAndRankAsync(query, corpus);
Console.WriteLine($"Retrieved {result.TopMResults.Count} results in {result.BM25Time + result.LMTime}ms");
```

---

## License

This project is licensed under the MIT License - see the [LICENSE.txt](LICENSE.txt) file for details.
