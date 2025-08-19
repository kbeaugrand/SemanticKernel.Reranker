// English stop words from spacy: https://github.com/explosion/spaCy/blob/master/spacy/lang/en/stop_words.py

namespace SemanticKernel.Reranker.BM25.StopWords
{
    internal static class English
    {
        // Stop words
        public static readonly HashSet<string> StopWords = new HashSet<string>(
            @"
a about above across after afterwards again against all almost alone along
already also although always am among amongst amount an and another any anyhow
anyone anything anyway anywhere are around as at

back be became because become becomes becoming been before beforehand behind
being below beside besides between beyond both bottom but by

call can cannot ca could

did do does doing done down due during

each eight either eleven else elsewhere empty enough even ever every
everyone everything everywhere except

few fifteen fifty first five for former formerly forty four from front full
further

get give go

had has have he hence her here hereafter hereby herein hereupon hers herself
him himself his how however hundred

i if in indeed into is it its itself

keep

last latter latterly least less

just

made make many may me meanwhile might mine more moreover most mostly move much
must my myself

name namely neither never nevertheless next nine no nobody none noone nor not
nothing now nowhere

of off often on once one only onto or other others otherwise our ours ourselves
out over own

part per perhaps please put

quite

rather re really regarding

same say see seem seemed seeming seems serious several she should show side
since six sixty so some somehow someone something sometime sometimes somewhere
still such

take ten than that the their them themselves then thence there thereafter
thereby therefore therein thereupon these they third this those though three
through throughout thru thus to together too top toward towards twelve twenty
two

under until up unless upon us used using

various very very via was we well were what whatever when whence whenever where
whereafter whereas whereby wherein whereupon wherever whether which while
whither who whoever whole whom whose why will with within without would

yet you your yours yourself yourselves
".Split(new char[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries),
            StringComparer.OrdinalIgnoreCase
        );

        static English()
        {
            var contractions = new[] { "n't", "'d", "'ll", "'m", "'re", "'s", "'ve" };

            // Add contractions to stop words
            foreach (var contraction in contractions)
            {
                StopWords.Add(contraction);
            }

            // Add contractions with different apostrophes
            var apostrophes = new[] { "'", "'" };
            foreach (var apostrophe in apostrophes)
            {
                foreach (var stopword in contractions)
                {
                    StopWords.Add(stopword.Replace("'", apostrophe));
                }
            }
        }
    }
}
