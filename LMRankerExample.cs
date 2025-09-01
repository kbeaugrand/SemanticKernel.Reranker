using Microsoft.SemanticKernel;
using SemanticKernel.Rankers.LMRanker;
using System.ComponentModel;

namespace BM25Sample
{
    /// <summary>
    /// Example demonstrating how to use the LMRanker with Semantic Kernel.
    /// This class shows how to set up and use the Language Model-based reranker.
    /// </summary>
    public class LMRankerExample
    {
        /// <summary>
        /// Demonstrates basic usage of the LMRanker with a local or cloud-based language model.
        /// </summary>
        public static async Task RunExample()
        {
            Console.WriteLine("=== Language Model Reranker Demo ===");
            Console.WriteLine();

            // Sample documents to rank
            var documents = new List<string>
            {
                "The quick brown fox jumps over the lazy dog.",
                "Machine learning is a subset of artificial intelligence that focuses on algorithms.",
                "Natural language processing helps computers understand and generate human language.",
                "Deep learning uses neural networks with multiple layers to process data.",
                "Information retrieval systems help users find relevant documents from large collections.",
                "Text mining extracts useful information and patterns from unstructured text data.",
                "Semantic search improves traditional keyword-based search by understanding meaning.",
                "Artificial intelligence aims to create systems that can perform tasks requiring human intelligence.",
                "The fox is a clever animal that lives in forests and urban areas.",
                "Dogs are loyal companions and popular pets worldwide."
            };

            try
            {
                // Create Semantic Kernel instance
                // NOTE: You need to configure this with your preferred AI service
                var kernel = CreateKernel();

                // Create LMRanker instance
                var lmRanker = new LMRanker(kernel);

                // Demo 1: Score all documents
                await DemoScoring(lmRanker, documents);

                // Demo 2: Rank and get top N documents
                await DemoRanking(lmRanker, documents);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine("Make sure to configure the Semantic Kernel with a valid AI service.");
                Console.WriteLine("You can use Azure OpenAI, OpenAI, or other supported language model services.");
            }
        }

        /// <summary>
        /// Creates and configures a Semantic Kernel instance.
        /// You need to customize this method with your AI service configuration.
        /// </summary>
        private static Kernel CreateKernel()
        {
            var builder = Kernel.CreateBuilder();

            // Option 1: Azure OpenAI (recommended for production)
            // builder.AddAzureOpenAIChatCompletion(
            //     deploymentName: "your-deployment-name",
            //     endpoint: "https://your-resource.openai.azure.com/",
            //     apiKey: "your-api-key"
            // );

            // Option 2: OpenAI
            // builder.AddOpenAIChatCompletion(
            //     modelId: "gpt-4",
            //     apiKey: "your-openai-api-key"
            // );

            // Option 3: Local model (e.g., using Ollama)
            // builder.AddOpenAIChatCompletion(
            //     modelId: "llama2",
            //     endpoint: new Uri("http://localhost:11434"),
            //     apiKey: "not-needed-for-local"
            // );

            // For demonstration purposes, we'll throw an exception if no service is configured
            // Remove this and uncomment one of the above options to use the LMRanker
            throw new InvalidOperationException(
                "Please configure a language model service in the CreateKernel method. " +
                "Uncomment and configure one of the AI service options (Azure OpenAI, OpenAI, or local model).");

            return builder.Build();
        }

        /// <summary>
        /// Demonstrates document scoring with the LMRanker.
        /// </summary>
        private static async Task DemoScoring(LMRanker ranker, List<string> documents)
        {
            Console.WriteLine("1. Document Scoring for Query: 'machine learning algorithms'");
            Console.WriteLine(new string('-', 70));

            var query = "machine learning algorithms";
            
            await foreach (var (document, score) in ranker.ScoreAsync(query, documents.ToAsyncEnumerable()))
            {
                Console.WriteLine($"Score: {score:F3} | {TruncateText(document, 60)}");
            }

            Console.WriteLine();
        }

        /// <summary>
        /// Demonstrates document ranking with the LMRanker.
        /// </summary>
        private static async Task DemoRanking(LMRanker ranker, List<string> documents)
        {
            Console.WriteLine("2. Top 5 Documents for Query: 'artificial intelligence and neural networks'");
            Console.WriteLine(new string('-', 70));

            var query = "artificial intelligence and neural networks";
            
            await foreach (var (document, score) in ranker.RankAsync(query, documents.ToAsyncEnumerable(), topN: 5))
            {
                Console.WriteLine($"Score: {score:F3} | {TruncateText(document, 60)}");
            }

            Console.WriteLine();
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
