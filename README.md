# BM25 Reranker

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


## Usage Example

```csharp
using SemanticKernel.Reranker.BM25;

// Sample documents to index
var documents = new List<string>
{
    "The quick brown fox jumps over the lazy dog.",
    "A brown dog jumps over another dog.",
    "The quick brown fox.",
    "Machine learning is a subset of artificial intelligence.",
    "Natural language processing helps computers understand human language."
};

// Create BM25 reranker with default parameters (k1=1.5, b=0.75)
var bm25 = new BM25Reranker(documents);

// Rank documents for a query
var results = await bm25.RankAsync("quick brown fox", topN: 3);

// Display results
foreach (var (documentIndex, score) in results)
{
    Console.WriteLine($"Document #{documentIndex}: Score = {score:F4}");
    Console.WriteLine($"Content: {documents[documentIndex]}");
    Console.WriteLine();
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

You can customize the BM25 algorithm behavior:

```csharp
// Custom k1 and b parameters
var bm25 = new BM25Reranker(documents, k1: 2.0, b: 0.5);
```

- **k1 (default: 1.5):** Controls term frequency saturation. Higher values give more weight to repeated terms.
- **b (default: 0.75):** Controls document length normalization. 0 = no normalization, 1 = full normalization.

### Language Support

The library automatically detects document language and applies appropriate NLP models. Supported languages include:

- English
- French  
- German
- Additional languages supported by Catalyst

---

## License

This project is licensed under the MIT License - see the [LICENSE.txt](LICENSE.txt) file for details.