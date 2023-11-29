global using System.Text;

namespace CrosswordMaker.Grids;

class WordBoard
{
    public static readonly int MaxSize = 200;
    public const int TerribleScore = -10000;

    readonly char[,] letters = new char[MaxSize, MaxSize]; // indexed column,row
    readonly Dictionary<string, WordPosition> usedWords = new();
    readonly BoardLetterIndex index = new();

    public WordBoard()
    {
        Clear();
    }

    public WordBoard(WordBoard copy)
    {
        Clear();
        foreach (var wd in copy.usedWords)
            Place(wd.Key, wd.Value);
    }

    public char LetterAt(int x, int y)
        => letters[x, y];

    public bool Any() => (Left >= 0);

    public int Left { get; private set; }
    public int Right { get; private set; }
    public int Top { get; private set; }
    public int Bottom { get; private set; }

    public int Width => (Right - Left + 1);
    public int Height => (Bottom - Top + 1);

    int weightSumX, weightSumY, weightTotal;

    public double CentroidX => (double)weightSumX / (double)weightTotal;
    public double CentroidY => (double)weightSumY / (double)weightTotal;

    public int CountIntersections { get; private set; }
    public int CountWords => usedWords.Count;
    public double IntersectionDensity => (double)CountIntersections / (double)CountWords;

    public IEnumerable<string> GetWordsUsed() => usedWords.Keys;
    public bool ContainsWord(string word) => usedWords.ContainsKey(word);

    public bool IsEquivalentTo(WordBoard compare)
    {
        if (Width != compare.Width || Height != compare.Height || CountWords != compare.CountWords || CountIntersections != compare.CountIntersections)
            return false;
        for (int myy = Top, cpy = compare.Top; myy <= Bottom; ++myy, ++cpy)
            for (int myx = Left, cpx = compare.Left; myx <= Right; ++myx, ++cpx)
                if (LetterAt(myx, myy) != compare.LetterAt(cpx, cpy))
                    return false;
        return true;
    }

    public void Clear()
    {
        for (int x = 0; x < MaxSize; ++x)
            for (int y = 0; y < MaxSize; ++y)
                letters[x, y] = ' ';
        Left = Top = -1; // flag as empty (to save time)
        Right = Bottom = -2; // to make Width & Height return 0
        weightSumX = weightSumY = weightTotal = 0;
        CountIntersections = 0;

        usedWords.Clear();
        index.Clear();

        // NotifyChanged();
    }

    override public string ToString()
    {
        if (!Any())
            return string.Empty;

        StringBuilder sb = new();
        for (int y = Top; y <= Bottom; ++y)
        {
            for (int x = Left; x <= Right; ++x)
                sb.Append(letters[x, y]);
            sb.AppendLine();
        }
        return sb.ToString();
    }

    public string ToCSV()
    {
        if (!Any())
            return string.Empty;

        StringBuilder sb = new();
        sb.AppendLine($"GRID,{Height},{Width}");
        for (int y = Top; y <= Bottom; ++y)
        {
            for (int x = Left; x <= Right; ++x)
            {
                if (letters[x, y] != ' ')
                    sb.Append(letters[x, y]);
                sb.Append(',');
            }
            sb.AppendLine();
        }
        return sb.ToString();
    }

