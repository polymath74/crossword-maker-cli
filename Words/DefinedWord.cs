namespace CrosswordMaker.Words;

record DefinedWord
{
    public readonly string Word;
    public readonly string Clue;

    public DefinedWord(string word, string clue)
    {
        Word = word;
        Clue = clue;
    }

    public override string ToString()
    {
        return $"{Word}={Clue}";
    }
}