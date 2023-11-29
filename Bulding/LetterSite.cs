using CrosswordMaker.Grids;

namespace CrosswordMaker.Building;

class LetterSite
{
    public readonly string word;
    public readonly int site;

    public LetterSite(string use_word, int use_site)
    {
        word = use_word;
        site = use_site;
    }

    public WordPosition StartOfWord(WordPosition where)
    {
        if (where.Direction == WordPosition.WordDirection.Across)
            return new WordPosition(where.X - site, where.Y, where.Direction);
        else
            return new WordPosition(where.X, where.Y - site, where.Direction);
    }
}
