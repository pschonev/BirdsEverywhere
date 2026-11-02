using System.Collections.Immutable;

namespace Birding.Domain;

public sealed record BirdDefinition(
    BirdId Id,
    string Name,
    string ScientificName,
    ImmutableArray<SpawnRule> Rules)
{
    public string? Shorthand { get; init; }

    public IReadOnlyList<SpawnRule> EligibleRules(Season season, Weather weather, int day) =>
        Rules.Where(rule => rule.When.MatchesDay(season, weather, day)).ToList();
}
