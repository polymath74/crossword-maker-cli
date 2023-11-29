namespace CrosswordMaker.Building;

/// <summary>
/// Track frequency of each letter across all the chosen words.
/// Assign each letter a score inversely related to its frequency.
/// </summary>
public class FrequencyLetterScores : LetterScores
{
    /// <summary>
    /// Count the letters in the provided <c>word</c> and assign a frequency-based score to each letter.
    /// </summary>
    /// <param name="word">Word containing the letters to count.</param>
    override public void Add(string word)
    {
        for (int cx = word.Length - 1; cx >= 0; --cx)
        {
            int freq = ++letterFrequencies[word[cx] - 'A'];
            if (freq > maxFrequency)
                maxFrequency = freq;
        }
    }

    /// <summary>
    /// Calculate the score for the provided letter.
    /// </summary>
    /// <param name="ch">Letter to score.</param>
    /// <returns>Score for this letter, or 0 if the letter is not present in the added words.</returns>
    override public int Score(char ch)
    {
        if (maxFrequency == 0)
            throw new InvalidOperationException("Add some words first");

        int freq = letterFrequencies[ch - 'A'];
        if (freq == 0)
            return 0;
        else
            return (int)Math.Floor(Math.Pow(MaxScore, (maxFrequency + 1 - freq) / (double)maxFrequency));
    }

    private readonly int[] letterFrequencies = new int[26];
    private int maxFrequency = 0;
}
