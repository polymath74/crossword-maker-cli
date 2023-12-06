using System.Diagnostics;

namespace Pdf;

public class PdfFont
{
    public readonly float AverageWidth = 0.670f; // this is a conservate estimate only for line breaking

    readonly int localId;

    internal PdfFont(int useId)
    {
        localId = useId;
    }

    public override string ToString()
    {
        Debug.Assert(localId != 0);
        return $"/F{localId}";
    }

    public const string Helvetica = "Helvetica";
    public const string HelveticaBold = "Helvetica-Bold";
    public const string HelveticaOblique = "Helvetica-Oblique";
    public const string HelveticaBoldOblique = "Helvetica-BoldOblique";

}
