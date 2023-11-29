using iText.Kernel.Geom;

namespace CrosswordMaker.Pdf;

static class Extensions
{
    internal static float GetCentreX(this Rectangle rect)
        => rect.GetX() + rect.GetWidth()/2;
    internal static float GetCentreY(this Rectangle rect)
        => rect.GetY() + rect.GetHeight()/2;
}