    /*
    public void FromCSV(string csv)
    {
        Clear();
        usedWords.Clear();
        index.Clear();

        StringReader sr = new StringReader(csv);
        string line;
        int x, y;

        y = 0;
        while ((line = sr.ReadLine()) != null)
        {
            x = 0;
            if (line.Length > 0)
            {
                foreach (string next in line.ToUpper().Split(','))
                {
                    if (next.Length >= 1)
                    {
                        if (Left < 0 || x < Left)
                            Left = x;
                        if (x > Right)
                            Right = x;
                        if (Top < 0 || y < Top)
                            Top = y;
                        if (y > Bottom)
                            Bottom = y;
                        letters[x, y] = next[0];
                    }
                    ++x;
                }
            }
            ++y;
        }

        StringBuilder wd = new StringBuilder();

        for (x = 1; x < MaxSize - 1; ++x)
            for (y = 1; y < MaxSize - 1; ++y)
                if (letters[x, y] != ' ')
                {
                    if (letters[x-1,y] == ' ' && letters[x+1,y] != ' ')
                    {
                        int ax;
                        wd.Clear();
                        for (ax = 0; letters[x+ax,y] != ' '; ++ax)
                            wd.Append(letters[x+ax, y]);
                        string wdstr = wd.ToString();
                        WordPosition where = new WordPosition(x, y, WordPosition.Direction.Across);
                        usedWords.Add(wdstr, where);
                        CheckForNewSites(wdstr, where);
                    }
                    if (letters[x,y-1] == ' ' && letters[x,y+1] != ' ')
                    {
                        wd.Clear();
                        for (int ay = 0; letters[x, y+ay] != ' '; ++ay)
                            wd.Append(letters[x, y+ay]);
                        string wdstr = wd.ToString();
                        WordPosition where = new WordPosition(x, y, WordPosition.Direction.Down);
                        usedWords.Add(wdstr, where);
                        CheckForNewSites(wdstr, where);
                    }
                }
    }
    */

    public bool CanPlace(string word, WordPosition where, out int overlaps)
    {
        overlaps = 0;

        if (usedWords.ContainsKey(word)) // can't add the same word in two places
            return false;

        if (where.Direction == WordPosition.WordDirection.Across)
        {
            if (where.X < 1 || where.X + word.Length >= MaxSize - 1 || where.Y < 1 || where.Y >= MaxSize - 1)
                return false;

            if (letters[where.X - 1, where.Y] != ' ')
                return false;
            for (int ix = 0; ix < word.Length; ++ix)
            {
                if (letters[where.X + ix, where.Y] != ' ')
                {
                    if (letters[where.X + ix, where.Y] != word[ix])
                        return false;
                    else
                        ++overlaps;
                }
                else
                {
                    if (letters[where.X + ix, where.Y - 1] != ' ' || letters[where.X + ix, where.Y + 1] != ' ')
                        return false;
                }
            }
            if (letters[where.X + word.Length, where.Y] != ' ')
                return false;
        }
        else
        {
            if (where.Y < 1 || where.Y + word.Length >= MaxSize - 1 || where.X < 1 || where.X >= MaxSize - 1)
                return false;

            if (letters[where.X, where.Y - 1] != ' ')
                return false;
            for (int iy = 0; iy < word.Length; ++iy)
            {
                if (letters[where.X, where.Y + iy] != ' ')
                {
                    if (letters[where.X, where.Y + iy] != word[iy])
                        return false;
                    else
                        ++overlaps;
                }
                else
                {
                    if (letters[where.X - 1, where.Y + iy] != ' ' || letters[where.X + 1, where.Y + iy] != ' ')
                        return false;
                }
            }
            if (letters[where.X, where.Y + word.Length] != ' ')
                return false;
        }

        return true;
    }

