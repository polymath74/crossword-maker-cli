namespace CrosswordMaker.Files;

static class WordListFile
{
    static string ToUppercaseLetters(string word)
    {
        StringBuilder sb = new();
        foreach (char ch in word)
        {
            if (ch >= 'A' && ch <= 'Z')
                sb.Append(ch);
            else if (ch >= 'a' && ch <= 'z')
                sb.Append(char.ToUpper(ch));
        }
        return sb.ToString();
    }

    public record DefinedWord
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

    public static List<DefinedWord> LoadWords(string path)
    {
        List<DefinedWord> words = new();

        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous | FileOptions.SequentialScan);
        using var reader = new StreamReader(stream);
        
        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            int cx = line.IndexOf('=');
            if (cx < 2)
                continue;

            string wd = ToUppercaseLetters(line[..cx]);
            if (wd.Length < 2)
                continue;

            words.Add(new(wd, line[(cx + 1)..]));
        }

        return words;
    }

}