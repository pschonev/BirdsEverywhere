using BirdsEverywhere.Content;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace BirdsEverwhyere.UnitTests;

public class TemplateMergerTests
{
    [Test]
    public void Resolve_RemovedAndReplacedValues_HaveOnlyEffectiveSources()
    {
        const string root = "/virtual/assets";
        string birdFile = ContentPaths.Bird(root, "test");
        string baseFile = ContentPaths.Template(root, "base");
        Dictionary<string, JObject> files = new()
        {
            [baseFile] = JObject.Parse(@"{""defaults"":{""textures"":{"""":1,""female"":2},""when"":{""days"":[[1,28]]}},""rules"":{""spring"":{""when"":{""seasons"":[""spring""]},""textures"":{""juvenile"":3}}}}"),
            [birdFile] = JObject.Parse(@"{""template"":""base"",""defaults"":{""textures"":{""female"":null,""male"":4}},""rules"":{""spring"":{""textures"":{""juvenile"":null},""when"":null}}}"),
        };

        (JObject document, IReadOnlyDictionary<string, string> sources, _) = TemplateMerger.Resolve(birdFile,
            name => ContentPaths.Template(root, name), path => files[path]);

        Assert.Multiple(() =>
        {
            Assert.That(document["rules"]!["spring"]!["textures"]!["female"], Is.Null);
            Assert.That(document["rules"]!["spring"]!["textures"]!["juvenile"], Is.Null);
            Assert.That(sources.ContainsKey("$.rules.spring.textures.female"), Is.False);
            Assert.That(sources.ContainsKey("$.rules.spring.textures.juvenile"), Is.False);
            Assert.That(sources["$.rules.spring.textures.male"], Is.EqualTo(birdFile));
            Assert.That(sources.ContainsKey("$.rules.spring.when.seasons"), Is.False);
        });
    }

    [Test]
    public void Resolve_ObjectReplacedByScalar_DropsDescendantSources()
    {
        const string root = "/virtual/assets";
        string birdFile = ContentPaths.Bird(root, "test");
        string baseFile = ContentPaths.Template(root, "base");
        Dictionary<string, JObject> files = new()
        {
            [baseFile] = JObject.Parse(@"{""rules"":{""spring"":{""textures"":{"""":1}}}}"),
            [birdFile] = JObject.Parse(@"{""template"":""base"",""rules"":{""spring"":{""textures"":""invalid""}}}"),
        };

        (_, IReadOnlyDictionary<string, string> sources, _) = TemplateMerger.Resolve(birdFile,
            name => ContentPaths.Template(root, name), path => files[path]);

        Assert.Multiple(() =>
        {
            Assert.That(sources["$.rules.spring.textures"], Is.EqualTo(birdFile));
            Assert.That(sources.ContainsKey("$.rules.spring.textures."), Is.False);
        });
    }

    [Test]
    public void Resolve_RuleRemovalOfDefault_DropsSource()
    {
        const string root = "/virtual/assets";
        string birdFile = ContentPaths.Bird(root, "test");
        Dictionary<string, JObject> files = new()
        {
            [birdFile] = JObject.Parse(@"{""defaults"":{""textures"":{""female"":2,""male"":1}},""rules"":{""spring"":{""textures"":{""female"":null}}}}"),
        };

        (JObject document, IReadOnlyDictionary<string, string> sources, _) = TemplateMerger.Resolve(birdFile,
            name => ContentPaths.Template(root, name), path => files[path]);

        Assert.Multiple(() =>
        {
            Assert.That(document["rules"]!["spring"]!["textures"]!["female"], Is.Null);
            Assert.That(sources.ContainsKey("$.rules.spring.textures.female"), Is.False);
            Assert.That(sources["$.rules.spring.textures.male"], Is.EqualTo(birdFile));
        });
    }