    public void Place(string word, WordPosition where)
    {
        if (!CanPlace(word, where, out int overlaps))
            throw new System.InvalidOperationException($"word \"{word}\" cannot be placed at {where}");

        //Debug.WriteLine($"Thread {Thread.CurrentThread.ManagedThreadId} placing {word} at {where}");

        if (where.Direction == WordPosition.WordDirection.Across)
        {
            for (int ix = 0; ix < word.Length; ++ix)
            {
                if (letters[where.X + ix, where.Y] == ' ')
                {
                    letters[where.X + ix, where.Y] = word[ix];
                }
                else
                {
                    // this letter is no longer available for crosswords
                    index.Remove(word[ix], new WordPosition(where.X + ix, where.Y, WordPosition.WordDirection.Across));
                    // note new intersection
                    ++CountIntersections;
                }

                // add new letter to weight
                weightSumX += where.X + ix;
                weightSumY += where.Y;
                ++weightTotal;
            }

            // adjust bounds
            if (Left < 0 || where.X < Left)
                Left = where.X;
            if (where.X + word.Length - 1 > Right)
                Right = where.X + word.Length - 1;
            if (Top < 0 || where.Y < Top)
                Top = where.Y;
            if (where.Y > Bottom)
                Bottom = where.Y;
        }
        else
        {
            for (int iy = 0; iy < word.Length; ++iy)
            {
                if (letters[where.X, where.Y + iy] == ' ')
                {
                    letters[where.X, where.Y + iy] = word[iy];
                }
                else
                {
                    // this letter is no longer available for crosswords
                    index.Remove(word[iy], new WordPosition(where.X, where.Y + iy, WordPosition.WordDirection.Down));
                    // note new intersection
                    ++CountIntersections;
                }

                // add new letter to weight
                weightSumX += where.X;
                weightSumY += where.Y + iy;
                ++weightTotal;
            }

            // adjust bounds
            if (Top < 0 || where.Y < Top)
                Top = where.Y;
            if (where.Y + word.Length - 1 > Bottom)
                Bottom = where.Y + word.Length - 1;
            if (Left < 0 || where.X < Left)
                Left = where.X;
            if (where.X > Right)
                Right = where.X;
        }

        usedWords.Add(word, where);
        CheckForNewSites(word, where);

        // NotifyChanged();
    }

    public bool IsBlackSquare(int x, int y) => LetterAt(x, y) == ' ' && IsSurroundedByLetters(x, y);

    private bool IsSurroundedByLetters(int x, int y)
    {
        return LetterAt(x - 1, y) != ' ' && LetterAt(x + 1, y) != ' ' && LetterAt(x, y - 1) != ' ' && LetterAt(x, y + 1) != ' ';
    }

    private void CheckForNewSites(string word, WordPosition where)
    {
        // if there's whitespace all around (except in the direction we're going) then a crossword could be added here
        // (that's 6 spaces to check, for each letter)
        // TODO - this could be optimised; if a cell ahead is full, neither here, here+1 nor here+2 will be possible
        if (where.Direction == WordPosition.WordDirection.Across)
        {
            for (int ix = 0; ix < word.Length; ++ix)
                if (letters[where.X + ix - 1, where.Y - 1] == ' ' && letters[where.X + ix, where.Y - 1] == ' ' && letters[where.X + ix + 1, where.Y - 1] == ' ' &&
                        letters[where.X + ix - 1, where.Y + 1] == ' ' && letters[where.X + ix, where.Y + 1] == ' ' && letters[where.X + ix + 1, where.Y + 1] == ' ')
                    index.Add(word[ix], new WordPosition(where.X + ix, where.Y, WordPosition.WordDirection.Down));
        }
        else
        {
            for (int iy = 0; iy < word.Length; ++iy)
                if (letters[where.X - 1, where.Y + iy - 1] == ' ' && letters[where.X - 1, where.Y + iy] == ' ' && letters[where.X - 1, where.Y + iy + 1] == ' ' &&
                        letters[where.X + 1, where.Y + iy - 1] == ' ' && letters[where.X + 1, where.Y + iy] == ' ' && letters[where.X + 1, where.Y + iy + 1] == ' ')
                    index.Add(word[iy], new WordPosition(where.X, where.Y + iy, WordPosition.WordDirection.Across));
        }
    }

    public IEnumerable<WordPosition> GetPositionsOfLetter(char letter)
        => index.GetPositionsOf(letter);

    public readonly struct NextLetterDistance
    {
        public readonly char letter;
        public readonly int distance;

        public NextLetterDistance(char use_letter, int use_distance)
        {
            letter = use_letter;
            distance = use_distance;
        }
    }

