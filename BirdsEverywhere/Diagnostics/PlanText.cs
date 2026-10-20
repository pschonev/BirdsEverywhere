using BirdsEverywhere.Spawners;

namespace BirdsEverywhere.Diagnostics;

public static class PlanText
{
    public static IEnumerable<string> Lines(DailySpawnPlan plan)
    {
        foreach (bool unseen in new[] { true, false })
        {
            List<string> group = Group(plan, unseen).ToList();
            if (group.Count > 0)
            {
                yield return unseen ? "Unseen:" : "Seen:";
                foreach (string line in group)
                {
                    yield return "  " + line;
                }
            }
        }
    }

    private static IEnumerable<string> Group(DailySpawnPlan plan, bool unseen)
    {
        foreach ((string location, IReadOnlyList<BirdPlacement> placements) in plan.Locations.OrderBy(entry => entry.Key, StringComparer.Ordinal))
        {
            foreach (BirdPlacement placement in placements.Where(placement => placement.Unseen == unseen))
            {
                string times = placement.Times.Count == 0 ? "all day" : string.Join(", ", placement.Times.Select(range => $"{range.Min}-{range.Max}"));
                yield return $"{placement.BirdId} / {placement.RuleId} at {location} ({(int)placement.Tile.X}, {(int)placement.Tile.Y}) [{times}]";
            }
        }
    }
}