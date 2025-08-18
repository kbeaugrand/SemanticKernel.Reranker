using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using SemanticKernel.Reranker.BM25;

namespace BM25Sample
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            // Create a kernel builder
            var builder = Kernel.CreateBuilder();

            // Add the OpenAI embedding generator
#pragma warning disable SKEXP0010 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
            _ = builder.Services.AddAzureOpenAIEmbeddingGenerator(
                endpoint: "https://xxx.openai.azure.com/",
                deploymentName: "text-embedding-ada-002",
                apiKey: "xxx");
#pragma warning restore SKEXP0010 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
            
            var kernel = builder.Build();

            var docs = new List<string>
                {
                    "The quick brown fox jumps over the lazy dog.",
                    "The fox",
                    "The dog",
                    "A brown dog jumps over another dog.",
                    "The quick brown fox."
                };

            // Use default k1 and b
            var bm25 = new Bm25SimilarityReranker(kernel.GetRequiredService<IEmbeddingGenerator<string, Embedding<float>>>(), docs);

            var results = await bm25.RankAsync("quick brown fox", topN: 3);

            foreach (var result in results)
                Console.WriteLine($"Doc #{result.Item1}: Score = {result.Item2}");
        }
    }
}
