using FluentAssertions;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel;
using SemanticKernel.Rankers.Abstractions;
using SemanticKernel.Rankers.LMRanker;
using System.Text.Json;
using Xunit;

namespace SemanticKernel.Rankers.LMRanker.Tests;

public class LMRankerBasicTests
{
    [Fact]
    public void Constructor_WithNullKernel_ThrowsArgumentNullException()
    {
        // Act & Assert
        var action = () => new LMRanker(null!);
        action.Should().Throw<ArgumentNullException>().WithParameterName("kernel");
    }

    [Fact]
    public void Constructor_WithValidKernel_CreatesInstance()
    {
        // Arrange
        var kernel = CreateMockKernel();

        // Act
        var ranker = new LMRanker(kernel);

        // Assert
        ranker.Should().NotBeNull();
    }

    [Fact]
    public void RelevanceResponse_Properties_CanBeSetAndGet()
    {
        // Arrange & Act
        var response = new RelevanceResponse
        {
            RelevanceScore = 0.85,
            Explanation = "Test explanation"
        };

        // Assert
        response.RelevanceScore.Should().Be(0.85);
        response.Explanation.Should().Be("Test explanation");
    }

    [Fact]
    public void RelevanceResponse_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var response = new RelevanceResponse();

        // Assert
        response.RelevanceScore.Should().Be(0.0);
        response.Explanation.Should().Be(string.Empty);
    }

    [Fact]
    public void RelevanceResponse_JsonSerialization_WorksCorrectly()
    {
        // Arrange
        var response = new RelevanceResponse
        {
            RelevanceScore = 0.75,
            Explanation = "Good match"
        };

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        };

        // Act
        var json = JsonSerializer.Serialize(response, options);
        var deserialized = JsonSerializer.Deserialize<RelevanceResponse>(json, options);

        // Assert
        json.Should().Contain("relevance_score");
        json.Should().Contain("explanation");
        deserialized.Should().NotBeNull();
        deserialized!.RelevanceScore.Should().Be(0.75);
        deserialized.Explanation.Should().Be("Good match");
    }

    [Fact]
    public async Task ScoreAsync_WithEmptyQuery_ReturnsZeroScores()
    {
        // Arrange
        var kernel = CreateMockKernel();
        var ranker = new LMRanker(kernel);
        var documents = CreateAsyncEnumerable(new[]
        {
            "Document about machine learning",
            "Document about cooking"
        });

        // Act
        var results = new List<(string, double)>();
        await foreach (var result in ranker.ScoreAsync("", documents))
        {
            results.Add(result);
        }

        // Assert
        results.Should().HaveCount(2);
        results.Should().AllSatisfy(r => r.Item2.Should().Be(0.0));
    }

    [Fact]
    public async Task ScoreAsync_WithWhitespaceQuery_ReturnsZeroScores()
    {
        // Arrange
        var kernel = CreateMockKernel();
        var ranker = new LMRanker(kernel);
        var documents = CreateAsyncEnumerable(new[]
        {
            "Document about machine learning",
            "Document about cooking"
        });

        // Act
        var results = new List<(string, double)>();
        await foreach (var result in ranker.ScoreAsync("   ", documents))
        {
            results.Add(result);
        }

        // Assert
        results.Should().HaveCount(2);
        results.Should().AllSatisfy(r => r.Item2.Should().Be(0.0));
    }

    [Fact]
    public async Task ScoreAsync_WithNullQuery_ReturnsZeroScores()
    {
        // Arrange
        var kernel = CreateMockKernel();
        var ranker = new LMRanker(kernel);
        var documents = CreateAsyncEnumerable(new[]
        {
            "Document about machine learning",
            "Document about cooking"
        });

        // Act
        var results = new List<(string, double)>();
        await foreach (var result in ranker.ScoreAsync(null!, documents))
        {
            results.Add(result);
        }

        // Assert
        results.Should().HaveCount(2);
        results.Should().AllSatisfy(r => r.Item2.Should().Be(0.0));
    }

    [Fact]
    public async Task ScoreAsync_WithEmptyDocuments_ReturnsEmptyResults()
    {
        // Arrange
        var kernel = CreateMockKernel();
        var ranker = new LMRanker(kernel);
        var documents = CreateAsyncEnumerable(Array.Empty<string>());

        // Act
        var results = new List<(string, double)>();
        await foreach (var result in ranker.ScoreAsync("test query", documents))
        {
            results.Add(result);
        }

        // Assert
        results.Should().BeEmpty();
    }

    [Fact]
    public async Task ScoreAsync_VectorSearchResults_WithEmptyQuery_ReturnsZeroScores()
    {
        // Arrange
        var kernel = CreateMockKernel();
        var ranker = new LMRanker(kernel);
        var searchResults = CreateAsyncEnumerable(new[]
        {
            new VectorSearchResult<TestDocument>(new TestDocument { Content = "Document 1" }, 0.9),
            new VectorSearchResult<TestDocument>(new TestDocument { Content = "Document 2" }, 0.8)
        });

        // Act
        var results = new List<(VectorSearchResult<TestDocument>, double)>();
        await foreach (var result in ranker.ScoreAsync("", searchResults, doc => doc.Content))
        {
            results.Add(result);
        }

        // Assert
        results.Should().HaveCount(2);
        results.Should().AllSatisfy(r => r.Item2.Should().Be(0.0));
    }

    [Fact]
    public async Task ScoreAsync_VectorSearchResults_WithNullQuery_ReturnsZeroScores()
    {
        // Arrange
        var kernel = CreateMockKernel();
        var ranker = new LMRanker(kernel);
        var searchResults = CreateAsyncEnumerable(new[]
        {
            new VectorSearchResult<TestDocument>(new TestDocument { Content = "Document 1" }, 0.9),
            new VectorSearchResult<TestDocument>(new TestDocument { Content = "Document 2" }, 0.8)
        });

        // Act
        var results = new List<(VectorSearchResult<TestDocument>, double)>();
        await foreach (var result in ranker.ScoreAsync(null!, searchResults, doc => doc.Content))
        {
            results.Add(result);
        }

        // Assert
        results.Should().HaveCount(2);
        results.Should().AllSatisfy(r => r.Item2.Should().Be(0.0));
    }

    [Fact]
    public async Task RankAsync_WithEmptyQuery_ReturnsZeroScores()
    {
        // Arrange
        var kernel = CreateMockKernel();
        var ranker = new LMRanker(kernel);
        var documents = CreateAsyncEnumerable(new[]
        {
            "Document about machine learning",
            "Document about cooking",
            "Document about sports"
        });

        // Act
        var results = new List<(string, double)>();
        await foreach (var result in ranker.RankAsync("", documents, topN: 2))
        {
            results.Add(result);
        }

        // Assert
        results.Should().HaveCount(2); // Should return topN results
        results.Should().AllSatisfy(r => r.Item2.Should().Be(0.0));
    }

    [Fact]
    public async Task RankAsync_WithTopNGreaterThanDocumentCount_ReturnsAllDocuments()
    {
        // Arrange
        var kernel = CreateMockKernel();
        var ranker = new LMRanker(kernel);
        var documents = CreateAsyncEnumerable(new[] { "Document 1", "Document 2" });

        // Act
        var results = new List<(string, double)>();
        await foreach (var result in ranker.RankAsync("", documents, topN: 5))
        {
            results.Add(result);
        }

        // Assert
        results.Should().HaveCount(2); // Should return all 2 documents, not 5
    }

    [Fact]
    public async Task RankAsync_VectorSearchResults_WithEmptyQuery_ReturnsZeroScores()
    {
        // Arrange
        var kernel = CreateMockKernel();
        var ranker = new LMRanker(kernel);
        var searchResults = CreateAsyncEnumerable(new[]
        {
            new VectorSearchResult<TestDocument>(new TestDocument { Content = "Low relevance document" }, 0.5),
            new VectorSearchResult<TestDocument>(new TestDocument { Content = "High relevance document" }, 0.8),
            new VectorSearchResult<TestDocument>(new TestDocument { Content = "Medium relevance document" }, 0.6)
        });

        // Act
        var results = new List<(VectorSearchResult<TestDocument>, double)>();
        await foreach (var result in ranker.RankAsync("", searchResults, doc => doc.Content, topN: 2))
        {
            results.Add(result);
        }

        // Assert
        results.Should().HaveCount(2);
        results.Should().AllSatisfy(r => r.Item2.Should().Be(0.0));
    }

    [Fact]
    public void LMRanker_ImplementsIRankerInterface()
    {
        // Arrange
        var kernel = CreateMockKernel();

        // Act
        var ranker = new LMRanker(kernel);

        // Assert
        ranker.Should().BeAssignableTo<IRanker>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t\n")]
    public async Task ScoreAsync_WithInvalidQueries_ReturnsZeroScores(string query)
    {
        // Arrange
        var kernel = CreateMockKernel();
        var ranker = new LMRanker(kernel);
        var documents = CreateAsyncEnumerable(new[] { "Test document" });

        // Act
        var results = new List<(string, double)>();
        await foreach (var result in ranker.ScoreAsync(query, documents))
        {
            results.Add(result);
        }

        // Assert
        results.Should().HaveCount(1);
        results[0].Item2.Should().Be(0.0);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(5)]
    [InlineData(10)]
    public async Task RankAsync_WithDifferentTopNValues_RespectsTopNLimit(int topN)
    {
        // Arrange
        var kernel = CreateMockKernel();
        var ranker = new LMRanker(kernel);
        var documents = CreateAsyncEnumerable(Enumerable.Range(1, 10).Select(i => $"Document {i}").ToArray());

        // Act
        var results = new List<(string, double)>();
        await foreach (var result in ranker.RankAsync("", documents, topN: topN))
        {
            results.Add(result);
        }

        // Assert
        var expectedCount = Math.Min(topN, 10);
        results.Should().HaveCount(expectedCount);
    }

    [Fact]
    public async Task RankAsync_WithDefaultTopNParameter_UsesDefaultValue()
    {
        // Arrange
        var kernel = CreateMockKernel();
        var ranker = new LMRanker(kernel);
        var documents = CreateAsyncEnumerable(Enumerable.Range(1, 10).Select(i => $"Document {i}").ToArray());

        // Act
        var results = new List<(string, double)>();
        await foreach (var result in ranker.RankAsync("", documents)) // No topN specified, should use default of 5
        {
            results.Add(result);
        }

        // Assert
        results.Should().HaveCount(5); // Default topN should be 5
    }

    [Fact]
    public async Task ScoreAsync_PreservesDocumentOrder()
    {
        // Arrange
        var kernel = CreateMockKernel();
        var ranker = new LMRanker(kernel);
        var expectedDocuments = new[] { "First document", "Second document", "Third document" };
        var documents = CreateAsyncEnumerable(expectedDocuments);

        // Act
        var results = new List<(string, double)>();
        await foreach (var result in ranker.ScoreAsync("", documents))
        {
            results.Add(result);
        }

        // Assert
        results.Should().HaveCount(3);
        for (int i = 0; i < expectedDocuments.Length; i++)
        {
            results[i].Item1.Should().Be(expectedDocuments[i]);
        }
    }

    [Fact]
    public async Task ScoreAsync_VectorSearchResults_PreservesResultOrder()
    {
        // Arrange
        var kernel = CreateMockKernel();
        var ranker = new LMRanker(kernel);
        var testDocs = new[]
        {
            new TestDocument { Content = "First document" },
            new TestDocument { Content = "Second document" },
            new TestDocument { Content = "Third document" }
        };
        var searchResults = CreateAsyncEnumerable(testDocs.Select((doc, i) => 
            new VectorSearchResult<TestDocument>(doc, 1.0 - i * 0.1)).ToArray());

        // Act
        var results = new List<(VectorSearchResult<TestDocument>, double)>();
        await foreach (var result in ranker.ScoreAsync("", searchResults, doc => doc.Content))
        {
            results.Add(result);
        }

        // Assert
        results.Should().HaveCount(3);
        for (int i = 0; i < testDocs.Length; i++)
        {
            results[i].Item1.Record.Content.Should().Be(testDocs[i].Content);
        }
    }

    /// <summary>
    /// Helper method to create async enumerable from array
    /// </summary>
    private static async IAsyncEnumerable<T> CreateAsyncEnumerable<T>(T[] items)
    {
        foreach (var item in items)
        {
            yield return item;
            await Task.Yield();
        }
    }

    /// <summary>
    /// Creates a basic kernel for testing
    /// </summary>
    private static Kernel CreateMockKernel()
    {
        var kernelBuilder = Kernel.CreateBuilder();
        return kernelBuilder.Build();
    }

    /// <summary>
    /// Test document class for vector search tests
    /// </summary>
    public class TestDocument
    {
        public string Content { get; set; } = string.Empty;
    }
}
