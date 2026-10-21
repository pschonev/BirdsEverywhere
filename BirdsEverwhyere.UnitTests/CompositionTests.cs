using BirdsEverywhere;
using BirdsEverywhere.BirdTypes;
using BirdsEverywhere.BirdTypes.Behaviors;
using BirdsEverywhere.Content;
using BirdsEverywhere.Domain;
using BirdsEverywhere.Spawners;
using Microsoft.Xna.Framework;
using NUnit.Framework;
using StardewValley;

namespace BirdsEverwhyere.UnitTests;

// Phase 10 step 3: routine and sound roles from JSON. Bird-touching tests need --arch x64.
public class CompositionTests
{
    private const string Root = "/virtual/assets";

    private static readonly string _template =
        @"{""defaults"":{""locations"":[""Forest""],""spawnPattern"":""GroundSpawner"",""birdType"":""LandBird"",""groupCount"":[1,2],""groupSize"":[1,2],""textures"":{"""":1}},""rules"":{""spring"":{""when"":{""seasons"":[""spring""]}}}}";

    /// <summary>A species defined only in JSON on the shipped land sheet: own idle weights, follow-up and sounds.</summary>
    private const string FixtureSpecies =
        @"{""id"":""test"",""name"":""Test"",""scName"":""Testus"",""template"":""base"",""defaults"":{
            ""routine"":{""chance"":0.02,""pick"":{""walk"":3,""peck"":1},""next"":{""peck"":{""sleep"":1}}},
            ""sounds"":{""alarm"":""crow"",""peck"":""""}}}";

    private static (BirdDefinition? Bird, IReadOnlyList<ValidationIssue> Issues) Load(string birdJson)
    {
        Dictionary<string, string> files = new()
        {
            [ContentPaths.Bird(Root, "test")] = birdJson,
            [ContentPaths.Template(Root, "base")] = _template,
        };
        (BirdDefinition? bird, IReadOnlyList<ValidationIssue> issues, _, _) = ContentLoader.LoadBird(Root, "test",
            new Dictionary<string, IReadOnlyList<string>>(), ContentTests.Patterns, ContentTests.BirdTypes, TestBirds.Services.Routines,
            path => files.GetValueOrDefault(path), _ => true);
        return (bird, issues);
    }

    private static BirdComposition FixtureComposition()
    {
        (BirdDefinition? bird, IReadOnlyList<ValidationIssue> issues) = Load(FixtureSpecies);
        Assert.That(issues.Where(issue => issue.Severity != IssueSeverity.Info), Is.Empty);
        return bird!.Rules.Single().Composition!;
    }

    [Test]
    public void FixtureSpecies_ResolvesInlineRoutineAndMergesSounds()
    {
        BirdComposition composition = FixtureComposition();

        Assert.Multiple(() =>
        {
            Assert.That(composition.Routine!.Chance, Is.EqualTo(0.02));
            Assert.That(composition.Routine.Pick, Is.EqualTo(new[] { new WeightedId("walk", 3), new WeightedId("peck", 1) }));
            Assert.That(composition.Sounds, Is.EquivalentTo(new Dictionary<string, string> { ["alarm"] = "crow", ["flap"] = "batFlap", ["peck"] = "" }));
        });
    }

    [TestCase("great_tit", "land_idle", "SpringBirds")]
    [TestCase("herring_gull", "waterland_idle", "seagulls")]
    public void ShippedSpecies_UseBirdTypeDefaults(string id, string routine, string alarm)
    {
        BirdComposition composition = ContentTests.LoadBird(id).Rules.First().Composition!;

        Assert.Multiple(() =>
        {
            Assert.That(composition.Routine, Is.SameAs(TestBirds.Services.Routines[routine]));
            Assert.That(composition.Sounds["alarm"], Is.EqualTo(alarm));
        });
    }

