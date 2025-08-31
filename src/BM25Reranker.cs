using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using Catalyst;
using Catalyst.Models;
using Microsoft.Extensions.VectorData;
using Mosaik.Core;
using Version = Mosaik.Core.Version;

namespace SemanticKernel.Reranker.BM25
{
    /// <summary>
    /// Contains pre-computed corpus statistics for efficient BM25 scoring.
    /// </summary>
    public class CorpusStatistics
    {
        public Dictionary<string, int> DocumentFrequencies { get; init; } = new();
        public int TotalDocuments { get; init; }
        public double AverageDocumentLength { get; init; }
    }

    /// <summary>
    /// Represents a processed document with cached tokenization results.
    /// </summary>
    internal class ProcessedDocument
    {
        public string Content { get; init; } = string.Empty;
        public List<string> Tokens { get; init; } = new();
        public Dictionary<string, int> TermFrequencies { get; init; } = new();
        public int Length { get; init; }
    }

    /// <summary>
    /// BM25Reranker.
    /// Optimized for performance with caching, streaming, and efficient data structures.
    /// </summary>
    public class BM25Reranker
    {
        private static readonly ConcurrentDictionary<Language, Pipeline> _pipelineCache = new();
        private static readonly ConcurrentDictionary<string, (List<string> tokens, Dictionary<string, int> termFreqs)> _tokenCache = new();
        private static readonly object _modelLock = new object();
        private static readonly HashSet<Language> _registeredLanguages = new();

        private readonly CorpusStatistics? _corpusStats;
        private readonly HashSet<Language>? _supportedLanguages;

        /// <summary>
        /// Initializes a new instance of BM25Reranker with optional pre-computed corpus statistics and supported languages.
        /// </summary>
        /// <param name="corpusStats">Pre-computed corpus statistics for better performance</param>
        /// <param name="supportedLanguages">Optional set of supported languages. If null, all languages are supported.</param>
        public BM25Reranker(CorpusStatistics? corpusStats = null, HashSet<Language>? supportedLanguages = null)
        {
            _corpusStats = corpusStats;
            _supportedLanguages = supportedLanguages;
        }
        /// <summary>
        /// Scores documents using the BM25 algorithm against a given query.
        /// Returns an async enumerable of document-score pairs in the order they were processed.
        /// Optimized for streaming with optional corpus statistics.
        /// </summary>
        /// <param name="query">The search query to score documents against</param>
        /// <param name="documents">An async enumerable of documents to score</param>
        /// <param name="k1">Controls term frequency impact. Typical values: 1.2-2.0. Default is 1.5</param>
        /// <param name="b">Controls document length normalization. Range: 0-1. Default is 0.75</param>
        /// <param name="k3">Controls query term frequency impact. Default is 1000</param>
        /// <returns>An async enumerable of tuples containing the document and its BM25 score</returns>
        public async IAsyncEnumerable<(string DocumentText, double Score)> ScoreAsync(string query, IAsyncEnumerable<string> documents, double k1 = 1.5, double b = 0.75, double k3 = 1000)
        {
            // Cache query tokenization
            var cacheKey = $"query_{query.GetHashCode()}";
            var (queryTokens, queryTermFreqs) = await GetOrCacheTokensAsync(cacheKey, query);
            
            if (_corpusStats != null)
            {
                // Use pre-computed statistics for true streaming
                await foreach (var document in documents)
                {
                    var docCacheKey = $"doc_{document.GetHashCode()}";
                    var (docTokens, docTermFreqs) = await GetOrCacheTokensAsync(docCacheKey, document);
                    
                    var score = CalculateBM25Score(queryTermFreqs, docTermFreqs, docTokens.Count, 
                        _corpusStats.DocumentFrequencies, _corpusStats.TotalDocuments, _corpusStats.AverageDocumentLength, k1, b, k3);
                    
                    yield return (document, score);
                }
            }
            else
            {
                // Fallback to two-pass approach with optimizations
                await foreach (var result in ScoreWithTwoPassAsync(query, documents, queryTokens, queryTermFreqs, k1, b, k3))
                {
                    yield return result;
                }
            }
        }

