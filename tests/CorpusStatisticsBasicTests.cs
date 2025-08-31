using FluentAssertions;

namespace SemanticKernel.Reranker.BM25.Tests;

public class CorpusStatisticsBasicTests
{
    [Fact]
    public async Task ComputeCorpusStatisticsAsync_WithValidDocuments_ReturnsCorrectStatistics()
    {
        // Arrange
        var documents = CreateAsyncEnumerable(new[]
        {
            "The quick brown fox jumps over the lazy dog",
            "A quick brown dog runs fast",
            "The lazy cat sleeps all day"
        });

        var ranker = new BM25Reranker();

        // Act
        var stats = await ranker.ComputeCorpusStatisticsAsync(documents);

        // Assert
        stats.TotalDocuments.Should().Be(3);
        stats.AverageDocumentLength.Should().BeGreaterThan(0);
        stats.DocumentFrequencies.Should().NotBeEmpty();
    }

    [Fact]
    public void CorpusStatistics_Properties_AreInitializedCorrectly()
    {
        // Arrange & Act
        var stats = new CorpusStatistics
        {
            DocumentFrequencies = new Dictionary<string, int> { ["test"] = 5 },
            TotalDocuments = 10,
            AverageDocumentLength = 25.5
        };

        // Assert
        stats.DocumentFrequencies.Should().ContainKey("test");
        stats.DocumentFrequencies["test"].Should().Be(5);
        stats.TotalDocuments.Should().Be(10);
        stats.AverageDocumentLength.Should().Be(25.5);
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
