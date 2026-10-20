using BirdsEverywhere.Domain;
using BirdsEverywhere.Spawners;

namespace BirdsEverywhere.Diagnostics;

public static class SpawnTarget
{
    public static bool TryResolve(BirdDefinition bird, BirdSelector selector, string currentLocation,
        Season season, Weather weather, int day, Func<Habitat, bool> habitatExists, Random random,
        out SpawnAssignment? assignment, out string error, Func<string, bool>? locationExists = null)
    {
        assignment = null;
        error = "No eligible rule and location found for this bird.";
        List<SpawnRule> rules = bird.Rules
            .Where(rule => (selector.RuleId is null || rule.Id == selector.RuleId)
                && (selector.Force || rule.When.MatchesDay(season, weather, day)))
            .ToList();
        if (rules.Count == 0)
        {
            return false;
        }

        foreach (SpawnRule rule in Utils.ShuffleByWeights(random, rules.ToList(), rules.Select(rule => rule.Weight).ToList()))
        {
            List<Habitat> habitats = rule.Locations.Select(Habitat.Parse).Where(habitatExists).ToList();
            if (selector.Location is not null)
            {
                IEnumerable<Habitat> matching = habitats.Where(habitat => habitat.Location == selector.Location);
                Habitat? chosen = matching.FirstOrDefault();
                if (chosen is null && selector.Force && locationExists?.Invoke(selector.Location) == true)
                {
                    chosen = Habitat.Parse(selector.Location);
                }

                if (chosen is not null)
                {
                    assignment = new SpawnAssignment(chosen, bird, rule);
                    return true;
                }
            }
            else if (habitats.Count > 0)
            {
                Habitat chosen = habitats.FirstOrDefault(habitat => habitat.Location == currentLocation)
                    ?? habitats[random.Next(habitats.Count)];
                assignment = new SpawnAssignment(chosen, bird, rule);
                return true;
            }
        }

        return false;
    }
}