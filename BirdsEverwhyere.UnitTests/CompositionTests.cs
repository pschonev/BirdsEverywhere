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

    private static (BirdDefinition? Bird, IReadOnlyList<ValidationIssue> Issues) Load(string birdJson, Func<string, bool>? fileExists = null)
    {
        Dictionary<string, string> files = new()
        {
            [ContentPaths.Bird(Root, "test")] = birdJson,
            [ContentPaths.Template(Root, "base")] = _template,
        };
        (BirdDefinition? bird, IReadOnlyList<ValidationIssue> issues, _, _) = ContentLoader.LoadBird(Root, "test",
            new Dictionary<string, IReadOnlyList<string>>(), ContentTests.Patterns, ContentTests.BirdTypes, TestBirds.Definitions,
            path => files.GetValueOrDefault(path), fileExists ?? (_ => true));
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

    [TestCase("song.wav", false)]
    [TestCase("other.ogg", true)]
    public void ModAudioCue_NeedsAFileInAssetsAudio(string existing, bool error)
    {
        string existingPath = Path.Combine(ContentPaths.AudioDirectory(Root), existing);
        IReadOnlyList<ValidationIssue> issues = Load(FixtureSpecies.Replace("\"alarm\":\"crow\"", "\"alarm\":\"audio:song\"", StringComparison.Ordinal),
            path => !path.Contains("/audio/", StringComparison.Ordinal) || path == existingPath).Issues;

        Assert.That(issues.Any(issue => issue.Severity == IssueSeverity.Error && issue.Path.EndsWith(".sounds.alarm", StringComparison.Ordinal)), Is.EqualTo(error));
    }

    [Test]
    public void SoundCooldown_ReachesCompositionAndRejectsNegatives()
    {
        (BirdDefinition? bird, _) = Load(FixtureSpecies.Replace("\"sounds\"", "\"soundCooldownMs\":{\"alarm\":5000},\"sounds\"", StringComparison.Ordinal));
        IReadOnlyList<ValidationIssue> negative = Load(FixtureSpecies.Replace("\"sounds\"", "\"soundCooldownMs\":{\"alarm\":-1},\"sounds\"", StringComparison.Ordinal)).Issues;

        Assert.Multiple(() =>
        {
            Assert.That(bird!.Rules.Single().Composition!.SoundCooldownMs, Is.EquivalentTo(new Dictionary<string, int> { ["alarm"] = 5000 }));
            Assert.That(negative, Has.Some.Matches<ValidationIssue>(issue => issue.Severity == IssueSeverity.Error && issue.Message.Contains("Cooldown")));
        });
    }

    [Test]
    public void Reactions_DefaultSharedAndInline()
    {
        Reactions Resolve(string reactions) =>
            Load(FixtureSpecies.Replace("\"sounds\"", reactions + ",\"sounds\"", StringComparison.Ordinal)).Bird!.Rules.Single().Composition!.Reactions!;

        Assert.Multiple(() =>
        {
            Assert.That(FixtureComposition().Reactions, Is.EqualTo(new Reactions(new ThreatReaction(4))));
            Assert.That(Resolve("\"reactions\":\"wary\""), Is.EqualTo(new Reactions(new ThreatReaction(3, new AlertReaction(7, "stand", null, 2000), 2))));
            Assert.That(Resolve("\"reactions\":{\"threat\":{\"runningMultiplier\":1.5}}"), Is.EqualTo(new Reactions(new ThreatReaction(4, null, 1.5))));
        });
    }

    [Test]
    public void Reactions_TrunkRelocatesWithSharedDefinition()
    {
        (BirdDefinition? bird, IReadOnlyList<ValidationIssue> issues) = Load("{\"id\":\"test\",\"name\":\"Test\",\"scName\":\"Testus\",\"template\":\"base\","
            + "\"defaults\":{\"birdType\":\"TreeTrunkBird\",\"spawnPattern\":\"TreeTrunkSpawner\",\"reactions\":\"tree_hopper\"}}");

        Assert.Multiple(() =>
        {
            Assert.That(issues.Where(issue => issue.Severity == IssueSeverity.Error), Is.Empty);
            Assert.That(bird!.Rules.Single().Composition!.Reactions, Is.EqualTo(new Reactions(new ThreatReaction(6, null, 1, new RelocateReaction("otherTree", 12, 2)))));
        });
    }

    [TestCase("LandBird", "GroundSpawner", "\"tree_hopper\"", "cannot relocate")]
    [TestCase("TreeTrunkBird", "TreeTrunkSpawner", "{\"threat\":{\"alert\":{\"radius\":8,\"clip\":\"peck\"}}}", "cannot alert")]
    [TestCase("BushBird", "BushSpawner", "{\"threat\":{\"fleeRadius\":3}}", "takes no reactions")]
    [TestCase("LandBird", "GroundSpawner", "\"skittish\"", "Unknown reactions 'skittish'")]
    [TestCase("LandBird", "GroundSpawner", "{\"threat\":{\"fleeRadius\":5,\"alert\":{\"radius\":5,\"clip\":\"stand\"}}}", "must be larger than fleeRadius")]
    [TestCase("LandBird", "GroundSpawner", "{\"threat\":{\"alert\":{\"radius\":7,\"clip\":\"crouch\"}}}", "has no clip 'crouch'")]
    public void Reactions_Errors(string birdType, string pattern, string reactions, string message)
    {
        string json = "{\"id\":\"test\",\"name\":\"Test\",\"scName\":\"Testus\",\"template\":\"base\",\"defaults\":{\"birdType\":\"" + birdType
            + "\",\"spawnPattern\":\"" + pattern + "\",\"reactions\":" + reactions + "}}";
        (BirdDefinition? bird, IReadOnlyList<ValidationIssue> issues) = Load(json);
        IEnumerable<string> messages = issues.Where(issue => issue.Severity == IssueSeverity.Error).Select(issue => issue.Message)
            .Concat(bird is null ? Enumerable.Empty<string>() : bird.Rules.SelectMany(rule =>
                LayoutValidator.Requirement(TestBirds.Services.Layouts[BirdTypeProfiles.ByBirdType[birdType].LayoutId], BirdTypeProfiles.ByBirdType[birdType], rule.Composition)));

        Assert.That(messages, Has.Some.Contains(message));
    }

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
    public void ActionOverride_ChangesSingleFieldsAndKeepsTheRest()
    {
        (BirdDefinition? bird, IReadOnlyList<ValidationIssue> issues) = Load(FixtureSpecies.Replace(@"""sounds""",
            @"""actions"":{""flee"":{""speed"":3,""sound"":null},""walk"":{""durationMs"":[200,400]}},""sounds""", StringComparison.Ordinal));
        Assert.That(issues.Where(issue => issue.Severity != IssueSeverity.Info), Is.Empty);
        IReadOnlyDictionary<string, ActionDefinition> actions = bird!.Rules.Single().Composition!.Actions!;

        Assert.Multiple(() =>
        {
            Assert.That(actions["flee"].Speed, Is.EqualTo(3));
            Assert.That(actions["flee"].Climb, Is.EqualTo(2));
            Assert.That(actions["flee"].Sound, Is.Null);
            Assert.That(actions["walk"].DurationMs, Is.EqualTo(new StepRange(200, 400, 1)));
            Assert.That(actions["sleep"].EndChance, Is.EqualTo(0.003));
        });
    }

    [TestCase(@"""actions"":{""sing"":{""do"":""walk"",""clip"":""walk"",""speed"":1,""durationMs"":[100,200]}}", "must be 'stay' with a clip")]
    [TestCase(@"""actions"":{""walk"":{""do"":""stay""}}", "runs 'walk' as 'walk', not 'stay'")]
    [TestCase(@"""actions"":{""flee"":{""speed"":null}}", "needs clip, speed and climb")]
    public void ActionOverride_Errors(string actions, string message) =>
        Assert.That(Load(FixtureSpecies.Replace(@"""sounds""", actions + @",""sounds""", StringComparison.Ordinal)).Issues,
            Has.Some.Matches<ValidationIssue>(issue => issue.Severity == IssueSeverity.Error && issue.Message.Contains(message)));

    [Test]
    public void CustomAction_OnTrunkBirdIsAnError() =>
        Assert.That(Load(@"{""id"":""test"",""name"":""Test"",""scName"":""Testus"",""template"":""base"",
            ""defaults"":{""birdType"":""TreeTrunkBird"",""spawnPattern"":""TreeTrunkSpawner"",""actions"":{""sing"":{""do"":""stay"",""clip"":""peck""}}}}").Issues,
            Has.Some.Matches<ValidationIssue>(issue => issue.Severity == IssueSeverity.Error && issue.Message.Contains("no idle routine for custom actions")));

    [Test]
    public void CustomStayAction_IsPickedAndPlaysItsClip()
    {
        (BirdDefinition? definition, IReadOnlyList<ValidationIssue> issues) = Load(FixtureSpecies.Replace(
            @"""pick"":{""walk"":3,""peck"":1}", @"""pick"":{""sing"":1}", StringComparison.Ordinal).Replace(@"""sounds"":{",
            @"""actions"":{""sing"":{""do"":""stay"",""clip"":""peck"",""sound"":""song""}},""sounds"":{""song"":""crow"",", StringComparison.Ordinal));
        Assert.That(issues.Where(issue => issue.Severity != IssueSeverity.Info), Is.Empty);
        LandBird bird = new() { BirdID = 42, State = BehaviorStatus.Stopped, IsReplica = true, Services = TestBirds.Services,
            Composition = definition!.Rules.Single().Composition!, sprite = new AnimatedSprite(), IdleRevision = 1, IdleCue = new IdleCue(0, "sing", 3) };

        bird.update(new GameTime(TimeSpan.Zero, TimeSpan.FromMilliseconds(16)), null!);

        Assert.Multiple(() =>
        {
            Assert.That(bird.State, Is.EqualTo(BehaviorStatus.Stopped));
            Assert.That(bird.sprite.CurrentAnimation, Is.Not.Null.And.Not.Empty);
        });
    }

    [Test]
    public void ActionWalkSpeed_DrivesFixtureBird()
    {
        BirdComposition composition = FixtureComposition();
        Dictionary<string, ActionDefinition> actions = new(composition.Actions!) { ["walk"] = composition.Actions!["walk"] with { DurationMs = new StepRange(300, 300, 1) } };
        LandBird bird = new() { BirdID = 42, Services = TestBirds.Services, Composition = composition with { Actions = actions }, sprite = new AnimatedSprite() };

        bird.StartWalk();

        Assert.That(bird.WalkTimer, Is.EqualTo(300));
    }

    [TestCase("LandBird", "land_actions")]
    [TestCase("WaterLandBird", "waterland_actions")]
    [TestCase("TreeTrunkBird", "trunk_actions")]
    [TestCase("BushBird", "bush_actions")]
    public void ShippedActionSets_CoverTheirBirdType(string birdType, string set)
    {
        BirdTypeProfile profile = BirdTypeProfiles.ByBirdType[birdType];
        IReadOnlyDictionary<string, ActionDefinition> actions = TestBirds.Services.Actions[set];

        Assert.That(profile.DefaultActions, Is.EqualTo(set));
        Assert.That(actions.ToDictionary(entry => entry.Key, entry => entry.Value.Do), Is.EquivalentTo(profile.ActionPrimitives));
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
        BirdComposition fromBird = Newtonsoft.Json.Linq.JArray.Parse(BirdSnapshots.Serialize(new[] { bird }))[0]["Composition"]!.ToObject<BirdComposition>()!;

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
