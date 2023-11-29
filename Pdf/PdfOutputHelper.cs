using System.Diagnostics;
using CrosswordMaker.Grids;
using iText.IO.Font;
using iText.IO.Font.Constants;
using iText.Kernel.Font;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas;
using iText.Layout;
using iText.Layout.Borders;
using iText.Layout.Element;
using iText.Layout.Properties;

namespace CrosswordMaker.Pdf;

class PdfOutputHelper
{
    readonly WordBoard Board;
    readonly List<NumberedClueWord> Across;
    readonly List<NumberedClueWord> Down;

    public PdfOutputHelper(WordBoard board, IEnumerable<NumberedClueWord> across, IEnumerable<NumberedClueWord> down)
    {
        Board = board;
        Across = new(across);
        Down = new(down);
    }
    
    public bool RenderSolution { get; set; }

    public string Title { get; set; } = string.Empty;

    public enum Orientation { Portrait, Landscape }
    public Orientation PageOrientation { get; private set; }

    static Rectangle GetInset(Rectangle from, float margin)
    {
        return new Rectangle(from.GetX() + margin, from.GetY() + margin, from.GetWidth() - 2*margin, from.GetHeight() - 2*margin);
    }

    public PageSize PaperSize
    {
        get => _paperSize;
        private set {
            _paperSize = value;
            _fitBounds = GetInset(_paperSize, PageMargin);
        }
    }
    private PageSize _paperSize = PageSize.A4;
    private Rectangle _fitBounds = PageSize.A4;

    public PdfCrosswordRenderer.CrosswordPosition CrosswordPosition { get; private set; }

    public enum Layout { SideBySide, Vertical, SeparatePages, SideBySideRotated }
    public Layout CluesLayout { get; private set; }

    public const float PageMargin = 30f;
    public const float HorizontalSeparation = 30f;
    public const float VerticalSeparation = 20f;

    private PdfCrosswordRenderer? crosswordRenderer;
    private PdfClueListRenderer? acrossRenderer, downRenderer;

    public void Initialize()
    {
        LoadFonts();
        CreateRenderers();
    }

    /// <summary>
    /// Save the crossword and clues to a PDF file written to the provided <see cref="Stream"/>.
    /// <see cref="ChooseLayout"/> must be called first.
    /// </summary>
    /// <param name="stream">Where to send the PDF file bytes.</param>
    public void WritePdf(Stream stream)
    {
        using (PdfWriter writer = new PdfWriter(stream))
        using (PdfDocument doc = new PdfDocument(writer))
        {
            PdfPage page = doc.AddNewPage(PaperSize);

            crosswordRenderer!.Page = page;
            crosswordRenderer.FitBounds = _fitBounds;
            crosswordRenderer.Position = CrosswordPosition;
            crosswordRenderer.FitCrossword();
            crosswordRenderer.RenderCrossword();

            Rectangle clueBounds;

            switch (CrosswordPosition)
            {
                case PdfCrosswordRenderer.CrosswordPosition.Top:
                    Debug.Assert(CluesLayout == Layout.SideBySide);
                    clueBounds = new Rectangle(_fitBounds);
                    clueBounds.SetHeight(crosswordRenderer.RenderBottom - _fitBounds.GetBottom() - VerticalSeparation);
                    RenderCluesSideBySide(page, clueBounds);
                    break;

                case PdfCrosswordRenderer.CrosswordPosition.Left:
                    Debug.Assert(CluesLayout == Layout.Vertical);
                    clueBounds = new Rectangle(crosswordRenderer.RenderRight + HorizontalSeparation, _fitBounds.GetBottom(), _fitBounds.GetRight() - crosswordRenderer.RenderRight - HorizontalSeparation, _fitBounds.GetHeight());
                    RenderCluesVertically(page, clueBounds);
                    break;

                case PdfCrosswordRenderer.CrosswordPosition.Whole:
                    switch (CluesLayout)
                    {
                        case Layout.SideBySide:
                            page = doc.AddNewPage(PaperSize);
                            RenderCluesSideBySide(page, _fitBounds);
                            break;

                        case Layout.Vertical:
                            page = doc.AddNewPage(PaperSize);
                            RenderCluesVertically(page, _fitBounds);
                            break;

                        case Layout.SeparatePages:
                            RenderCluesOnSeparatePages(doc);
                            break;

                        case Layout.SideBySideRotated:
                            PaperSize = PaperSize.Rotate();
                            page = doc.AddNewPage(PaperSize);
                            RenderCluesSideBySide(page, _fitBounds);
                            break;
                    }
                    break;
            }

            try
            {
                doc.Close();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Exception: " + ex);
            }
        }

    }

