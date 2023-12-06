/*
using iText.Kernel;
using iText.Kernel.Exceptions;
using iText.Kernel.Font;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas;
using iText.Kernel.Pdf.Xobject;
using iText.Layout;
using iText.Layout.Borders;
using iText.Layout.Element;
using iText.Layout.Layout;
using iText.Layout.Properties;
using iText.Layout.Renderer;

namespace CrosswordMaker.Pdf;

public class PdfClueListRenderer
{
    public enum ClueDirection { Across, Down }
    internal ClueDirection Direction { get; }

    internal List<NumberedClueWord> Clues
    {
        get => _clues;
        set { _clues = value; clueListTable = null; }
    }
    internal bool IncludeAnswers
    {
        get => _includeAnswers;
        set { _includeAnswers = value; clueListTable = null; }
    }

    public PdfFont? HeadingFont { get; set; }
    public PdfFont? ClueFont { get; set; }
    public PdfFont? AnswerFont { get; set; }

    // all dimensions in PDF are in points (1/72 in)

    public const float HeadingTextSize = 11f;
    public const float ClueTextSize = 11f;
    public const float ClueLeading = 15f;

    public PdfClueListRenderer(ClueDirection dir)
    {
        Direction = dir;
    }

    /// <summary>
    /// Calculate the height of this clue list, if bound to the specified width.
    /// NB: This operation is SLOW. It uses an in-memory PDF file to calculate the needed height.
    /// </summary>
    /// <param name="width">Available width for the clue list, in PDF points.</param>
    /// <returns>Calculated height of the clue list, in PDF points.</returns>
    public float CalculateHeight(float width)
    {
        //Stopwatch sw = new Stopwatch();
        //sw.Start();

        BuildClueListTable();

        float height = float.NaN;

        // try
        // {
            using (MemoryStream stream = new MemoryStream())
            using (PdfWriter writer = new PdfWriter(stream))
            using (PdfDocument pdfdoc = new PdfDocument(writer))
            {
                PageSize pageSize = new PageSize(width, 3000);
                Document doc = new Document(pdfdoc, pageSize);

                IRenderer tr = clueListTable!.CreateRendererSubTree();
                LayoutResult layout = tr.SetParent(doc.GetRenderer()).Layout(
                    new LayoutContext(new LayoutArea(1, new Rectangle(width, 3000))));

                height = layout.GetOccupiedArea().GetBBox().GetHeight();

                //sw.Stop();
                //Debug.WriteLine($"Calculated cluelist height={height}pt in {sw.ElapsedMilliseconds}ms");
            }
        // }
        // catch (PdfException ex)
        // {
        //     // DocumentHasNoPages is expected
        //     // if (ex.Message != PdfException.DocumentHasNoPages)
        //         throw;
        // }

        return height;
    }

    /// <summary>
    /// Render the clue list to the <see cref="PdfCanvas"/> within the specified <c>Rectangle</c>.
    /// </summary>
    public void RenderClueList(PdfCanvas pdfCanvas, Rectangle bounds)
    {
        BuildClueListTable();

        Canvas canvas = new Canvas(pdfCanvas, bounds);
        canvas.Add(clueListTable);
        canvas.Close();
    }

    /// <summary>
    /// Render the clue list to the <c>Document</c> using its renderer.
    /// </summary>
    public void RenderClueList(Document doc)
    {
        BuildClueListTable();

        doc.Add(clueListTable);
    }

    private List<NumberedClueWord> _clues = new();
    private bool _includeAnswers;

    private Table? clueListTable;

    private void BuildClueListTable()
    {
        clueListTable = new Table(new float[] { 1, 20 });
        clueListTable.SetBorder(Border.NO_BORDER);
        Cell heading = new Cell(1, 2)
            .SetBorder(Border.NO_BORDER)
            .Add(new Paragraph(Direction.ToString())
                .SetFont(HeadingFont)
                .SetFontSize(ClueTextSize));
        clueListTable.AddHeaderCell(heading);
        foreach (var clue in Clues)
        {
            Cell cell = new Cell()
                .SetBorder(Border.NO_BORDER)
                .SetTextAlignment(TextAlignment.RIGHT)
                .SetPadding(0f)
                .SetPaddingTop(2f)
                .SetPaddingRight(6f)
                .Add(new Paragraph(clue.Number.ToString())
                    .SetFont(ClueFont)
                    .SetFontSize(ClueTextSize)
                    .SetFixedLeading(ClueLeading));
            clueListTable.AddCell(cell);
            Paragraph paragraph = new Paragraph(clue.Clue)
                    .SetFont(ClueFont)
                    .SetFontSize(ClueTextSize)
                    .SetFixedLeading(ClueLeading);
            if (IncludeAnswers)
            {
                paragraph.Add(new Text($" ({clue.Word})")
                    .SetFont(AnswerFont)
                    .SetFontSize(ClueTextSize));
            }
            cell = new Cell()
                .SetBorder(Border.NO_BORDER)
                .SetTextAlignment(TextAlignment.LEFT)
                .SetPadding(0f)
                .SetPaddingTop(2f)
                .Add(paragraph);
            clueListTable.AddCell(cell);
        }

    }
}
*/