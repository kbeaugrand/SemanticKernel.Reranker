using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel;
using SemanticKernel.Rankers.Abstractions;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SemanticKernel.Rankers.LMRanker;

/// <summary>
/// Represents the response from the language model for document relevance scoring.
/// </summary>
public class RelevanceResponse
{
    [JsonPropertyName("relevance_score")]
    public double RelevanceScore { get; set; }

    [JsonPropertyName("explanation")]
    public string Explanation { get; set; } = string.Empty;
}

/// <summary>
/// A Language Model-based reranker that uses Semantic Kernel to predict document relevance.
/// This ranker leverages large language models to assess how well documents answer a given query.
/// </summary>
public class LMRanker : IRanker
{
    private readonly Kernel _kernel;
    private readonly KernelFunction _relevanceFunction;
    private readonly JsonSerializerOptions _jsonOptions;

    /// <summary>
    /// Initializes a new instance of the LMRanker with the specified Semantic Kernel instance.
    /// </summary>
    /// <param name="kernel">The Semantic Kernel instance to use for language model operations</param>
    public LMRanker(Kernel kernel)
    {
        _kernel = kernel ?? throw new ArgumentNullException(nameof(kernel));
        
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            PropertyNameCaseInsensitive = true
        };

        // Create the relevance scoring function with structured output
        var prompt = """
        You are an expert at evaluating document relevance. Your task is to determine how relevant a document is for answering a specific query.

        Query: {{$query}}
        Document: {{$document}}

        Analyze the document and determine its relevance to the query. Consider:
        1. How directly the document answers the query
        2. The quality and specificity of the information provided
        3. The semantic relationship between the query and document content

        Provide your response as a JSON object with the following structure:
        {
          "relevance_score": <number between 0.0 and 1.0>,
          "explanation": "<brief explanation of the relevance score>"
        }

        The relevance_score should be:
        - 0.0-0.2: Not relevant or completely off-topic
        - 0.2-0.4: Somewhat relevant but lacks specificity
        - 0.4-0.6: Moderately relevant with some useful information
        - 0.6-0.8: Highly relevant with good information
        - 0.8-1.0: Extremely relevant and directly answers the query
        """;

