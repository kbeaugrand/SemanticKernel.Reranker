using SemanticKernel.Reranker.BM25;
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
                    "The quick brown fox."
                };

            // Use default k1 and b
            var bm25 = new BM25Reranker(docs);

            _ = await bm25.RankAsync("quick brown fox", topN: 3);

            var stopwatch = Stopwatch.StartNew();

            IEnumerable<(int Index, double Score)> ranked = await bm25.RankAsync("quick brown fox", topN: 3);

            stopwatch.Stop();

            foreach (var result in ranked)
                Console.WriteLine($"Doc #{result.Index}: Score = {result.Score}");

            Console.WriteLine($"Elapsed time: {stopwatch.ElapsedMilliseconds} ms");
        }
    }
}
