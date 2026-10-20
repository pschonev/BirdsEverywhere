using BirdsEverywhere.Domain;

namespace BirdsEverywhere.Diagnostics;

public static class RuleText
{
    private const string Eligible = "✅";
    private const string Ineligible = "❌";

    public static string Header(BirdDefinition bird, int eligibleCount, string? region = null, int? rank = null) =>
        $"{bird.Name} ({bird.Id.Value}): {eligibleCount}/{bird.Rules.Length} rules eligible today." + (region is null ? "" : rank is null ? $" Not in a region list ({region})." : $" Region {region}, rank {rank + 1}.");

    public static IEnumerable<string> Lines(SpawnRule rule, bool eligible)
    {
        yield return $"{(eligible ? Eligible : Ineligible)} {rule.Id}  weight {rule.Weight}";
        yield return $"   when:   seasons {List(rule.When.Seasons)} | weather {List(rule.When.Weathers)} | days {Ranges(rule.When.Days)} | times {Ranges(rule.When.Times)}";
        yield return $"   where:  {string.Join(", ", rule.Locations)}";
        yield return $"   spawn:  {rule.SpawnPattern.Value} → {rule.BirdType.Value} ({InitialState(rule)})";
        yield return $"   count:  {Count(rule)}";
        yield return $"   textures: {string.Join(", ", rule.Textures.Select(t => $"{(t.Key.Length == 0 ? "default" : t.Key)} {t.Value}"))}";
        if (rule.Params.ShoreDistance is IntRange shore)
        {
            yield return $"   shore distance: {shore.Min}-{shore.Max} tiles";
        }
    }

    public static bool IsAnchorPattern(SpawnPatternId pattern) =>
        pattern.Value is "TreeTrunkSpawner" or "BushSpawner";

    private static string Count(SpawnRule rule)
    {
        IntRange groups = rule.GroupCount;
        if (IsAnchorPattern(rule.SpawnPattern))
        {
            return $"{groups.Min}-{groups.Max} anchors with 1 bird = {groups.Min}-{groups.Max} birds in total";
        }
        IntRange size = rule.GroupSize;
        return $"{groups.Min}-{groups.Max} groups with {size.Min}-{size.Max} birds = {groups.Min * size.Min}-{groups.Max * size.Max} birds in total";
    }

    private static string InitialState(SpawnRule rule) => (rule.BirdType.Value, rule.SpawnPattern.Value) switch
    {
        ("LandBird", _) => "on ground, pecking",
        ("WaterLandBird", "WaterSpawner") => "on water, swimming",
        ("WaterLandBird", "WaterOrGroundSpawner") => "on water swimming, or on ground standing",
        ("WaterLandBird", _) => "on ground, standing; swims if it reaches water",
        ("TreeTrunkBird", _) => "on a tree trunk, pecking",
        ("BushBird", _) => "on ground beside a bush, runs into it when approached",
        _ => "unknown bird type",
    };

    private static string List<T>(IReadOnlyCollection<T> values) =>
        values.Count == 0 ? "any" : string.Join(",", values);

    private static string Ranges(IReadOnlyCollection<IntRange> ranges) =>
        ranges.Count == 0 ? "any" : string.Join(",", ranges.Select(r => $"{r.Min}-{r.Max}"));
}
