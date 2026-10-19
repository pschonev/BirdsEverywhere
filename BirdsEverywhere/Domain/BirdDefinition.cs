using System.Collections.Immutable;

namespace BirdsEverywhere.Domain;

public sealed record BirdDefinition(
    BirdId Id,
    string Name,
    string ScientificName,
    string Description,
    ImmutableArray<SpawnRule> Rules)
{
    public SpawnRule? PickRule(Season season, Weather weather, int day, Random random)
    {
        SpawnRule[] eligible = Rules.Where(rule => rule.When.MatchesDay(season, weather, day)).ToArray();
        if (eligible.Length == 0)
        {
            return null;
        }

        int choice = random.Next(eligible.Sum(rule => rule.Weight));
        foreach (SpawnRule rule in eligible)
        {
            choice -= rule.Weight;
            if (choice < 0)
            {
                return rule;
            }
        }

        throw new InvalidOperationException("A validated rule must have a positive weight.");
    }
}