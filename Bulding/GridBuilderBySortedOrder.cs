namespace CrosswordMaker.Building;

class GridBuilderBySortedOrder : GridBuilderByGivenWordOrder
{
    readonly Comparison<string> comparison;

    public GridBuilderBySortedOrder(LetterScores letterScorer, Comparison<string> use_comparison)
    : base(letterScorer)
    {
        comparison = use_comparison;
    }

    public override void AddWords(IEnumerable<string> words, CancellationToken cancel)
    {
        List<string> permute = new(words);
        permute.Sort(comparison);
        //Debug.WriteLine(string.Join(" ", permute));
        cancel.ThrowIfCancellationRequested();
        base.AddWords(permute, cancel);
    }
}
