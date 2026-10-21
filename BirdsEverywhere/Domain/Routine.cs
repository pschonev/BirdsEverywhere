namespace BirdsEverywhere.Domain;

/// <summary>Idle behavior: per-tick <c>Chance</c> to schedule a cue, weighted <c>Pick</c>, and sparse follow-ups when an action ends.</summary>
public sealed record Routine(double Chance, IReadOnlyList<WeightedId> Pick, IReadOnlyDictionary<string, IReadOnlyList<WeightedId>> Next)
{
    public const string Stop = "stop";

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
