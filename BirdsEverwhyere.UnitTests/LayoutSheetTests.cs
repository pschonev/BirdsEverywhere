using BirdsEverywhere.Content;
using BirdsEverywhere.Domain;
using NUnit.Framework;

namespace BirdsEverwhyere.UnitTests;

public class LayoutSheetTests
{
    private static readonly string _assetsDir = Path.Combine(AppContext.BaseDirectory, "assets");

    private static IEnumerable<string> LayoutIds() =>
        Directory.GetFiles(ContentPaths.LayoutsDirectory(_assetsDir), "*.json").Select(path => Path.GetFileNameWithoutExtension(path)!);

    private static SpriteLayout Load(string id) =>
        LayoutLoader.Parse(id, File.ReadAllText(ContentPaths.Layout(_assetsDir, id)), id);

    [TestCaseSource(nameof(LayoutIds))]
    public void ShippedLayout_Parses(string id) => Assert.That(Load(id).Clips, Is.Not.Empty);

    [TestCase("land_32", 9, "0  stand, walk, peck.end")]
    [TestCase("trunk_16", 5, "2  fly")]
    public void Text_ListsCellPoses(string id, int cells, string line)
    {
        IReadOnlyList<string> text = LayoutSheet.Text(Load(id), cells);

        Assert.That(text, Has.Count.EqualTo(cells + 1));
        Assert.That(text.Select(item => string.Join(' ', item.Split(' ', StringSplitOptions.RemoveEmptyEntries))), Does.Contain(string.Join(' ', line.Split(' ', StringSplitOptions.RemoveEmptyEntries))));
    }

    [Test]
    public void Svg_SizeMatchesSheetGeometry()
    {
        string svg = LayoutSheet.Svg(Load("land_32"), cols: 5);

        Assert.That(svg, Does.StartWith("<svg").And.Contain("width=\"160\"").And.Contain("height=\"88\""));
    }

    [TestCase(@"{""frameSize"":[32],""clips"":{""a"":[[0,1]]}}")]
    [TestCase(@"{""frameSize"":[32,32],""clips"":{""a"":[[0]]}}")]
    [TestCase(@"{""frameSize"":[32,32],""clips"":{""a"":[[0,[5,1]]]}}")]
    public void Parse_RejectsInvalid(string json) =>
        Assert.Throws<FormatException>(() => LayoutLoader.Parse("x", json, "x.json"));

    private static (int Width, int Height)? TextureSize(string path) =>
        File.Exists(path) ? LayoutValidator.PngSize(File.ReadAllBytes(path)) : null;

    private static IEnumerable<string> BirdIds() => ContentPaths.BirdIds(_assetsDir).OrderBy(id => id, StringComparer.Ordinal);

    [TestCaseSource(nameof(BirdIds))]
    public void ShippedBird_FitsItsLayouts(string id) =>
        Assert.That(LayoutValidator.ValidateBird(ContentTests.LoadBird(id), _assetsDir, TestBirds.Services.Layouts, TextureSize), Is.Empty);

    [TestCase(160, 64, true)]
    [TestCase(96, 96, true)]
    [TestCase(128, 32, false)]
    public void ValidateBird_ChecksTextureCells(int width, int height, bool fits) =>
        Assert.That(LayoutValidator.ValidateBird(ContentTests.LoadBird("great_tit"), _assetsDir, TestBirds.Services.Layouts, _ => (width, height)).Count == 0,
            Is.EqualTo(fits));

    [Test]
    public void ValidateBird_ReportsMissingClipAndUnknownEvent()
    {
        SpriteLayout broken = LayoutLoader.Parse("land_32", @"{""frameSize"":[32,32],""clips"":{""stand"":[[0,1,""sing""]]}}", "land_32.json");
        Dictionary<string, SpriteLayout> layouts = new() { ["land_32"] = broken };

        IEnumerable<string> messages = LayoutValidator.ValidateBird(ContentTests.LoadBird("great_tit"), _assetsDir, layouts, _ => null).Select(issue => issue.Message);

        Assert.That(messages, Has.Some.Contains("no clip 'peck'").And.Some.Contains("unknown event 'sing'"));
    }

    [TestCase("missing", "is missing or invalid")]
    [TestCase("land_32", "no clip 'swim'")]
    public void ValidateBird_RejectsInvalidStageLayout(string layoutId, string expected)
    {
        SpawnRule rule = ContentTests.LoadBird("herring_gull").Rules.First();
        BirdComposition composition = rule.Composition ?? throw new InvalidOperationException("Expected a resolved composition.");
        BirdDefinition bird = ContentTests.LoadBird("herring_gull") with
        {
            Rules = System.Collections.Immutable.ImmutableArray.Create(rule with
            {
                Stages = new Dictionary<string, BirdComposition> { ["young"] = composition with { LayoutId = layoutId } },
            }),
        };

        IEnumerable<ValidationIssue> issues = LayoutValidator.ValidateBird(bird, _assetsDir, TestBirds.Services.Layouts, _ => null);

        Assert.That(issues, Has.Some.Matches<ValidationIssue>(issue => issue is not null
            && issue.Path?.EndsWith(".stages.young.layout", StringComparison.Ordinal) == true
            && issue.Message?.Contains(expected, StringComparison.Ordinal) == true));
    }
}