    [Test]
    public void SharedRoutineReference_Resolves() =>
        Assert.That(Load(FixtureSpecies.Replace(@"""routine"":{""chance"":0.02,""pick"":{""walk"":3,""peck"":1},""next"":{""peck"":{""sleep"":1}}}", @"""routine"":""land_idle""", StringComparison.Ordinal))
            .Bird!.Rules.Single().Composition!.Routine, Is.SameAs(TestBirds.Services.Routines["land_idle"]));

    [Test]
    public void UnknownSoundRole_Warns() =>
        Assert.That(Load(FixtureSpecies.Replace(@"""alarm"":""crow""", @"""song"":""crow""", StringComparison.Ordinal)).Issues,
            Has.Some.Matches<ValidationIssue>(issue => issue.Severity == IssueSeverity.Warn && issue.Path.EndsWith(".sounds.song", StringComparison.Ordinal)));

    [Test]
    public void TrunkBirdWithRoutine_IsAnError() =>
        Assert.That(Load(@"{""id"":""test"",""name"":""Test"",""scName"":""Testus"",""template"":""base"",
            ""defaults"":{""birdType"":""TreeTrunkBird"",""spawnPattern"":""TreeTrunkSpawner"",""routine"":""land_idle""}}").Issues,
            Has.Some.Matches<ValidationIssue>(issue => issue.Severity == IssueSeverity.Error && issue.Message.Contains("has no idle routine")));

    [Test]
    public void FixtureBird_SchedulesOnlyItsOwnActions()
    {
        LandBird bird = new() { BirdID = 42, Services = TestBirds.Services, Composition = FixtureComposition(), sprite = new AnimatedSprite() };

        Assert.That(bird.IdleRoutine, Is.SameAs(bird.Composition.Routine));
        HashSet<string> picked = new();
        Random random = new(1);
        for (int i = 0; i < 200; i++)
        {
            picked.Add(Routine.Roll(bird.IdleRoutine.Pick, random));
        }
        Assert.That(picked, Is.EquivalentTo(new[] { "walk", "peck" }));
    }

    [Test]
    public void FixtureBird_PeckFollowUpComesFromRoutine()
    {
        LandBird bird = new() { BirdID = 42, Services = TestBirds.Services, Composition = FixtureComposition(), sprite = new AnimatedSprite() };

        bird.StartPeckAnimation();
        bird.sprite.CurrentAnimation[^1].frameStartBehavior(null!);

        Assert.That(bird.State, Is.EqualTo(BehaviorStatus.Sleeping));
    }

    [Test]
    public void EveryProfileAction_IsARuntimeAction() =>
        Assert.That(BirdTypeProfiles.ByBirdType.Values.SelectMany(profile => profile.Actions.Concat(profile.FollowUps.Values.SelectMany(next => next))).Distinct(),
            Is.SubsetOf(BirdActionIds.All));

    [Test]
    public void Composition_SurvivesPlanAndBirdSnapshots()
    {
        BirdComposition composition = FixtureComposition();
        BirdPlacement placement = new("test", "spring", "LandBird", "GroundSpawner", new Vector2(1, 2), null,
            new List<IntRange>(), "test", 42, Composition: composition);
        DailySpawnPlan plan = new(new Dictionary<string, IReadOnlyList<BirdPlacement>> { ["Forest"] = new[] { placement } });

        BirdComposition fromPlan = MessageJson.Deserialize<DailySpawnPlan>(MessageJson.Serialize(plan)).Locations["Forest"][0].Composition!;
        LandBird bird = new() { BirdID = 42, Composition = composition, sprite = new AnimatedSprite() };
        // Full bird deserialization loads textures; the composition token is what the reader binds.
        BirdComposition fromBird = Newtonsoft.Json.Linq.JArray.Parse(MessageJson.Serialize(new List<BirdCritter> { bird }))[0]["composition"]!.ToObject<BirdComposition>()!;

        foreach (BirdComposition copy in new[] { fromPlan, fromBird })
        {
            Assert.Multiple(() =>
            {
                Assert.That(copy.Routine!.Chance, Is.EqualTo(composition.Routine!.Chance));
                Assert.That(copy.Routine.Pick, Is.EqualTo(composition.Routine.Pick));
                Assert.That(copy.Routine.Next["peck"], Is.EqualTo(composition.Routine.Next["peck"]));
                Assert.That(copy.Sounds, Is.EquivalentTo(composition.Sounds));
            });
        }
    }
}
