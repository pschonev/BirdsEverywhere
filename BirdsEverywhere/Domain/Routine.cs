namespace BirdsEverywhere.Domain;

/// <summary>Idle behavior: per-tick <c>Chance</c> to schedule a cue, weighted <c>Pick</c>, sparse follow-ups when an action ends, and <c>Rare</c> actions tried first.</summary>
public sealed record Routine(double Chance, IReadOnlyList<WeightedId> Pick, IReadOnlyDictionary<string, IReadOnlyList<WeightedId>> Next,
    IReadOnlyList<RareAction>? Rare = null)
{
    public const string Stop = "stop";

    /// <summary>Whether any pick or rare action passes <paramref name="allowed"/>; scheduling draws nothing otherwise.</summary>
    public bool HasOptions(Func<string, bool> allowed) =>
        Pick.Any(option => allowed(option.Id)) || (Rare ?? Array.Empty<RareAction>()).Any(rare => allowed(rare.Id));

    /// <summary>Each allowed rare action whose conditions match rolls its chance in order; otherwise one weighted roll over the allowed picks. Without rare actions and filtering this equals <see cref="Roll"/>.</summary>
    public string? Choose(Random random, Func<string, bool> allowed, IdleWorld? world)
    {
        foreach (RareAction rare in Rare ?? Array.Empty<RareAction>())
        {
            if (allowed(rare.Id) && rare.Matches(world) && random.NextDouble() < rare.Chance)
            {
                return rare.Id;
            }
        }
        List<WeightedId> options = Pick.Where(option => allowed(option.Id)).ToList();
        return options.Count == 0 ? null : Roll(options, random);
    }

    /// <summary>One <c>Next(total)</c> walked cumulatively, so a legacy list with duplicates consumes the RNG identically.</summary>
    public static string Roll(IReadOnlyList<WeightedId> options, Random random)
    {
        int total = 0;
        foreach (WeightedId option in options)
        {
            total += option.Weight;
        }

        int roll = random.Next(total);
        foreach (WeightedId option in options)
        {
            roll -= option.Weight;
            if (roll < 0)
            {
                return option.Id;
            }
        }
        throw new InvalidOperationException("A validated routine has positive weights.");
    }

    /// <summary>Follow-up after <paramref name="action"/> ends, or <see cref="Stop"/> without drawing when none is defined.</summary>
    public string After(string action, Random random) =>
        Next.TryGetValue(action, out IReadOnlyList<WeightedId>? options) ? Roll(options, random) : Stop;
}
