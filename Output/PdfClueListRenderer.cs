using CrosswordMaker.Grids;
using Pdf;

namespace CrosswordMaker.Output;

class PdfClueListRenderer
{
    public enum ClueDirection { Across, Down }
    internal ClueDirection Direction { get; }

    internal List<NumberedWord> Words = new();
    internal Dictionary<string, string> Clues = new();

    internal bool IncludeAnswers;

    public PdfFont? HeadingFont { get; set; }
    public PdfFont? ClueFont { get; set; }
    public PdfFont? AnswerFont { get; set; }

    // all dimensions in PDF are in points (1/72 in)

    public const float HeadingTextSize = 11f;
    public const float ClueTextSize = 11f;
    public const float ClueLeading = 15f;
    public const float ClueHorizontalSeparation = ClueTextSize * 0.5f;
    public const float ClueIndent = ClueTextSize * 1.5f + ClueHorizontalSeparation;

    public PdfClueListRenderer(ClueDirection dir)
    {
        Direction = dir;
    }

    string GetClueString(NumberedWord word)
    {
        string clue = Clues[word.Word];
        if (IncludeAnswers)
            clue += $" ({word.Word})";
        return clue;
    }

    int NumberOfLines(string clue, float width)
    {
        int lines = 1;

        float lineWidth = 0f;
        foreach (var word in clue.Split())
        {
            float wordWidth = word.Length * ClueTextSize * 0.660f;
            lineWidth += wordWidth;
            if (lineWidth > width)
            {
                ++lines;
                lineWidth = width;
            }
        }

        return lines;
    }

    /// <summary>
    /// Calculate the height of this clue list, if bound to the specified width.
    /// NB: This operation only estimates. (Hey, it's faster than the alternative!)
    /// </summary>
    /// <param name="width">Available width for the clue list, in PDF points.</param>
    /// <returns>Calculated height of the clue list, in PDF points.</returns>
    public float CalculateHeight(float width)
    {
        width -= ClueIndent;
        return (Words.Sum(w => NumberOfLines(GetClueString(w), width)) + 2) * ClueLeading;
    }

    /// <summary>
    /// Render the clue list to the <see cref="PdfPage"/> within the specified <c>Rectangle</c>.
    /// <returns>Bottom boundary of the clue list, in PDF points.</returns>
    /// </summary>
    public float RenderClueList(PdfPage page, Rectangle bounds)
    {
        // Console.WriteLine($"RenderClueList({bounds})");
        page.AddText(bounds.Left, bounds.Top - ClueLeading, Direction == ClueDirection.Across ? "Across" : "Down", HeadingFont!, HeadingTextSize);

        float y = bounds.Top - 3*ClueLeading;
        float width = bounds.Width - ClueIndent;

        foreach (var word in Words)
        {
            page.AddText(bounds.Left, y, word.Number.ToString(), ClueFont!, ClueTextSize);

            string clue = GetClueString(word);
            float lineWidth = 0f;
            StringBuilder line = new();
            foreach (var w in clue.Split())
            {
                float wordWidth = w.Length * ClueTextSize * 0.660f;
                lineWidth += wordWidth;
                if (lineWidth <= width)
                {
                    line.Append(w);
                    line.Append(' ');
                }
                else
                {
                    page.AddText(bounds.Left + ClueIndent, y, line.ToString(), ClueFont!, ClueTextSize);
                    y -= ClueLeading;
                    lineWidth = width;
                    line = new();
                    line.Append(w);
                    line.Append(' ');
                }
            }

            page.AddText(bounds.Left + ClueIndent, y, line.ToString(), ClueFont!, ClueTextSize);
            y -= ClueLeading;
        }

        page.ClosePath(stroke: false, fill: true);

        return y;
    }

    private bool _includeAnswers;

}