    private void RenderCluesOnSeparatePages(PdfDocument pdfDoc)
    {
        Rectangle[] columns;

        if (PageOrientation == Orientation.Portrait)
        {
            columns = new Rectangle[1] { _fitBounds };
        }
        else
        {
            Rectangle col1 = new Rectangle(_fitBounds);
            col1.SetWidth((_fitBounds.GetWidth() - HorizontalSeparation) / 2);
            Rectangle col2 = new Rectangle(col1);
            col2.MoveRight(col1.GetWidth() + HorizontalSeparation);
            columns = new Rectangle[2] { col1, col2 };
        }

        Document doc = new Document(pdfDoc, PaperSize);
        doc.SetRenderer(new ColumnDocumentRenderer(doc, columns));
        doc.Add(new AreaBreak(AreaBreakType.NEXT_PAGE));
        acrossRenderer!.RenderClueList(doc);
        doc.Add(new AreaBreak(AreaBreakType.NEXT_PAGE));
        downRenderer!.RenderClueList(doc);
        doc.Close();
    }

    private void RenderCluesVertically(PdfPage page, Rectangle bounds)
    {
        PdfCanvas pdfCanvas = new PdfCanvas(page);
        Rectangle clueBounds = new Rectangle(bounds);
        float acrossHeight = acrossRenderer!.CalculateHeight(clueBounds.GetWidth());
        acrossRenderer.RenderClueList(pdfCanvas, clueBounds);
        clueBounds.DecreaseHeight(acrossHeight + VerticalSeparation);
        downRenderer!.RenderClueList(pdfCanvas, clueBounds);
    }

    private void RenderCluesSideBySide(PdfPage page, Rectangle bounds)
    {
        PdfCanvas pdfCanvas = new PdfCanvas(page);
        Rectangle clueBounds = new Rectangle(bounds);
        clueBounds.SetWidth((_fitBounds.GetWidth() - HorizontalSeparation) / 2);
        acrossRenderer!.RenderClueList(pdfCanvas, clueBounds);
        clueBounds.MoveRight(clueBounds.GetWidth() + HorizontalSeparation);
        downRenderer!.RenderClueList(pdfCanvas, clueBounds);
    }

