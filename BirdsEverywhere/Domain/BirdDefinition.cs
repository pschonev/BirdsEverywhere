using System.Collections.Immutable;

namespace BirdsEverywhere.Domain;

public sealed record BirdDefinition(
    BirdId Id,
    string Name,
    string ScientificName,
    string Description,
    ImmutableArray<SpawnRule> Rules)
{
    public IReadOnlyList<SpawnRule> EligibleRules(Season season, Weather weather, int day) =>
        Rules.Where(rule => rule.When.MatchesDay(season, weather, day)).ToList();
}