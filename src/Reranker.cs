
namespace SemanticKernel.Reranker
{
    public interface IReranker
    {
        Task<List<(int, double)>> RankAsync(string query, int topN = 5);
    }
}