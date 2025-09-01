using FluentAssertions;
using Microsoft.SemanticKernel;
using SemanticKernel.Rankers.LMRanker;
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
}
