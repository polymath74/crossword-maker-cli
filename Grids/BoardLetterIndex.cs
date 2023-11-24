namespace CrosswordMaker.Grids;

class BoardLetterIndex
{
    private readonly List<WordPosition>[] positions;

    public BoardLetterIndex()
    {
        positions = new List<WordPosition>[26];
        for (int ci = 0; ci < 26; ++ci)
            positions[ci] = new List<WordPosition>();
    }

    public void Add(char letter, WordPosition where)
    {
        positions[letter - 'A'].Add(where);
    }

    public void Remove(char letter, WordPosition where)
    {
        positions[letter - 'A'].Remove(where);
    }

    public IEnumerable<WordPosition> GetPositionsOf(char letter)
        => positions[letter - 'A'];

    public bool IsAvailable(char letter, WordPosition where)
        => positions[letter - 'A'].Contains(where);

    public void Clear()
    {
        for (int ci = 0; ci < 26; ++ci)
            positions[ci].Clear();
    }
}
