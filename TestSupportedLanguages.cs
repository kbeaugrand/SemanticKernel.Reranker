using Catalyst;
using SemanticKernel.Rankers.BM25;

// Test 1: Default behavior (no language restrictions)
Console.WriteLine("=== Test 1: Default behavior (no language restrictions) ===");
var defaultReranker = new BM25Reranker();
await TestReranker(defaultReranker, "Default");

// Test 2: Only English supported
Console.WriteLine("\n=== Test 2: Only English supported ===");
var englishOnlyReranker = new BM25Reranker(null, new HashSet<Language> { Language.English });
await TestReranker(englishOnlyReranker, "English-only");

// Test 3: English and French supported
Console.WriteLine("\n=== Test 3: English and French supported ===");
var englishFrenchReranker = new BM25Reranker(null, new HashSet<Language> { Language.English, Language.French });
await TestReranker(englishFrenchReranker, "English-French");

static async Task TestReranker(BM25Reranker reranker, string testName)
{
    try
    {
        var query = "cat";
        var documents = new[]
        {
            "The cat is sleeping on the mat",
            "Dogs are loyal pets",
            "A black cat crossed the street"
        }.ToAsyncEnumerable();

        var results = new List<(string, double)>();
        await foreach (var result in reranker.ScoreAsync(query, documents))
        {
            results.Add(result);
        }

        Console.WriteLine($"{testName} - Successfully processed {results.Count} documents");
        foreach (var (doc, score) in results)
        {
            Console.WriteLine($"  Score: {score:F4} - {doc}");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"{testName} - Error: {ex.Message}");
    }
}
