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

    public override string ToString()
    {
        return $"{X:0.##} {Y:0.##}";
    }
}

public readonly struct Rectangle : IComparable<Rectangle>, IEquatable<Rectangle>
{
    public readonly float X1, Y1, X2, Y2;

    Rectangle(float x1, float y1, float x2, float y2)
    {
        X1 = x1;
        Y1 = y1;
        X2 = x2;
        Y2 = y2;
    }

    public int CompareTo(Rectangle other)
    {
        float diff = Y1 - other.Y1;
        if (diff == 0)
            diff = X1 - other.X1;
        if (diff == 0)
            diff = Y2 - other.Y2;
        if (diff == 0)
            diff = X2 - other.X2;
        return diff < 0 ? -1 : diff > 0 ? 1 : 0;
    }

    public bool Equals(Rectangle other)
    {
        return X1 == other.X1 && Y1 == other.Y1 && X2 == other.X2 && Y2 == other.Y2;
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
        => new(X1 + x, Y1 + y, X2 + x, Y2 + y);
    public Rectangle Offset(Point by)
        => new(X1 + by.X, Y1 + by.Y, X2 + by.X, Y2 + by.Y);

    public Rectangle Inset(float by)
        => new(X1 + by, Y1 + by, X2 - by, Y2 - by);
    public Rectangle Inset(float x, float y)
        => new(X1 + x, Y1 + y, X2 - x, Y2 - y);
    public Rectangle Inset(Point by)
        => new(X1 + by.X, Y1 + by.Y, X2 - by.X, Y2 - by.Y);

    public Rectangle Outset(float by)
        => new(X1 - by, Y1 - by, X2 + by, Y2 + by);
    public Rectangle Outset(float x, float y)
        => new(X1 - x, Y1 - y, X2 + x, Y2 + y);
    public Rectangle Outset(Point by)
        => new(X1 - by.X, Y1 - by.Y, X2 + by.X, Y2 + by.Y);
    
    public float Width => X2 - X1;
    public float Height => Y2 - Y1;

    public override string ToString()
    {
        return $"{X1:0.##} {Y1:0.##} {X2:0.##} {Y2:0.##}";
    }
}
