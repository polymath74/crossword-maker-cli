using CrosswordMaker.Grids;

namespace CrosswordMaker.Building;

class GridBuilderByGivenWordOrder : GridBuilder
{
    public GridBuilderByGivenWordOrder(LetterScores letterScorer)
    : base(letterScorer)
    {
    }

    override public void AddWords(IEnumerable<string> words, CancellationToken cancel)
    {
        List<string> remaining = new(words);
        if (remaining.Count == 0)
            return;

        PlaceFirstWord(remaining[0]);
        remaining.RemoveAt(0);
        if (remaining.Count == 0)
            return;

        List<WordLetterIndex> index = new(remaining.Select(wd => new WordLetterIndex(wd)));

        bool changed = true;
        while (changed && remaining.Count > 0)
        {
            cancel.ThrowIfCancellationRequested();

            int wx = 0;
            changed = false;
            while (wx < remaining.Count)
            {
                WordPlacement? best = FindBestPlacement(index[wx]);
                if (best == null)
                {
                    ++wx;
                }
                else
                {
                    //Debug.WriteLine($"Placing {node.Value.Item1} at {best.where}");
                    Board.Place(remaining[wx], best.Where);
                    remaining.RemoveAt(wx);
                    index.RemoveAt(wx);
                    changed = true;
                }
            }
        }
    }

}
