using System.Collections.Generic;

/// <summary>Four disks, numbered from smallest (1) to largest (4). Bottom first.</summary>
public sealed class HanoiPuzzle
{
    public const int DiskCount = 4;
    private readonly List<int>[] rods = { new List<int>(), new List<int>(), new List<int>() };
    public int Moves { get; private set; }
    public bool IsWon => rods[2].Count == DiskCount;

    public HanoiPuzzle() => Reset();

    public void Reset()
    {
        foreach (var rod in rods) rod.Clear();
        for (int disk = DiskCount; disk > 0; disk--) rods[0].Add(disk);
        Moves = 0;
    }

    public int Count(int rod) => rods[rod].Count;
    public int DiskAt(int rod, int level) => rods[rod][level];
    public int Top(int rod) => rods[rod].Count == 0 ? 0 : rods[rod][rods[rod].Count - 1];

    public bool TryMove(int from, int to)
    {
        if (from < 0 || from > 2 || to < 0 || to > 2 || from == to || IsWon) return false;
        int disk = Top(from);
        if (disk == 0 || (Top(to) != 0 && disk > Top(to))) return false;
        rods[from].RemoveAt(rods[from].Count - 1);
        rods[to].Add(disk);
        Moves++;
        return true;
    }
}
