using Microsoft.SemanticKernel;
using SemanticKernel.Rankers.LMRanker;
using System.Diagnostics;

namespace LMRankerSample
{
    /// <summary>
    /// Comprehensive sample demonstrating the Language Model Reranker capabilities.
    /// This sample shows how to configure and use LMRanker with different AI services.
    /// </summary>
    internal class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("=== Language Model Reranker Sample ===");
            Console.WriteLine();

            // Sample documents covering various topics
            var documents = GetSampleDocuments();

            try
            {
                // Create and configure Semantic Kernel
                var kernel = CreateKernel();
                
                if (kernel == null)
                {
                    Console.WriteLine("⚠️  No AI service configured. Please configure an AI service to run the sample.");
                    Console.WriteLine("   See the ConfigureAIService() method for configuration options.");
                    return;
                }

                // Create LMRanker instance
                var lmRanker = new LMRanker(kernel);

                Console.WriteLine("🚀 Running LMRanker demonstrations...");
                Console.WriteLine();

                // Demo 1: Basic document scoring
                await DemoBasicScoring(lmRanker, documents);

                // Demo 2: Document ranking with different queries
                await DemoRanking(lmRanker, documents);

                // Demo 3: Performance comparison
                await DemoPerformanceComparison(lmRanker, documents);

                // Demo 4: Multiple query comparison
                await DemoMultipleQueries(lmRanker, documents);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error: {ex.Message}");
                Console.WriteLine();
                Console.WriteLine("💡 Make sure to:");
                Console.WriteLine("   1. Configure a valid AI service in ConfigureAIService()");
                Console.WriteLine("   2. Provide valid API keys or endpoints");
                Console.WriteLine("   3. Ensure network connectivity to the AI service");
            }
        }

        /// <summary>
        /// Creates and configures a Semantic Kernel instance with an AI service.
        /// Modify this method to configure your preferred AI service.
        /// </summary>
        private static Kernel? CreateKernel()
        {
            var builder = Kernel.CreateBuilder();

            // Try to configure an AI service
            var configured = ConfigureAIService(builder);
            
            if (!configured)
            {
                return null;
            }

            return builder.Build();
        }

        /// <summary>
        /// Configure your AI service here. Uncomment and configure one of the options below.
        /// </summary>
        private static bool ConfigureAIService(IKernelBuilder builder)
        {
            // Option 1: Azure OpenAI (Recommended for production)
            var endpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT");
            var apiKey = Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY");
            var deploymentName = Environment.GetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT_NAME") ?? "gpt-4.1-mini";

            if (!string.IsNullOrEmpty(endpoint) && !string.IsNullOrEmpty(apiKey))
            {
                builder.AddAzureOpenAIChatCompletion(
                    deploymentName: deploymentName,
                    endpoint: endpoint,
                    apiKey: apiKey
                );
                Console.WriteLine($"✅ Configured Azure OpenAI: {deploymentName}");
                return true;
            }

            // Option 2: OpenAI
            // var openAIKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
            // var model = Environment.GetEnvironmentVariable("OPENAI_MODEL") ?? "gpt-4";
            // 
            // if (!string.IsNullOrEmpty(openAIKey))
            // {
            //     builder.AddOpenAIChatCompletion(
            //         modelId: model,
            //         apiKey: openAIKey
            //     );
            //     Console.WriteLine($"✅ Configured OpenAI: {model}");
            //     return true;
            // }

            // Option 3: Local model (e.g., Ollama)
            // Uncomment this if you have a local model running on localhost:11434
            // try
            // {
            //     builder.AddOpenAIChatCompletion(
            //         modelId: "llama3.1",
            //         endpoint: new Uri("http://localhost:11434"),
            //         apiKey: "not-needed-for-local"
            //     );
            //     Console.WriteLine("✅ Configured local model via Ollama");
            //     return true;
            // }
            // catch
            // {
            //     Console.WriteLine("⚠️  Could not connect to local model");
            // }

            return false;
        }

        /// <summary>
        /// Demonstrates basic document scoring functionality.
        /// </summary>
        private static async Task DemoBasicScoring(LMRanker ranker, List<string> documents)
        {
            Console.WriteLine("📊 Demo 1: Basic Document Scoring");
            Console.WriteLine("Query: 'machine learning algorithms and neural networks'");
            Console.WriteLine(new string('=', 70));

            var query = "machine learning algorithms and neural networks";
            var stopwatch = Stopwatch.StartNew();

            var results = new List<(string document, double score)>();
            
            await foreach (var (document, score) in ranker.ScoreAsync(query, documents.ToAsyncEnumerable()))
            {
                results.Add((document, score));
                Console.WriteLine($"Score: {score:F3} | {TruncateText(document, 55)}");
            }

            stopwatch.Stop();
            Console.WriteLine($"\n⏱️  Scored {results.Count} documents in {stopwatch.ElapsedMilliseconds}ms");
            Console.WriteLine($"📈 Average score: {results.Average(r => r.score):F3}");
            Console.WriteLine();
        }

        /// <summary>
        /// Demonstrates document ranking functionality with different queries.
        /// </summary>
        private static async Task DemoRanking(LMRanker ranker, List<string> documents)
        {
            Console.WriteLine("🏆 Demo 2: Document Ranking (Top 5)");
            
            var queries = new[]
            {
                "artificial intelligence and deep learning",
                "natural language processing techniques",
                "animals and wildlife behavior"
            };

            foreach (var query in queries)
            {
                Console.WriteLine($"\nQuery: '{query}'");
                Console.WriteLine(new string('-', 60));

                var stopwatch = Stopwatch.StartNew();
                var rank = 1;

                await foreach (var (document, score) in ranker.RankAsync(query, documents.ToAsyncEnumerable(), topN: 5))
                {
                    Console.WriteLine($"{rank}. Score: {score:F3} | {TruncateText(document, 45)}");
                    rank++;
                }

                stopwatch.Stop();
                Console.WriteLine($"   ⏱️  Ranked in {stopwatch.ElapsedMilliseconds}ms");
            }

            Console.WriteLine();
        }

        /// <summary>
        /// Demonstrates performance characteristics of the LMRanker.
        /// </summary>
        private static async Task DemoPerformanceComparison(LMRanker ranker, List<string> documents)
        {
            Console.WriteLine("⚡ Demo 3: Performance Analysis");
            Console.WriteLine(new string('=', 50));

            var query = "information retrieval and text processing";
            
            // Test with different document set sizes
            var testSizes = new[] { 5, 10, documents.Count };

            foreach (var size in testSizes)
            {
                var testDocs = documents.Take(size).ToList();
                var stopwatch = Stopwatch.StartNew();
                
                var results = new List<double>();
                await foreach (var (_, score) in ranker.ScoreAsync(query, testDocs.ToAsyncEnumerable()))
                {
                    results.Add(score);
                }
                
                stopwatch.Stop();
                
                Console.WriteLine($"📋 {size,2} documents: {stopwatch.ElapsedMilliseconds,4}ms | " +
                                $"Avg: {results.Average():F3} | " +
                                $"Max: {results.Max():F3}");
            }

            Console.WriteLine();
        }

        /// <summary>
        /// Demonstrates how different queries affect ranking results.
        /// </summary>
        private static async Task DemoMultipleQueries(LMRanker ranker, List<string> documents)
        {
            Console.WriteLine("🔍 Demo 4: Query Comparison Analysis");
            Console.WriteLine(new string('=', 60));

            var queries = new Dictionary<string, string>
            {
                ["Technical"] = "machine learning algorithms and data processing",
                ["Animals"] = "fox and dog behavior in nature",
                ["Broad AI"] = "artificial intelligence applications"
            };

            foreach (var (category, query) in queries)
            {
                Console.WriteLine($"\n{category} Query: '{query}'");
                Console.WriteLine(new string('-', 50));

                var topResults = new List<(string doc, double score)>();
                
                await foreach (var (document, score) in ranker.RankAsync(query, documents.ToAsyncEnumerable(), topN: 3))
                {
                    topResults.Add((document, score));
                }

                for (int i = 0; i < topResults.Count; i++)
                {
                    var (doc, score) = topResults[i];
                    Console.WriteLine($"{i + 1}. [{score:F3}] {TruncateText(doc, 40)}");
                }
            }

            Console.WriteLine();
        }

        /// <summary>
        /// Returns a comprehensive set of sample documents for testing.
        /// </summary>
        private static List<string> GetSampleDocuments()
        {
            return new List<string>
            {
                // AI and Machine Learning
                "Machine learning is a subset of artificial intelligence that focuses on algorithms that improve through experience.",
                "Deep learning uses neural networks with multiple layers to process and analyze complex data patterns.",
                "Natural language processing enables computers to understand, interpret, and generate human language effectively.",
                "Artificial intelligence aims to create systems that can perform tasks typically requiring human intelligence.",
                "Neural networks are computing systems inspired by biological neural networks that constitute animal brains.",
                
                // Information Retrieval and Text Processing
                "Information retrieval systems help users find relevant documents from large collections of text data.",
                "Text mining extracts useful information and patterns from unstructured textual data sources.",
                "Semantic search improves traditional keyword-based search by understanding the meaning and context.",
                "Document ranking algorithms determine the relevance of documents to user queries and information needs.",
                "Text classification automatically assigns predefined categories to documents based on their content.",
                
                // Animals and Nature
                "The quick brown fox jumps over the lazy dog in the forest clearing.",
                "Foxes are clever and adaptable animals that live in forests, grasslands, and urban environments.",
                "Dogs are loyal companions and have been domesticated by humans for thousands of years.",
                "Wildlife behavior patterns change seasonally as animals adapt to environmental conditions.",
                "Forest ecosystems support diverse animal populations including predators, prey, and omnivores.",
                
                // Technology and Computing
                "The algorithm processes large datasets efficiently using parallel computing techniques.",
                "Software engineering practices ensure reliable and maintainable code development processes.",
                "Database systems store and retrieve information using structured query languages and indexing.",
                "Cloud computing provides scalable infrastructure for modern applications and services.",
                "Cybersecurity measures protect digital systems from threats, attacks, and unauthorized access."
            };
        }

        /// <summary>
        /// Helper method to truncate text for display purposes.
        /// </summary>
        private static string TruncateText(string text, int maxLength)
        {
            if (text.Length <= maxLength)
                return text;
            
            return text.Substring(0, maxLength - 3) + "...";
        }
    }

    /// <summary>
    /// Extension methods to help with async enumerable operations.
    /// </summary>
    public static class EnumerableExtensions
    {
        /// <summary>
        /// Converts a regular enumerable to an async enumerable.
        /// </summary>
        public static async IAsyncEnumerable<T> ToAsyncEnumerable<T>(this IEnumerable<T> source)
        {
            foreach (var item in source)
            {
                yield return item;
                await Task.Yield(); // Allow other async operations to proceed
            }
        }
    }
}
