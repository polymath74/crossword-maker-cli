namespace Pdf;

public readonly struct Point : IComparable<Point>, IEquatable<Point>
{
    public readonly float X, Y;

    public Point(float x, float y)
    {
        X = x;
        Y = y;
    }

    public int CompareTo(Point other)
    {
        float diff = Y - other.Y;
        if (diff == 0)
            diff = X - other.X;
        return diff < 0 ? -1 : diff > 0 ? 1 : 0;
    }

    public bool Equals(Point other)
    {
        return X == other.X && Y == other.Y;
    }

    public Point Offset(float x, float y)
        => new(X + x, Y + y);
    public Point Offset(Point by)
        => new(X + by.X, Y + by.Y);

    public Point Transposed()
        => new(Y, X);

    public override string ToString()
    {
        return $"{X:0.##} {Y:0.##}";
    }
}

public readonly struct Rectangle : IComparable<Rectangle>, IEquatable<Rectangle>
{
    public readonly float Left, Bottom, Right, Top;

    Rectangle(float x1, float y1, float x2, float y2)
    {
        // normalise
        if (x1 > x2)
            (x1, x2) = (x2, x1);
        if (y1 > y2)
            (y1, y2) = (y2, y1);

        Left = x1;
        Bottom = y1;
        Right = x2;
        Top = y2;
    }

    public int CompareTo(Rectangle other)
    {
        float diff = Bottom - other.Bottom;
        if (diff == 0)
            diff = Left - other.Left;
        if (diff == 0)
            diff = Top - other.Top;
        if (diff == 0)
            diff = Right - other.Right;
        return diff < 0 ? -1 : diff > 0 ? 1 : 0;
    }

    public bool Equals(Rectangle other)
    {
        return Left == other.Left && Bottom == other.Bottom && Right == other.Right && Top == other.Top;
    }

    public static Rectangle FromCorners(float x1, float y1, float x2, float y2)
        => new(x1, y1, x2, y2);
    public static Rectangle FromCorners(Point p, Point q)
        => new(p.X, p.Y, q.X, q.Y);
    
    public static Rectangle FromSize(float x, float y, float width, float height)
        => new(x, y, x + width, y + height);

    public static Rectangle FromSize(Point p, Point size)
        => new(p.X, p.Y, p.X + size.X, p.Y + size.Y);

    public Rectangle Offset(float x, float y)
        => new(Left + x, Bottom + y, Right + x, Top + y);
    public Rectangle Offset(Point by)
        => new(Left + by.X, Bottom + by.Y, Right + by.X, Top + by.Y);

    public Rectangle Inset(float by)
        => new(Left + by, Bottom + by, Right - by, Top - by);
    public Rectangle Inset(float x, float y)
        => new(Left + x, Bottom + y, Right - x, Top - y);
    public Rectangle Inset(Point by)
        => new(Left + by.X, Bottom + by.Y, Right - by.X, Top - by.Y);

    public Rectangle Outset(float by)
        => new(Left - by, Bottom - by, Right + by, Top + by);
    public Rectangle Outset(float x, float y)
        => new(Left - x, Bottom - y, Right + x, Top + y);
    public Rectangle Outset(Point by)
        => new(Left - by.X, Bottom - by.Y, Right + by.X, Top + by.Y);
    
    public Rectangle WithLeft(float x)
        => new(x, Bottom, Right, Top);
    public Rectangle WithBottom(float y)
        => new(Left, y, Right, Top);
    public Rectangle WithRight(float x)
        => new(Left, Bottom, x, Top);
    public Rectangle WithTop(float y)
        => new(Left, Bottom, Right, y);

    public Rectangle Transposed()
        => new(Bottom, Left, Top, Right);
    
    public float Width => Right - Left;
    public float Height => Top - Bottom;

    public float CentreX => (Left + Right) / 2f;
    public float CentreY => (Top + Bottom) / 2f;

    public Point BottomLeft => new Point(Left, Bottom);
    public Point Size => new Point(Width, Height);

    public override string ToString()
    {
        return $"{Left:0.##} {Bottom:0.##} {Right:0.##} {Top:0.##}";
    }
}
