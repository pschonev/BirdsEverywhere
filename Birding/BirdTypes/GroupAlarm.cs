using Microsoft.Xna.Framework;
using Birding.Domain;

namespace Birding.BirdTypes;

/// <summary>A flock member as the alarm wave sees it; <paramref name="Available"/> is false for birds that already flee, hide or have no cohesion.</summary>
internal readonly record struct AlarmMember(long Id, Vector2 Tile, bool Available);

/// <summary>Pure alarm wave: which flock mates a fleeing bird informs, and after how long.</summary>
internal static class GroupAlarm
{
    /// <summary>Neighbour wave from <paramref name="trigger"/> through members listed in id order; each candidate rolls <c>spread</c> once from its own seed, so the shared random stream is never used.</summary>
    public static List<(int Index, int DelayMs)> Plan(IReadOnlyList<AlarmMember> members, int trigger, Cohesion cohesion, long triggerMs)
    {
        List<(int, int)> informedBirds = new();
        if (cohesion.Spread <= 0)
        {
            return informedBirds;
        }
        bool[] informed = new bool[members.Count];
        informed[trigger] = true;
        Queue<int> queue = new();
        queue.Enqueue(trigger);
        Vector2 origin = members[trigger].Tile;
        int spanMs = cohesion.DelayMs.Max - cohesion.DelayMs.Min;
        while (queue.Count > 0)
        {
            int from = queue.Dequeue();
            for (int index = 0; index < members.Count; index++)
            {
                AlarmMember candidate = members[index];
                if (informed[index] || !candidate.Available || Vector2.Distance(candidate.Tile, members[from].Tile) > cohesion.LinkRadius)
                {
                    continue;
                }
                Random random = new(unchecked((int)(candidate.Id * 0x9E3779B1L + triggerMs)));
                double spreadRoll = random.NextDouble();
                double jitter = random.NextDouble();
                if (spreadRoll >= cohesion.Spread)
                {
                    continue;
                }
                informed[index] = true;
                queue.Enqueue(index);
                double distance = Math.Min(1.0, Vector2.Distance(candidate.Tile, origin) / (cohesion.LinkRadius * 2.0));
                informedBirds.Add((index, cohesion.DelayMs.Min + (int)(spanMs * (0.5 * distance + 0.5 * jitter))));
            }
        }
        return informedBirds;
    }
}
