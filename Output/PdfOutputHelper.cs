using System.Diagnostics;
using CrosswordMaker.Grids;
using Pdf;

namespace CrosswordMaker.Output;

class PdfOutputHelper
{
    readonly WordBoard Board;
    readonly Dictionary<string, string> Clues;

    public PdfOutputHelper(WordBoard board, Dictionary<string, string> use_clues)
    {
        Board = board;
        Clues = use_clues;

        crosswordRenderer = new PdfCrosswordRenderer();

        acrossRenderer = new PdfClueListRenderer(PdfClueListRenderer.ClueDirection.Across);
        downRenderer = new PdfClueListRenderer(PdfClueListRenderer.ClueDirection.Down);
    }
    
    public bool RenderSolution { get; set; }

    public string Title { get; set; } = string.Empty;

    public enum Orientation { Portrait, Landscape }
    public Orientation PageOrientation { get; private set; }

    Point paperSize;
    Rectangle fitBounds;

    public Point PaperSize
    {
        get => paperSize;
        private set {
            paperSize = value;
            fitBounds = Rectangle.FromSize(new Point(), PaperSize).Inset(PageMargin);
        }
    }

    public PdfCrosswordRenderer.CrosswordPosition CrosswordPosition { get; private set; }

    public enum Layout { SideBySide, Vertical, SeparatePages, SideBySideRotated }
    public Layout CluesLayout { get; private set; }

    public const float PageMargin = 30f;
    public const float HorizontalSeparation = 30f;
    public const float VerticalSeparation = 20f;

    private PdfCrosswordRenderer crosswordRenderer;
    private PdfClueListRenderer acrossRenderer, downRenderer;

    /// <summary>
    /// Save the crossword and clues to a PDF file written to the provided <see cref="Stream"/>.
    /// <see cref="ChooseLayout"/> must be called first.
    /// </summary>
    /// <param name="stream">Where to send the PDF file bytes.</param>
    public void WritePdf(string path)
    {
        try
        {
            var doc = new PdfDocument(path);
            doc.Begin();
            LoadFonts(doc);
            ChooseLayout();

            var page = new PdfPage(PaperSize);

            crosswordRenderer!.Page = page;
            crosswordRenderer.FitBounds = fitBounds;
            crosswordRenderer.Position = CrosswordPosition;
            crosswordRenderer.FitCrossword();
            crosswordRenderer.RenderCrossword();

            Rectangle clueBounds;

            switch (CrosswordPosition)
            {
                case PdfCrosswordRenderer.CrosswordPosition.Top:
                    Debug.Assert(CluesLayout == Layout.SideBySide);
                    // Console.WriteLine($"Top SideBySide {crosswordRenderer.RenderBottom}");
                    clueBounds = fitBounds.WithTop(crosswordRenderer.RenderBottom - VerticalSeparation);
                    // Console.WriteLine($"Top SideBySide {clueBounds}");
                    RenderCluesSideBySide(page, clueBounds);
                    doc.AddPage(page);
                    break;

                case PdfCrosswordRenderer.CrosswordPosition.Left:
                    Debug.Assert(CluesLayout == Layout.Vertical);
                    // Console.WriteLine($"Left Vertical {crosswordRenderer.RenderRight}");
                    clueBounds = fitBounds.WithLeft(crosswordRenderer.RenderRight + HorizontalSeparation);
                    // Console.WriteLine($"Left Vertical {clueBounds}");
                    RenderCluesVertically(page, clueBounds);
                    doc.AddPage(page);
                    break;

                case PdfCrosswordRenderer.CrosswordPosition.Whole:
                    doc.AddPage(page);
                    // Console.WriteLine($"Whole");

                    switch (CluesLayout)
                    {
                        case Layout.SideBySide:
                            page = new(PaperSize);
                            RenderCluesSideBySide(page, fitBounds);
                            doc.AddPage(page);
                            break;

                        case Layout.Vertical:
                            page = new(PaperSize);
                            RenderCluesVertically(page, fitBounds);
                            doc.AddPage(page);
                            break;

                        case Layout.SeparatePages:
                            RenderCluesOnSeparatePages(doc);
                            break;

                        case Layout.SideBySideRotated:
                            PaperSize = PaperSize.Transposed();
                            page = new(PaperSize);
                            RenderCluesSideBySide(page, fitBounds);
                            doc.AddPage(page);
                            break;
                    }
                    break;
            }

            doc.End();
        }
        catch (Exception ex)
        {
            Debug.WriteLine("Exception: " + ex);
        }

    }

