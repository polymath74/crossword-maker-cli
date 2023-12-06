// using Mono.Options;

using CrosswordMaker.Files;
using CrosswordMaker.Generator;
using CrosswordMaker.Grids;
using CrosswordMaker.Output;

namespace CrosswordMaker;
class Program
{
    static async Task Main(string[] args)
    {
        // var opts = new OptionSet() {
        //     { "" }
        // };

        Console.WriteLine(Directory.GetCurrentDirectory());

        string title = args[0];

        Dictionary<string, string> allWords = new();

        foreach (var path in args[1..]) {
            var fileWords = await WordListFile.LoadWordsAsync(path);
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

            pdfHelper.Title = title;

            pdfHelper.RenderSolution = false;
            pdfHelper.WritePdf("crossword.pdf");

            pdfHelper.RenderSolution = true;
            pdfHelper.WritePdf("solution.pdf");

            // var doc = new PdfDocument("crossword.pdf");
            // doc.Begin();
            // var page = new PdfPage(PdfPage.A4);
            // page.AddRectangle(Rectangle.FromSize(20f, 20f, 30f, 40f));
            // page.AddText(new Point(60f, 70f), "Hello", doc.GetFont(PdfFont.Helvetica), 12f);
            // page.ClosePath();
            // doc.AddPage(page);
            // doc.End();

        }
        else
        {
            Console.WriteLine("No suitable crosswords generated. Sorry.");
        }
    }
}
