namespace CrosswordMaker.Building;

/// <summary>
/// Assign scores to letters based on some rule.
/// </summary>
public abstract class LetterScores 
{
    public const int MaxScore = 10;
    public const int MinScore = 0;

    /// <summary>
    /// Assign scores to the letters in the provided <c>words</c>.
    /// </summary>
    /// <param name="words">Words containing the letters to account for.</param>
    public void Add(IEnumerable<string> words)
    {
        foreach (string wd in words)
            Add(wd);
    }

    /// <summary>
    /// Assign a score to each letter in the provided <c>word</c>.
    /// </summary>
    /// <param name="word">Word containing the letters to account for.</param>
    public abstract void Add(string word);

    /// <summary>
    /// Calculate the score for the provided letter.
    /// </summary>
    /// <param name="ch">Letter to score.</param>
    /// <returns>Score for this letter, or 0 if the letter is not present in the added words.</returns>
    public abstract int Score(char ch);

    /// <summary>
    /// Calculate the score for the provided word.
    /// </summary>
    /// <param name="word">Word to score.</param>
    /// <returns>Score for this word, calculated by summing the scores for the individual letters in the word.</returns>
    public int Score(string word)
    {
        int sc = 0;
        for (int cx = word.Length - 1; cx >= 0; --cx)
            sc += Score(word[cx]);
        return sc;
    }
}
