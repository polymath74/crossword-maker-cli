// using Mono.Options;

using CrosswordMaker.Files;
using CrosswordMaker.Generator;
using CrosswordMaker.Grids;
using CrosswordMaker.Words;

namespace CrosswordMaker;
class Program
{
    static async Task Main(string[] args)
    {
        // var opts = new OptionSet() {
        //     { "" }
        // };

        Dictionary<string, DefinedWord> allWords = new();

        foreach (var path in args) {
            var fileWords = await WordListFile.LoadWordsAsync(path);
            Console.WriteLine($"{path}: {fileWords.Count} words");

            int skipped = 0;
            foreach (var word in fileWords) {
                if (!allWords.ContainsKey(word.Word))
                    allWords[word.Word] = word;
                else
                    ++skipped;
            }
            if (skipped > 0)
                Console.WriteLine($"skipped {skipped} previously loaded");
        }

        CancellationTokenSource tokenSource = new();
        Console.CancelKeyPress += (s, e) => {
            Console.WriteLine("Stopping...");
            tokenSource.Cancel();
            e.Cancel = true;
        };

        GridGenerator generator = new();
        Console.WriteLine("Generating grids... (this may take a while)");
        await generator.GenerateGridsAsync(allWords.Keys, tokenSource.Token);

        WordBoard? board = generator.BestGenerated();
        if (board != null)
        {
            Console.WriteLine();
            Console.WriteLine(board);
        }
        else
        {
            Console.WriteLine("No suitable crosswords generated. Sorry.");
        }
    }
}
