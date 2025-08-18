
using System.Text.RegularExpressions;

namespace SemanticKernel.Reranker.BM25
{
    public class BM25Reranker : IReranker
    {
        private readonly List<List<string>> _documents;
        private readonly Dictionary<string, int> _df;
        private readonly List<int> _docLens;
        private readonly double _avgDocLen;
        private readonly int _N;
        private readonly double _k1;
        private readonly double _b;

        public BM25Reranker(IEnumerable<string> documents, double k1 = 1.5, double b = 0.75)
        {
            _k1 = k1;
            _b = b;
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

        public List<(int, double)> Rank(string query, int topN = 5)
        {
            var queryTokens = Tokenize(query);
            var scores = new List<(int, double)>();

            for (int i = 0; i < _documents.Count; i++)
            {
                double score = 0;
                foreach (var term in queryTokens.Distinct())
                {
                    score += Score(i, term, queryTokens.Count(t => t == term));
                }
                scores.Add((i, score));
            }

            return scores.OrderByDescending(s => s.Item2).Take(topN).ToList();
        }

        private double Score(int docIndex, string term, int qf)
        {
            int f = _documents[docIndex].Count(t => t == term);
            if (!_df.ContainsKey(term) || f == 0) return 0;

            double idf = Math.Log(1 + (_N - _df[term] + 0.5) / (_df[term] + 0.5));
            double tf = f * (_k1 + 1) / (f + _k1 * (1 - _b + _b * _docLens[docIndex] / _avgDocLen));
            return idf * tf;
        }

        private static List<string> Tokenize(string text)
        {
            return Regex.Split(text.ToLower(), @"\W+").Where(w => w.Length > 0).ToList();
        }
    }
}