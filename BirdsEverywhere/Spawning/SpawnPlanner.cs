using BirdsEverywhere.Domain;

namespace BirdsEverywhere.Spawners;

public sealed record SpawnAssignment(string Location, BirdDefinition Bird, SpawnRule Rule);

public static class SpawnPlanner
{
    public static IReadOnlyList<SpawnAssignment> Plan(IReadOnlyDictionary<string, BirdDefinition> catalog, IReadOnlySet<string> seenBirds,
        Season season, Weather weather, int day, Random random, Func<string, bool> locationExists)
    {
        int limit = Math.Max(1, catalog.Values.SelectMany(bird => bird.Rules).SelectMany(rule => rule.Locations).Distinct(StringComparer.Ordinal).Count() / 2);
        Dictionary<string, SpawnAssignment> assignments = new(StringComparer.Ordinal);
        IEnumerable<BirdDefinition> candidates = catalog.Values
            .OrderBy(bird => seenBirds.Contains(bird.Id.Value))
            .ThenBy(bird => bird.Id.Value, StringComparer.Ordinal);
        foreach (BirdDefinition bird in candidates)
        {
            SpawnRule? rule = bird.PickRule(season, weather, day, random);
            if (rule is null || rule.Locations.IsEmpty)
            {
                continue;
            }

            IEnumerable<string> choices = seenBirds.Contains(bird.Id.Value)
                ? rule.Locations
                : new[] { rule.Locations[random.Next(rule.Locations.Length)] };
            foreach (string location in choices)
            {
                if (assignments.ContainsKey(location) || !locationExists(location))
                {
                    continue;
                }

                assignments[location] = new SpawnAssignment(location, bird, rule);
                if (assignments.Count >= limit)
                {
                    return assignments.Values.ToList();
                }
            }
        }

        return assignments.Values.ToList();
    }
}
