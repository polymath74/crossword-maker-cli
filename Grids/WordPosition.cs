namespace CrosswordMaker.Grids;

readonly struct WordPosition : IEquatable<WordPosition>, IComparable<WordPosition>
{
    public enum WordDirection { Across, Down }

    public readonly int X, Y;
    public readonly WordDirection Direction;

    public WordPosition(int use_x, int use_y, WordDirection use_dir)
    {
        X = use_x;
        Y = use_y;
        Direction = use_dir;
    }

    // public WordPosition(WordPosition copyOf)
    // {
    //     x = copyOf.x;
    //     y = copyOf.y;
    //     dir = copyOf.dir;
    // }

    public bool Equals(WordPosition other)
        => X == other.X && Y == other.Y && Direction == other.Direction;

    public int CompareTo(WordPosition other)
    {
        int diff;
        diff = Y - other.Y;
        if (diff != 0)
            return diff;
        diff = X - other.X;
        if (diff != 0)
            return diff;
        diff = Direction - other.Direction;
        return diff;
    }

    public override string ToString()
    {
        return $"({X},{Y}:{Direction})";
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(X, Y, Direction);
    }

    public override bool Equals(object? obj)
    {
        return obj is WordPosition position && Equals(position);
    }

    public static bool operator ==(WordPosition left, WordPosition right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(WordPosition left, WordPosition right)
    {
        return !left.Equals(right);
    }

    public static bool operator <(WordPosition left, WordPosition right)
    {
        return left.CompareTo(right) < 0;
    }

    public static bool operator <=(WordPosition left, WordPosition right)
    {
        return left.CompareTo(right) <= 0;
    }

    public static bool operator >(WordPosition left, WordPosition right)
    {
        return left.CompareTo(right) > 0;
    }

    public static bool operator >=(WordPosition left, WordPosition right)
    {
        return left.CompareTo(right) >= 0;
    }
}

