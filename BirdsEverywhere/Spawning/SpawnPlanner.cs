using BirdsEverywhere.Domain;

namespace BirdsEverywhere.Spawners;

public sealed record SpawnAssignment(Habitat Habitat, BirdDefinition Bird, SpawnRule Rule, string Region = "", bool Unseen = false)
{
    public string Location => Habitat.Location;
}

public sealed record BirdWeight(BirdDefinition Bird, int Rank, double Weight);

public static class SpawnPlanner
{
    public const int MaxBirdsPerLocation = 3;

    private const double TripleChance = 0.02;
    private const double DoubleChance = 0.15;
    private const double LuckToTriple = 0.1;
    private const double LevelToTriple = 0.002;
    private const double LevelToDouble = 0.01;
    private const double LuckToExponent = 5;
    private const double LevelToExponent = 0.05;
    private const double ExtraPerArea = 0.15;
    private const double AreaReference = 3000;
    private const double LuckToExtra = 0.5;
    private const double LevelToExtra = 0.02;

    private sealed record Candidate(BirdDefinition Bird, IReadOnlyList<SpawnRule> Rules, double Weight);

    /// <summary>Luck and birding level flatten the rank curve: the exponent drops below 1, so rarer species gain weight.</summary>
    public static double Exponent(double luck, int level) =>
        Math.Clamp(1 - LuckToExponent * luck - LevelToExponent * level, 0.3, 1.5);

    /// <summary>Weight of each bird that has an eligible rule today: (ratio^rank x best rule weight)^exponent.</summary>
    public static IReadOnlyList<BirdWeight> Weights(Region region, IEnumerable<BirdDefinition> birds,
        Season season, Weather weather, int day, double luck, int level)
    {
        double exponent = Exponent(luck, level);
        List<BirdWeight> weights = new();
        foreach (BirdDefinition bird in birds)
        {
            int rank = region.Species.ToList().IndexOf(bird.Id.Value);
            IReadOnlyList<SpawnRule> rules = bird.EligibleRules(season, weather, day);
            if (rank >= 0 && rules.Count > 0)
            {
                weights.Add(new BirdWeight(bird, rank, Math.Pow(Math.Pow(region.Ratio, rank) * rules.Max(rule => rule.Weight), exponent)));
            }
        }

        return weights.OrderBy(weight => weight.Rank).ToList();
    }

    /// <summary>Plans assignments per region: unseen birds once per region, seen birds per location; tries another habitat or rule when placement fails.</summary>
    public static IReadOnlyList<SpawnAssignment> Plan(IReadOnlyList<Region> regions, IReadOnlyDictionary<string, BirdDefinition> catalog,
        IReadOnlySet<string> seenBirds, Season season, Weather weather, int day, double luck, int level, Random random,
        Func<Habitat, bool> habitatExists, Func<SpawnAssignment, bool> place,
        Func<string, int>? locationArea = null, Action<string>? skipped = null)
    {
        List<SpawnAssignment> assigned = new();

        bool TryPlace(SpawnAssignment assignment)
        {
            string label = $"{assignment.Bird.Id.Value} / {assignment.Rule.Id}";
            if (!habitatExists(assignment.Habitat))
            {
                skipped?.Invoke($"{label}: habitat {assignment.Habitat.Key} unavailable.");
                return false;
            }

            if (!place(assignment))
            {
                skipped?.Invoke($"{label}: {assignment.Habitat.Key} placed nothing; trying next habitat.");
                return false;
            }

            assigned.Add(assignment);
            return true;
        }

        List<SpawnRule> ByWeight(IEnumerable<SpawnRule> rules)
        {
            List<SpawnRule> list = rules.ToList();
            return Utils.ShuffleByWeights(random, list, list.Select(rule => rule.Weight).ToList());
        }

        List<Candidate> Ordered(IEnumerable<Candidate> pool)
        {
            List<Candidate> list = pool.ToList();
            return Utils.ShuffleByWeights(random, list, list.Select(candidate => candidate.Weight).ToList());
        }

        foreach (Region region in regions)
        {
            List<Candidate> candidates = Weights(region, region.Species.Where(catalog.ContainsKey).Select(id => catalog[id]), season, weather, day, luck, level)
                .Select(weight => new Candidate(weight.Bird, weight.Bird.EligibleRules(season, weather, day), weight.Weight))
                .ToList();

            int unseenTarget = UnseenCount(luck, level, random);
            int unseenPlaced = 0;
            foreach (Candidate candidate in Ordered(candidates.Where(c => !seenBirds.Contains(c.Bird.Id.Value))))
            {
                if (unseenPlaced >= unseenTarget)
                {
                    break;
                }

                bool placed = ByWeight(candidate.Rules).Any(rule =>
                    Utils.Shuffle(random, rule.Locations).Select(Habitat.Parse).Any(habitat => TryPlace(new SpawnAssignment(habitat, candidate.Bird, rule, region.Id, Unseen: true))));
                if (placed)
                {
                    unseenPlaced++;
                }
            }

            List<Candidate> seen = candidates.Where(c => seenBirds.Contains(c.Bird.Id.Value)).ToList();
            IEnumerable<string> locations = seen.SelectMany(c => c.Rules).SelectMany(rule => rule.Locations)
                .Select(key => Habitat.Parse(key).Location).Distinct(StringComparer.Ordinal).OrderBy(name => name, StringComparer.Ordinal);
            foreach (string location in locations)
            {
                int target = LocationCount(locationArea?.Invoke(location) ?? 0, luck, level, random);
                int placedHere = 0;
                foreach (Candidate candidate in Ordered(seen.Where(c => c.Rules.Any(rule => rule.Locations.Any(key => Habitat.Parse(key).Location == location)))))
                {
                    if (placedHere >= target)
                    {
                        break;
                    }

                    bool placed = ByWeight(candidate.Rules).Any(rule =>
                        Utils.Shuffle(random, rule.Locations).Select(Habitat.Parse).Where(habitat => habitat.Location == location)
                            .Any(habitat => TryPlace(new SpawnAssignment(habitat, candidate.Bird, rule, region.Id, Unseen: false))));
                    if (placed)
                    {
                        placedHere++;
                    }
                }
            }
        }

        return assigned;
    }

    /// <summary>Unseen birds per region and day: usually 1, with luck and level adding chances of 2 or 3.</summary>
    public static int UnseenCount(double luck, int level, Random random)
    {
        double roll = random.NextDouble();
        if (roll < Math.Clamp(TripleChance + LuckToTriple * luck + LevelToTriple * level, 0, 1))
        {
            return 3;
        }

        return roll < Math.Clamp(DoubleChance + luck + LevelToDouble * level, 0, 1) ? 2 : 1;
    }

    /// <summary>Chance of each extra species at a location; larger maps, luck, and level raise it.</summary>
    public static double ExtraChance(int area, double luck, int level) =>
        Math.Clamp(ExtraPerArea * area / AreaReference + LuckToExtra * luck + LevelToExtra * level, 0, 0.9);

    /// <summary>Seen species at a location: 1 plus up to two extras, each rolled at a falling chance.</summary>
    public static int LocationCount(int area, double luck, int level, Random random)
    {
        double chance = ExtraChance(area, luck, level);
        int count = 1;
        while (count < MaxBirdsPerLocation && random.NextDouble() < chance / count)
        {
            count++;
        }

        return count;
    }
}
