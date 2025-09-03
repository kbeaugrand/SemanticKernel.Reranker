using Microsoft.Extensions.VectorData;
using SemanticKernel.Rankers.Abstractions;
using Xunit;

namespace SemanticKernel.Rankers.Pipelines.Tests
{
    /// <summary>
    /// Simple mock ranker for testing purposes
    /// </summary>
    public class MockRanker : IRanker
    {
        private readonly string _name;
        private readonly double _baseScore;

        public MockRanker(string name, double baseScore = 0.5)
        {
            _name = name;
            _baseScore = baseScore;
        }

        public async IAsyncEnumerable<(string DocumentText, double Score)> RankAsync(string query, IAsyncEnumerable<string> documents, int topN = 5)
        {
            var allResults = new List<(string DocumentText, double Score)>();
            await foreach (var item in ScoreAsync(query, documents))
            {
                allResults.Add(item);
            }

            foreach (var item in allResults.OrderByDescending(x => x.Score).Take(topN))
            {
                yield return item;
            }
        }

        public async IAsyncEnumerable<(VectorSearchResult<T> Result, double Score)> RankAsync<T>(string query, IAsyncEnumerable<VectorSearchResult<T>> documents, System.Linq.Expressions.Expression<Func<T, string>> textProperty, int topN = 5)
        {
            var allResults = new List<(VectorSearchResult<T> Result, double Score)>();
            await foreach (var item in ScoreAsync(query, documents, textProperty))
            {
                allResults.Add(item);
            }

            foreach (var item in allResults.OrderByDescending(x => x.Score).Take(topN))
            {
                yield return item;
            }
        }

        public async IAsyncEnumerable<(string DocumentText, double Score)> ScoreAsync(string query, IAsyncEnumerable<string> documents)
        {
            await foreach (var doc in documents)
            {
                // Simple scoring: base score + length bonus
                var score = _baseScore + (doc.Length / 100.0);
                yield return (doc, score);
            }
        }

        public async IAsyncEnumerable<(VectorSearchResult<T> Result, double Score)> ScoreAsync<T>(string query, IAsyncEnumerable<VectorSearchResult<T>> searchResults, System.Linq.Expressions.Expression<Func<T, string>> textProperty)
        {
            var textFunc = textProperty.Compile();
            await foreach (var result in searchResults)
            {
                var text = textFunc(result.Record);
                var score = _baseScore + (text.Length / 100.0);
                yield return (result, score);
            }
        }
    }

    public class CascadeRerankPipelineTests
    {
        [Fact]
        public async Task CascadeRerankPipeline_ScoreAsync_ReturnsFilteredResults()
        {
            // Arrange
            var ranker1 = new MockRanker("Ranker1", 0.3);
            var ranker2 = new MockRanker("Ranker2", 0.7);
            var rankers = new List<IRanker> { ranker1, ranker2 };
            
            var config = new CascadeRerankPipelineConfig
            {
                TopK = 3,
                TopM = 2,
                ScoreThreshold = 0.0
            };

            var pipeline = new CascadeRerankPipeline(rankers, config);
            var documents = new[] { "Short", "Medium length document", "This is a much longer document with more content", "Another doc" };

            // Act
            var results = new List<(string DocumentText, double Score)>();
            await foreach (var item in pipeline.ScoreAsync("test query", ToAsyncEnumerable(documents)))
            {
                results.Add(item);
            }

            // Assert
            Assert.NotEmpty(results);
            Assert.True(results.Count <= config.TopM);
            Assert.True(results.All(r => r.Score >= config.ScoreThreshold));
        }

        [Fact]
        public async Task CascadeRerankPipeline_RankAsync_ReturnsTopNResults()
        {
            // Arrange
            var ranker1 = new MockRanker("Ranker1", 0.3);
            var ranker2 = new MockRanker("Ranker2", 0.7);
            var rankers = new List<IRanker> { ranker1, ranker2 };
            
            var config = new CascadeRerankPipelineConfig
            {
                TopK = 3,
                TopM = 5,
                ScoreThreshold = 0.0
            };

            var pipeline = new CascadeRerankPipeline(rankers, config);
            var documents = new[] { "Short", "Medium length document", "This is a much longer document with more content", "Another doc" };
            var topN = 2;

            // Act
            var results = new List<(string DocumentText, double Score)>();
            await foreach (var item in pipeline.RankAsync("test query", ToAsyncEnumerable(documents), topN))
            {
                results.Add(item);
            }

            // Assert
            Assert.NotEmpty(results);
            Assert.True(results.Count <= topN);
            
            // Verify results are sorted by score descending
            for (int i = 1; i < results.Count; i++)
            {
                Assert.True(results[i-1].Score >= results[i].Score);
            }
        }

        [Fact]
        public void CascadeRerankPipeline_EmptyRankers_ThrowsArgumentException()
        {
            // Arrange & Act & Assert
            var config = new CascadeRerankPipelineConfig();
            Assert.Throws<ArgumentException>(() => new CascadeRerankPipeline(new List<IRanker>(), config));
        }

        [Fact]
        public void CascadeRerankPipeline_NullRankers_ThrowsArgumentNullException()
        {
            // Arrange & Act & Assert
            var config = new CascadeRerankPipelineConfig();
            Assert.Throws<ArgumentNullException>(() => new CascadeRerankPipeline(null!, config));
        }

        [Fact]
        public void CascadeRerankPipeline_NullConfig_ThrowsArgumentNullException()
        {
            // Arrange & Act & Assert
            var rankers = new List<IRanker> { new MockRanker("Test") };
            Assert.Throws<ArgumentNullException>(() => new CascadeRerankPipeline(rankers, null!));
        }

        private static async IAsyncEnumerable<T> ToAsyncEnumerable<T>(IEnumerable<T> enumerable)
        {
            foreach (var item in enumerable)
            {
                yield return item;
            }
            await Task.CompletedTask;
        }
    }
}