    public NextLetterDistance? GetNextAvailableLetter(WordPosition from)
    {
        if (from.Direction == WordPosition.WordDirection.Across)
        {
            for (int x2 = from.X + 2; x2 <= Right; ++x2)
            {
                char ch2 = letters[x2, from.Y];
                if (ch2 != ' ')
                {
                    if (index.IsAvailable(ch2, new WordPosition(x2, from.Y, WordPosition.WordDirection.Across)))
                        return new NextLetterDistance(ch2, x2 - from.X);
                    else
                        return null;
                }
            }
        }
        else
        {
            for (int y2 = from.Y + 2; y2 <= Bottom; ++y2)
            {
                char ch2 = letters[from.X, y2];
                if (ch2 != ' ')
                {
                    if (index.IsAvailable(ch2, new WordPosition(from.X, y2, WordPosition.WordDirection.Down)))
                        return new NextLetterDistance(ch2, y2 - from.Y);
                    else
                        return null;
                }
            }
        }
        return null;
    }

    private readonly struct CluePosition : IComparable<CluePosition>
    {
        public readonly string word;
        public readonly WordPosition where;

        public CluePosition(string clue_word, WordPosition clue_where)
        {
            word = clue_word;
            where = clue_where;
        }

        public int CompareTo(CluePosition other)
            => where.CompareTo(other.where);
    }

    public readonly struct ClueNumberPosition
    {
        public readonly int number, x, y;

        public ClueNumberPosition(int use_number, int use_x, int use_y)
        {
            number = use_number;
            x = use_x;
            y = use_y;
        }
    }

    public List<ClueNumberPosition> GetClueLocations()
    {
        List<ClueNumberPosition> result = new();

        if (usedWords.Count == 0)
            return result;

        List<CluePosition> inBoardOrder = new(usedWords.Select(kvp => new CluePosition(kvp.Key, kvp.Value)));
        inBoardOrder.Sort();

        int nextClueNumber = 1;

        for (int ix = 0; ix < inBoardOrder.Count; ++ix)
        {
            CluePosition here = inBoardOrder[ix];
            result.Add(new(nextClueNumber, here.where.X, here.where.Y));

            if (ix < inBoardOrder.Count - 1)
            {
                CluePosition after = inBoardOrder[ix + 1];
                if (here.where.X == after.where.X && here.where.Y == after.where.Y)
                {
                    ++ix; // skip the next word, already done
                }
            }

            ++nextClueNumber;
        }

        return result;
    }

    public void GetNumberedWords(out List<NumberedWord> across, out List<NumberedWord> down)
    {
        across = new();
        down = new();

        if (usedWords.Count == 0)
            return;

        List<CluePosition> inBoardOrder = new(usedWords.Select(kvp => new CluePosition(kvp.Key, kvp.Value)));
        inBoardOrder.Sort();

        int nextClueNumber = 1;

        for (int ix = 0; ix < inBoardOrder.Count; ++ix)
        {
            CluePosition here = inBoardOrder[ix];
            string? aw = null, dw = null;
            if (here.where.Direction == WordPosition.WordDirection.Across)
                aw = here.word;
            else
                dw = here.word;

            if (ix < inBoardOrder.Count - 1)
            {
                CluePosition after = inBoardOrder[ix + 1];
                if (here.where.X == after.where.X && here.where.Y == after.where.Y)
                {
                    // Debug.Assert(here.where.Direction != after.where.Direction);
                    if (after.where.Direction == WordPosition.WordDirection.Across)
                        aw = after.word; // should never happen, if the sort is working
                    else
                        dw = after.word;
                    ++ix; // skip the next word, already done
                }
            }

            if (aw != null)
                across.Add(new(nextClueNumber, aw));
            if (dw != null)
                down.Add(new(nextClueNumber, dw));

            ++nextClueNumber;
        }
    }

    // public event NotifyCollectionChangedEventHandler CollectionChanged;

    // private void NotifyChanged()
    // {
    //     CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    // }
}
