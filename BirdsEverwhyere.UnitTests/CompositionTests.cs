using System.Collections.Immutable;
using BirdsEverywhere;
using BirdsEverywhere.BirdTypes;
using BirdsEverywhere.BirdTypes.Behaviors;
using BirdsEverywhere.Content;
using BirdsEverywhere.Domain;
using BirdsEverywhere.Spawners;
using Microsoft.Xna.Framework;
using Newtonsoft.Json.Linq;
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

    private const string FamilySpecies =
        @"{""id"":""test"",""name"":""Duck family"",""scName"":""Anas testus"",""template"":""base"",""defaults"":{
            ""birdType"":""WaterLandBird"",""spawnPattern"":""WaterSpawner"",""groupSize"":[4,6],
            ""spawnParams"":{""group"":{""formation"":""cluster"",""radius"":5,""members"":{
                ""parent"":{""count"":[1,2],""stage"":""adult""},""duckling"":{""count"":[3,4],""stage"":""young""}}}},
            ""reactions"":{""threat"":[{""if"":{""groupHasRole"":""parent"",""nearWater"":true,""stage"":""young""},""flee"":{""radius"":3,""do"":""swimAway""}},
                {""if"":{},""flee"":{""radius"":4,""do"":""flyOff""}}]},
            ""stages"":{""adult"":{""sounds"":{""alarm"":""crow""}},""young"":{""routine"":{""chance"":0.02,""pick"":{""followParent"":1}},""actions"":{""flee"":{""do"":""swimAway"",""clip"":""swim"",""speed"":2},""followParent"":{""do"":""follow"",""target"":""groupRole:parent"",""speedFactor"":1.2}}}}}}";

    private static (BirdDefinition? Bird, IReadOnlyList<ValidationIssue> Issues) Load(string birdJson, Func<string, bool>? fileExists = null,
        SharedDefinitions? definitions = null)
    {
        Dictionary<string, string> files = new()
        {
            [ContentPaths.Bird(Root, "test")] = birdJson,
            [ContentPaths.Template(Root, "base")] = _template,
        };
        (BirdDefinition? bird, IReadOnlyList<ValidationIssue> issues, _, _) = ContentLoader.LoadBird(Root, "test",
            new Dictionary<string, IReadOnlyList<string>>(), ContentTests.Patterns, ContentTests.BirdTypes, definitions ?? TestBirds.Definitions,
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

    [Test]
    public void FamilyFixture_ResolvesStagesAndMemberRoles()
    {
        (BirdDefinition? bird, IReadOnlyList<ValidationIssue> issues) = Load(FamilySpecies);

        Assert.That(issues.Where(issue => issue.Severity == IssueSeverity.Error), Is.Empty);
        SpawnRule rule = bird!.Rules.Single();
        Assert.Multiple(() =>
        {
            Assert.That(rule.Params.Group!.Members!["duckling"], Is.EqualTo(new GroupMember(new IntRange(3, 4), "young")));
            Assert.That(rule.Stages!["adult"].Sounds["alarm"], Is.EqualTo("crow"));
            Assert.That(rule.Stages["young"].Routine!.Pick, Is.EqualTo(new[] { new WeightedId("followParent", 1) }));
            Assert.That(rule.Stages["young"].Actions!["followParent"].Target, Is.EqualTo("groupRole:parent"));
            Assert.That(rule.Stages["young"].Actions["flee"].Do, Is.EqualTo("swimAway"));
            Assert.That(rule.Stages["young"].Reactions!.Select(new ReactionContext(new[] { "parent" }, true,
                BirdsEverywhere.Domain.Season.Spring, Weather.Sun, 900, "young")).Flee?.Do, Is.EqualTo("swimAway"));
        });
    }

    [Test]
    public void FamilyFixture_StageExtendsSharedRoutineWithoutDroppingRareActions()
    {
        Routine parent = TestBirds.Definitions.Routines["waterland_idle"] with
        {
            Rare = new[] { new RareAction("walk", 0.25, new SpawnConditions(ImmutableArray<BirdsEverywhere.Domain.Season>.Empty,
                ImmutableArray.Create(Weather.Rain), ImmutableArray<IntRange>.Empty, ImmutableArray<IntRange>.Empty)) },
        };
        SharedDefinitions definitions = TestBirds.Definitions with
        {
            Routines = TestBirds.Definitions.Routines.Concat(new[] { new KeyValuePair<string, Routine>("family_idle", parent) })
                .ToDictionary(entry => entry.Key, entry => entry.Value, StringComparer.Ordinal),
        };
        string fixture = FamilySpecies.Replace(@"""routine"":{""chance"":0.02,""pick"":{""followParent"":1}}",
            @"""routine"":{""extends"":""family_idle"",""pick"":{""followParent"":1}}", StringComparison.Ordinal);

        (BirdDefinition? bird, IReadOnlyList<ValidationIssue> issues) = Load(fixture, definitions: definitions);

        Assert.That(issues.Where(issue => issue.Severity == IssueSeverity.Error), Is.Empty);
        Routine routine = bird!.Rules.Single().Stages!["young"].Routine!;
        RareAction rare = routine.Rare!.Single();
        Assert.Multiple(() =>
        {
            Assert.That(routine.Chance, Is.EqualTo(parent.Chance));
            Assert.That(routine.Pick, Is.EqualTo(parent.Pick.Append(new WeightedId("followParent", 1))));
            Assert.That(rare.Id, Is.EqualTo("walk"));
            Assert.That(rare.Chance, Is.EqualTo(0.25));
            Assert.That(rare.When.Weathers, Is.EqualTo(new[] { Weather.Rain }));
        });
    }

    [Test]
    public void FamilyFixture_StageSelectsLayout()
    {
        string fixture = FamilySpecies.Replace(@"""young"":{""routine""", @"""young"":{""layout"":""waterland_32"",""routine""", StringComparison.Ordinal);

        (BirdDefinition? bird, IReadOnlyList<ValidationIssue> issues) = Load(fixture);

        Assert.That(issues.Where(issue => issue.Severity == IssueSeverity.Error), Is.Empty);
        Assert.That(bird!.Rules.Single().Stages!["young"].LayoutId, Is.EqualTo("waterland_32"));
    }

    [Test]
    public void FamilyFixture_RejectsUnknownStages()
    {
        IReadOnlyList<ValidationIssue> issues = Load(FamilySpecies.Replace(@"""stage"":""young""", @"""stage"":""missing""", StringComparison.Ordinal)).Issues;

        Assert.That(issues, Has.Some.Matches<ValidationIssue>(issue => issue.Severity == IssueSeverity.Error && issue.Path.EndsWith(".stage", StringComparison.Ordinal)));
    }

    [Test]
    public void FamilyFixture_RejectsUnknownReactionRole()
    {
        IReadOnlyList<ValidationIssue> issues = Load(FamilySpecies.Replace(@"""groupHasRole"":""parent""", @"""groupHasRole"":""stranger""", StringComparison.Ordinal)).Issues;

        Assert.That(issues, Has.Some.Matches<ValidationIssue>(issue => issue.Severity == IssueSeverity.Error
            && issue.Path.EndsWith(".threat.if.groupHasRole", StringComparison.Ordinal)));
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
    public void Reactions_ChooseFirstMatchingAlternativeOrLegacyFallback()
    {
        Reactions reactions = ReactionLoader.Parse(JObject.Parse(
            @"{""threat"":[{""if"":{""groupHasRole"":""parent"",""nearWater"":true},""flee"":{""radius"":3,""do"":""swimAway""}},
                {""if"":{""nearWater"":true},""flee"":{""radius"":5,""do"":""flyOff""}}]}"),
            new Reactions(new ThreatReaction(4)), "fixture", "$.reactions");

        Assert.Multiple(() =>
        {
            Assert.That(reactions.Select(new ReactionContext(new[] { "parent" }, true, BirdsEverywhere.Domain.Season.Spring, Weather.Sun, 900, "young")).Flee?.Do, Is.EqualTo("swimAway"));
            Assert.That(reactions.Select(new ReactionContext(Array.Empty<string>(), true, BirdsEverywhere.Domain.Season.Spring, Weather.Sun, 900, "young")).Threat.FleeRadius, Is.EqualTo(5));
            Assert.That(reactions.Select(new ReactionContext(Array.Empty<string>(), false, BirdsEverywhere.Domain.Season.Spring, Weather.Sun, 900, "young")).Threat.FleeRadius, Is.EqualTo(4));
            Assert.That(reactions.Select(new ReactionContext(Array.Empty<string>(), false, BirdsEverywhere.Domain.Season.Spring, Weather.Sun, 900, "young")).Flee, Is.Null);
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

    /// <summary>A diving water bird on the shipped water-land sheet: <c>peck</c> stands in for the dive and worm-pull clips.</summary>
    private const string Diver =
        "{\"id\":\"test\",\"name\":\"Test\",\"scName\":\"Testus\",\"template\":\"base\",\"defaults\":{\"birdType\":\"WaterLandBird\",\"spawnPattern\":\"WaterSpawner\","
        + "\"actions\":{\"dive\":{\"extends\":\"forage.dive\",\"clip\":\"peck\"},\"grub\":{\"extends\":\"forage.wormPull\",\"clip\":\"peck\",\"sound\":null}},"
        + "\"routine\":{\"chance\":0.01,\"pick\":{\"peck\":2,\"dive\":1},\"rare\":{\"grub\":{\"chance\":0.1,\"when\":{\"weather\":[\"rain\"]}}}}}}";

    [Test]
    public void Diver_ExtendsSharedForageActions()
    {
        (BirdDefinition? bird, IReadOnlyList<ValidationIssue> issues) = Load(Diver);
        Assert.That(issues.Where(issue => issue.Severity != IssueSeverity.Info), Is.Empty);
        BirdComposition composition = bird!.Rules.Single().Composition!;

        Assert.Multiple(() =>
        {
            Assert.That(composition.Actions!["dive"], Is.EqualTo(new ActionDefinition("dive", "peck", null, 1, null, null, null, null, null, false, null, new StepRange(2000, 5000, 100), 3)));
            Assert.That(composition.Actions["grub"].Sound, Is.Null);
            Assert.That(composition.Routine!.Rare!.Single().Id, Is.EqualTo("grub"));
        });
    }

    [TestCase("\"actions\":{\"dive\":{\"extends\":\"forage.dive\",\"clip\":\"peck\"}},\"sounds\"", "cannot do 'dive'")]
    [TestCase("\"actions\":{\"dive\":{\"extends\":\"forage.swim\"}},\"sounds\"", "Unknown shared action 'forage.swim'")]
    [TestCase("\"routine\":{\"chance\":0.1,\"pick\":{\"peck\":1},\"rare\":{\"sing\":{\"chance\":0.1}}},\"sounds\"", "cannot perform action 'sing'")]
    public void ForageActions_Errors(string replacement, string message) =>
        Assert.That(Load(FixtureSpecies.Replace("\"sounds\"", replacement, StringComparison.Ordinal)).Issues,
            Has.Some.Matches<ValidationIssue>(issue => issue.Severity == IssueSeverity.Error && issue.Message.Contains(message)));

    [Test]
    public void Diver_ReplicaDivesHidesAndSnapsToTheResurfacePosition()
    {
        BirdComposition composition = Load(Diver).Bird!.Rules.Single().Composition!;
        WaterLandBird replica = new() { BirdID = 5, State = BehaviorStatus.Swimming, IsReplica = true, Services = TestBirds.Services, Composition = composition,
            sprite = new AnimatedSprite(), IdleRevision = 1, IdleCue = new IdleCue(0, "dive", 3) };
        GameTime tick = new(TimeSpan.Zero, TimeSpan.FromMilliseconds(16));

        replica.update(tick, null!);
        Assert.That(replica.State, Is.EqualTo(BehaviorStatus.Diving));
        Assert.That(replica.Hidden, Is.False);

        WaterLandBird owner = new() { State = BehaviorStatus.Diving, Hidden = true, DiveTimer = 3000, IdleRevision = 2, sprite = new AnimatedSprite() };
        replica.ApplyReplicaState(owner);
        Assert.That(replica.Hidden, Is.True);

        owner = new() { State = BehaviorStatus.Swimming, Hidden = false, position = new Vector2(320, 128), IdleRevision = 2, sprite = new AnimatedSprite() };
        replica.ApplyReplicaState(owner);
        Assert.Multiple(() =>
        {
            Assert.That(replica.Hidden, Is.False);
            Assert.That(replica.State, Is.EqualTo(BehaviorStatus.Swimming));
            Assert.That(replica.position, Is.EqualTo(new Vector2(320, 128)));
        });
    }

    [TestCase("\"birdType\":\"TreeTrunkBird\",\"spawnPattern\":\"GroundSpawner\"", "needs TreeTrunkSpawner")]
    [TestCase("\"birdType\":\"LandBird\",\"spawnPattern\":\"BushSpawner\"", "needs a tile spawn pattern")]
    [TestCase("\"birdType\":\"BushBird\",\"spawnPattern\":\"BushSpawner\",\"spawnParams\":{\"group\":{\"formation\":\"line\"}}", "takes no group layout")]
    [TestCase("\"spawnParams\":{\"group\":{\"formation\":\"vee\"}}", "Unknown formation 'vee'")]
    [TestCase("\"spawnParams\":{\"group\":{\"formation\":\"line\",\"radius\":0}}", "Radius must be")]
    [TestCase("\"spawnParams\":{\"speciesDistance\":50}", "Species distance must be")]
    public void Placement_Errors(string defaults, string message) =>
        Assert.That(Load("{\"id\":\"test\",\"name\":\"Test\",\"scName\":\"Testus\",\"template\":\"base\",\"defaults\":{" + defaults + "}}").Issues,
            Has.Some.Matches<ValidationIssue>(issue => issue.Severity == IssueSeverity.Error && issue.Message.Contains(message)));

    [Test]
    public void Placement_GroupReachesTheRule() =>
        Assert.That(Load("{\"id\":\"test\",\"name\":\"Test\",\"scName\":\"Testus\",\"template\":\"base\",\"defaults\":{\"spawnParams\":"
            + "{\"group\":{\"formation\":\"scattered\",\"radius\":5,\"groupSpacing\":12},\"speciesDistance\":4}}}").Bird!.Rules.Single().Params,
            Is.EqualTo(new SpawnParams(null, new GroupLayout(Formation.Scattered, 5, 1, 12), 4)));

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

    [TestCase(@"""actions"":{""sing"":{""do"":""walk"",""clip"":""walk"",""speed"":1,""durationMs"":[100,200]}}", "cannot do 'walk' in a custom action")]
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