    private void RenderCluesOnSeparatePages(PdfDocument doc)
    {
        // TODO: two columns in landscape?

        Point size = PaperSize;
        if (size.X > size.Y)
            size = size.Transposed();
        
        Rectangle bounds = Rectangle.FromSize(new Point(), size).Inset(PageMargin);
        
        // TODO: handle overflow

        var page = new PdfPage(size);
        acrossRenderer.RenderClueList(page, bounds);
        doc.AddPage(page);

        page = new PdfPage(size);
        downRenderer.RenderClueList(page, bounds);
        doc.AddPage(page);
    }

    private void RenderCluesVertically(PdfPage page, Rectangle bounds)
    {
        Rectangle clueBounds = bounds;
        float acrossBottom = acrossRenderer.RenderClueList(page, clueBounds);
        clueBounds = clueBounds.WithTop(acrossBottom - VerticalSeparation);
        downRenderer!.RenderClueList(page, clueBounds);
    }

    private void RenderCluesSideBySide(PdfPage page, Rectangle bounds)
    {
        Rectangle col1 = bounds.WithRight(bounds.CentreX - HorizontalSeparation/2f);
        Rectangle col2 = bounds.WithLeft(col1.Right + HorizontalSeparation);
        
        acrossRenderer.RenderClueList(page, col1);
        downRenderer.RenderClueList(page, col2);
    }