        /// <summary>
        /// Scores a set of search results using the BM25 algorithm.
        /// </summary>
        /// <summary>
        /// Scores a set of search results using the BM25 algorithm.
        /// This overload extracts text content from VectorSearchResult objects using a property expression.
        /// </summary>
        /// <typeparam name="T">The type of records contained in the VectorSearchResult objects</typeparam>
        /// <param name="query">The search query to score documents against</param>
        /// <param name="searchResults">An async enumerable of VectorSearchResult objects to score</param>
        /// <param name="textProperty">An expression that specifies which property of type T contains the text content</param>
        /// <param name="k1">Controls term frequency impact. Typical values: 1.2-2.0. Default is 1.5</param>
        /// <param name="b">Controls document length normalization. Range: 0-1. Default is 0.75</param>
        /// <param name="k3">Controls query term frequency impact. Default is 1000</param>
        /// <returns>An async enumerable of tuples containing the VectorSearchResult and its BM25 score</returns>
        public async IAsyncEnumerable<(VectorSearchResult<T> Result, double Score)> ScoreAsync<T>(string query, IAsyncEnumerable<VectorSearchResult<T>> searchResults, Expression<Func<T, string>> textProperty, double k1 = 1.5, double b = 0.75, double k3 = 1000)
        {
            var searchResultsList = new List<VectorSearchResult<T>>();
            var documentTexts = new List<string>();
            var getText = textProperty.Compile();

            await foreach (var searchResult in searchResults)
            {
                searchResultsList.Add(searchResult);
                var text = getText(searchResult.Record);
                documentTexts.Add(text);
            }

            var index = 0;
            await foreach (var result in ScoreAsync(query, ToAsyncEnumerable(documentTexts), k1, b, k3))
            {
                yield return (searchResultsList[index], result.Score);
                index++;
            }
        }

        private static async IAsyncEnumerable<T> ToAsyncEnumerable<T>(IEnumerable<T> source)
        {
            foreach (var item in source)
            {
                yield return item;
            }
        }

        /// <summary>
        /// Scores documents using the BM25 algorithm with a two-pass approach when corpus statistics are not available.
        /// First pass collects document statistics, second pass calculates and yields BM25 scores.
        /// </summary>
        /// <param name="query">The search query to score documents against</param>
        /// <param name="documents">An async enumerable of documents to score</param>
        /// <param name="queryTokens">Pre-tokenized query terms for performance optimization</param>
        /// <param name="queryTermFreqs">Query term frequencies for BM25 calculation</param>
        /// <param name="k1">Controls term frequency impact. Typical values: 1.2-2.0</param>
        /// <param name="b">Controls document length normalization. Range: 0-1</param>
        /// <param name="k3">Controls query term frequency impact</param>
        /// <returns>An async enumerable of tuples containing the document and its BM25 score</returns>
        private async IAsyncEnumerable<(string, double)> ScoreWithTwoPassAsync(string query, IAsyncEnumerable<string> documents,
            List<string> queryTokens, Dictionary<string, int> queryTermFreqs, double k1, double b, double k3)
        {
            // First pass: collect document statistics with optimizations
            var processedDocs = new List<ProcessedDocument>();
            var df = new Dictionary<string, int>();
            double totalLength = 0;

            await foreach (var document in documents)
            {
                var cacheKey = $"doc_{document.GetHashCode()}";
                var (docTokens, docTermFreqs) = await GetOrCacheTokensAsync(cacheKey, document);

                var processed = new ProcessedDocument
                {
                    Content = document,
                    Tokens = docTokens,
                    TermFrequencies = docTermFreqs,
                    Length = docTokens.Count
                };

                processedDocs.Add(processed);
                totalLength += processed.Length;

                // Optimized document frequency updates
                foreach (var term in docTermFreqs.Keys)
                {
                    df.TryGetValue(term, out var count);
                    df[term] = count + 1;
                }
            }

            var avgDocLen = processedDocs.Count > 0 ? totalLength / processedDocs.Count : 0;

            // Second pass: yield results with optimized scoring
            foreach (var doc in processedDocs)
            {
                var score = CalculateBM25Score(queryTermFreqs, doc.TermFrequencies, doc.Length, df, processedDocs.Count, avgDocLen, k1, b, k3);
                yield return (doc.Content, score);
            }
        }

