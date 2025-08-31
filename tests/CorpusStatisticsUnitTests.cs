using FluentAssertions;
using SemanticKernel.Reranker.BM25;
using Xunit;

namespace SemanticKernel.Reranker.BM25.Tests;

public class CorpusStatisticsUnitTests
{
    [Fact]
    public void CorpusStatistics_Properties_AreInitializedCorrectly()
    {
        // Arrange & Act
        var stats = new CorpusStatistics
        {
            DocumentFrequencies = new Dictionary<string, int> 
            { 
                ["test"] = 5,
                ["sample"] = 3,
                ["document"] = 2
            },
            TotalDocuments = 10,
            AverageDocumentLength = 25.5
        };

        // Assert
        stats.DocumentFrequencies.Should().ContainKey("test");
        stats.DocumentFrequencies["test"].Should().Be(5);
        stats.DocumentFrequencies["sample"].Should().Be(3);
        stats.DocumentFrequencies["document"].Should().Be(2);
        stats.TotalDocuments.Should().Be(10);
        stats.AverageDocumentLength.Should().Be(25.5);
    }

    [Fact]
    public void CorpusStatistics_DefaultInitialization_HasEmptyProperties()
    {
        // Arrange & Act
        var stats = new CorpusStatistics();

        // Assert
        stats.DocumentFrequencies.Should().NotBeNull();
        stats.DocumentFrequencies.Should().BeEmpty();
        stats.TotalDocuments.Should().Be(0);
        stats.AverageDocumentLength.Should().Be(0);
    }

    [Fact]
    public void BM25Reranker_Constructor_WithNullStats_DoesNotThrow()
    {
        // Arrange & Act
        var action = () => new BM25Reranker(null);

        // Assert
        action.Should().NotThrow();
    }

    [Fact]
    public void BM25Reranker_Constructor_WithValidStats_DoesNotThrow()
    {
        // Arrange
        var stats = new CorpusStatistics
        {
            DocumentFrequencies = new Dictionary<string, int> { ["test"] = 1 },
            TotalDocuments = 1,
            AverageDocumentLength = 5.0
        };

        // Act
        var action = () => new BM25Reranker(stats);

        // Assert
        action.Should().NotThrow();
    }

    [Fact]
    public void BM25Reranker_ClearCache_DoesNotThrow()
    {
        // Arrange & Act
        var action = () => BM25Reranker.ClearCache();

        // Assert
        action.Should().NotThrow();
    }
}
