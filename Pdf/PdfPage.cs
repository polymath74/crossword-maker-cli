namespace Pdf;

public class PdfPage
{
    public readonly Point Size;
    public readonly Rectangle MediaBox;

    List<Byte> contentStream = new();
    internal List<Byte> GetContents()
        => contentStream;

    public PdfPage(Point size)
    {
        Size = size;
        MediaBox = Rectangle.FromSize(new Point(), size);
    }

    public static readonly Point A4 = new(595.28f, 841.89f);
    public static readonly Point A3 = new(841.89f, 1190.55f);


    public void AddRectangle(Rectangle rectangle)
    {
        contentStream.AddRange(Encoding.ASCII.GetBytes($"{rectangle.Left} {rectangle.Bottom} {rectangle.Width} {rectangle.Height} re\n"));
    }

    public void AddText(Point where, string text, PdfFont font, float size)
        => AddText(where.X, where.Y, text, font, size);

    public void AddText(float x, float y, string text, PdfFont font, float size)
    {
        contentStream.AddRange(Encoding.ASCII.GetBytes($"BT {font} {size} Tf {x} {y} Td ("));
        // contentStream.AddRange(Encoding.BigEndianUnicode.GetPreamble());
        // contentStream.AddRange(Encoding.BigEndianUnicode.GetBytes(PdfDocument.Escaped(text)));
        contentStream.AddRange(Encoding.ASCII.GetBytes(PdfDocument.Escaped(text)));
        contentStream.AddRange(Encoding.ASCII.GetBytes($") Tj ET\n"));
    }

    public void LineWidth(float width)
    {
        contentStream.AddRange(Encoding.ASCII.GetBytes($"{width} w\n"));
    }

    public void ClosePath(bool stroke, bool fill)
    {
        byte op = stroke ? fill ? (byte)'b' : (byte)'s' : fill ? (byte)'f' : (byte)'n';
        contentStream.Add(op);
        contentStream.Add((byte)'\n');
    }
}
