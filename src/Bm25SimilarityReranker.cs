using Microsoft.Extensions.AI;
using System.Numerics.Tensors;
using System.Text.RegularExpressions;

namespace SemanticKernel.Reranker.BM25
{
    public class Bm25SimilarityReranker : IReranker
    {
        private readonly IEmbeddingGenerator<string, Embedding<float>> _embeddingGenerator;
        private readonly List<List<string>> _documents;
        private readonly Dictionary<string, int> _df;
        private readonly List<int> _docLens;
        private readonly double _avgDocLen;
        private readonly int _N;
        private readonly double _k1;
        private readonly double _b;
        private readonly float _similarityThreshold;

        public Bm25SimilarityReranker(
            IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator,
            IEnumerable<string> documents,
            double k1 = 1.5, 
            double b = 0.75, 
            float similarityThreshold = 0.9f)
        {
            _embeddingGenerator = embeddingGenerator;
            _k1 = k1;
            _b = b;
            _similarityThreshold = similarityThreshold;
            _documents = documents.Select(Tokenize).ToList();
            _df = new Dictionary<string, int>();
            _docLens = _documents.Select(d => d.Count).ToList();
            _N = _documents.Count;
            _avgDocLen = _docLens.Average();

            foreach (var doc in _documents)
            {
                foreach (var word in doc.Distinct())
                {
                    if (_df.ContainsKey(word))
                        _df[word]++;
                    else
                        _df[word] = 1;
                }
            }
        }

        public async Task<List<(int, double)>> RankAsync(string query, int topN = 5)
        {
            var queryTerms = Tokenize(query)
                                    .ToList();

            var queryVector = await _embeddingGenerator.GenerateAsync(queryTerms);

            var scores = new List<(int, double)>();

            for (int i = 0; i < _documents.Count; i++)
            {
                double score = 0;
                foreach (var item in queryVector.Select((value, index) => (value, index))
                                                .DistinctBy(c => c.value))
                {
                    score += await ScoreAsync(i, (queryTerms.ElementAt(item.index), item.value), queryTerms.Count(t => t == queryTerms.ElementAt(item.index)));
                }
                scores.Add((i, score));
            }

            return scores.OrderByDescending(s => s.Item2).Take(topN).ToList();
        }

        private async Task<double> ScoreAsync(int docIndex, (string word, Embedding<float> vector) queryTerm, int qf)
        {
            // We look for similar words
            int f = 0;

            var vectors = await _embeddingGenerator.GenerateAsync(_documents[docIndex]);

            foreach (var word in vectors)
            {
                if (IsSimilar(queryTerm.vector, word))
                    f++;
            }
            if (f == 0) return 0;

            var dfWords = _df.Keys.ToList();
            var dfVectors = await _embeddingGenerator.GenerateAsync(dfWords);

            // For DF, we only count the documents that contain a word similar to the query
            int df = _df.TryGetValue(queryTerm.word, out var dfValue)
                    ? dfValue
                    : // Otherwise, sum counts for similar words
                    dfVectors
            .Select((emb, idx) => IsSimilar(queryTerm.vector, emb) ? _df[dfWords[idx]] : 0)
            .Sum();

            if (df == 0) return 0;

            double idf = Math.Log(1 + (_N - df + 0.5) / (df + 0.5));
            double tf = f * (_k1 + 1) / (f + _k1 * (1 - _b + _b * _docLens[docIndex] / _avgDocLen));
            return idf * tf;
        }

        private bool IsSimilar(Embedding<float> w1, Embedding<float> w2)
        {
            if (w1 == w2) return true;
            var similarity = TensorPrimitives.CosineSimilarity(w1.Vector.Span, w2.Vector.Span);
            
            return similarity >= _similarityThreshold;
        }

        private static List<string> Tokenize(string text)
        {
            return Regex.Split(text.ToLower(), @"\W+").Where(w => w.Length > 0).ToList();
        }
    }
}
