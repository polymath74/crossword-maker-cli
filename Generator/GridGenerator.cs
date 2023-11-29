using CrosswordMaker.Building;
using CrosswordMaker.Grids;

namespace CrosswordMaker.Generator;

class GridGenerator
{
    class GeneratedGrid
    {
        public readonly string Method;
        public readonly GridBuilder Builder;
        public WordBoard Board => Builder.Board;

        public GeneratedGrid(string method, GridBuilder builder)
        {
            Method = method;
            Builder = builder;
        }

        public double AspectRatio => (double)Board.Width / (double)Board.Height;

        double _occupied = -1d;
        public double Occupied
        {
            get
            {
                if (_occupied < 0d)
                {
                    int occupied = 0;
                    for (int y = Board.Top; y <= Board.Bottom; ++y)
                        for (int x = Board.Left; x <= Board.Right; ++x)
                            if (Board.LetterAt(x, y) != ' ')
                                ++occupied;
                    _occupied = (double)occupied / (double)Board.Width / (double)Board.Height;
                }
                return _occupied;
            }
        }

        double _score = -1d;
        public double Score
        {
            get
            {
                if (_score < 0d)
                    _score = Board.CountIntersections /* * 1000d */ * Occupied;
                return _score;
            }
        }
    }


    List<string> wordList = new();

    List<GeneratedGrid> generatedGrids = new();

    async Task GenerateAsync(string method, GridBuilder builder, CancellationToken token)
    {
        var gg = new GeneratedGrid(method, builder);
        await Task.Run(() => gg.Builder.AddWords(wordList, token), token);
        token.ThrowIfCancellationRequested();
        lock (generatedGrids)
        {
            Console.WriteLine($"({method} => {gg.Board.Height}x{gg.Board.Width} @{gg.Board.CountIntersections} {gg.Occupied*100:0.#}% {gg.Score:0.##})");
            if (gg.Board.CountWords == wordList.Count && !AlreadyHaveEquivalent(gg))
                generatedGrids.Add(gg);
        }
    }

    bool AlreadyHaveEquivalent(GeneratedGrid gg)
    {
        foreach (GeneratedGrid grid in generatedGrids)
            if (grid.Board.IsEquivalentTo(gg.Board))
                return true;
        return false;
    }

    public Task GenerateGridsAsync(IEnumerable<string> words, CancellationToken token)
    {
        wordList = new(words);

        LetterScores letterScores = new FrequencyLetterScores();
        letterScores.Add(wordList);

        List<Task> generators = new()
        {
            GenerateAsync("best",
                new GridBuilderByBestBoardPlacement(letterScores), token),
            GenerateAsync("decreasing length",
                new GridBuilderBySortedOrder(letterScores, (s1, s2) => s2.Length - s1.Length), token),
            GenerateAsync("increasing length",
                new GridBuilderBySortedOrder(letterScores, (s1, s2) => s1.Length - s2.Length), token),
            GenerateAsync("decreasing score",
                new GridBuilderBySortedOrder(letterScores, (s1, s2) => letterScores.Score(s2) - letterScores.Score(s1)), token),
            GenerateAsync("increasing score",
                new GridBuilderBySortedOrder(letterScores, (s1, s2) => letterScores.Score(s1) - letterScores.Score(s2)), token)
        };

        Random random = new();

        int numRandom = 15;
        if (numRandom > wordList.Count)
           numRandom = wordList.Count;
        for (int rx = 0; rx < numRandom; ++rx)
        {
            generators.Add(GenerateAsync($"random {rx+1}", new GridBuilderByRandomOrder(letterScores, random), token));
        }

        return Task.WhenAll(generators);
    }

    public WordBoard? BestGenerated()
    {
        int maxIntersections = generatedGrids.Max(gg => gg.Board.CountIntersections);
        return generatedGrids
            .Where(gg => gg.Board.CountIntersections == maxIntersections)
            .MaxBy(gg => gg.Score)?.Board;
    }
}
