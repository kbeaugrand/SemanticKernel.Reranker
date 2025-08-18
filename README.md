# BM25 Embedding-Enhanced Reranker

**A robust C# library for semantic reranking of search results using the classic BM25 algorithm, enhanced with word embeddings, leveraging on Microsoft's Semantic Kernel.**

---

## Table of Contents

- [Introduction](#introduction)
- [Why BM25 + Embeddings?](#why-bm25--embeddings)
- [Features](#features)
- [Getting Started](#getting-started)
- [Usage Example](#usage-example)
- [How It Works](#how-it-works)
- [Customization](#customization)
- [License](#license)

---

## Introduction

This project provides a flexible C# implementation of BM25, a state-of-the-art ranking function used by search engines, **augmented with semantic matching via word embeddings**.  
With this library, you can rerank search results or candidate passages in a way that is both statistically and semantically aware.

---

## Why BM25 + Embeddings?

Traditional BM25 relies on exact token overlap between query and document. However, language is rich:  
- Users make typos, use synonyms, or related terms.
- "House" ≈ "home", "car" ≈ "vehicle", "ordinateur" ≈ "computer", etc.

**By incorporating word embeddings:**
- The reranker can match semantically similar words even when their surface forms are different.
- This drastically improves recall and user satisfaction in search and retrieval tasks, especially in noisy or diverse datasets.

**Embeddings bridge the gap between lexical matching (BM25) and semantic understanding.**

---

## Features

- **BM25 core algorithm:** Highly tunable (`k1`, `b`).
- **Word embedding integration:** Plug in any embedding model using the Semantic Kernel.
- **Semantic term similarity:** Matches and scores terms based on cosine similarity.
- **Asynchronous, high-performance code:** Async embedding generation and scoring.
- **Easy to extend:** Customizable similarity threshold, scoring, tokenization, and more.

---

## Getting Started

### Prerequisites

- .NET 8.0+
- NuGet packages:
  - `System.Numerics.Tensors` (for vector ops)
  - `Microsoft.SemanticKernel` (for embeddings)

### Installation

