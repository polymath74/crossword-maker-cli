namespace CrosswordMaker.Grids;

readonly struct NumberedWord
{
    public readonly int Number;
    public readonly string Word;

    public NumberedWord(int number, string word)
    {
        Number = number;
        Word = word;
    }
}

