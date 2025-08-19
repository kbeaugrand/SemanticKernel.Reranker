using SemanticKernel.Reranker.BM25;

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

            var results = await bm25.RankAsync("quick brown fox", topN: 3);

            foreach (var result in results)
                Console.WriteLine($"Doc #{result.Item1}: Score = {result.Item2}");
        }
    }
}
