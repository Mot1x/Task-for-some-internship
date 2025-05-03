using System;
using System.Collections.Generic;
using System.Linq;

class Program
{
    private static readonly Dictionary<char, int> KeyBit = new Dictionary<char, int>();
    private static readonly List<Point> StartPositions = new List<Point>();
    private static readonly List<Point> KeyPositions = new List<Point>();
    private static Point[] points;
    private static List<Edge>[] graph;
    private static int[,] minSteps;
    private static readonly int[] dx = { -1, 1, 0, 0 };
    private static readonly int[] dy = { 0, 0, -1, 1 };
    private static int rows, cols, importantPointsCount, keysMask;
    const int INF = 1000000;

    private class BucketQueue<TValue>
    {
        private readonly List<Queue<TValue>> _buckets;
        private int _current;

        public BucketQueue(int maxPriority)
        {
            _buckets = new List<Queue<TValue>>(maxPriority + 1);
            for (var i = 0; i <= maxPriority; i++)
                _buckets.Add(new Queue<TValue>());
            _current = 0;
            Count = 0;
        }

        public void Enqueue(TValue value, int priority)
        {
            if (priority < 0 || priority >= _buckets.Count)
                throw new ArgumentOutOfRangeException();
            _buckets[priority].Enqueue(value);
            if (priority < _current)
                _current = priority;
            Count++;
        }

        public (TValue, int) Dequeue()
        {
            while (_current < _buckets.Count)
            {
                var q = _buckets[_current];
                if (q.Count > 0)
                {
                    Count--;
                    return (q.Dequeue(), _current);
                }

                _current++;
            }

            return (default(TValue), -1);
        }

        public int Count { get; private set; }
    }

    private struct Point
    {
        public int X, Y;

        public Point(int x, int y)
        {
            X = x;
            Y = y;
        }
    }

    private struct Edge
    {
        public int To, Dist, RequiredMask;

        public Edge(int to, int dist, int requiredMask)
        {
            To = to;
            Dist = dist;
            RequiredMask = requiredMask;
        }
    }

    private static ulong Pack(int p1, int p2, int p3, int p4, int mask) =>
        (uint)mask | ((ulong)p1 << 26) | ((ulong)p2 << 32) | ((ulong)p3 << 38) | ((ulong)p4 << 44);

    private static (int mask, int[] ps) Unpack(ulong v)
    {
        var mask = (int)(v & ((1u << 26) - 1));
        var ps = new int[4];
        ps[0] = (int)((v >> 26) & 0x3F);
        ps[1] = (int)((v >> 32) & 0x3F);
        ps[2] = (int)((v >> 38) & 0x3F);
        ps[3] = (int)((v >> 44) & 0x3F);
        return (mask, ps);
    }

    private static void BuildAllPairs()
    {
        var n = points.Length;
        minSteps = new int[n, n];

        for (var i = 0; i < n; i++)
        for (var j = 0; j < n; j++)
            if (i != j)
                minSteps[i, j] = INF;

        for (var i = 0; i < n; i++)
            foreach (var e in graph[i])
                minSteps[i, e.To] = Math.Min(minSteps[i, e.To], e.Dist);

        for (var i = 0; i < n; i++)
        for (var j = 0; j < n; j++)
        for (var k = 0; k < n; k++)
            if (minSteps[i, k] + minSteps[k, j] < minSteps[i, j])
                minSteps[i, j] = minSteps[i, k] + minSteps[k, j];
    }

    private static int AStarMinSteps()
    {
        var distG = new Dictionary<ulong, int>();
        var queue = new BucketQueue<ulong>(rows * cols + 1);

        var start = Pack(0, 1, 2, 3, 0);
        distG[start] = 0;
        var h0 = Heuristic(new int[] { 0, 1, 2, 3 }, 0, minSteps);
        queue.Enqueue(start, h0);

        while (queue.Count > 0)
        {
            var (state, f) = queue.Dequeue();
            if (state == 0u)
                break;

            var (mask, robots) = Unpack(state);
            var g = distG[state];
            if (g + Heuristic(robots, mask, minSteps) != f)
                continue;
            if (mask == keysMask)
                return g;

            for (var i = 0; i < 4; i++)
                foreach (var edge in graph[robots[i]])
                {
                    if ((mask & edge.RequiredMask) != edge.RequiredMask)
                        continue;
                    var newMask = mask;
                    if (edge.To >= 4)
                        newMask |= 1 << (edge.To - 4);
                    var newRobots = new int[] { robots[0], robots[1], robots[2], robots[3] };
                    newRobots[i] = edge.To;
                    var nsState = Pack(newRobots[0], newRobots[1], newRobots[2], newRobots[3], newMask);
                    var newG = g + edge.Dist;
                    if (distG.TryGetValue(nsState, out var old) && old <= newG)
                        continue;
                    distG[nsState] = newG;
                    var h = Heuristic(newRobots, newMask, minSteps);
                    queue.Enqueue(nsState, newG + h);
                }
        }

        return -1;
    }

