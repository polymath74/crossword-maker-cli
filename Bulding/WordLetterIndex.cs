namespace CrosswordMaker.Building;

class WordLetterIndex
{
    readonly HashSet<string> words;
    readonly List<LetterSite>[] letters;

    public WordLetterIndex()
    {
        words = new();
        letters = new List<LetterSite>[26];
        for (int cx = 0; cx < 26; ++cx)
            letters[cx] = new();
    }

    public WordLetterIndex(string newWord)
        : this()
    {
        Add(newWord);
    }

    public WordLetterIndex(IEnumerable<string> newWords)
        : this()
    {
        Add(newWords);
    }

    public int Count => words.Count;

    public bool Contains(string word)
        => words.Contains(word);

    public void Add(string word)
    {
        if (words.Add(word))
        {
            for (int cx = 0; cx < word.Length; ++cx)
                letters[word[cx] - 'A'].Add(new LetterSite(word, cx));
        }
    }

    public void Add(IEnumerable<string> newWords)
    {
        foreach (string wd in newWords)
            Add(wd);
    }

    public void Remove(string word)
    {
        words.Remove(word);
        for (int cx = 0; cx < 26; ++cx)
            letters[cx].RemoveAll(site => site.word == word);
    }

    public void Clear()
    {
        words.Clear();
        for (int cx = 0; cx < 26; ++cx)
            letters[cx].Clear();
    }

    public IEnumerable<string> GetWords()
        => words;

    public IEnumerable<LetterSite> GetSites(char letter)
        => letters[letter - 'A'];

    public IEnumerable<char> GetUsedLetters()
    {
        for (int cx = 0; cx < 26; ++cx)
            if (letters[cx].Count > 0)
                yield return (char)(cx + 'A');
    }
}
