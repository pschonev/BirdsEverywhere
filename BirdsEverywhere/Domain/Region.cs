namespace BirdsEverywhere.Domain;

/// <summary>A real-world bird region: ranked species (most common first) and the location groups it owns.</summary>
public sealed record Region(
    string Id,
    double Ratio,
    IReadOnlyList<string> Species,
    IReadOnlyDictionary<string, IReadOnlyList<string>> Groups)
{
    public const double DefaultRatio = 0.6;

    public static IReadOnlyDictionary<string, IReadOnlyList<string>> AllGroups(IEnumerable<Region> regions) =>
        regions.SelectMany(region => region.Groups).ToDictionary(group => group.Key, group => group.Value, StringComparer.Ordinal);
}
