using Microsoft.SemanticKernel;
using SemanticKernel.Rankers.BM25;
using SemanticKernel.Rankers.LMRanker;
using Mosaik.Core;
using Xunit;

namespace SemanticKernel.Rankers.Pipelines.Tests
{
    public class BM25ThenLMRankerPipelineTests
    {
        [Fact]
        public void Constructor_WithValidParameters_CreatesInstance()
        {
            // Arrange
            var bm25 = new BM25Reranker(supportedLanguages: [Language.English]);
            var kernel = CreateMockKernel();
            var lmRanker = new LMRanker.LMRanker(kernel);
            var config = new BM25ThenLMRankerPipelineConfig
            {
                TopK = 10,
                TopM = 3,
                ScoreThreshold = 0.5
            };

            // Act
            var pipeline = new BM25ThenLMRankerPipeline(bm25, lmRanker, config);

            // Assert
            Assert.NotNull(pipeline);
        }

        [Fact]
        public void Constructor_NullBM25Ranker_ThrowsArgumentNullException()
        {
            // Arrange
            var kernel = CreateMockKernel();
            var lmRanker = new LMRanker.LMRanker(kernel);
            var config = new BM25ThenLMRankerPipelineConfig();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new BM25ThenLMRankerPipeline(null!, lmRanker, config));
        }