        /// <summary>
        /// Ranks documents using the BM25 algorithm and returns the top N results sorted by relevance score.
        /// Uses a priority queue for memory-efficient top-N selection.
        /// </summary>
        /// <param name="query">The search query to rank documents against</param>
        /// <param name="documents">An async enumerable of documents to rank</param>
        /// <param name="topN">The maximum number of top-ranked documents to return. Default is 5</param>
        /// <param name="k1">Controls term frequency impact. Typical values: 1.2-2.0. Default is 1.5</param>
        /// <param name="b">Controls document length normalization. Range: 0-1. Default is 0.75</param>
        /// <param name="k3">Controls query term frequency impact. Default is 1000</param>
        /// <returns>An async enumerable of tuples containing the top N documents and their BM25 scores, sorted by relevance</returns>
        public async IAsyncEnumerable<(string DocumentText, double Score)> RankAsync(string query, IAsyncEnumerable<string> documents, int topN = 5, double k1 = 1.5, double b = 0.75, double k3 = 1000)
        {
            // Use a min-heap to maintain top N results efficiently
            var topResults = new PriorityQueue<(string document, double score), double>();

            await foreach (var (document, score) in ScoreAsync(query, documents, k1, b, k3))
            {
                var floatScore = (float)score;
                
                if (topResults.Count < topN)
                {
                    topResults.Enqueue((document, floatScore), floatScore);
                }
                else if (floatScore > topResults.Peek().score)
                {
                    topResults.Dequeue();
                    topResults.Enqueue((document, floatScore), floatScore);
                }
            }
            
            // Extract results and sort in descending order
            var results = new List<(string document, double score)>();
            while (topResults.Count > 0)
            {
                results.Add(topResults.Dequeue());
            }
            
            results.Reverse(); // PriorityQueue is min-heap, so reverse for descending order
            
            foreach (var result in results)
            {
                yield return result;
            }
        }

        /// <summary>
        /// Ranks vector search results using the BM25 algorithm and returns the top N results sorted by relevance score.
        /// This overload is specifically designed to work with VectorSearchResult objects by extracting text content
        /// from the search results using a provided property expression.
        /// </summary>
        /// <typeparam name="T">The type of records contained in the VectorSearchResult objects</typeparam>
        /// <param name="query">The search query to rank documents against</param>
        /// <param name="documents">An async enumerable of VectorSearchResult objects containing the records to rank</param>
        /// <param name="textProperty">An expression that specifies which property of type T contains the text content to be ranked</param>
        /// <param name="topN">The maximum number of top-ranked documents to return. Default is 5</param>
        /// <param name="k1">Controls term frequency impact. Typical values: 1.2-2.0. Default is 1.5</param>
        /// <param name="b">Controls document length normalization. Range: 0-1. Default is 0.75</param>
        /// <param name="k3">Controls query term frequency impact. Default is 1000</param>
        /// <returns>An async enumerable of tuples containing the top N text documents and their BM25 scores, sorted by relevance</returns>
        /// <remarks>
        /// This method serves as a convenient wrapper around the core RankAsync method for VectorSearchResult objects.
        /// It extracts text content from each search result using the provided property expression and then
        /// applies BM25 ranking to determine relevance scores. The method maintains the same performance
        /// characteristics as the core ranking implementation, including efficient top-N selection using
        /// a priority queue for memory optimization.
        /// </remarks>
        public async IAsyncEnumerable<(VectorSearchResult<T> Result, double Score)> RankAsync<T>(string query, IAsyncEnumerable<VectorSearchResult<T>> documents, Expression<Func<T, string>> textProperty, int topN = 5, double k1 = 1.5, double b = 0.75, double k3 = 1000)
        {
            var topResults = new PriorityQueue<(VectorSearchResult<T> document, double score), double>();

            await foreach (var (document, score) in ScoreAsync(query, documents, textProperty, k1, b, k3))
            {
                var floatScore = (float)score;

                if (topResults.Count < topN)
                {
                    topResults.Enqueue((document, floatScore), floatScore);
                }
                else if (floatScore > topResults.Peek().score)
                {
                    topResults.Dequeue();
                    topResults.Enqueue((document, floatScore), floatScore);
                }
            }

             // Extract results and sort in descending order
            var results = new List<(VectorSearchResult<T> document, double score)>();
            while (topResults.Count > 0)
            {
                results.Add(topResults.Dequeue());
            }
            
            results.Reverse(); // PriorityQueue is min-heap, so reverse for descending order
            
            foreach (var result in results)
            {
                yield return result;
            }
        }

