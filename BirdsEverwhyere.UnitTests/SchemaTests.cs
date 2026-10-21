using System.Text.Json;
using System.Text.Json.Nodes;
using BirdsEverywhere.Content;
using BirdsEverywhere.Domain;
using Json.Schema;
using NUnit.Framework;

namespace BirdsEverwhyere.UnitTests;

public class SchemaTests
{
    private static readonly string _assetsDir = Path.Combine(AppContext.BaseDirectory, "assets");
    private static readonly string _schemasDir = Path.Combine(AppContext.BaseDirectory, "schemas");

    private static readonly EvaluationOptions _options = new() { OutputFormat = OutputFormat.List };

    private static readonly Dictionary<string, JsonSchema> _schemas = LoadSchemas();

    private static Dictionary<string, JsonSchema> LoadSchemas()
    {
        BuildOptions build = new() { SchemaRegistry = new SchemaRegistry() };
        return new[] { "common", "routine", "action", "rule", "regions", "bird", "template", "compat", "layout" }.ToDictionary(
            name => name,
            name => JsonSchema.FromFile(Path.Combine(_schemasDir, $"{name}.schema.json"), build));
    }

    private static EvaluationResults Evaluate(string schemaName, string json)
    {
        using JsonDocument document = JsonDocument.Parse(json);
        return _schemas[schemaName].Evaluate(document.RootElement, _options);
    }

    private static bool IsValid(string schemaName, string json) => Evaluate(schemaName, json).IsValid;

    private static readonly Dictionary<string, string> _folderSchemas = new(StringComparer.Ordinal)
    {
        ["templates"] = "template",
        ["compat"] = "compat",
        ["layouts"] = "layout",
        ["routines"] = "routine",
        ["actions"] = "action",
    };

    /// <summary>Schema for an asset path relative to <c>assets/</c>; null if no schema covers it.</summary>
    private static string? SchemaFor(string relative)
    {
        string[] parts = relative.Replace('\\', '/').Split('/');
        if (parts.Length == 1)
        {
            return parts[0] == "regions.json" ? "regions" : null;
        }
        if (parts.Length != 2)
        {
            return null;
        }
        return _folderSchemas.TryGetValue(parts[0], out string? schema) ? schema
            : parts[1] == parts[0] + ".json" ? "bird" : null;
    }

    private static IEnumerable<string> AssetJson() =>
        Directory.GetFiles(_assetsDir, "*.json", SearchOption.AllDirectories)
            .Select(path => Path.GetRelativePath(_assetsDir, path)).OrderBy(path => path, StringComparer.Ordinal);

    private static IEnumerable<TestCaseData> AssetFiles() =>
        AssetJson().Where(path => SchemaFor(path) is not null)
            .Select(path => new TestCaseData(SchemaFor(path), Path.Combine(_assetsDir, path)).SetArgDisplayNames(path));

    [Test]
    public void EveryAssetJson_HasASchema() =>
        Assert.That(AssetJson().Where(path => SchemaFor(path) is null), Is.Empty);

    [TestCaseSource(nameof(AssetFiles))]
    public void ShippedAsset_MatchesSchema(string schemaName, string path)
    {
        EvaluationResults results = Evaluate(schemaName, File.ReadAllText(path));
        string errors = string.Join("; ", (results.Details ?? new List<EvaluationResults>()).Where(d => d.Errors is not null).SelectMany(d => d.Errors!.Select(e => $"{d.InstanceLocation}: {e.Value}")));
        Assert.That(results.IsValid, Is.True, errors);
    }

    private const string Identity = @"""id"":""x"",""name"":""X"",""scName"":""X x""";

