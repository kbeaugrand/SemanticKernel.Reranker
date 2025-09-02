using SemanticKernel.Rankers.Abstractions;
using SemanticKernel.Rankers.BM25;

namespace SemanticKernel.Rankers.Pipelines
{
    public class BM25ThenLMRankerPipelineConfig
    {
        public int TopK { get; set; } = 20;
        public int TopM { get; set; } = 5;
        public double ScoreThreshold { get; set; } = 0.0;
    }

    public class BM25ThenLMRankerPipeline : CascadeRerankPipeline
    {
        private readonly BM25ThenLMRankerPipelineConfig _config;

        public BM25ThenLMRankerPipeline(BM25Reranker bm25, SemanticKernel.Rankers.LMRanker.LMRanker lmRanker, BM25ThenLMRankerPipelineConfig config)
            : base(new List<IRanker> { bm25 ?? throw new ArgumentNullException(nameof(bm25)), lmRanker ?? throw new ArgumentNullException(nameof(lmRanker)) }, 
                   new CascadeRerankPipelineConfig
                   {
                       TopK = (config ?? throw new ArgumentNullException(nameof(config))).TopK,
                       TopM = config.TopM,
                       ScoreThreshold = config.ScoreThreshold
                   })
        {
            _config = config;
        }
    }
}
