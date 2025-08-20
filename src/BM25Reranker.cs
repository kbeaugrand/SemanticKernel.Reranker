using System.Reflection;
using Catalyst;
using Catalyst.Models;
using Microsoft.ML;
using Mosaik.Core;
using Version = Mosaik.Core.Version;

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
            _df = new Dictionary<string, int>();

            // Tokenize documents asynchronously and wait for completion
            var tokenizedDocs = Task.WhenAll(documents.Select(TokenizeAsync)).GetAwaiter().GetResult();
            _documents = tokenizedDocs.ToList();
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
            var queryTokens = await TokenizeAsync(query);
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

        private static async Task<List<string>> TokenizeAsync(string text)
        {
            var doc = new Document(text);
            var cld2LanguageDetector = await LanguageDetector.FromStoreAsync(Language.Any, Version.Latest, "");

            cld2LanguageDetector.Process(doc);

            RegisterLanguageModel(doc.Language);

            var nlp = await Pipeline.ForAsync(doc.Language);

            var tokens = nlp.ProcessSingle(doc).Spans
                .SelectMany(c => c.Tokens.Select(t => t))
                .Where(token => !Catalyst.StopWords.Spacy.For(doc.Language).Contains(token.Lemma))
                .Where(token => token.POS != PartOfSpeech.PUNCT && token.POS != PartOfSpeech.SYM)
            .Select(token => token.Lemma)
            .ToList();

            return tokens;
        }

        private static void RegisterLanguageModel(Language language)
        {
            try
            {
                // Load the assembly for the specific language
                var assemblyName = $"Catalyst.Models.{language}";
                var assembly = Assembly.Load(assemblyName);

                // Find the type that contains the Register method
                var registerType = assembly.GetTypes()
                    .FirstOrDefault(t => t.Name == language.ToString() && t.Namespace == "Catalyst.Models");

                if (registerType != null)
                {
                    // Find and invoke the Register method
                    var registerMethod = registerType.GetMethod("Register", BindingFlags.Public | BindingFlags.Static);
                    registerMethod?.Invoke(null, null);
                }

                LoadStopWords(language);
            }
            catch (Exception)
            {
                throw;
            }
        }

        private static void LoadStopWords(Language language)
        {
            try
            {
                // Get the current assembly
                var currentAssembly = Assembly.GetExecutingAssembly();

                // Build the type name for the stop words class
                var stopWordsTypeName = $"{currentAssembly.GetName().Name}.StopWords.{language}";

                // Get the type
                var stopWordsType = currentAssembly.GetType(stopWordsTypeName);

                if (stopWordsType != null)
                {
                    // Get the StopWords property/field
                    var stopWordsProperty = stopWordsType.GetField("StopWords", BindingFlags.Public | BindingFlags.Static);

                    if (stopWordsProperty != null && stopWordsProperty.FieldType == typeof(HashSet<string>))
                    {
                        var stopWords = (HashSet<string>?)stopWordsProperty.GetValue(null);
                        if (stopWords != null)
                        {
                            Catalyst.StopWords.Spacy.Register(language, new ReadOnlyHashSet<string>(stopWords));
                        }
                    }
                }
            }
            catch (Exception)
            {

            }
        }
    }
}