    [Test]
    public void Resolve_RemovedThenReaddedRule_DoesNotRestoreOldValues()
    {
        const string root = "/virtual/assets";
        string birdFile = ContentPaths.Bird(root, "test");
        string middleFile = ContentPaths.Template(root, "middle");
        string baseFile = ContentPaths.Template(root, "base");
        Dictionary<string, JObject> files = new()
        {
            [baseFile] = JObject.Parse(@"{""defaults"":{""textures"":{"""":1}},""rules"":{""spring"":{""groupSize"":[4,8]}}}"),
            [middleFile] = JObject.Parse(@"{""template"":""base"",""rules"":{""spring"":null}}"),
            [birdFile] = JObject.Parse(@"{""template"":""middle"",""rules"":{""spring"":{""groupCount"":[1,2]}}}"),
        };

        (JObject document, IReadOnlyDictionary<string, string> sources, _) = TemplateMerger.Resolve(birdFile,
            name => ContentPaths.Template(root, name), path => files[path]);

        Assert.Multiple(() =>
        {
            Assert.That(document["rules"]!["spring"]!["groupSize"], Is.Null);
            Assert.That(sources.ContainsKey("$.rules.spring.groupSize"), Is.False);
            Assert.That(sources["$.rules.spring.textures."], Is.EqualTo(baseFile));
            Assert.That(sources["$.rules.spring.groupCount"], Is.EqualTo(birdFile));
        });
    }

    [Test]
    public void Merge_PreservesRuleOverridesAndRemovesNullKeys()
    {
        JObject inherited = JObject.Parse(@"{""defaults"":{""locations"":[""Forest""],""textures"":{"""":1,""female"":2}},""rules"":{""winter"":{""groupSize"":[4,10],""textures"":{""juvenile"":1}},""summer"":{""when"":{""seasons"":[""summer""]}}}}");
        JObject bird = JObject.Parse(@"{""defaults"":{""locations"":[""Farm""],""textures"":{""female"":null,""male"":3},""groupSize"":[2,8]},""rules"":{""summer"":null,""winter"":{""textures"":{""juvenile"":null}}}}");

        JObject merged = TemplateMerger.Merge(inherited, bird);
        JObject effective = TemplateMerger.Merge((JObject)merged["defaults"]!, (JObject)merged["rules"]!["winter"]!);

        Assert.Multiple(() =>
        {
            Assert.That(merged["rules"]!["summer"], Is.Null);
            Assert.That(effective["locations"]!.Values<string>(), Is.EqualTo(new[] { "Farm" }));
            Assert.That(effective["groupSize"]!.Values<int>(), Is.EqualTo(new[] { 4, 10 }));
            Assert.That(effective["textures"]![""], Is.EqualTo(new JValue(1)));
            Assert.That(effective["textures"]!["male"], Is.EqualTo(new JValue(3)));
            Assert.That(effective["textures"]!["female"], Is.Null);
            Assert.That(effective["textures"]!["juvenile"], Is.Null);
        });
    }

    [Test]
    public void LoadBird_NewRuleWithoutWhen_IsAnError()
    {
        const string root = "/virtual/assets";
        Dictionary<string, string> files = new()
        {
            [ContentPaths.Bird(root, "test")] = @"{""id"":""test"",""name"":""Test"",""scName"":""Testus"",""template"":""base"",""rules"":{""wintre"":{""groupSize"":[2,3]}}}",
            [ContentPaths.Template(root, "base")] = @"{""defaults"":{""locations"":[""Forest""],""spawnPattern"":""GroundSpawner"",""birdType"":""LandBird"",""groupCount"":[1,2],""groupSize"":[1,2],""textures"":{"""":1}},""rules"":{""winter"":{""when"":{""seasons"":[""winter""]}}}}",
        };

        (var bird, var issues, _, _) = ContentLoader.LoadBird(root, "test",
            new Dictionary<string, IReadOnlyList<string>>(), ContentTests.Patterns, ContentTests.BirdTypes, TestBirds.Services.Routines,
            path => files.GetValueOrDefault(path), _ => true);

        Assert.Multiple(() =>
        {
            Assert.That(bird, Is.Null);
            Assert.That(issues, Has.Some.Matches<ValidationIssue>(issue => issue!.Severity == IssueSeverity.Error && issue.Path == "$.rules.wintre"));
        });
    }

    [TestCase("{\"defaults\":{\"groupCount\":[3,2]}}", "$.rules.spring.groupCount")]
    [TestCase("{\"defaults\":{\"spawnPattern\":\"NotASpawner\"}}", "$.rules.spring.spawnPattern")]
    [TestCase("{\"defaults\":{\"locations\":[\"@missing\"]}}", "$.rules.spring.locations")]
    [TestCase("{\"defaults\":{\"textures\":{\"missing\":1}}}", "$.rules.spring.textures.missing")]
    [TestCase("{\"rules\":{\"spring\":{\"when\":{\"seasons\":[\"invalid\"]}}}}", "$.rules.spring.when.seasons")]
    [TestCase("{\"rules\":{\"spring\":{\"weight\":0}}}", "$.rules.spring.weight")]
    [TestCase("{\"defaults\":{\"spawnParams\":{\"shoreDistance\":[5,2]}}}", "$.rules.spring.spawnParams.shoreDistance")]
    [TestCase("{\"defaults\":{\"spawnParams\":{\"shoreDistance\":[0,99]}}}", "$.rules.spring.spawnParams.shoreDistance")]
    [TestCase("{\"defaults\":{\"routine\":\"missing\"}}", "$.rules.spring.routine")]
    [TestCase("{\"defaults\":{\"routine\":{\"chance\":2,\"pick\":{\"peck\":1}}}}", "$.rules.spring.routine")]
    [TestCase("{\"defaults\":{\"routine\":{\"chance\":0.1,\"pick\":{\"rest\":1}}}}", "$.rules.spring.routine.pick")]
    [TestCase("{\"defaults\":{\"routine\":{\"chance\":0.1,\"pick\":{\"peck\":1},\"next\":{\"walk\":{\"stop\":1}}}}}", "$.rules.spring.routine.next.walk")]
    [TestCase("{\"defaults\":{\"routine\":{\"chance\":0.1,\"pick\":{\"peck\":1},\"next\":{\"peck\":{\"hop\":1}}}}}", "$.rules.spring.routine.next.peck.hop")]
    public void LoadBird_InvalidOverride_ReportsBirdSource(string overrideJson, string expectedPath)
    {
        const string root = "/virtual/assets";
        string birdFile = ContentPaths.Bird(root, "test");
        string templateFile = ContentPaths.Template(root, "base");
        Dictionary<string, string> files = new()
        {
            [templateFile] = @"{""defaults"":{""locations"":[""Forest""],""spawnPattern"":""GroundSpawner"",""birdType"":""LandBird"",""groupCount"":[1,2],""groupSize"":[1,2],""textures"":{"""":1}},""rules"":{""spring"":{""when"":{""seasons"":[""spring""]}}}}",
            [birdFile] = @"{""id"":""test"",""name"":""Test"",""scName"":""Testus"",""template"":""base""}",
        };
        JObject bird = JObject.Parse(files[birdFile]);
        bird.Merge(JObject.Parse(overrideJson));
        files[birdFile] = bird.ToString();

        (var loaded, var issues, _, _) = ContentLoader.LoadBird(root, "test",
            new Dictionary<string, IReadOnlyList<string>>(), ContentTests.Patterns, ContentTests.BirdTypes, TestBirds.Services.Routines,
            path => files.GetValueOrDefault(path), path => !path.EndsWith("missing.png", System.StringComparison.Ordinal));

        Assert.Multiple(() =>
        {
            Assert.That(loaded, Is.Null);
            Assert.That(issues, Has.Some.Matches<ValidationIssue>(issue =>
                issue!.Severity == IssueSeverity.Error && issue.Path == expectedPath && issue.File == birdFile), string.Join("; ", issues));
        });
    }

    [TestCase("defaults", "[]")]
    [TestCase("rules", "[]")]
    [TestCase("rules", "null")]
    public void LoadBird_WrongType_DoesNotThrow(string field, string value)
    {
        const string root = "/virtual/assets";
        string birdFile = ContentPaths.Bird(root, "test");
        string text = $"{{\"id\":\"test\",\"name\":\"Test\",\"scName\":\"Testus\",\"{field}\":{value}}}";

        (var bird, var issues, _, _) = ContentLoader.LoadBird(root, "test",
            new Dictionary<string, IReadOnlyList<string>>(), ContentTests.Patterns, ContentTests.BirdTypes, TestBirds.Services.Routines,
            path => path == birdFile ? text : null, _ => true);

        Assert.Multiple(() =>
        {
            Assert.That(bird, Is.Null);
            Assert.That(issues, Has.Some.Matches<ValidationIssue>(issue => issue!.Severity == IssueSeverity.Error && issue.File == birdFile));
        });
    }

    [TestCase("missing", "Missing content file")]
    [TestCase("cycle", "Template cycle")]
    public void LoadBird_InvalidTemplateChain_ReportsError(string template, string message)
    {
        const string root = "/virtual/assets";
        string birdFile = ContentPaths.Bird(root, "test");
        Dictionary<string, string> files = new()
        {
            [birdFile] = $"{{\"id\":\"test\",\"template\":\"{template}\"}}",
            [ContentPaths.Template(root, "cycle")] = @"{""template"":""cycle""}",
        };

        (var bird, var issues, _, _) = ContentLoader.LoadBird(root, "test",
            new Dictionary<string, IReadOnlyList<string>>(), ContentTests.Patterns, ContentTests.BirdTypes, TestBirds.Services.Routines,
            path => files.GetValueOrDefault(path), _ => true);

        Assert.Multiple(() =>
        {
            Assert.That(bird, Is.Null);
            Assert.That(issues, Has.Some.Matches<ValidationIssue>(issue => issue!.Severity == IssueSeverity.Error && issue.Message.Contains(message)), string.Join("; ", issues));
        });
    }

    [Test]
    public void LoadBird_UnknownRuleField_ReportsError()
    {
        const string root = "/virtual/assets";
        string birdFile = ContentPaths.Bird(root, "test");
        string text = @"{""id"":""test"",""name"":""Test"",""scName"":""Testus"",""rules"":{""spring"":{""when"":{},""locations"":[""Forest""],""spawnPattern"":""GroundSpawner"",""birdType"":""LandBird"",""groupCount"":[1,2],""groupSize"":[1,2],""textures"":{"""":1},""typo"":true}}}";

        (var bird, var issues, _, _) = ContentLoader.LoadBird(root, "test",
            new Dictionary<string, IReadOnlyList<string>>(), ContentTests.Patterns, ContentTests.BirdTypes, TestBirds.Services.Routines,
            path => path == birdFile ? text : null, _ => true);

        Assert.That(bird, Is.Null);
        Assert.That(issues, Has.Some.Matches<ValidationIssue>(issue => issue!.Severity == IssueSeverity.Error && issue.Message.Contains("typo")));
    }

}