        /// <summary>
        /// Pre-computes corpus statistics for efficient streaming BM25 scoring.
        /// </summary>
        /// <param name="documents">The corpus of documents to analyze</param>
        /// <returns>Corpus statistics that can be used to initialize BM25Reranker</returns>
        public async Task<CorpusStatistics> ComputeCorpusStatisticsAsync(IAsyncEnumerable<string> documents)
        {
            var df = new Dictionary<string, int>();
            int totalDocuments = 0;
            double totalLength = 0;

            await foreach (var document in documents)
            {
                var cacheKey = $"doc_{document.GetHashCode()}";
                var (docTokens, docTermFreqs) = await GetOrCacheTokensStaticAsync(cacheKey, document);

                totalLength += docTokens.Count;
                totalDocuments++;

                // Optimized document frequency updates
                foreach (var term in docTermFreqs.Keys)
                {
                    df.TryGetValue(term, out var count);
                    df[term] = count + 1;
                }
            }

            return new CorpusStatistics
            {
                DocumentFrequencies = df,
                TotalDocuments = totalDocuments,
                AverageDocumentLength = totalDocuments > 0 ? totalLength / totalDocuments : 0
            };
        }

        /// <summary>
        /// Gets or caches tokenization results for better performance.
        /// </summary>
        private async Task<(List<string> tokens, Dictionary<string, int> termFreqs)> GetOrCacheTokensAsync(string cacheKey, string text)
        {
            if (_tokenCache.TryGetValue(cacheKey, out var cached))
            {
                return cached;
            }
            
            var tokens = await TokenizeAsync(text);
            var termFreqs = tokens.GroupBy(t => t).ToDictionary(g => g.Key, g => g.Count());
            
            var result = (tokens, termFreqs);
            _tokenCache.TryAdd(cacheKey, result);
            return result;
        }

        /// <summary>
        /// Static version for corpus statistics computation.
        /// </summary>
        private async Task<(List<string> tokens, Dictionary<string, int> termFreqs)> GetOrCacheTokensStaticAsync(string cacheKey, string text)
        {
            if (_tokenCache.TryGetValue(cacheKey, out var cached))
            {
                return cached;
            }
            
            var tokens = await TokenizeStaticAsync(text);
            var termFreqs = tokens.GroupBy(t => t).ToDictionary(g => g.Key, g => g.Count());
            
            var result = (tokens, termFreqs);
            _tokenCache.TryAdd(cacheKey, result);
            return result;
        }

