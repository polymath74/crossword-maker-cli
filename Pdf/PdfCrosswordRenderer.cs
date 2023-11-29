using CrosswordMaker.Grids;
using iText.IO.Font;
using iText.IO.Font.Constants;
using iText.Kernel.Colors;
using iText.Kernel.Font;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using static iText.Kernel.Pdf.Canvas.PdfCanvasConstants;

namespace CrosswordMaker.Pdf;

class PdfCrosswordRenderer
{
    public WordBoard Board
    {
        get => _board!;
        set { _board = value; isScaled = false; }
    }

    public PdfPage? Page { get; set; }
    public PdfFont? Font { get; set; }
    public Rectangle? FitBounds { get; set; }

    private PdfCanvas? pdfCanvas;

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

        float scale = Math.Min(FitScale(Board.Width, FitBounds!.GetWidth()), FitScale(Board.Height, FitBounds.GetHeight() - TitleBuffer));
        if (scale * StdSquareSize < MinSquareSize)
        {
            scale = MinSquareSize / StdSquareSize;
            fits = false;
        }

        SquareSize = StdSquareSize * scale;
        LetterSize = StdLetterSize * scale;
        ClueNumberSize = StdClueNumberSize * scale;

        RenderTop = FitBounds.GetTop() - TitleBuffer;
        RenderLeft = Position == CrosswordPosition.Left ? (FitBounds.GetLeft() + BoxThickness / 2f) : ((FitBounds.GetLeft() + FitBounds.GetRight() - RenderWidth) / 2f);

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
        pdfCanvas = new PdfCanvas(Page);

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
        Text text = new Text(Title)
            .SetFont(Font)
            .SetFontSize(TitleFontSize)
            .SetUnderline();
        Paragraph paragraph = new Paragraph(text)
            .SetFixedLeading(0f)
            .SetTextAlignment(TextAlignment.CENTER);

        Rectangle rect = new Rectangle(FitBounds);
        if (Position == CrosswordPosition.Left)
            rect.SetWidth(RenderWidth);
        rect.SetHeight(TitleBuffer);
        rect.SetY(RenderTop);
        Canvas canvas = new Canvas(Page, rect);
        canvas.Add(paragraph)
            .Close();
    }

    private void DrawLetterSquares()
    {
        pdfCanvas!.SaveState()
            .SetStrokeColor(ColorConstants.BLACK)
            .SetLineWidth(BoxThickness)
            .SetLineJoinStyle(LineJoinStyle.MITER)
            .SetFillColor(ColorConstants.BLACK);

        if (DrawSolution)
            pdfCanvas.SetFontAndSize(Font, LetterSize);

        for (int y = Board.Top; y <= Board.Bottom; ++y)
            for (int x = Board.Left; x <= Board.Right; ++x)
            {
                char ch = Board.LetterAt(x, y);
                if (ch != ' ')
                {
                    var square = GetSquareRect(x, y);
                    pdfCanvas.Rectangle(square).Stroke();

                    if (DrawSolution)
                    {
                        float wd = Font!.GetWidth(ch, LetterSize);
                        float ds = Font.GetDescent(ch, LetterSize);
                        float ht = Font.GetAscent(ch, LetterSize) + ds;
                        pdfCanvas.BeginText()
                            .SetTextMatrix(square.GetCentreX() - wd / 2, square.GetCentreY() - ht / 2 + ds - ClueNumberSize / 4f)
                            .ShowText(ch.ToString())
                            .EndText();
                    }
                }
            }

        pdfCanvas.RestoreState();
    }

    private void FillBlacks()
    {
        pdfCanvas!.SaveState()
            .SetFillColor(ColorConstants.DARK_GRAY);

        for (int y = Board.Top; y <= Board.Bottom; ++y)
            for (int x = Board.Left; x <= Board.Right; ++x)
                if (Board.IsBlackSquare(x, y))
                {
                    pdfCanvas.Rectangle(GetSquareRect(x, y))
                        .Fill();
                }

        pdfCanvas.RestoreState();
    }

    private void AddClueNumbers()
    {
        pdfCanvas!.SaveState()
            .SetFillColor(DrawSolution ? ColorConstants.DARK_GRAY : ColorConstants.BLACK)
            .SetFontAndSize(Font, ClueNumberSize);

        foreach (var clue in Board.GetClueLocations())
        {
            var square = GetSquareRect(clue.x, clue.y);
            pdfCanvas.BeginText()
                .SetTextMatrix(square.GetLeft() + ClueNumberOffsetLeft, square.GetTop() - ClueNumberOffsetTop)
                .ShowText(clue.number.ToString())
                .EndText();
        }

        pdfCanvas.RestoreState();
    }

    private Rectangle GetSquareRect(int x, int y)
    {
        return new Rectangle(RenderLeft + GetCanvasLeft(x), RenderBottom + GetCanvasBottom(y), (float)SquareSize, (float)SquareSize);
    }

    private float GetCanvasLeft(int x) => (float)((x - Board.Left) * SquareSize + BoxThickness / 2);
    private float GetCanvasBottom(int y) => (float)((Board.Bottom - y) * SquareSize + BoxThickness / 2);


}

