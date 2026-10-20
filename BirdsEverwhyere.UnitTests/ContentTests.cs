using BirdsEverywhere.Content;
using BirdsEverywhere.Domain;
using NUnit.Framework;

namespace BirdsEverwhyere.UnitTests;

public class ContentTests
{
    private static readonly string _assetsDir = Path.Combine(System.AppContext.BaseDirectory, "assets");

    internal static readonly SpawnPatternId[] Patterns = new[] { "GroundSpawner", "WaterSpawner", "WaterOrGroundSpawner", "SpawnableGroundSpawner", "TreeTrunkSpawner", "BushSpawner" }
        .Select(id => new SpawnPatternId(id)).ToArray();

    internal static readonly BirdTypeId[] BirdTypes = new[] { "LandBird", "WaterLandBird", "TreeTrunkBird", "BushBird" }
        .Select(id => new BirdTypeId(id)).ToArray();

    private static IEnumerable<string> BirdIds() => ContentPaths.BirdIds(_assetsDir).OrderBy(name => name);

    private static IEnumerable<string> CompatModIds() =>
        Directory.GetFiles(ContentPaths.CompatDirectory(_assetsDir), "*.json").Select(Path.GetFileNameWithoutExtension).Select(name => name!);

    internal static BirdDefinition LoadBird(string id)
    {
        string? Read(string path) => File.Exists(path) ? File.ReadAllText(path) : null;
        var (regions, groupIssues) = ContentLoader.LoadRegions(_assetsDir, System.Array.Empty<string>(), Read, ContentPaths.BirdIds(_assetsDir).ToList());
        var (bird, issues, _, _) = ContentLoader.LoadBird(_assetsDir, id, Region.AllGroups(regions), Patterns, BirdTypes, Read, File.Exists);
        Assert.That(groupIssues.Concat(issues).Where(issue => issue.Severity != IssueSeverity.Info), Is.Empty);
        Assert.That(bird, Is.Not.Null);
        Assert.That(bird!.Id.Value, Is.EqualTo(id));
        Assert.That(bird.Rules.SelectMany(rule => rule.Locations), Is.Not.Empty);
        return bird!;
    }

    [TestCaseSource(nameof(BirdIds))]
    public void Bird_TexturesExist(string id)
    {
        BirdDefinition bird = LoadBird(id);
        IEnumerable<string> textureKeys = bird.Rules.SelectMany(rule => rule.Textures.Keys).Distinct();

        Assert.That(Path.Combine(_assetsDir, id, $"{id}_icon.png"), Does.Exist);
        Assert.That(Path.Combine(_assetsDir, id, $"{id}.png"), Does.Exist);
        foreach (string key in textureKeys.Where(k => k != ""))
        {
            Assert.That(Path.Combine(_assetsDir, id, $"{id}_{key}.png"), Does.Exist);
        }
    }

    [Test]
    public void Regions_HaveSpeciesAndGroups()
    {
        var (regions, issues) = ContentLoader.LoadRegions(_assetsDir, CompatModIds(), File.ReadAllText, BirdIds().ToList());
        Assert.That(issues, Is.Empty);
        Assert.That(regions.SelectMany(region => region.Species), Is.EquivalentTo(BirdIds()));
        Assert.That(Region.AllGroups(regions).Values.SelectMany(locations => locations), Is.Not.Empty);
    }

    [Test]
    public void Regions_MergeCompatFiles()
    {
        Dictionary<string, string> files = new()
        {
            [ContentPaths.Regions("/a")] = @"{""valley"":{""species"":[""x""],""groups"":{""coast"":[""Beach""]}}}",
            [ContentPaths.CompatRegions("/a", "Some.Mod")] = @"{""valley"":{""species"":[""y""],""groups"":{""coast"":[""Beach"",""Custom_Reef""],""river"":[""Custom_River""]}}}",
        };
        var (regions, issues) = ContentLoader.LoadRegions("/a", new[] { "Some.Mod" }, path => files.GetValueOrDefault(path), new[] { "x", "y" });
        Assert.That(issues, Is.Empty);
        Region valley = regions.Single();
        Assert.That(valley.Species, Is.EqualTo(new[] { "x", "y" }));
        Assert.That(valley.Ratio, Is.EqualTo(Region.DefaultRatio));
        Assert.That(valley.Groups["coast"], Is.EqualTo(new[] { "Beach", "Custom_Reef" }));
        Assert.That(valley.Groups["river"], Is.EqualTo(new[] { "Custom_River" }));
    }

    [TestCase(@"{""valley"":{""ratio"":1.5}}", "Ratio")]
    [TestCase(@"{""valley"":{""species"":[""x""]},""sea"":{""species"":[""x""]}}", "several regions")]
    [TestCase(@"{""valley"":{""groups"":{""g"":[""A""]}},""sea"":{""groups"":{""g"":[""B""]}}}", "several regions")]
    [TestCase(@"{""valley"":{""species"":[""nope""]}}", "unknown bird")]
    [TestCase(@"{""valley"":{""colour"":1}}", "Unknown region field")]
    public void Regions_RejectInvalidFiles(string json, string message)
    {
        var (_, issues) = ContentLoader.LoadRegions("/a", System.Array.Empty<string>(), path => path == ContentPaths.Regions("/a") ? json : null, new[] { "x" });
        Assert.That(issues, Has.Some.Matches<ValidationIssue>(issue => issue!.Severity == IssueSeverity.Error && issue.Message.Contains(message)));
    }

    [TestCase("black_woodpecker", "TreeTrunkSpawner", "TreeTrunkBird", "Forest,Farm,Backwoods", 1, 4, 1, 7)]
    [TestCase("blue_tit", "SpawnableGroundSpawner", "LandBird", "Forest,Farm,Backwoods", 1, 3, 1, 7)]
    [TestCase("coal_tit", "SpawnableGroundSpawner", "LandBird", "Forest,Farm,Backwoods", 1, 3, 1, 7)]
    [TestCase("great_tit", "SpawnableGroundSpawner", "LandBird", "Forest,Farm,Backwoods", 1, 3, 1, 7)]
    [TestCase("grey_partridge", "BushSpawner", "BushBird", "Forest,Farm,Backwoods", 1, 3, 1, 7)]
    [TestCase("herring_gull", "WaterOrGroundSpawner", "WaterLandBird", "Beach", 1, 3, 1, 7)]
    public void Bird_MergedSpawnData(string id, string pattern, string birdType, string locations, int minGroupCount, int maxGroupCount, int minGroupSize, int maxGroupSize)
    {
        BirdDefinition bird = LoadBird(id);
        Assert.That(bird.Rules.Select(rule => rule.Id), Is.EquivalentTo(new[] { "spring", "summer", "fall", "winter" }));
        foreach (SpawnRule rule in bird.Rules)
        {
            Assert.That(rule.SpawnPattern.Value, Is.EqualTo(pattern));
            Assert.That(rule.BirdType.Value, Is.EqualTo(birdType));
            Assert.That(rule.Locations, Is.EqualTo(locations.Split(',')));
            Assert.That((rule.GroupCount.Min, rule.GroupCount.Max, rule.GroupSize.Min, rule.GroupSize.Max), Is.EqualTo((minGroupCount, maxGroupCount, minGroupSize, maxGroupSize)));
        }
    }
}
