namespace SemanticKernel.Rankers.BM25.Tests;

/// <summary>
/// Extension methods to support testing async enumerables
/// </summary>
public static class TestExtensions
{
    /// <summary>
    /// Converts an array to an async enumerable for testing purposes
    /// </summary>
    public static async IAsyncEnumerable<T> ToAsyncEnumerable<T>(this IEnumerable<T> source)
    {
        foreach (var item in source)
        {
            await Task.Yield(); // Simulate async behavior
            yield return item;
        }
    }

    /// <summary>
    /// Converts an async enumerable to a list for easier testing
    /// </summary>
    public static async Task<List<T>> ToListAsync<T>(this IAsyncEnumerable<T> source)
    {
        var result = new List<T>();
        await foreach (var item in source)
        {
            result.Add(item);
        }
        return result;
    }
}
