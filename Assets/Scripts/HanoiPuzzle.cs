using System;
using System.Collections.Generic;

/// <summary>
/// Hanoi game with 4/5/6 disks and an optional one-disk bonus holder (rod index 3).
/// Normal goal is always to move all disks to rod index 2.
/// </summary>
public sealed class HanoiPuzzle
{
    // Kept for compatibility with older project checks that expect a four-disk default.
    public const int DiskCount = 4;
    public const int BonusRod = 3;
    private readonly List<int>[] rods =
    {
        new List<int>(), new List<int>(), new List<int>(), new List<int>()
    };

    public int ActiveDiskCount { get; private set; } = DiskCount;
    public int Moves { get; private set; }
    public bool BonusUnlocked { get; private set; }
    public bool BonusUsed { get; private set; }
    public bool IsWon => rods[2].Count == ActiveDiskCount;
    public int MinimumMoves => (1 << ActiveDiskCount) - 1;

    public HanoiPuzzle() => Reset(DiskCount);

    public void Reset() => Reset(DiskCount);

    public void Reset(int count)
    {
        if (count < 4 || count > 6)
            throw new ArgumentOutOfRangeException(nameof(count), "Choose 4, 5 or 6 disks.");

        foreach (List<int> rod in rods) rod.Clear();
        ActiveDiskCount = count;
        for (int disk = count; disk >= 1; disk--) rods[0].Add(disk);
        Moves = 0;
        BonusUnlocked = false;
        BonusUsed = false;
    }

    public bool UnlockBonus()
    {
        if (BonusUnlocked || IsWon) return false;
        BonusUnlocked = true;
        return true;
    }

    public int Count(int rod) => IsValidRod(rod) ? rods[rod].Count : 0;
    public int DiskAt(int rod, int level) => rods[rod][level];
    public int Top(int rod) => Count(rod) == 0 ? 0 : rods[rod][rods[rod].Count - 1];

    private bool IsValidRod(int rod) => rod >= 0 && rod < rods.Length;

    public bool TryMove(int from, int to)
    {
        if (!IsValidRod(from) || !IsValidRod(to) || from == to || IsWon)
            return false;
        if ((from == BonusRod || to == BonusRod) && !BonusUnlocked)
            return false;
        if (rods[from].Count == 0) return false;
        if (to == BonusRod && rods[to].Count != 0) return false;

        int movingDisk = Top(from);
        int targetDisk = Top(to);
        if (targetDisk != 0 && movingDisk > targetDisk) return false;

        rods[from].RemoveAt(rods[from].Count - 1);
        rods[to].Add(movingDisk);
        Moves++;
        if (to == BonusRod) BonusUsed = true;
        return true;
    }
}
