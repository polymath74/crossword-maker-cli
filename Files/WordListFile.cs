using CrosswordMaker.Words;

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

    public static async Task<List<DefinedWord>> LoadWordsAsync(string path)
    {
        List<DefinedWord> words = new();

        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous | FileOptions.SequentialScan);
        using var reader = new StreamReader(stream);
        
        string? line;
        while ((line = await reader.ReadLineAsync()) != null)
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