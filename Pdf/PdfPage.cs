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
        contentStream.AddRange(Encoding.ASCII.GetBytes($"{rectangle.X1} {rectangle.Y1} {rectangle.Width} {rectangle.Height} re\n"));
    }

    public void AddText(Point where, string text, PdfFont font, float size)
    {
        contentStream.AddRange(Encoding.ASCII.GetBytes($"BT {font} {size} Tf {where.X} {where.Y} Td ("));
        // contentStream.AddRange(Encoding.BigEndianUnicode.GetPreamble());
        // contentStream.AddRange(Encoding.BigEndianUnicode.GetBytes(PdfDocument.Escaped(text)));
        contentStream.AddRange(Encoding.ASCII.GetBytes(PdfDocument.Escaped(text)));
        contentStream.AddRange(Encoding.ASCII.GetBytes($") Tj\n"));
    }

    public void ClosePath(bool stroke = true, bool fill = false)
    {
        byte op = stroke ? fill ? (byte)'b' : (byte)'s' : fill ? (byte)'f' : (byte)'n';
        contentStream.Add(op);
        contentStream.Add((byte)'\n');
    }
}