        /// Calculates the BM25 relevance score between a query and a document using an optimized implementation.
        /// BM25 is a probabilistic ranking function used in information retrieval to estimate the relevance
        /// of documents to a given search query.
        /// </summary>
        /// <param name="queryTermFreqs">Dictionary containing the frequency of each term in the query</param>
        /// <param name="docTermFreqs">Dictionary containing the frequency of each term in the document</param>
        /// <param name="docLen">The total number of terms in the document</param>
        /// <param name="df">Document frequency dictionary - number of documents containing each term</param>
        /// <param name="N">Total number of documents in the collection</param>
        /// <param name="avgDocLen">Average document length across the entire collection</param>
        /// <param name="k1">Term frequency saturation parameter (typically 1.2-2.0)</param>
        /// <param name="b">Field length normalization parameter (typically 0.75)</param>
        /// <param name="k3">Query term frequency saturation parameter (typically 1.2-2.0)</param>
        /// <returns>The BM25 relevance score as a double value</returns>
        private static double CalculateBM25Score(Dictionary<string, int> queryTermFreqs, Dictionary<string, int> docTermFreqs, 
            int docLen, Dictionary<string, int> df, int N, double avgDocLen, double k1, double b, double k3)
        {
            double score = 0;
            
            foreach (var (term, queryTermFreq) in queryTermFreqs)
            {
                if (!docTermFreqs.TryGetValue(term, out var docTermFreq) || !df.TryGetValue(term, out var documentFreq))
                    continue;
                
                if (docTermFreq == 0) continue;

                double idf = Math.Log(1 + (N - documentFreq + 0.5) / (documentFreq + 0.5));
                double tf = docTermFreq * (k1 + 1) / (docTermFreq + k1 * (1 - b + b * docLen / avgDocLen));
                double qtf = queryTermFreq * (k3 + 1) / (queryTermFreq + k3);
                
                score += idf * tf * qtf;
            }
            
            return score;
        }

        /// <summary>
        /// Optimized tokenization with caching and reduced reflection overhead.
        /// </summary>
        private async Task<List<string>> TokenizeAsync(string text)
        {
            var doc = new Document(text);
            var cld2LanguageDetector = await LanguageDetector.FromStoreAsync(Language.Any, Version.Latest, "");

            cld2LanguageDetector.Process(doc);

            // Get or create cached pipeline
            var pipeline = await GetOrCreatePipelineAsync(doc.Language);
            
            var tokens = pipeline.ProcessSingle(doc).Spans
                .SelectMany(c => c.Tokens)
                .Where(token => !Catalyst.StopWords.Spacy.For(doc.Language).Contains(token.Lemma))
                .Where(token => token.POS != PartOfSpeech.PUNCT && token.POS != PartOfSpeech.SYM)
                .Select(token => token.Lemma)
                .ToList();

            return tokens;
        }

        /// <summary>
        /// Static version of tokenization for corpus statistics computation.
        /// </summary>
        private async Task<List<string>> TokenizeStaticAsync(string text)
        {
            var doc = new Document(text);
            var cld2LanguageDetector = await LanguageDetector.FromStoreAsync(Language.Any, Version.Latest, "");

            cld2LanguageDetector.Process(doc);

            if (doc.Language == Language.Unknown)
            {
                doc.Language = Language.English; // Default to English if detection fails
            }

            var pipeline = await GetOrCreatePipelineStaticAsync(doc.Language);
            
            var tokens = pipeline.ProcessSingle(doc).Spans
                .SelectMany(c => c.Tokens)
                .Where(token => !Catalyst.StopWords.Spacy.For(doc.Language).Contains(token.Lemma))
                .Where(token => token.POS != PartOfSpeech.PUNCT && token.POS != PartOfSpeech.SYM)
                .Select(token => token.Lemma)
                .ToList();

            return tokens;
        }

        /// <summary>
        /// Static version of pipeline creation for corpus statistics computation.
        /// </summary>
        private async Task<Pipeline> GetOrCreatePipelineStaticAsync(Language language)
        {
            if (_pipelineCache.TryGetValue(language, out var cached))
            {
                return cached;
            }

            // Ensure language model is registered (with thread safety) - no language filtering for static version
            if (!_registeredLanguages.Contains(language))
            {
                lock (_modelLock)
                {
                    if (!_registeredLanguages.Contains(language))
                    {
                        try
                        {
                            RegisterLanguageModel(language); // No filtering for static version
                            _registeredLanguages.Add(language);
                        }
                        catch (Exception)
                        {
                            // Fall back to English if language model is not available
                            language = Language.English;
                            if (!_registeredLanguages.Contains(language))
                            {
                                RegisterLanguageModel(language);
                                _registeredLanguages.Add(language);
                            }
                        }
                    }
                }
            }

            var pipeline = await Pipeline.ForAsync(language);
            _pipelineCache.TryAdd(language, pipeline);
            return pipeline;
        }

