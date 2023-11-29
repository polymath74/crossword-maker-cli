using CrosswordMaker.Grids;

namespace CrosswordMaker.Building;

class WordPlacement
{
    public readonly string Word;
    public readonly WordPosition Where;
    public readonly int Overlaps;
    public readonly int Score;

    public WordPlacement(string use_word, WordPosition use_where, int use_overlaps, int use_score)
    {
        Word = use_word;
        Where = use_where;
        Overlaps = use_overlaps;
        Score = use_score;
    }

    override public string ToString()
    {
        return $"<{Word}@{Where}={Score}>";
    }

    public override bool Equals(object? obj)
    {
        return obj is WordPlacement placement &&
                Word == placement.Word &&
                Where.Equals(placement.Where);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Word, Where);
    }
}
