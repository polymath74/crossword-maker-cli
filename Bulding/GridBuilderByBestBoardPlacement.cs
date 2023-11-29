using CrosswordMaker.Grids;

namespace CrosswordMaker.Building;

class GridBuilderByBestBoardPlacement : GridBuilder
{
    public GridBuilderByBestBoardPlacement(LetterScores letterScorer)
    : base(letterScorer)
    {
    }

    override public void AddWords(IEnumerable<string> words, CancellationToken cancel)
    {
        HashSet<string> curWords = new(words);
        if (curWords.Count == 0)
            return;

        string first = ChooseFirstWord(curWords)!;
        PlaceFirstWord(first);
        curWords.Remove(first);

        if (curWords.Count == 0)
            return;
        
        WordLetterIndex availableLetters = new(curWords);

        while (curWords.Count > 0)
        {
            cancel.ThrowIfCancellationRequested();

            WordPlacement? best = FindBestPlacement(availableLetters);
            if (best == null)
                break;
            //Debug.WriteLine($"Placing {best.word} at {best.where}");
            Board.Place(best.Word, best.Where);
            curWords.Remove(best.Word);
            availableLetters.Remove(best.Word);
        }
    }

    private string? ChooseFirstWord(HashSet<string> words)
    {
        string? best = null;
        int score = int.MinValue;
        foreach (var wd in words)
        {
            bool copyNew = false;
            if (best == null)
                copyNew = true;
            else if (wd.Length > best.Length)
                copyNew = true;
            else if (LetterScores.Score(wd) > score)
                copyNew = true;
            if (copyNew)
            {
                best = wd;
                score = LetterScores.Score(wd);
            }
        }

        return best;
    }

}
