# LMRanker Sample

This sample demonstrates how to use the Language Model Reranker (LMRanker) with Semantic Kernel to rank and score documents based on their relevance to search queries.

## Overview

The LMRanker uses large language models to assess document relevance by leveraging the semantic understanding capabilities of modern AI models. Unlike traditional ranking methods that rely on statistical measures, LMRanker can understand context, meaning, and nuanced relationships between queries and documents.

## Features Demonstrated

### 1. Basic Document Scoring

- Score individual documents against a query
- Get relevance scores between 0.0 and 1.0
- Stream results for memory efficiency

### 2. Document Ranking

- Rank multiple documents by relevance
- Get top-N most relevant documents
- Compare performance across different queries

### 3. Performance Analysis

- Measure ranking performance with different dataset sizes
- Compare processing times and accuracy
- Analyze score distributions

### 4. Query Comparison

- Test various query types (technical, specific, broad)
- Understand how query formulation affects results
- Compare ranking consistency across query categories

## Prerequisites

Before running this sample, you need to configure an AI service. The sample supports:

1. **Azure OpenAI** (Recommended for production)
2. **OpenAI**
3. **Local models** (e.g., via Ollama)

## Configuration

### Option 1: Azure OpenAI

1. Set environment variables:

   ```bash
   set AZURE_OPENAI_ENDPOINT=https://your-resource.openai.azure.com/
   set AZURE_OPENAI_API_KEY=your-api-key
   set AZURE_OPENAI_DEPLOYMENT_NAME=gpt-4
   ```

2. Uncomment the Azure OpenAI configuration in `ConfigureAIService()` method

### Option 2: OpenAI

1. Set environment variables:

   ```bash
   set OPENAI_API_KEY=your-openai-api-key
   set OPENAI_MODEL=gpt-4
   ```

2. Uncomment the OpenAI configuration in `ConfigureAIService()` method

### Option 3: Local Model (Ollama)

1. Install and run Ollama with a compatible model:

   ```bash
   ollama run llama3.1
   ```

2. Uncomment the local model configuration in `ConfigureAIService()` method

## Running the Sample

1. Configure your AI service (see Configuration section above)

2. Build and run the project:

   ```bash
   dotnet build
   dotnet run
   ```

3. The sample will execute four demonstrations:
   - **Demo 1**: Basic document scoring
   - **Demo 2**: Document ranking with multiple queries
   - **Demo 3**: Performance analysis
   - **Demo 4**: Query comparison analysis

## Expected Output

```text
=== Language Model Reranker Sample ===

✅ Configured Azure OpenAI: gpt-4
🚀 Running LMRanker demonstrations...

📊 Demo 1: Basic Document Scoring
Query: 'machine learning algorithms and neural networks'
======================================================================
Score: 0.851 | Machine learning is a subset of artificial intel...
Score: 0.823 | Deep learning uses neural networks with multiple...
Score: 0.734 | Natural language processing enables computers to...
...

⏱️  Scored 19 documents in 2847ms
📈 Average score: 0.456

🏆 Demo 2: Document Ranking (Top 5)

Query: 'artificial intelligence and deep learning'
------------------------------------------------------------
1. Score: 0.876 | Deep learning uses neural networks with mult...
2. Score: 0.845 | Machine learning is a subset of artificial...
3. Score: 0.823 | Artificial intelligence aims to create syst...
...
```

## Understanding the Results

### Relevance Scores

- **0.0-0.2**: Not relevant or completely off-topic
- **0.2-0.4**: Somewhat relevant but lacks specificity  
- **0.4-0.6**: Moderately relevant with some useful information
- **0.6-0.8**: Highly relevant with good information
- **0.8-1.0**: Extremely relevant and directly answers the query

### Performance Considerations

- LMRanker makes API calls to language models, so it's slower than statistical methods
- Batch processing and caching can improve performance
- Consider cost implications when using cloud-based models
- Local models provide privacy but may have lower accuracy

## Advanced Usage

### Custom Scoring Logic

The LMRanker uses a sophisticated prompt that considers:

- Direct relevance to the query
- Quality and specificity of information
- Semantic relationships between query and content

### Integration with Vector Search

LMRanker can be used to rerank results from vector search systems:

```csharp
// Rerank vector search results
await foreach (var (result, score) in ranker.ScoreAsync(query, vectorResults, r => r.Text))
{
    // Process reranked results
}
```

### Error Handling

The sample includes comprehensive error handling for:

- Missing AI service configuration
- Network connectivity issues
- Malformed responses from language models
- Invalid input data

## Troubleshooting

### Common Issues

1. **"No AI service configured"**
   - Ensure you've uncommented one of the AI service configurations
   - Verify environment variables are set correctly

2. **API key errors**
   - Check that your API keys are valid and have sufficient permissions
   - Verify the endpoint URLs are correct

3. **Slow performance**
   - This is expected behavior - LMRanker prioritizes accuracy over speed
   - Consider using smaller document sets for testing
   - Implement caching for repeated queries

4. **Low relevance scores**
   - Ensure your queries are well-formed and specific
   - Check that documents contain relevant content
   - Verify the language model is appropriate for your domain

## Next Steps

- Experiment with different query formulations
- Test with your own document collections
- Integrate with existing search systems
- Compare results with traditional ranking methods like BM25
- Optimize for your specific use case and performance requirements

## Related Documentation

- [Semantic Kernel Documentation](https://learn.microsoft.com/semantic-kernel/)
- [Azure OpenAI Service](https://learn.microsoft.com/azure/cognitive-services/openai/)
- [BM25 Reranker Sample](../samples/BM25Sample) for comparison with statistical methods