    private static int Heuristic(int[] ps, int mask, int[,] distPoints)
    {
        var remaining = keysMask & ~mask;
        var h = 0;
        for (var bit = 0; bit < 26; bit++)
            if ((remaining & (1 << bit)) != 0)
            {
                var target = 4 + bit;
                var best = INF;
                foreach (var t in ps)
                    best = Math.Min(best, distPoints[t, target]);

                if (best < INF)
                    h = Math.Max(h, best);
            }

        return h;
    }

    private static void ParseField(List<List<char>> grid)
    {
        for (var x = 0; x < rows; x++)
        for (var y = 0; y < cols; y++)
        {
            var cell = grid[x][y];
            if (cell == '@')
                StartPositions.Add(new Point(x, y));
            else if (cell >= 'a' && cell <= 'z' && !KeyBit.ContainsKey(cell))
            {
                KeyBit[cell] = KeyBit.Count;
                KeyPositions.Add(new Point(x, y));
            }
        }
    }

    private static void GetImportantPoints()
    {
        points = new Point[importantPointsCount];
        for (var i = 0; i < 4; i++)
            points[i] = StartPositions[i];
        foreach (var kv in KeyBit)
            points[4 + kv.Value] = KeyPositions[kv.Value];
    }

    private static void BuildPointGraph(List<List<char>> grid)
    {
        graph = new List<Edge>[importantPointsCount];
        for (var i = 0; i < importantPointsCount; i++)
            graph[i] = new List<Edge>();
        for (var i = 0; i < importantPointsCount; i++)
        {
            var dist = new int[rows, cols];
            var required = new int[rows, cols];
            for (var x = 0; x < rows; x++)
            for (var y = 0; y < cols; y++)
                dist[x, y] = -1;

            var queue = new Queue<Point>();
            var start = points[i];
            dist[start.X, start.Y] = 0;
            queue.Enqueue(start);
            while (queue.Count > 0)
            {
                var p = queue.Dequeue();
                var d = dist[p.X, p.Y];
                var mask = required[p.X, p.Y];
                for (var m = 0; m < 4; m++)
                {
                    var newX = p.X + dx[m];
                    var newY = p.Y + dy[m];
                    if (newX < 0 || newX >= rows || newY < 0 || newY >= cols || dist[newX, newY] != -1)
                        continue;

                    var c = grid[newX][newY];
                    if (c == '#')
                        continue;

                    var newMask = mask;
                    if (c >= 'A' && c <= 'Z' && KeyBit.TryGetValue(char.ToLower(c), out var b))
                        newMask |= 1 << b;
                    dist[newX, newY] = d + 1;
                    required[newX, newY] = newMask;
                    queue.Enqueue(new Point(newX, newY));
                    if (c >= 'a' && c <= 'z' && KeyBit.TryGetValue(c, out var bit))
                        graph[i].Add(new Edge(4 + bit, d + 1, newMask));
                }
            }
        }
    }

    static int Solve(List<List<char>> grid)
    {
        rows = grid.Count;
        cols = grid[0].Count;
        ParseField(grid);

        var keyBitCount = KeyBit.Count;
        keysMask = (1 << keyBitCount) - 1;
        importantPointsCount = 4 + keyBitCount;

        GetImportantPoints();
        BuildPointGraph(grid);
        BuildAllPairs();

        return AStarMinSteps();
    }

    private static List<List<char>> GetInput()
    {
        var data = new List<List<char>>();
        string line;
        while ((line = Console.ReadLine()) != null && line != "")
            data.Add(line.ToCharArray().ToList());
        return data;
    }

    static void Main()
    {
        var data = GetInput();
        var result = Solve(data);
        Console.WriteLine(result == -1 ? "No solution found" : result.ToString());
    }
}