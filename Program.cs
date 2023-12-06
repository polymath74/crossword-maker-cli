using Mono.Options;

using CrosswordMaker.Files;
using CrosswordMaker.Generator;
using CrosswordMaker.Grids;
using CrosswordMaker.Output;

namespace CrosswordMaker;
class Program
{
    readonly static Dictionary<string, string> allWords = new();

    static void LoadWords(string path)
    {
        var fileWords = WordListFile.LoadWords(path);
        Console.WriteLine($"{path}: {fileWords.Count} words");

        int skipped = 0;
        foreach (var word in fileWords) {
            if (!allWords.ContainsKey(word.Word))
                allWords[word.Word] = word.Clue;
            else
                ++skipped;
        }
        if (skipped > 0)
            Console.WriteLine($"skipped {skipped} previously loaded");
    }

    static async Task Main(string[] args)
    {
        string? title = null;
        string? crosswordPath = null;
        string? solutionPath = null;
        bool showHelp = false;

        var opts = new OptionSet() {
            { "t|title=", "Crossword title (displayed above the grid)",
                v => title = v },
            { "c|crossword=", "Path for crossword output (PDF)",
                v => crosswordPath = v },
            { "s|solution=", "Path for solution output (PDF)",
                v => solutionPath = v },
            { "w|words=", "Path of word list text file", 
                v => LoadWords(v) },
            { "h|help", "Show help",
                v => showHelp = true },
        };

        try
        {
            var rest = opts.Parse(args);
            if (rest != null)
                foreach (string v in rest)
                    LoadWords(v);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return;
        }

        if (showHelp || allWords.Count == 0)
        {
            Console.WriteLine("Options:");
            opts.WriteOptionDescriptions(Console.Out);
            return;
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

            board.GetNumberedWords(out var across, out var down);

            Console.WriteLine("Writing PDF");

            var pdfHelper = new PdfOutputHelper(board, allWords);

            pdfHelper.Title = title ?? string.Empty;

            if (crosswordPath == null && solutionPath == null)
            {
                crosswordPath = "crossword.pdf";
                solutionPath = "solution.pdf";
            }

            if (crosswordPath != null)
            {
                pdfHelper.RenderSolution = false;
                pdfHelper.WritePdf(crosswordPath);
            }

            if (solutionPath != null)
            {
                pdfHelper.RenderSolution = true;
                pdfHelper.WritePdf(solutionPath);
            }

        }
        else
        {
            Console.WriteLine("No suitable crosswords generated. Sorry.");
        }
    }
}
