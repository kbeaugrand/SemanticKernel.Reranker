using SemanticKernel.Rankers.BM25;
using System.Diagnostics;

namespace BM25Sample
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var docs = new List<string>
                {
                    "The quick brown fox jumps over the lazy dog.",
                    "The fox",
                    "The dog",
                    "A brown dog jumps over another dog.",
                    "The quick brown fox.",
                    "Machine learning is a subset of artificial intelligence.",
                    "Natural language processing helps computers understand human language.",
                    "The algorithm processes documents efficiently.",
                    "Information retrieval systems rank documents by relevance.",
                    "Text mining extracts useful information from unstructured data."
                };

            Console.WriteLine("=== Performance Optimized BM25 Reranker Demo ===");
            Console.WriteLine();

            // Demo 1: Basic usage with optimized streaming
            await DemoBasicUsage(docs);
            
            // Demo 2: Pre-computed corpus statistics for better performance
            await DemoWithCorpusStatistics(docs);
            
            // Demo 3: Memory-efficient top-N ranking
            await DemoTopNRanking(docs);
            
            // Demo 4: Cache performance
            await DemoCachePerformance(docs);
        }

        static async Task DemoBasicUsage(List<string> docs)
        {
            Console.WriteLine("1. Basic Usage (Optimized Streaming)");
            Console.WriteLine(new string('-', 50));
            
            var bm25 = new BM25Reranker();
            var stopwatch = Stopwatch.StartNew();

            Console.WriteLine("Scoring documents for query: 'quick brown fox'");
            
            await foreach (var (document, score) in bm25.ScoreAsync("quick brown fox", docs.ToAsyncEnumerable()))
            {
                Console.WriteLine($"Score: {score:F4} | Document: \"{document}\"");
            }

            stopwatch.Stop();
            Console.WriteLine($"Elapsed time: {stopwatch.ElapsedMilliseconds} ms");
            Console.WriteLine();
        }

        static async Task DemoWithCorpusStatistics(List<string> docs)
        {
            Console.WriteLine("2. Pre-computed Corpus Statistics (Fastest for Multiple Queries)");
            Console.WriteLine(new string('-', 50));
            
            var stopwatch = Stopwatch.StartNew();

            var ranker = new BM25Reranker();

            // Pre-compute corpus statistics once
            var corpusStats = await ranker.ComputeCorpusStatisticsAsync(docs.ToAsyncEnumerable());
            var bm25WithStats = new BM25Reranker(corpusStats);
            
            Console.WriteLine("Scoring documents for query: 'machine learning'");
            
            await foreach (var (document, score) in bm25WithStats.ScoreAsync("machine learning", docs.ToAsyncEnumerable()))
            {
                Console.WriteLine($"Score: {score:F4} | Document: \"{document}\"");
            }

            stopwatch.Stop();
            Console.WriteLine($"Elapsed time (including corpus stats): {stopwatch.ElapsedMilliseconds} ms");
            Console.WriteLine();
        }

        static async Task DemoTopNRanking(List<string> docs)
        {
            Console.WriteLine("3. Memory-Efficient Top-N Ranking");
            Console.WriteLine(new string('-', 50));
            
            var bm25 = new BM25Reranker();
            var stopwatch = Stopwatch.StartNew();

            Console.WriteLine("Top 3 documents for query: 'information processing'");
            
            await foreach (var (document, score) in bm25.RankAsync("information processing", docs.ToAsyncEnumerable(), topN: 3))
            {
                Console.WriteLine($"Score: {score:F4} | Document: \"{document}\"");
            }

            stopwatch.Stop();
            Console.WriteLine($"Elapsed time: {stopwatch.ElapsedMilliseconds} ms");
            Console.WriteLine();
        }

        static async Task DemoCachePerformance(List<string> docs)
        {
            Console.WriteLine("4. Cache Performance Demo");
            Console.WriteLine(new string('-', 50));
            
            var bm25 = new BM25Reranker();
            
            // First run - cache miss
            var stopwatch = Stopwatch.StartNew();
            var firstRunResults = new List<(string, double)>();
            await foreach (var result in bm25.ScoreAsync("quick brown", docs.ToAsyncEnumerable()))
            {
                firstRunResults.Add(result);
            }
            stopwatch.Stop();
            var firstRunTime = stopwatch.ElapsedMilliseconds;
            
            // Second run - cache hit
            stopwatch.Restart();
            var secondRunResults = new List<(string, double)>();
            await foreach (var result in bm25.ScoreAsync("quick brown", docs.ToAsyncEnumerable()))
            {
                secondRunResults.Add(result);
            }
            stopwatch.Stop();
            var secondRunTime = stopwatch.ElapsedMilliseconds;
            
            Console.WriteLine($"First run (cache miss): {firstRunTime} ms");
            Console.WriteLine($"Second run (cache hit): {secondRunTime} ms");
            Console.WriteLine($"Performance improvement: {(double)firstRunTime / Math.Max(secondRunTime, 1):F1}x faster");
            
            // Clear cache for cleanup
            BM25Reranker.ClearCache();
            Console.WriteLine("Cache cleared.");
        }
    }

    // Extension method to convert List to IAsyncEnumerable for demo purposes
    public static class ListExtensions
    {
        public static async IAsyncEnumerable<T> ToAsyncEnumerable<T>(this IEnumerable<T> source)
        {
            foreach (var item in source)
            {
                yield return item;
                await Task.Yield(); // Simulate async behavior
            }
        }
    }
}
