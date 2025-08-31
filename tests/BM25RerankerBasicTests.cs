using FluentAssertions;
using Mosaik.Core;
using SemanticKernel.Reranker.BM25;
using Xunit;

namespace SemanticKernel.Reranker.BM25.Tests;

public class BM25RerankerBasicTests
{
    private readonly BM25Reranker _reranker;

    public BM25RerankerBasicTests()
    {
        _reranker = new BM25Reranker(supportedLanguages: [Language.English]);
    }

    [Fact]
    public async Task ScoreAsync_WithSimpleQuery_ReturnsExpectedScores()
    {
        // Arrange
        var query = "cat";
        var documents = CreateAsyncEnumerable(new[]
        {
            "The cat is sleeping on the mat",
            "Dogs are loyal pets",
            "A black cat crossed the street"
        });

        // Act
        var results = new List<(string, double)>();
        await foreach (var result in _reranker.ScoreAsync(query, documents))
        {
            results.Add(result);
        }

        // Assert
        results.Should().HaveCount(3);
        
        // Documents containing "cat" should have higher scores than the one without
        var catDocScores = results.Where(r => r.Item1.Contains("cat")).Select(r => r.Item2).ToList();
        var dogDocScore = results.First(r => r.Item1.Contains("Dogs")).Item2;
        
        catDocScores.Should().AllSatisfy(score => score.Should().BeGreaterThan(dogDocScore));
    }

    [Fact]
    public async Task ScoreAsync_WithEmptyQuery_ReturnsZeroScores()
    {
        // Arrange
        var query = "";
        var documents = CreateAsyncEnumerable(new[]
        {
            "Some document content",
            "Another document"
        });

        // Act
        var results = new List<(string, double)>();
        await foreach (var result in _reranker.ScoreAsync(query, documents))
        {
            results.Add(result);
        }

        // Assert
        results.Should().HaveCount(2);
        results.Should().AllSatisfy(r => r.Item2.Should().Be(0));
    }

    [Fact]
    public async Task RankAsync_ReturnsTopNResults()
    {
        // Arrange
        var query = "machine learning";
        var documents = CreateAsyncEnumerable(new[]
        {
            "Machine learning is a subset of artificial intelligence",
            "Deep learning uses neural networks",
            "Artificial intelligence encompasses machine learning",
            "Neural networks are used in machine learning",
            "Cooking recipes for beginners",
            "Machine learning algorithms are powerful"
        });

        // Act
        var results = new List<(string, double)>();
        await foreach (var result in _reranker.RankAsync(query, documents, topN: 3))
        {
            results.Add(result);
        }

        // Assert
        results.Should().HaveCount(3);
        
        // Results should be sorted by relevance (descending order)
        for (int i = 1; i < results.Count; i++)
        {
            results[i - 1].Item2.Should().BeGreaterOrEqualTo(results[i].Item2);
        }
    }

    private static async IAsyncEnumerable<string> CreateAsyncEnumerable(IEnumerable<string> source)
    {
        foreach (var item in source)
        {
            await Task.Yield();
            yield return item;
        }
    }
}
