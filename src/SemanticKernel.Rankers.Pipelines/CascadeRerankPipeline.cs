using Microsoft.Extensions.VectorData;
using SemanticKernel.Rankers.Abstractions;
using System.Diagnostics;
using System.Linq.Expressions;

namespace SemanticKernel.Rankers.Pipelines
{
    public class CascadeRerankPipelineConfig
    {
        public int TopK { get; set; } = 20;
        public int TopM { get; set; } = 5;
        public double ScoreThreshold { get; set; } = 0.0;
    }

    /// <summary>
    /// A pipeline that implements IRanker and chains multiple rankers in cascade.
    /// Each stage filters the top-K results for the next stage until the final top-M results.
    /// </summary>
    public class CascadeRerankPipeline : IRanker
    {
        private readonly IList<IRanker> _rankers;
        private readonly CascadeRerankPipelineConfig _config;

        public CascadeRerankPipeline(IList<IRanker> rankers, CascadeRerankPipelineConfig config)
        {
            _rankers = rankers ?? throw new ArgumentNullException(nameof(rankers));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            
            if (!rankers.Any())
            {
                throw new ArgumentException("At least one ranker must be provided", nameof(rankers));
            }
        }

        /// <summary>
        /// Rank documents returning the top N results.
        /// </summary>
        public async IAsyncEnumerable<(string DocumentText, double Score)> RankAsync(string query, IAsyncEnumerable<string> documents, int topN = 5)
        {
            var allScores = new List<(string DocumentText, double Score)>();
            
            await foreach (var item in ScoreAsync(query, documents))
            {
                allScores.Add(item);
            }

            foreach (var item in allScores.OrderByDescending(x => x.Score).Take(topN))
            {
                yield return item;
            }
        }

        /// <summary>
        /// Rank VectorSearchResult documents returning the top N results.
        /// </summary>
        public async IAsyncEnumerable<(VectorSearchResult<T> Result, double Score)> RankAsync<T>(string query, IAsyncEnumerable<VectorSearchResult<T>> documents, Expression<Func<T, string>> textProperty, int topN = 5)
        {
            var allScores = new List<(VectorSearchResult<T> Result, double Score)>();
            
            await foreach (var item in ScoreAsync(query, documents, textProperty))
            {
                allScores.Add(item);
            }

            foreach (var item in allScores.OrderByDescending(x => x.Score).Take(topN))
            {
                yield return item;
            }
        }

        /// <summary>
        /// Score all documents using the cascade pipeline.
        /// </summary>
        public async IAsyncEnumerable<(string DocumentText, double Score)> ScoreAsync(string query, IAsyncEnumerable<string> documents)
        {
            var currentDocs = new List<string>();
            await foreach (var doc in documents)
            {
                currentDocs.Add(doc);
            }

            var scores = new Dictionary<string, double>();
            var currentAsyncDocs = ToAsyncEnumerable(currentDocs);

            for (int i = 0; i < _rankers.Count; i++)
            {
                var ranker = _rankers[i];
                var rankedResults = new List<(string DocumentText, double Score)>();

                // Get scores from current ranker
                await foreach (var (docText, score) in ranker.ScoreAsync(query, currentAsyncDocs))
                {
                    if (score >= _config.ScoreThreshold)
                    {
                        rankedResults.Add((docText, score));
                        scores[docText] = score; // Store latest score
                    }
                }

                // Filter for next stage (except for the last ranker)
                if (i < _rankers.Count - 1)
                {
                    int limit = _config.TopK;
                    currentDocs = rankedResults
                        .OrderByDescending(x => x.Score)
                        .Take(limit)
                        .Select(x => x.DocumentText)
                        .ToList();
                    currentAsyncDocs = ToAsyncEnumerable(currentDocs);
                }
                else
                {
                    // Last stage - apply final filtering
                    var finalResults = rankedResults
                        .OrderByDescending(x => x.Score)
                        .Take(_config.TopM);

                    foreach (var result in finalResults)
                    {
                        yield return result;
                    }
                    yield break;
                }
            }
        }

        /// <summary>
        /// Score VectorSearchResult documents using the cascade pipeline.
        /// </summary>
        public async IAsyncEnumerable<(VectorSearchResult<T> Result, double Score)> ScoreAsync<T>(string query, IAsyncEnumerable<VectorSearchResult<T>> searchResults, Expression<Func<T, string>> textProperty)
        {
            var currentResults = new List<VectorSearchResult<T>>();
            await foreach (var result in searchResults)
            {
                currentResults.Add(result);
            }

            var scores = new Dictionary<VectorSearchResult<T>, double>();
            var currentAsyncResults = ToAsyncEnumerable(currentResults);

            for (int i = 0; i < _rankers.Count; i++)
            {
                var ranker = _rankers[i];
                var rankedResults = new List<(VectorSearchResult<T> Result, double Score)>();

                // Get scores from current ranker
                await foreach (var (result, score) in ranker.ScoreAsync(query, currentAsyncResults, textProperty))
                {
                    if (score >= _config.ScoreThreshold)
                    {
                        rankedResults.Add((result, score));
                        scores[result] = score; // Store latest score
                    }
                }

                // Filter for next stage (except for the last ranker)
                if (i < _rankers.Count - 1)
                {
                    int limit = _config.TopK;
                    currentResults = rankedResults
                        .OrderByDescending(x => x.Score)
                        .Take(limit)
                        .Select(x => x.Result)
                        .ToList();
                    currentAsyncResults = ToAsyncEnumerable(currentResults);
                }
                else
                {
                    // Last stage - apply final filtering
                    var finalResults = rankedResults
                        .OrderByDescending(x => x.Score)
                        .Take(_config.TopM);

                    foreach (var result in finalResults)
                    {
                        yield return result;
                    }
                    yield break;
                }
            }
        }

        /// <summary>
        /// Legacy method for backward compatibility. Use RankAsync or ScoreAsync instead.
        /// </summary>
        public async Task<CascadeRerankResult> RankAsync(string query, IEnumerable<string> corpus)
        {
            var timings = new List<TimeSpan>();
            var scores = new Dictionary<string, List<double>>();
            var finalScores = new Dictionary<string, double>();

            var sw = Stopwatch.StartNew();
            await foreach (var (docText, score) in ScoreAsync(query, ToAsyncEnumerable(corpus)))
            {
                finalScores[docText] = score;
            }
            sw.Stop();
            timings.Add(sw.Elapsed);

            // Convert to legacy format
            foreach (var kv in finalScores)
            {
                scores[kv.Key] = new List<double> { kv.Value };
            }

            return new CascadeRerankResult
            {
                Results = finalScores.OrderByDescending(x => x.Value).Select(x => x.Key).ToList(),
                Timings = timings,
                Scores = scores
            };
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

    public class CascadeRerankResult
    {
        public List<string> Results { get; set; } = new();
        public List<TimeSpan> Timings { get; set; } = new();
        public Dictionary<string, List<double>> Scores { get; set; } = new();
    }
}
