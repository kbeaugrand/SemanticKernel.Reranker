![Build & Test](https://github.com/kbeaugrand/SemanticKernel.Rankers/actions/workflows/build_tests.yml/badge.svg)
![Create Release](https://github.com/kbeaugrand/SemanticKernel.Rankers/actions/workflows/publish.yml/badge.svg)
![Version](https://img.shields.io/github/v/release/kbeaugrand/SemanticKernel.Rankers)
![License](https://img.shields.io/github/license/kbeaugrand/SemanticKernel.Rankers)

# BM25 Rankers

**A robust C# library for reranking search results using the classic BM25 algorithm with advanced natural language processing, leveraging the Catalyst NLP library.**

---

## Table of Contents

- [Introduction](#introduction)
- [Why BM25 with NLP?](#why-bm25-with-nlp)
- [Features](#features)
- [Getting Started](#getting-started)
- [Usage Example](#usage-example)
- [How It Works](#how-it-works)
- [Customization](#customization)
- [License](#license)

---

## Introduction

This project provides a flexible C# implementation of BM25, a state-of-the-art ranking function used by search engines, **enhanced with advanced natural language processing capabilities**.  
With this library, you can rerank search results or candidate passages using sophisticated tokenization, lemmatization, stop word removal, and multi-language support through the Catalyst NLP library.

---

## Why BM25 with NLP?

Traditional BM25 relies on exact token overlap between query and document. However, raw text processing can be noisy:

- Text contains punctuation, stop words, and varying word forms.
- "running" vs "run", "cars" vs "car", mixed case, etc.
- Different languages require different processing approaches.

**By incorporating advanced NLP preprocessing:**

- The reranker uses lemmatization to normalize word forms (running → run).
- Automatic language detection ensures proper processing for multilingual content.
- Stop words are filtered out to focus on meaningful terms.
- Part-of-speech tagging helps identify important content words.

**NLP preprocessing enhances the precision and effectiveness of traditional BM25 scoring.**

---

## Features

- **BM25 core algorithm:** Highly tunable (`k1`, `b` parameters).
- **Advanced NLP processing:** Powered by the Catalyst library for tokenization and linguistic analysis.
- **Multi-language support:** Automatic language detection with support for English, French, German, and more.
- **Intelligent preprocessing:** Lemmatization, stop word removal, and part-of-speech filtering.
- **Asynchronous processing:** Async tokenization and scoring for high performance.
- **Easy to extend:** Customizable parameters and configurable language models.

---

## Getting Started

### Prerequisites

- .NET 8.0+

### Installation

1. Install the package via NuGet Package Manager or via the .NET CLI:

```dotnetcli
dotnet add package SemanticKernel.Reranker.BM25
```

## Usage Example

```csharp
using SemanticKernel.Reranker.BM25;

// Sample documents to rank
var documents = new List<string>
{
    "The quick brown fox jumps over the lazy dog.",
    "A brown dog jumps over another dog.",
    "The quick brown fox.",
    "Machine learning is a subset of artificial intelligence.",
    "Natural language processing helps computers understand human language."
};

// Create BM25 reranker
var bm25 = new BM25Reranker();

// Method 1: Basic scoring - get all document scores
Console.WriteLine("=== Basic Scoring ===");
await foreach (var (document, score) in bm25.ScoreAsync("quick brown fox", documents.ToAsyncEnumerable()))
{
    Console.WriteLine($"Score: {score:F4} | Document: \"{document}\"");
}

// Method 2: Top-N ranking - get only the best results
Console.WriteLine("\n=== Top-N Ranking ===");
await foreach (var (document, score) in bm25.RankAsync("quick brown fox", documents.ToAsyncEnumerable(), topN: 3))
{
    Console.WriteLine($"Score: {score:F4} | Document: \"{document}\"");
}

// Method 3: Optimized approach with pre-computed corpus statistics
Console.WriteLine("\n=== Optimized with Corpus Statistics ===");
var corpusStats = await bm25.ComputeCorpusStatisticsAsync(documents.ToAsyncEnumerable());
var optimizedBm25 = new BM25Reranker(corpusStats);

await foreach (var (document, score) in optimizedBm25.ScoreAsync("machine learning", documents.ToAsyncEnumerable()))
{
    Console.WriteLine($"Score: {score:F4} | Document: \"{document}\"");
}

// Extension method to convert List to IAsyncEnumerable
public static class ListExtensions
{
    public static async IAsyncEnumerable<T> ToAsyncEnumerable<T>(this IEnumerable<T> source)
    {
        foreach (var item in source)
        {
            yield return item;
            await Task.Yield(); // Simulate async behavior
        }
    }
}
```

---

## How It Works

1. **Document Preprocessing:** Each document is processed through the Catalyst NLP pipeline:
   - Automatic language detection
   - Tokenization into individual words
   - Lemmatization to normalize word forms
   - Stop word removal
   - Part-of-speech filtering (removes punctuation and symbols)

2. **Index Building:** The system builds an inverted index with:
   - Document frequency (DF) for each term
   - Document lengths and average document length
   - Preprocessed token lists for efficient scoring

3. **Query Processing:** Query text undergoes the same NLP preprocessing as documents

4. **BM25 Scoring:** For each document, calculates the BM25 score using:
   - Term frequency (TF) in the document
   - Inverse document frequency (IDF)
   - Document length normalization
   - Tunable parameters k1 and b

---

## Customization

### BM25 Parameters

You can customize the BM25 algorithm behavior by passing parameters to the scoring methods:

```csharp
// Custom k1 and b parameters
var bm25 = new BM25Reranker();

// Use custom parameters in scoring
await foreach (var (document, score) in bm25.ScoreAsync("query", documents.ToAsyncEnumerable(), k1: 2.0, b: 0.5))
{
    Console.WriteLine($"Score: {score:F4} | Document: \"{document}\"");
}

// Or in ranking with top-N
await foreach (var (document, score) in bm25.RankAsync("query", documents.ToAsyncEnumerable(), topN: 5, k1: 2.0, b: 0.5))
{
    Console.WriteLine($"Score: {score:F4} | Document: \"{document}\"");
}
```

- **k1 (default: 1.5):** Controls term frequency saturation. Higher values give more weight to repeated terms.
- **b (default: 0.75):** Controls document length normalization. 0 = no normalization, 1 = full normalization.

### Language Support

The library automatically detects document language and applies appropriate NLP models. You can also optionally restrict the supported languages:

```csharp
using Catalyst;

// Create reranker with specific language support
var supportedLanguages = new HashSet<Language> { Language.English, Language.French, Language.German };
var bm25 = new BM25Reranker(supportedLanguages: supportedLanguages);

// Or combine with corpus statistics
var corpusStats = await new BM25Reranker().ComputeCorpusStatisticsAsync(documents.ToAsyncEnumerable());
var optimizedBm25 = new BM25Reranker(corpusStats, supportedLanguages);
```

Supported languages include:

- English
- French  
- German
- Additional languages supported by Catalyst

### Performance Optimization

The library includes several performance optimizations:

**Caching:** The reranker automatically caches tokenization results for better performance with repeated queries or documents:

```csharp
var bm25 = new BM25Reranker();

// First run - cache miss
await foreach (var result in bm25.ScoreAsync("query", documents.ToAsyncEnumerable())) { }

// Second run - cache hit (much faster)
await foreach (var result in bm25.ScoreAsync("query", documents.ToAsyncEnumerable())) { }

// Clear cache when needed
BM25Reranker.ClearCache();
```

**Corpus Statistics:** For multiple queries on the same document set, pre-compute corpus statistics:

```csharp
var bm25 = new BM25Reranker();
var corpusStats = await bm25.ComputeCorpusStatisticsAsync(documents.ToAsyncEnumerable());
var optimizedBm25 = new BM25Reranker(corpusStats);

// Now all queries will be faster
await foreach (var result in optimizedBm25.ScoreAsync("query1", documents.ToAsyncEnumerable())) { }
await foreach (var result in optimizedBm25.ScoreAsync("query2", documents.ToAsyncEnumerable())) { }
```

---

## License

This project is licensed under the MIT License - see the [LICENSE.txt](LICENSE.txt) file for details.