    void ChooseLayout()
    {
        SetupRenderers();

        float width, ah, dh;

        if (Board.Width >= Board.Height)
        {
            // try A4 portrait

            PaperSize = PdfPage.A4;

            crosswordRenderer!.Position = PdfCrosswordRenderer.CrosswordPosition.Top;
            crosswordRenderer.FitBounds = fitBounds;
            if (crosswordRenderer.FitCrossword())
            {
                PageOrientation = Orientation.Portrait;

                // try clues below the grid

                width = (fitBounds.Width - HorizontalSeparation) / 2;
                float htavail = crosswordRenderer.RenderBottom - fitBounds.Bottom - VerticalSeparation;

                if (htavail > 144)  // don't bother trying if less than two inches (really need much more)
                {
                    ah = acrossRenderer!.CalculateHeight(width);
                    if (ah < htavail)
                    {
                        dh = downRenderer!.CalculateHeight(width);
                        if (dh < htavail)
                        {
                            CrosswordPosition = PdfCrosswordRenderer.CrosswordPosition.Top;
                            CluesLayout = Layout.SideBySide;

                            return;
                        }
                    }
                }

                // nope, clues need a new page
                // try together on one page

                CrosswordPosition = PdfCrosswordRenderer.CrosswordPosition.Whole;

                width = fitBounds.Width;
                ah = acrossRenderer!.CalculateHeight(width);
                dh = downRenderer!.CalculateHeight(width);
                if (ah + dh + VerticalSeparation < fitBounds.Height)
                    CluesLayout = Layout.Vertical;
                else // clues don't fit together on one page
                    CluesLayout = Layout.SeparatePages;

                return;
            }

        }
        else // Height > Width
        {
            // try A4 landscape

            PaperSize = PdfPage.A4.Transposed();

            crosswordRenderer!.Position = PdfCrosswordRenderer.CrosswordPosition.Left;
            crosswordRenderer.FitBounds = fitBounds;
            if (crosswordRenderer.FitCrossword())
            {
                PageOrientation = Orientation.Landscape;

                // try clues together beside crossword

                width = fitBounds.Right - crosswordRenderer.RenderRight - HorizontalSeparation;
                if (width > 144) // don't bother trying if less than two inches (really need much more)
                {
                    ah = acrossRenderer!.CalculateHeight(width);
                    dh = downRenderer!.CalculateHeight(width);
                    if (ah + dh < fitBounds.Height)
                    {
                        CrosswordPosition = PdfCrosswordRenderer.CrosswordPosition.Left;
                        CluesLayout = Layout.Vertical;

                        return;
                    }
                }

                // nope, put the clues on a new page

                CrosswordPosition = PdfCrosswordRenderer.CrosswordPosition.Whole;

                // will the clues fit on one page?

                width = (fitBounds.Width - HorizontalSeparation) / 2;
                float htavail = fitBounds.Height;

                ah = acrossRenderer!.CalculateHeight(width);
                if (ah < htavail)
                {
                    dh = downRenderer!.CalculateHeight(width);
                    if (dh < htavail)
                    {
                        CluesLayout = Layout.SideBySide;

                        return;
                    }
                }

                CluesLayout = Layout.SeparatePages;

                return;
            }
        }

        // didn't fit on A4, use A3 instead

        CrosswordPosition = PdfCrosswordRenderer.CrosswordPosition.Whole;

        if (Board.Height > Board.Width) // taller than it is wide -> portrait
        { 
            PaperSize = PdfPage.A3;
            PageOrientation = Orientation.Portrait;

            // will the clues fit together on the next page?

            ah = acrossRenderer!.CalculateHeight(fitBounds.Width);
            dh = downRenderer!.CalculateHeight(fitBounds.Width);
            if (ah + dh + VerticalSeparation < fitBounds.Width)
            {
                CluesLayout = Layout.Vertical;

                return;
            }

            // will the clues fit together on a landscape page?

            width = (fitBounds.Width - HorizontalSeparation) / 2;
            float htavail = fitBounds.Width;

            ah = acrossRenderer.CalculateHeight(width);
            if (ah < htavail)
            {
                dh = downRenderer.CalculateHeight(width);
                if (dh < htavail)
                {
                    CluesLayout = Layout.SideBySideRotated;

                    return;
                }
            }

            // no, put the clues on separate pages

            CluesLayout = Layout.SeparatePages;

            return;
        }
        else // wider than it is tall -> landscape
        {
            PaperSize = PdfPage.A3.Transposed();
            PageOrientation = Orientation.Landscape;

            // will the clues fit together on the next page?

            width = (fitBounds.Width - HorizontalSeparation) / 2;
            float htavail = fitBounds.Width;

            ah = acrossRenderer!.CalculateHeight(width);
            if (ah < htavail)
            {
                dh = downRenderer!.CalculateHeight(width);
                if (dh < htavail)
                {
                    CluesLayout = Layout.SideBySide;

                    return;
                }
            }

            // no, put the clues on separate pages

            CluesLayout = Layout.SeparatePages;

            return;
        }

    }

    private PdfFont? RegularFont, BoldFont, ObliqueFont, BoldObliqueFont;

    private void LoadFonts(PdfDocument doc)
    {
        RegularFont = doc.GetFont(PdfFont.Helvetica);
        BoldFont = doc.GetFont(PdfFont.HelveticaBold);
        ObliqueFont = doc.GetFont(PdfFont.HelveticaOblique);
        BoldObliqueFont = doc.GetFont(PdfFont.HelveticaBoldOblique);
    }

    private void SetupRenderers()
    {
        crosswordRenderer.Board = Board;
        crosswordRenderer.Title = Title;
        crosswordRenderer.DrawSolution = RenderSolution;

        Board.GetNumberedWords(out var across, out var down);

        crosswordRenderer.Font = RegularFont;

        acrossRenderer.HeadingFont = BoldFont;
        acrossRenderer.ClueFont = RegularFont;
        acrossRenderer.AnswerFont = ObliqueFont;

        downRenderer.HeadingFont = BoldFont;
        downRenderer.ClueFont = RegularFont;
        downRenderer.AnswerFont = ObliqueFont;

        acrossRenderer.Words = across;
        acrossRenderer.Clues = Clues;
        acrossRenderer.IncludeAnswers = RenderSolution;

        downRenderer.Words = down;
        downRenderer.Clues = Clues;
        downRenderer.IncludeAnswers = RenderSolution;
    }
}