        [Fact]
        public void Constructor_NullLMRanker_ThrowsArgumentNullException()
        {
            // Arrange
            var bm25 = new BM25Reranker(supportedLanguages: [Language.English]);
            var config = new BM25ThenLMRankerPipelineConfig();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new BM25ThenLMRankerPipeline(bm25, null!, config));
        }

        [Fact]
        public void Constructor_NullConfig_ThrowsArgumentNullException()
        {
            // Arrange
            var bm25 = new BM25Reranker(supportedLanguages: [Language.English]);
            var kernel = CreateMockKernel();
            var lmRanker = new LMRanker.LMRanker(kernel);

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new BM25ThenLMRankerPipeline(bm25, lmRanker, null!));
        }

        [Fact]
        public async Task ScoreAsync_WithValidInputs_ReturnsResults()
        {
            // Arrange
            var bm25 = new BM25Reranker(supportedLanguages: [Language.English]);
            var kernel = CreateMockKernel();
            var lmRanker = new LMRanker.LMRanker(kernel);
            var config = new BM25ThenLMRankerPipelineConfig
            {
                TopK = 5,
                TopM = 3,
                ScoreThreshold = 0.0
            };

            var pipeline = new BM25ThenLMRankerPipeline(bm25, lmRanker, config);
            var query = "machine learning algorithms";
            var documents = new[]
            {
                "Machine learning is a subset of artificial intelligence",
                "Algorithms are step-by-step procedures for calculations",
                "Deep learning uses neural networks with multiple layers",
                "Natural language processing helps computers understand text"
            };

            // Act
            var results = new List<(string DocumentText, double Score)>();
            await foreach (var item in pipeline.ScoreAsync(query, ToAsyncEnumerable(documents)))
            {
                results.Add(item);
            }

            // Assert
            Assert.NotEmpty(results);
            Assert.True(results.Count <= config.TopM);
            Assert.True(results.All(r => r.Score >= config.ScoreThreshold));
        }

        [Fact]
        public async Task RankAsync_WithValidInputs_ReturnsTopNResults()
        {
            // Arrange
            var bm25 = new BM25Reranker(supportedLanguages: [Language.English]);
            var kernel = CreateMockKernel();
            var lmRanker = new LMRanker.LMRanker(kernel);
            var config = new BM25ThenLMRankerPipelineConfig
            {
                TopK = 4,
                TopM = 6,
                ScoreThreshold = 0.0
            };

            var pipeline = new BM25ThenLMRankerPipeline(bm25, lmRanker, config);
            var query = "data science and analytics";
            var documents = new[]
            {
                "Data science combines statistics and programming",
                "Analytics helps make informed business decisions",
                "Big data requires scalable processing solutions",
                "Visualization makes complex data understandable",
                "Statistical modeling predicts future outcomes",
                "Machine learning automates analytical model building"
            };
            var topN = 3;

            // Act
            var results = new List<(string DocumentText, double Score)>();
            await foreach (var item in pipeline.RankAsync(query, ToAsyncEnumerable(documents), topN))
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
        public async Task ScoreAsync_EmptyDocuments_ReturnsEmptyResults()
        {
            // Arrange
            var bm25 = new BM25Reranker(supportedLanguages: [Language.English]);
            var kernel = CreateMockKernel();
            var lmRanker = new LMRanker.LMRanker(kernel);
            var config = new BM25ThenLMRankerPipelineConfig();

            var pipeline = new BM25ThenLMRankerPipeline(bm25, lmRanker, config);
            var query = "test query";
            var documents = Array.Empty<string>();

            // Act
            var results = new List<(string DocumentText, double Score)>();
            await foreach (var item in pipeline.ScoreAsync(query, ToAsyncEnumerable(documents)))
            {
                results.Add(item);
            }

            // Assert
            Assert.Empty(results);
        }

        [Fact]
        public async Task ScoreAsync_EmptyQuery_ProcessesDocuments()
        {
            // Arrange
            var bm25 = new BM25Reranker(supportedLanguages: [Language.English]);
            var kernel = CreateMockKernel();
            var lmRanker = new LMRanker.LMRanker(kernel);
            var config = new BM25ThenLMRankerPipelineConfig
            {
                TopK = 5,
                TopM = 3,
                ScoreThreshold = 0.0
            };

            var pipeline = new BM25ThenLMRankerPipeline(bm25, lmRanker, config);
            var query = "";
            var documents = new[] { "Document 1", "Document 2", "Document 3" };

            // Act
            var results = new List<(string DocumentText, double Score)>();
            await foreach (var item in pipeline.ScoreAsync(query, ToAsyncEnumerable(documents)))
            {
                results.Add(item);
            }

            // Assert - Should still process documents even with empty query
            Assert.NotEmpty(results);
        }

        [Fact]
        public async Task Pipeline_AppliesConfigurationCorrectly()
        {
            // Arrange
            var bm25 = new BM25Reranker(supportedLanguages: [Language.English]);
            var kernel = CreateMockKernel();
            var lmRanker = new LMRanker.LMRanker(kernel);
            var config = new BM25ThenLMRankerPipelineConfig
            {
                TopK = 2, // BM25 will filter to top 2
                TopM = 1, // Final result will have at most 1 document
                ScoreThreshold = 0.0
            };

            var pipeline = new BM25ThenLMRankerPipeline(bm25, lmRanker, config);
            var query = "artificial intelligence and machine learning";
            var documents = new[]
            {
                "Artificial intelligence systems can perform complex tasks",
                "Machine learning algorithms learn from data patterns",
                "Deep learning is a subset of machine learning techniques",
                "Neural networks are inspired by biological brain structure",
                "Computer vision enables machines to interpret visual information"
            };

            // Act
            var results = new List<(string DocumentText, double Score)>();
            await foreach (var item in pipeline.ScoreAsync(query, ToAsyncEnumerable(documents)))
            {
                results.Add(item);
            }

            // Assert
            Assert.NotEmpty(results);
            // Due to cascade pipeline configuration, we should get at most TopM results
            Assert.True(results.Count <= config.TopM);
            Assert.True(results.All(r => r.Score >= config.ScoreThreshold));
        }

        [Fact]
        public void BM25ThenLMRankerPipelineConfig_DefaultValues_AreCorrect()
        {
            // Act
            var config = new BM25ThenLMRankerPipelineConfig();

            // Assert
            Assert.Equal(20, config.TopK);
            Assert.Equal(5, config.TopM);
            Assert.Equal(0.0, config.ScoreThreshold);
        }

        [Fact]
        public void BM25ThenLMRankerPipelineConfig_CustomValues_AreSetCorrectly()
        {
            // Arrange & Act
            var config = new BM25ThenLMRankerPipelineConfig
            {
                TopK = 15,
                TopM = 8,
                ScoreThreshold = 0.3
            };

            // Assert
            Assert.Equal(15, config.TopK);
            Assert.Equal(8, config.TopM);
            Assert.Equal(0.3, config.ScoreThreshold);
        }

        /// <summary>
        /// Creates a basic kernel for testing
        /// </summary>
        private static Kernel CreateMockKernel()
        {
            var kernelBuilder = Kernel.CreateBuilder();
            return kernelBuilder.Build();
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
