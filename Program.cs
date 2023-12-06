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

        Console.WriteLine(Directory.GetCurrentDirectory());

        string title = args[0];

        Dictionary<string, DefinedWord> allWords = new();

        foreach (var path in args[1..]) {
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

            board.GetNumberedWords(out var across, out var down);

            Console.WriteLine("Writing PDF");

            /*
            var pdfHelper = new PdfOutputHelper(board,
                across.Select(nw => new NumberedClueWord(nw.Number, nw.Word, allWords[nw.Word].Clue)),
                down.Select(nw => new NumberedClueWord(nw.Number, nw.Word, allWords[nw.Word].Clue)));
            pdfHelper.Initialize();

            pdfHelper.Title = title;

            pdfHelper.RenderSolution = false;
            using (var cs = new FileStream("crossword.pdf", FileMode.Create))
            {
                pdfHelper.ChooseLayout();
                pdfHelper.WritePdf(cs);
            }

            pdfHelper.RenderSolution = true;
            using (var ss = new FileStream("solution.pdf", FileMode.Create))
            {
                pdfHelper.ChooseLayout();
                pdfHelper.WritePdf(ss);
            }
            */

            var doc = new PdfDocument("crossword.pdf");
            doc.Begin();
            var page = new PdfPage(PdfPage.A4);
            page.AddRectangle(Rectangle.FromSize(20f, 20f, 30f, 40f));
            page.AddText(new Point(60f, 70f), "Hello", doc.GetFont(PdfFont.Helvetica), 12f);
            page.ClosePath();
            doc.AddPage(page);
            doc.End();

        }
        else
        {
            Console.WriteLine("No suitable crosswords generated. Sorry.");
        }
    }
}
