using BirdsEverywhere.Domain;

namespace BirdsEverywhere.Diagnostics;

public static class BirdLookup
{
    public static IReadOnlyList<BirdDefinition> FindMatches(IEnumerable<BirdDefinition> birds, string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return Array.Empty<BirdDefinition>();
        }

        string normalizedQuery = Normalize(query);
        return birds.Where(bird => Aliases(bird).Any(alias => Normalize(alias) == normalizedQuery)).ToArray();
    }

    private static IEnumerable<string> Aliases(BirdDefinition bird)
    {
        yield return bird.Id.Value;
        yield return bird.Name;
        yield return bird.ScientificName;
        if (bird.Shorthand is not null)
        {
            yield return bird.Shorthand;
        }
    }

    private static string Normalize(string value) =>
        new string(value.Where(character => !char.IsWhiteSpace(character) && character != '_').ToArray()).ToUpperInvariant();
}