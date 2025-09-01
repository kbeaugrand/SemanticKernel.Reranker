using Microsoft.Extensions.VectorData;
using System.Linq.Expressions;

namespace SemanticKernel.Rankers.Abstractions;

public interface IRanker
{
    IAsyncEnumerable<(string DocumentText, double Score)> RankAsync(string query, IAsyncEnumerable<string> documents, int topN = 5);
    IAsyncEnumerable<(VectorSearchResult<T> Result, double Score)> RankAsync<T>(string query, IAsyncEnumerable<VectorSearchResult<T>> documents, Expression<Func<T, string>> textProperty, int topN = 5);
    IAsyncEnumerable<(string DocumentText, double Score)> ScoreAsync(string query, IAsyncEnumerable<string> documents);
    IAsyncEnumerable<(VectorSearchResult<T> Result, double Score)> ScoreAsync<T>(string query, IAsyncEnumerable<VectorSearchResult<T>> searchResults, Expression<Func<T, string>> textProperty);
}
