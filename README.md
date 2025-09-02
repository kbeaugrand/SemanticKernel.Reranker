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
- [Usage Examples](#usage-examples)
- [License](#license)

---

## Introduction

This project provides a flexible C# implementation of Rankers for Microsoft's Semantic Kernel, including:

- **BM25Ranker**: A classic ranking function widely used in search engines, based on term frequency and document length normalization.
- **LMRanker**: A neural ranker leveraging advanced language models for semantic reranking.

Both rankers support sophisticated tokenization, lemmatization, stop word removal, and multi-language support through the Catalyst NLP library.

---

## Installation

Install via NuGet:

```shell
dotnet add package SemanticKernel.Rankers
```

Or using the .NET CLI:

```shell
dotnet add package SemanticKernel.Rankers
```

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

---

## License

This project is licensed under the MIT License - see the [LICENSE.txt](LICENSE.txt) file for details.