    /// <summary>
    /// Choose a layout for the crossword and clues.
    /// <c>Board</c>, <c>AcrossClues</c> and <c>DownClues</c> must be set first.
    /// After this operation, the following properties will have valid values
    /// reflecting the outcome of the layout planning process:
    /// <c>PageOrientation</c>, <c>PaperSize</c>, <c>CrosswordPosition</c>, <c>CluesLayout</c>.
    /// </summary>
    public void ChooseLayout()
    {
        SetupRenderers();

        float width, ah, dh;

        if (Board.Width >= Board.Height)
        {
            // try A4 portrait

            PaperSize = PageSize.A4;

            crosswordRenderer!.Position = PdfCrosswordRenderer.CrosswordPosition.Top;
            crosswordRenderer.FitBounds = _fitBounds;
            if (crosswordRenderer.FitCrossword())
            {
                PageOrientation = Orientation.Portrait;

                // try clues below the grid

                width = (_fitBounds.GetWidth() - HorizontalSeparation) / 2;
                float htavail = crosswordRenderer.RenderBottom - _fitBounds.GetBottom() - VerticalSeparation;

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

                width = _fitBounds.GetWidth();
                ah = acrossRenderer!.CalculateHeight(width);
                dh = downRenderer!.CalculateHeight(width);
                if (ah + dh + VerticalSeparation < _fitBounds.GetHeight())
                    CluesLayout = Layout.Vertical;
                else // clues don't fit together on one page
                    CluesLayout = Layout.SeparatePages;

                return;
            }

        }
        else // Height > Width
        {
            // try A4 landscape

            PaperSize = PageSize.A4.Rotate();

            crosswordRenderer!.Position = PdfCrosswordRenderer.CrosswordPosition.Left;
            crosswordRenderer.FitBounds = _fitBounds;
            if (crosswordRenderer.FitCrossword())
            {
                PageOrientation = Orientation.Landscape;

                // try clues together beside crossword

                width = _fitBounds.GetRight() - crosswordRenderer.RenderRight - HorizontalSeparation;
                if (width > 144) // don't bother trying if less than two inches (really need much more)
                {
                    ah = acrossRenderer!.CalculateHeight(width);
                    dh = downRenderer!.CalculateHeight(width);
                    if (ah + dh < _fitBounds.GetHeight())
                    {
                        CrosswordPosition = PdfCrosswordRenderer.CrosswordPosition.Left;
                        CluesLayout = Layout.Vertical;

                        return;
                    }
                }

                // nope, put the clues on a new page

                CrosswordPosition = PdfCrosswordRenderer.CrosswordPosition.Whole;

                // will the clues fit on one page?

                width = (_fitBounds.GetWidth() - HorizontalSeparation) / 2;
                float htavail = _fitBounds.GetHeight();

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
            PaperSize = PageSize.A3;
            PageOrientation = Orientation.Portrait;

            // will the clues fit together on the next page?

            ah = acrossRenderer!.CalculateHeight(_fitBounds.GetWidth());
            dh = downRenderer!.CalculateHeight(_fitBounds.GetWidth());
            if (ah + dh + VerticalSeparation < _fitBounds.GetHeight())
            {
                CluesLayout = Layout.Vertical;

                return;
            }

            // will the clues fit together on a landscape page?

            width = (_fitBounds.GetHeight() - HorizontalSeparation) / 2;
            float htavail = _fitBounds.GetWidth();

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
            PaperSize = PageSize.A3.Rotate();
            PageOrientation = Orientation.Landscape;

            // will the clues fit together on the next page?

            width = (_fitBounds.GetWidth() - HorizontalSeparation) / 2;
            float htavail = _fitBounds.GetHeight();

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

    private void LoadFonts()
    {
        RegularFont = PdfFontFactory.CreateFont(@"..\fonts\FreeSans.ttf");
        BoldFont = PdfFontFactory.CreateFont(@"..\fonts\FreeSansBold.ttf");
        ObliqueFont = PdfFontFactory.CreateFont(@"..\fonts\FreeSansOblique");
        BoldObliqueFont = PdfFontFactory.CreateFont(@"..\fonts\FreeSansBoldOblique");
    }

    private void CreateRenderers()
    {
        crosswordRenderer = new PdfCrosswordRenderer()
        {
            Font = RegularFont!,
        };

        acrossRenderer = new PdfClueListRenderer(PdfClueListRenderer.ClueDirection.Across)
        {
            HeadingFont = BoldFont!,
            ClueFont = RegularFont!,
            AnswerFont = ObliqueFont!,
        };
        downRenderer = new PdfClueListRenderer(PdfClueListRenderer.ClueDirection.Down)
        {
            HeadingFont = BoldFont!,
            ClueFont = RegularFont!,
            AnswerFont = ObliqueFont!,
        };
    }

    private void SetupRenderers()
    {
        crosswordRenderer!.Board = Board;
        crosswordRenderer.Title = Title;
        crosswordRenderer.DrawSolution = RenderSolution;

        acrossRenderer!.Clues = Across;
        acrossRenderer.IncludeAnswers = RenderSolution;

        downRenderer!.Clues = Down;
        downRenderer.IncludeAnswers = RenderSolution;
    }
}

