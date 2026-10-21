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
        return new[] { "rule", "regions", "bird", "template", "compat" }.ToDictionary(
            name => name,
            name => JsonSchema.FromFile(Path.Combine(_schemasDir, $"{name}.schema.json"), build));
    }

    private static EvaluationResults Evaluate(string schemaName, string json)
    {
        using JsonDocument document = JsonDocument.Parse(json);
        return _schemas[schemaName].Evaluate(document.RootElement, _options);
    }

    private static bool IsValid(string schemaName, string json) => Evaluate(schemaName, json).IsValid;

    private static IEnumerable<TestCaseData> AssetFiles()
    {
        foreach (string id in ContentPaths.BirdIds(_assetsDir).OrderBy(name => name))
        {
            yield return new TestCaseData("bird", Path.Combine(_assetsDir, id, $"{id}.json")).SetArgDisplayNames($"bird/{id}");
        }

        foreach (string path in Directory.GetFiles(Path.Combine(_assetsDir, "templates"), "*.json"))
        {
            yield return new TestCaseData("template", path).SetArgDisplayNames($"template/{Path.GetFileName(path)}");
        }

        yield return new TestCaseData("regions", Path.Combine(_assetsDir, "regions.json")).SetArgDisplayNames("regions");

        foreach (string path in Directory.GetFiles(ContentPaths.CompatDirectory(_assetsDir), "*.json"))
        {
            yield return new TestCaseData("compat", path).SetArgDisplayNames($"compat/{Path.GetFileName(path)}");
        }
    }

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

    private static string[] RuleEnum(params string[] path)
    {
        JsonNode node = JsonNode.Parse(File.ReadAllText(Path.Combine(_schemasDir, "rule.schema.json")))!;
        foreach (string step in path)
        {
            node = node[step]!;
        }

        return node["enum"]!.AsArray().Select(value => value!.GetValue<string>()).ToArray();
    }

    [Test]
    public void SchemaEnums_MatchCode()
    {
        Assert.Multiple(() =>
        {
            Assert.That(RuleEnum("properties", "spawnPattern"), Is.EquivalentTo(ContentTests.Patterns.Select(id => id.Value)));
            Assert.That(RuleEnum("properties", "birdType"), Is.EquivalentTo(ContentTests.BirdTypes.Select(id => id.Value)));
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