        /// <summary>
        /// Gets or creates a cached NLP pipeline for the specified language.
        /// </summary>
        private async Task<Pipeline> GetOrCreatePipelineAsync(Language language)
        {
            if (_pipelineCache.TryGetValue(language, out var cached))
            {
                return cached;
            }

            // Check if language is supported before attempting registration
            var originalLanguage = language;
            if (_supportedLanguages != null && !_supportedLanguages.Contains(language))
            {
                // Fall back to English if language is not supported
                language = Language.English;
                if (_pipelineCache.TryGetValue(language, out var fallbackCached))
                {
                    return fallbackCached;
                }
            }

            // Ensure language model is registered (with thread safety)
            if (!_registeredLanguages.Contains(language))
            {
                lock (_modelLock)
                {
                    if (!_registeredLanguages.Contains(language))
                    {
                        var registrationSuccess = RegisterLanguageModel(language);
                        if (!registrationSuccess && language != Language.English)
                        {
                            // Fall back to English if registration failed
                            language = Language.English;
                            if (!_registeredLanguages.Contains(language))
                            {
                                RegisterLanguageModel(language);
                                _registeredLanguages.Add(language);
                            }
                        }
                        else
                        {
                            _registeredLanguages.Add(language);
                        }
                    }
                }
            }

            var pipeline = await Pipeline.ForAsync(language);
            _pipelineCache.TryAdd(language, pipeline);
            return pipeline;
        }

        /// <summary>
        /// Language model registration.
        /// </summary>
        private bool RegisterLanguageModel(Language language)
        {
            // Skip registration if language is not in supported languages
            if (_supportedLanguages != null && !_supportedLanguages.Contains(language))
            {
                return false;
            }

            try
            {
                // Cache assembly loading
                var assemblyName = $"Catalyst.Models.{language}";
                var assembly = Assembly.Load(assemblyName);

                // More efficient type discovery
                var registerType = assembly.GetType($"Catalyst.Models.{language}");
                if (registerType != null)
                {
                    var registerMethod = registerType.GetMethod("Register", BindingFlags.Public | BindingFlags.Static);
                    registerMethod?.Invoke(null, null);
                }

                LoadStopWords(language);
            }
            catch (Exception)
            {
                // Silently ignore unsupported language models
                // The caller will handle fallbacks if needed
            }

            return true;
        }

        /// <summary>
        /// Optimized stop words loading with reduced reflection overhead.
        /// </summary>
        private static void LoadStopWords(Language language)
        {
            try
            {
                var currentAssembly = Assembly.GetExecutingAssembly();
                var stopWordsTypeName = $"{currentAssembly.GetName().Name}.StopWords.{language}";
                var stopWordsType = currentAssembly.GetType(stopWordsTypeName);

                if (stopWordsType != null)
                {
                    var stopWordsField = stopWordsType.GetField("StopWords", BindingFlags.Public | BindingFlags.Static);
                    if (stopWordsField?.FieldType == typeof(HashSet<string>))
                    {
                        var stopWords = (HashSet<string>?)stopWordsField.GetValue(null);
                        if (stopWords != null)
                        {
                            Catalyst.StopWords.Spacy.Register(language, new ReadOnlyHashSet<string>(stopWords));
                        }
                    }
                }
            }
            catch (Exception)
            {
                // Silently continue if stop words can't be loaded
            }
        }

        /// <summary>
        /// Clears the tokenization cache. Useful for memory management in long-running applications.
        /// </summary>
        public static void ClearCache()
        {
            _tokenCache.Clear();
        }
    }
}