        _relevanceFunction = _kernel.CreateFunctionFromPrompt(prompt);
    }

    /// <summary>
    /// Scores documents using a language model to predict relevance to the query.
    /// Returns an async enumerable of document-score pairs in the order they were processed.
    /// </summary>
    /// <param name="query">The search query to score documents against</param>
    /// <param name="documents">An async enumerable of documents to score</param>
    /// <returns>An async enumerable of tuples containing the document and its relevance score</returns>
    public async IAsyncEnumerable<(string DocumentText, double Score)> ScoreAsync(string query, IAsyncEnumerable<string> documents)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            await foreach (var document in documents)
            {
                yield return (document, 0.0);
            }
            yield break;
        }

        await foreach (var document in documents)
        {
            var score = await ScoreDocumentAsync(query, document);
            yield return (document, score);
        }
    }

    /// <summary>
    /// Scores vector search results using a language model to predict relevance to the query.
    /// </summary>
    /// <typeparam name="T">The type of the vector search result</typeparam>
    /// <param name="query">The search query to score documents against</param>
    /// <param name="searchResults">An async enumerable of vector search results to score</param>
    /// <param name="textProperty">Expression to extract text from the search result</param>
    /// <returns>An async enumerable of tuples containing the search result and its relevance score</returns>
    public async IAsyncEnumerable<(VectorSearchResult<T> Result, double Score)> ScoreAsync<T>(
        string query, 
        IAsyncEnumerable<VectorSearchResult<T>> searchResults, 
        Expression<Func<T, string>> textProperty)
    {
        var textExtractor = CompileTextExtractor(textProperty);

        if (string.IsNullOrWhiteSpace(query))
        {
            await foreach (var result in searchResults)
            {
                yield return (result, 0.0);
            }
            yield break;
        }

        await foreach (var result in searchResults)
        {
            var text = textExtractor(result.Record);
            var score = await ScoreDocumentAsync(query, text);
            yield return (result, score);
        }
    }

    /// <summary>
    /// Ranks documents using a language model and returns the top N most relevant documents.
    /// Results are sorted by relevance score in descending order.
    /// </summary>
    /// <param name="query">The search query to rank documents against</param>
    /// <param name="documents">An async enumerable of documents to rank</param>
    /// <param name="topN">The maximum number of top results to return</param>
    /// <returns>An async enumerable of the top N documents with their scores, sorted by relevance</returns>
    public async IAsyncEnumerable<(string DocumentText, double Score)> RankAsync(
        string query, 
        IAsyncEnumerable<string> documents, 
        int topN = 5)
    {
        var scoredDocuments = new List<(string DocumentText, double Score)>();

        // Score all documents first
        await foreach (var (document, score) in ScoreAsync(query, documents))
        {
            scoredDocuments.Add((document, score));
        }

        // Sort by score descending and take top N
        var topResults = scoredDocuments
            .OrderByDescending(x => x.Score)
            .Take(topN);

        foreach (var result in topResults)
        {
            yield return result;
        }
    }

    /// <summary>
    /// Ranks vector search results using a language model and returns the top N most relevant results.
    /// Results are sorted by relevance score in descending order.
    /// </summary>
    /// <typeparam name="T">The type of the vector search result</typeparam>
    /// <param name="query">The search query to rank documents against</param>
    /// <param name="documents">An async enumerable of vector search results to rank</param>
    /// <param name="textProperty">Expression to extract text from the search result</param>
    /// <param name="topN">The maximum number of top results to return</param>
    /// <returns>An async enumerable of the top N results with their scores, sorted by relevance</returns>
    public async IAsyncEnumerable<(VectorSearchResult<T> Result, double Score)> RankAsync<T>(
        string query, 
        IAsyncEnumerable<VectorSearchResult<T>> documents, 
        Expression<Func<T, string>> textProperty, 
        int topN = 5)
    {
        var scoredResults = new List<(VectorSearchResult<T> Result, double Score)>();

        // Score all documents first
        await foreach (var (result, score) in ScoreAsync(query, documents, textProperty))
        {
            scoredResults.Add((result, score));
        }

        // Sort by score descending and take top N
        var topResults = scoredResults
            .OrderByDescending(x => x.Score)
            .Take(topN);

        foreach (var result in topResults)
        {
            yield return result;
        }
    }

    /// <summary>
    /// Scores a single document against a query using the language model.
    /// </summary>
    /// <param name="query">The search query</param>
    /// <param name="document">The document to score</param>
    /// <returns>A relevance score between 0.0 and 1.0</returns>
    private async Task<double> ScoreDocumentAsync(string query, string document)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(document))
            {
                return 0.0;
            }

            var arguments = new KernelArguments
            {
                ["query"] = query,
                ["document"] = document
            };

            var result = await _relevanceFunction.InvokeAsync(_kernel, arguments);
            var responseText = result.ToString();

            // Parse the structured JSON response
            var relevanceResponse = JsonSerializer.Deserialize<RelevanceResponse>(responseText, _jsonOptions);
            
            // Ensure score is within valid range
            var score = Math.Max(0.0, Math.Min(1.0, relevanceResponse?.RelevanceScore ?? 0.0));
            return score;
        }
        catch (Exception)
        {
            // Return a neutral score if there's an error parsing the response
            return 0.0;
        }
    }

    /// <summary>
    /// Compiles a text extraction function from a lambda expression.
    /// </summary>
    /// <typeparam name="T">The type of the object to extract text from</typeparam>
    /// <param name="textProperty">The lambda expression defining how to extract text</param>
    /// <returns>A compiled function that extracts text from an object of type T</returns>
    private static Func<T, string> CompileTextExtractor<T>(Expression<Func<T, string>> textProperty)
    {
        if (textProperty.Body is MemberExpression memberExpression)
        {
            var propertyInfo = memberExpression.Member as PropertyInfo;
            if (propertyInfo != null)
            {
                return obj => propertyInfo.GetValue(obj)?.ToString() ?? string.Empty;
            }
        }

        // Fallback to compiling the expression
        return textProperty.Compile();
    }
}
