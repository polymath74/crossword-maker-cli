using CrosswordMaker.Grids;

namespace CrosswordMaker.Building;

abstract class GridBuilder
{
    public WordBoard Board { get; }
    public LetterScores LetterScores { get; }

    public GridBuilder(LetterScores letterScorer)
    {
        Board = new();
        LetterScores = letterScorer;
    }

    public abstract void AddWords(IEnumerable<string> words, CancellationToken cancel);

    protected void PlaceFirstWord(string wd)
    {
        Board.Place(wd, new WordPosition((WordBoard.MaxSize - wd.Length) / 2, WordBoard.MaxSize / 2, WordPosition.WordDirection.Across));
    }

    protected virtual WordPlacement? FindBestPlacement(WordLetterIndex availableLetters)
    {
        WordPlacement? best = null;

        foreach (char ch in availableLetters.GetUsedLetters())
        {
            List<WordPosition> anchors = new(Board.GetPositionsOfLetter(ch));
            if (anchors.Count > 0)
                foreach (LetterSite site in availableLetters.GetSites(ch))
                    foreach (WordPosition where in anchors)
                    {
                        WordPosition fromStart = site.StartOfWord(where);
                        if (Board.CanPlace(site.word, fromStart, out int overlaps))
                        {
                            int newScore = PlacementScore(site.word, fromStart);
                            int diff;
                            bool copyNew = false;
                            if (best == null)
                                copyNew = true;
                            else if ((diff = newScore - best.Score) != 0)
                                copyNew = diff > 0;
                            else if ((diff = site.word.Length - best.Word.Length) != 0)
                                copyNew = diff > 0;
                            if (copyNew)
                                best = new WordPlacement(site.word, fromStart, overlaps, newScore);
                        }
                    }
        }

        return best;
    }

    protected virtual int PlacementScore(string word, WordPosition where)
    {
        // this method *assumes* CanPlace(word, where) is true
        // (ie. it does not check, but if false then the resulting score is meaningless)

        int score = 0;
        if (LetterScores != null)
            score = LetterScores.Score(word);
        int overlaps = 0;
        int wordCentreX, wordCentreY;

        if (where.Direction == WordPosition.WordDirection.Across)
        {
            for (int ix = 0; ix < word.Length; ++ix)
            {
                if (Board.LetterAt(where.X + ix, where.Y) != ' ')
                    ++overlaps;
                else
                    ++score;
            }
            if (where.X < Board.Left)
                score -= Board.Left - where.X;
            if (where.X + word.Length > Board.Right)
                score -= where.X + word.Length - Board.Right;
            wordCentreX = (where.X + word.Length) / 2;
            wordCentreY = where.Y;
        }
        else
        {
            for (int iy = 0; iy < word.Length; ++iy)
            {
                if (Board.LetterAt(where.X, where.Y + iy) != ' ')
                    ++overlaps;
                else
                    ++score;
            }
            if (where.Y < Board.Top)
                score -= Board.Top - where.Y;
            if (where.Y + word.Length > Board.Bottom)
                score -= where.Y + word.Length - Board.Bottom;
            wordCentreX = where.X;
            wordCentreY = (where.Y + word.Length) / 2;
        }

        score -= 1 * (int)Math.Floor(Math.Sqrt(Math.Pow(wordCentreX - Board.CentroidX, 2) + Math.Pow(wordCentreY - Board.CentroidY, 2)));

        return score + 100 * (overlaps - 1);
    }

}
