using CrosswordMaker.Grids;
using Pdf;

namespace CrosswordMaker.Output;

class PdfCrosswordRenderer
{
    public WordBoard Board
    {
        get => _board!;
        set { _board = value; isScaled = false; }
    }

    public PdfPage? Page { get; set; }
    public PdfFont? Font { get; set; }
    public Rectangle FitBounds { get; set; }

    // all dimensions in PDF are in points (1/72 in)

    public const float TitleFontSize = 14f;
    public const float TitleSpace = 25f;

    public const float BoxThickness = 1.4f;
    public const float MinSquareSize = 16f;
    public const float StdSquareSize = 21f;
    public float SquareSize { get; private set; }

    public const float StdLetterSize = 14f;
    public const float StdClueNumberSize = 7f;
    public float LetterSize { get; private set; }
    public float ClueNumberSize { get; private set; }
    public const float ClueNumberOffsetLeft = 1.5f;
    public float ClueNumberOffsetTop => ClueNumberSize;

    public float RenderLeft { get; private set; }
    public float RenderTop { get; private set; }
    public float RenderWidth => Board.Width * SquareSize;
    public float RenderHeight => Board.Height * SquareSize;
    public float RenderBottom => RenderTop - RenderHeight;
    public float RenderRight => RenderLeft + RenderWidth;

    public enum CrosswordPosition { Top, Left, Whole };
    public CrosswordPosition Position { get; set; } = CrosswordPosition.Whole;

    public string Title { get; set; } = string.Empty;

    public bool DrawSolution { get; set; } = false;

    private WordBoard? _board;
    private bool isScaled = false;

    /// <summary>
    /// Scale the crossword to fit within the provided <c>FitBounds</c> <see cref="Rectangle"/> on the page.
    /// This operation must be called before calling RenderCrossword.
    /// </summary>
    /// <returns><c>true</c> if the crossword will fit within the <c>FitBounds</c>.
    /// <c>false</c> if the crossword cannot be scaled small enough to fit.</returns>
    public bool FitCrossword()
    {
        bool fits = true;

        float scale = Math.Min(FitScale(Board.Width, FitBounds.Width), FitScale(Board.Height, FitBounds.Height - TitleBuffer));
        if (scale * StdSquareSize < MinSquareSize)
        {
            scale = MinSquareSize / StdSquareSize;
            fits = false;
        }

        SquareSize = StdSquareSize * scale;
        LetterSize = StdLetterSize * scale;
        ClueNumberSize = StdClueNumberSize * scale;

        RenderTop = FitBounds.Top - TitleBuffer;
        RenderLeft = Position == CrosswordPosition.Left ? (FitBounds.Left + BoxThickness / 2f) : ((FitBounds.Left + FitBounds.Right - RenderWidth) / 2f);

        isScaled = true;
        return fits;
    }

    /// <summary>
    /// Draw the crossword grid on the <c>Page</c> within the <c>FitBounds</c> <see cref="Rectangle"/>,
    /// as scaled by the most recent call to <see cref="FitCrossword"/>.
    /// </summary>
    public void RenderCrossword()
    {
        if (!isScaled)
            throw new InvalidOperationException($"Call {nameof(FitCrossword)} before calling {nameof(RenderCrossword)}");

        if (Page == null)
            throw new InvalidOperationException("Page needed before rendering can begin");

        if (!string.IsNullOrEmpty(Title))
            DrawTitle();

        FillBlacks();
        DrawLetterSquares();
        AddClueNumbers();
    }

    private float FitScale(int numSquares, float pointsAvail)
    {
        float natural = numSquares * StdSquareSize + BoxThickness;
        if (natural <= pointsAvail)
            return 1f;
        else
            return pointsAvail / natural;
    }

    private float TitleBuffer => string.IsNullOrEmpty(Title) ? 0f : TitleSpace;

    private void DrawTitle()
    {
        float y = RenderTop + TitleSpace;
        float x = RenderLeft + RenderWidth/2f - Title.Length*0.330f; // approximately half the title width

        Page!.AddText(x, y, Title, Font!, TitleFontSize);
        Page.ClosePath(stroke: false, fill: true);
    }

    private void DrawLetterSquares()
    {
        Page!.LineWidth(BoxThickness);

        for (int y = Board.Top; y <= Board.Bottom; ++y)
            for (int x = Board.Left; x <= Board.Right; ++x)
            {
                char ch = Board.LetterAt(x, y);
                if (ch != ' ')
                {
                    var square = GetSquareRect(x, y);
                    Page.AddRectangle(square);
                }
            }

        Page.ClosePath(stroke: true, fill: false);

        if (!DrawSolution)
            return;

        for (int y = Board.Top; y <= Board.Bottom; ++y)
            for (int x = Board.Left; x <= Board.Right; ++x)
            {
                char ch = Board.LetterAt(x, y);
                if (ch != ' ')
                {
                    var square = GetSquareRect(x, y);

                    float wd = LetterSize*0.330f; //Font!.Width(ch, LetterSize);
                    float ds = LetterSize*0.2f; // Font.GetDescent(ch, LetterSize);
                    float ht = LetterSize; // Font.GetAscent(ch, LetterSize) + ds;

                    Page.AddText(square.CentreX - wd, square.Bottom + ds, ch.ToString(), Font!, LetterSize);
                }
            }

        Page.ClosePath(stroke: false, fill: true);
            
    }

    private void FillBlacks()
    {
        // pdfCanvas!.SaveState()
        //     .SetFillColor(ColorConstants.DARK_GRAY);

        bool any = false;

        for (int y = Board.Top; y <= Board.Bottom; ++y)
            for (int x = Board.Left; x <= Board.Right; ++x)
                if (Board.IsBlackSquare(x, y))
                {
                    any = true;
                    Page!.AddRectangle(GetSquareRect(x, y));
                }

        if (any)
            Page!.ClosePath(stroke: false, fill: true);
    }

    private void AddClueNumbers()
    {
        foreach (var clue in Board.GetClueLocations())
        {
            // Console.WriteLine($"({clue.Number} @ {clue.X},{clue.Y})");

            var square = GetSquareRect(clue.X, clue.Y);
            Page!.AddText(square.Left + ClueNumberOffsetLeft, square.Top - ClueNumberOffsetTop, clue.Number.ToString(), Font!, ClueNumberSize);
        }

        Page!.ClosePath(stroke: false, fill: true);
    }

    private Rectangle GetSquareRect(int x, int y)
    {
        return Rectangle.FromSize(RenderLeft + GetSquareLeft(x), RenderBottom + GetSquareBottom(y), (float)SquareSize, (float)SquareSize);
    }

    private float GetSquareLeft(int x) => (x - Board.Left) * SquareSize + BoxThickness / 2f;
    private float GetSquareBottom(int y) => (Board.Bottom - y) * SquareSize + BoxThickness / 2f;


}
