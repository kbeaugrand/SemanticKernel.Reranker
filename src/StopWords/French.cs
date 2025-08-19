// English stop words from spacy: https://github.com/explosion/spaCy/blob/master/spacy/lang/de/stop_words.py

namespace SemanticKernel.Reranker.BM25.StopWords
{
    internal static class French
    {
        // French stop words
        public static readonly HashSet<string> StopWords = new HashSet<string>(
            @"
a à â abord afin ah ai aie ainsi ait allaient allons
alors anterieur anterieure anterieures antérieur antérieure antérieures
apres après as assez attendu au
aupres auquel aura auraient aurait auront
aussi autre autrement autres autrui aux auxquelles auxquels avaient
avais avait avant avec avoir avons ayant

bas basee bat

c ça car ce ceci cela celle celle-ci celle-la celle-là celles celles-ci celles-la celles-là
celui celui-ci celui-la celui-là cent cependant certain certaine certaines certains certes ces
cet cette ceux ceux-ci ceux-là chacun chacune chaque chez ci cinq cinquantaine cinquante
cinquantième cinquième combien comme comment compris concernant

d da dans de debout dedans dehors deja dejà delà depuis derriere
derrière des desormais desquelles desquels dessous dessus deux deuxième
deuxièmement devant devers devra different differente differentes differents différent
différente différentes différents dire directe directement dit dite dits divers
diverse diverses dix dix-huit dix-neuf dix-sept dixième doit doivent donc dont
douze douzième du duquel durant dès déja déjà désormais

effet egalement eh elle elle-meme elle-même elles elles-memes elles-mêmes en encore
enfin entre envers environ es ès est et etaient étaient etais étais etait était
etant étant etc etre être eu eux eux-mêmes exactement excepté également

fais faisaient faisant fait facon façon feront font

gens

ha hem hep hi ho hormis hors hou houp hue hui huit huitième
hé i il ils importe

j je jusqu jusque juste

l la laisser laquelle le lequel les lesquelles lesquels leur leurs longtemps
lors lorsque lui lui-meme lui-même là lès

m ma maint maintenant mais malgre malgré me meme memes merci mes mien
mienne miennes miens mille moi moi-meme moi-même moindres moins
mon même mêmes

n na ne neanmoins neuvième ni nombreuses nombreux nos notamment
notre nous nous-mêmes nouveau nul néanmoins nôtre nôtres

o ô on ont onze onzième or ou ouias ouste outre
ouvert ouverte ouverts où

par parce parfois parle parlent parler parmi partant
pas pendant pense permet personne peu peut peuvent peux plus
plusieurs plutot plutôt possible possibles pour pourquoi
pourrais pourrait pouvait prealable precisement
premier première premièrement
pres procedant proche près préalable précisement pu puis puisque

qu quand quant quant-à-soi quarante quatorze quatre quatre-vingt
quatrième quatrièmement que quel quelconque quelle quelles quelqu'un quelque
quelques quels qui quiconque quinze quoi quoique

relative relativement rend rendre restant reste
restent retour revoici revoila revoilà

s sa sait sans sauf se seize selon semblable semblaient
semble semblent sent sept septième sera seraient serait seront ses seul seule
seulement seuls seules si sien sienne siennes siens sinon six sixième soi soi-meme soi-même soit
soixante son sont sous souvent specifique specifiques spécifique spécifiques stop
suffisant suffisante suffit suis suit suivant suivante
suivantes suivants suivre sur surtout

t ta tant te tel telle tellement telles tels tenant tend tenir tente
tes tien tienne tiennes tiens toi toi-meme toi-même ton touchant toujours tous
tout toute toutes treize trente tres trois troisième troisièmement très
tu té

un une unes uns

va vais vas vers via vingt voici voila voilà vont vos
votre votres vous vous-mêmes vu vé vôtre vôtres

y
".Split(new char[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries),
            StringComparer.OrdinalIgnoreCase
        );

        static French()
        {
            // French contractions and elisions
            var contractions = new[] { "c'", "d'", "j'", "l'", "m'", "n'", "qu'", "s'", "t'" };

            // Add contractions to stop words
            foreach (var contraction in contractions)
            {
                StopWords.Add(contraction);
            }

            // Add contractions with different apostrophes
            var apostrophes = new[] { "'", "'" };
            foreach (var apostrophe in apostrophes)
            {
                foreach (var contraction in contractions)
                {
                    StopWords.Add(contraction.Replace("'", apostrophe));
                }
            }
        }
    }
}
