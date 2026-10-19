using BirdsEverywhere.Domain;

namespace BirdsEverywhere.Spawners;

public sealed record SpawnAssignment(Habitat Habitat, BirdDefinition Bird, SpawnRule Rule)
{
    public string Location => Habitat.Location;
}

public static class SpawnPlanner
{
    private const double TripleChance = 0.02;
    private const double DoubleChance = 0.15;
    private const double LuckToTriple = 0.1;
    private const double LuckToWeightExponent = 5;

    private sealed record Candidate(BirdDefinition Bird, IReadOnlyList<SpawnRule> Rules);

    /// <param name="place">Commits an assignment and returns true, or false when the pattern finds no spot; the planner then tries the next location, rule or bird.</param>
    public static IReadOnlyList<SpawnAssignment> Plan(IReadOnlyDictionary<string, BirdDefinition> catalog, IReadOnlySet<string> seenBirds,
        Season season, Weather weather, int day, double luck, Random random, Func<Habitat, bool> habitatExists, Func<SpawnAssignment, bool> place)
    {
        int limit = Math.Max(1, catalog.Values.SelectMany(bird => bird.Rules).SelectMany(rule => rule.Locations)
            .Select(key => Habitat.Parse(key).Location).Distinct(StringComparer.Ordinal).Count() / 2);
        double exponent = Math.Clamp(1 - LuckToWeightExponent * luck, 0.5, 1.5);
        List<SpawnAssignment> assigned = new();
        HashSet<string> taken = new(StringComparer.Ordinal);

        bool TryAssign(Candidate candidate, bool singleLocation)
        {
            foreach (SpawnRule rule in Utils.shuffleByWeights(random, candidate.Rules.ToList(), candidate.Rules.Select(r => (double)r.Weight).ToList()))
            {
                bool placed = false;
                foreach (string key in Utils.Shuffle(random, rule.Locations))
                {
                    Habitat habitat = Habitat.Parse(key);
                    if (assigned.Count >= limit || taken.Contains(habitat.Location) || !habitatExists(habitat))
                    {
                        continue;
                    }

                    SpawnAssignment assignment = new(habitat, candidate.Bird, rule);
                    if (!place(assignment))
                    {
                        continue;
                    }

                    assigned.Add(assignment);
                    taken.Add(habitat.Location);
                    placed = true;
                    if (singleLocation)
                    {
                        return true;
                    }
                }

                if (placed)
                {
                    return true;
                }
            }

            return false;
        }

        List<Candidate> Ordered(bool seen)
        {
            List<Candidate> group = catalog.Values
                .Where(bird => seenBirds.Contains(bird.Id.Value) == seen)
                .OrderBy(bird => bird.Id.Value, StringComparer.Ordinal)
                .Select(bird => new Candidate(bird, bird.EligibleRules(season, weather, day)))
                .Where(candidate => candidate.Rules.Count > 0)
                .ToList();
            return Utils.shuffleByWeights(random, group, group.Select(c => Math.Pow(c.Rules.Sum(rule => rule.Weight), exponent)).ToList());
        }

        int unseenTarget = UnseenCount(luck, random);
        int unseenPlaced = 0;
        foreach (Candidate candidate in Ordered(seen: false))
        {
            if (assigned.Count >= limit || unseenPlaced >= unseenTarget)
            {
                break;
            }

            if (TryAssign(candidate, singleLocation: true))
            {
                unseenPlaced++;
            }
        }

        foreach (Candidate candidate in Ordered(seen: true))
        {
            if (assigned.Count >= limit)
            {
                break;
            }

            TryAssign(candidate, singleLocation: false);
        }

        return assigned;
    }

    public static int UnseenCount(double luck, Random random)
    {
        double roll = random.NextDouble();
        if (roll < Math.Clamp(TripleChance + LuckToTriple * luck, 0, 1))
        {
            return 3;
        }

        return roll < Math.Clamp(DoubleChance + luck, 0, 1) ? 2 : 1;
    }
}
