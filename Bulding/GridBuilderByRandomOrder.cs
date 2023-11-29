namespace CrosswordMaker.Building;

class GridBuilderByRandomOrder : GridBuilderByGivenWordOrder
{
    readonly Random random;

    public GridBuilderByRandomOrder(LetterScores letterScorer, Random use_random)
    : base(letterScorer)
    {
        random = use_random;
    }

    void Shuffle<T>(IList<T> list)  
    {  
        int n = list.Count;  
        while (n > 1)
        {
            --n;
            int k = random.Next(n + 1);
            (list[n], list[k]) = (list[k], list[n]);
        }
    }

    public override void AddWords(IEnumerable<string> words, CancellationToken cancel)
    {
        List<string> order = new(words);
        Shuffle(order);
        cancel.ThrowIfCancellationRequested();

        base.AddWords(order, cancel);
    }

}