    [TestCase("bird", "{" + Identity + "}", true)]
    [TestCase("bird", "{" + Identity + @",""rules"":{""a"":{""when"":{""seasons"":[""Spring""],""days"":[[1,28]],""times"":[[600,2600]]},""weight"":0.5}}}", true)]
    [TestCase("bird", "{" + Identity + @",""extra"":1}", false)]
    [TestCase("bird", @"{""id"":""x"",""name"":""X""}", false)]
    [TestCase("bird", "{" + Identity + @",""defaults"":{""when"":{""seasons"":[""monsoon""]}}}", false)]
    [TestCase("bird", "{" + Identity + @",""defaults"":{""when"":{""days"":[[1,2,3]]}}}", false)]
    [TestCase("bird", "{" + Identity + @",""defaults"":{""when"":{""days"":[[0,5]]}}}", false)]
    [TestCase("bird", "{" + Identity + @",""defaults"":{""groupSize"":[0,2]}}", false)]
    [TestCase("bird", "{" + Identity + @",""defaults"":{""weight"":0}}", false)]
    [TestCase("bird", "{" + Identity + @",""defaults"":{""locations"":[]}}", false)]
    [TestCase("bird", "{" + Identity + @",""defaults"":{""spawnPattern"":""Nope""}}", false)]
    [TestCase("bird", "{" + Identity + @",""defaults"":{""spawnParams"":{""shoreDistance"":[0,21]}}}", false)]
    [TestCase("bird", "{" + Identity + @",""defaults"":{""routine"":""land_idle"",""sounds"":{""alarm"":""crow"",""peck"":""""}}}", true)]
    [TestCase("bird", "{" + Identity + @",""defaults"":{""routine"":{""chance"":0.01,""pick"":{""peck"":1}}}}", true)]
    [TestCase("bird", "{" + Identity + @",""defaults"":{""routine"":{""pick"":{""peck"":1}}}}", false)]
    [TestCase("bird", "{" + Identity + @",""defaults"":{""sounds"":{""alarm"":1}}}", false)]
    [TestCase("bird", "{" + Identity + @",""defaults"":{""routineWeights"":{""peck"":1}}}", false)]
    [TestCase("template", @"{""template"":""base_bird"",""defaults"":{""groupSize"":[1,3]}}", true)]
    [TestCase("template", @"{""id"":""x""}", false)]
    [TestCase("regions", @"{""valley"":{""ratio"":0.5,""species"":[""a""],""groups"":{""g"":[""Beach""]}}}", true)]
    [TestCase("regions", @"{""valley"":{""ratio"":1,""species"":[""a""]}}", false)]
    [TestCase("regions", @"{""valley"":{""groups"":{""g"":[""@g2""]}}}", false)]
    [TestCase("regions", @"{""valley"":{""groups"":{""g"":[]}}}", false)]
    [TestCase("compat", @"{""valley"":{""species"":[""a""]}}", true)]
    [TestCase("compat", @"{""valley"":{""ratio"":0.5}}", false)]
    public void Schema_Verdict(string schemaName, string json, bool expected) =>
        Assert.That(IsValid(schemaName, json), Is.EqualTo(expected));

    [TestCase(@"{""frameSize"":[32,32],""clips"":{""a"":[[0,100]]}}", true)]
    [TestCase(@"{""frameSize"":[32,32],""clips"":{""a"":[[0,[500,1400,100],""flip"",""peck""]]}}", true)]
    [TestCase(@"{""frameSize"":[16,16],""clips"":{""a"":{""repeat"":{""frames"":[[0,100]],""count"":[2,7]},""end"":[[0,200]]}}}", true)]
    [TestCase(@"{""frameSize"":[32,32],""clips"":{""a"":{""frames"":[[0,100]],""loop"":true}}}", true)]
    [TestCase(@"{""frameSize"":[32],""clips"":{""a"":[[0,100]]}}", false)]
    [TestCase(@"{""frameSize"":[32,32],""clips"":{}}", false)]
    [TestCase(@"{""frameSize"":[32,32],""clips"":{""a"":[]}}", false)]
    [TestCase(@"{""frameSize"":[32,32],""clips"":{""a"":[[0]]}}", false)]
    [TestCase(@"{""frameSize"":[32,32],""clips"":{""a"":[[-1,100]]}}", false)]
    [TestCase(@"{""frameSize"":[32,32],""clips"":{""a"":[[0,100,5]]}}", false)]
    [TestCase(@"{""frameSize"":[32,32],""clips"":{""a"":[[0,[1,2,0]]]}}", false)]
    [TestCase(@"{""frameSize"":[32,32],""clips"":{""a"":{""loop"":true}}}", false)]
    [TestCase(@"{""frameSize"":[32,32],""clips"":{""a"":{""frames"":[[0,1]],""speed"":2}}}", false)]
    [TestCase(@"{""frameSize"":[32,32],""clips"":{""a"":[[0,1]]},""extra"":1}", false)]
    public void Layout_SchemaAndLoaderAgree(string json, bool valid) =>
        AssertAgreement("layout", json, valid, () => LayoutLoader.Parse("x", json, "x.json"));

    [TestCase(@"{""chance"":0.008,""pick"":{""peck"":1,""walk"":2}}", true)]
    [TestCase(@"{""chance"":1,""pick"":{""peck"":1},""next"":{""peck"":{""peck"":1,""stop"":1}}}", true)]
    [TestCase(@"{""pick"":{""peck"":1}}", false)]
    [TestCase(@"{""chance"":0,""pick"":{""peck"":1}}", false)]
    [TestCase(@"{""chance"":1.5,""pick"":{""peck"":1}}", false)]
    [TestCase(@"{""chance"":0.1,""pick"":{}}", false)]
    [TestCase(@"{""chance"":0.1,""pick"":{""peck"":0}}", false)]
    [TestCase(@"{""chance"":0.1,""pick"":{""peck"":1.5}}", false)]
    [TestCase(@"{""chance"":0.1,""pick"":{""peck"":1},""next"":{""peck"":{}}}", false)]
    [TestCase(@"{""chance"":0.1,""pick"":{""peck"":1},""cooldown"":{}}", false)]
    public void Routine_SchemaAndLoaderAgree(string json, bool valid) =>
        AssertAgreement("routine", json, valid, () => RoutineLoader.Parse(Newtonsoft.Json.Linq.JToken.Parse(json), "x.json", "$"));

    [TestCase(@"{""a"":{""do"":""stay"",""clip"":""peck""}}", true)]
    [TestCase(@"{""a"":{""do"":""walk"",""clip"":""walk"",""speed"":1,""durationMs"":[500,1400,100]}}", true)]
    [TestCase(@"{""a"":{""do"":""walk"",""clip"":""walk"",""speed"":2,""endChance"":0.005}}", true)]
    [TestCase(@"{""a"":{""do"":""flyOff"",""clip"":""fly"",""speed"":6,""climb"":2,""sound"":""alarm"",""soundChance"":0.85}}", true)]
    [TestCase(@"{""a"":{""do"":""hop"",""impulse"":2,""turn"":true}}", true)]
    [TestCase(@"{""a"":{""do"":""fly""}}", false)]
    [TestCase(@"{""a"":{""clip"":""peck""}}", false)]
    [TestCase(@"{""a"":{""do"":""stay""}}", false)]
    [TestCase(@"{""a"":{""do"":""walk"",""clip"":""walk"",""speed"":1}}", false)]
    [TestCase(@"{""a"":{""do"":""hop""}}", false)]
    [TestCase(@"{""a"":{""do"":""flyOff"",""clip"":""fly"",""speed"":6}}", false)]
    [TestCase(@"{""a"":{""do"":""stay"",""clip"":""peck"",""soundChance"":2}}", false)]
    [TestCase(@"{""a"":{""do"":""stay"",""clip"":""peck"",""speed"":-1}}", false)]
    [TestCase(@"{""a"":{""do"":""stay"",""clip"":""peck"",""extra"":1}}", false)]
    public void Action_SchemaAndLoaderAgree(string json, bool valid) =>
        AssertAgreement("action", json, valid, () => ActionLoader.ParseSet(Newtonsoft.Json.Linq.JObject.Parse(json), "x.json"));

    private static void AssertAgreement(string schemaName, string json, bool valid, Action load)
    {
        bool loads;
        try
        {
            load();
            loads = true;
        }
        catch (FormatException)
        {
            loads = false;
        }

        Assert.Multiple(() =>
        {
            Assert.That(IsValid(schemaName, json), Is.EqualTo(valid), "schema");
            Assert.That(loads, Is.EqualTo(valid), "loader");
        });
    }

    private static string[] RuleEnum(params string[] path)
    {
        JsonNode node = JsonNode.Parse(File.ReadAllText(Path.Combine(_schemasDir, "rule.schema.json")))!;
        foreach (string step in path)
        {
            node = node[step]!;
        }

        return node["enum"]!.AsArray().Select(value => value!.GetValue<string>()).ToArray();
    }

    private static IEnumerable<TestCaseData> RegistryEnums()
    {
        yield return new TestCaseData(new[] { "properties", "spawnPattern" }, ContentTests.Patterns.Select(id => id.Value).ToArray()).SetArgDisplayNames("spawnPattern");
        yield return new TestCaseData(new[] { "properties", "birdType" }, ContentTests.BirdTypes.Select(id => id.Value).ToArray()).SetArgDisplayNames("birdType");
        yield return new TestCaseData(new[] { "properties", "birdType" }, BirdTypeProfiles.ByBirdType.Keys.ToArray()).SetArgDisplayNames("birdType profiles");
    }

    [TestCaseSource(nameof(RegistryEnums))]
    public void SchemaEnum_MatchesRegistry(string[] path, string[] code) =>
        Assert.That(RuleEnum(path), Is.EquivalentTo(code));

    [Test]
    public void SchemaEnums_MatchCode()
    {
        Assert.Multiple(() =>
        {
            Assert.That(RuleEnum("properties", "when", "properties", "seasons", "items").Select(s => s.ToLowerInvariant()).Distinct(),
                Is.EquivalentTo(Enum.GetNames<Season>().Select(s => s.ToLowerInvariant())));
            Assert.That(RuleEnum("properties", "when", "properties", "weather", "items").Select(s => s.ToLowerInvariant()).Distinct(),
                Is.EquivalentTo(Enum.GetNames<Weather>().Select(s => s.ToLowerInvariant())));
        });

        JsonNode rule = JsonNode.Parse(File.ReadAllText(Path.Combine(_schemasDir, "rule.schema.json")))!;
        int max = rule["properties"]!["spawnParams"]!["properties"]!["shoreDistance"]!["items"]!["maximum"]!.GetValue<int>();
        Assert.That(max, Is.EqualTo(SpawnParams.MaxShoreDistance));
    }
}
