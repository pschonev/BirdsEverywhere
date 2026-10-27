using BirdsEverywhere;
using BirdsEverywhere.BirdTypes;
using BirdsEverywhere.BirdTypes.Behaviors;
using BirdsEverywhere.Domain;
using BirdsEverywhere.Multiplayer;
using BirdsEverywhere.Registries;
using BirdsEverywhere.Spawners;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using NUnit.Framework;
using StardewValley;

namespace BirdsEverwhyere.UnitTests;

// Touches game types: run with `dotnet test --arch x64` on Apple Silicon.
public class GameTypeRegressionTests
{
    [TestCase(0.008, 50, 110)]
    [TestCase(0.003, 12, 55)]
    public void IdleSchedule_RetainsActionRateAndVariety(double chance, int minimumActions, int maximumActions)
    {
        IdleProbe bird = new() { BirdID = 42 };
        Routine routine = Uniform(chance, "sleep", "peck", "hop", "walk");
        Dictionary<BirdAction, int> counts = new();

        for (int tick = 0; tick < 10_000; tick++)
        {
            BirdAction action = bird.Next(routine);
            if (action != BirdAction.None)
            {
                counts[action] = counts.GetValueOrDefault(action) + 1;
            }
        }

        Assert.Multiple(() =>
        {
            Assert.That(counts.Values.Sum(), Is.InRange(minimumActions, maximumActions));
            Assert.That(counts.Keys, Is.EquivalentTo(new[] { BirdAction.Sleep, BirdAction.Peck, BirdAction.Hop, BirdAction.Walk }));
        });
    }

    [Test]
    public void IdleSchedule_CanActOnFirstEligibleTick()
    {
        IdleProbe bird = new() { BirdID = 42 };

        Assert.That(bird.Next(Uniform(1, "hop")), Is.EqualTo(BirdAction.Hop));
    }

    private static Routine Uniform(double chance, params string[] actions) =>
        new(chance, actions.Select(action => new WeightedId(action, 1)).ToList(), new Dictionary<string, IReadOnlyList<WeightedId>>());

    private sealed class IdleProbe : BirdCritter
    {
        public override string BirdTypeName => "IdleProbe";

        public BirdAction Next(Routine routine) => AdvanceIdle(routine, eligible: true);
    }

    [Test]
    public void CurrentAnimatedSprite_CapturesCurrentAnimation()
    {
        AnimatedSprite sprite = new AnimatedSprite();
        sprite.setCurrentAnimation(new List<FarmerSprite.AnimationFrame>
        {
            new FarmerSprite.AnimationFrame(3, 100),
            new FarmerSprite.AnimationFrame(4, 200),
        });

        CurrentAnimatedSprite current = new CurrentAnimatedSprite(sprite);

        Assert.That(current.CurrentAnimation, Is.EqualTo(new List<(int, int)> { (3, 100), (4, 200) }));
    }

    [Test]
    public void StageLayout_SetsSpriteFrameSizeAndSurvivesSnapshot()
    {
        WaterLandBird bird = new()
        {
            BirdID = 42, BirdName = "test", Texture = "", Services = TestBirds.Services,
            Composition = new BirdComposition(null, new Dictionary<string, string>(), LayoutId: "trunk_16"),
            sprite = new AnimatedSprite { SpriteWidth = 32, SpriteHeight = 32 },
        };

        bird.ApplyLayout();
        BirdState restored = JsonConvert.DeserializeObject<BirdState>(JsonConvert.SerializeObject(BirdSnapshots.Capture(bird)))
            ?? throw new InvalidOperationException("Expected a snapshot.");

        Assert.Multiple(() =>
        {
            Assert.That(bird.sprite.SpriteWidth, Is.EqualTo(16));
            Assert.That(bird.sprite.SpriteHeight, Is.EqualTo(16));
            Assert.That(restored.Sprite.SpriteWidth, Is.EqualTo(16));
            Assert.That(restored.Sprite.SpriteHeight, Is.EqualTo(16));
        });
    }

    [Test]
    public void FamilyMetadata_SurvivesSnapshotRoundTrip()
    {
        WaterLandBird bird = new() { BirdID = 42, GroupId = 7, Role = "duckling", Stage = "young", sprite = new AnimatedSprite() };

        BirdState? restored = JsonConvert.DeserializeObject<BirdState>(JsonConvert.SerializeObject(BirdSnapshots.Capture(bird)));

        Assert.Multiple(() =>
        {
            Assert.That(restored?.GroupId, Is.EqualTo(7));
            Assert.That(restored?.Role, Is.EqualTo("duckling"));
            Assert.That(restored?.Stage, Is.EqualTo("young"));
        });
    }

    [Test]
    public void FamilyMovement_SurvivesSnapshotAndReplicaUpdate()
    {
        WaterLandBird owner = new()
        {
            BirdID = 42, GroupId = 7, Role = "duckling", Stage = "young", State = BehaviorStatus.SwimmingAway,
            position = new Vector2(64, 64), sprite = new AnimatedSprite(),
        };
        BirdState state = BirdSnapshots.Capture(owner) with { FollowRole = "parent", FollowSpeedFactor = 1.2f, SwimDirection = new Vector2(1f, 0f) };
        BirdState? restored = JsonConvert.DeserializeObject<BirdState>(JsonConvert.SerializeObject(state));
        Assert.That(restored, Is.Not.Null);
        WaterLandBird replica = new() { IsReplica = true, sprite = new AnimatedSprite(), Services = TestBirds.Services };
        owner.RestoreExtras(restored!);
        replica.ApplyReplicaState(owner);

        Assert.Multiple(() =>
        {
            Assert.That(restored!.FollowRole, Is.EqualTo("parent"));
            Assert.That(restored.SwimDirection, Is.EqualTo(new Vector2(1f, 0f)));
            Assert.That(replica.State, Is.EqualTo(BehaviorStatus.SwimmingAway));
            Assert.That(replica.FollowRole, Is.EqualTo("parent"));
            Assert.That(replica.SwimDirection, Is.EqualTo(new Vector2(1f, 0f)));
        });
        Vector2 before = replica.position;
        Assert.That(replica.update(new GameTime(TimeSpan.Zero, TimeSpan.FromMilliseconds(16)), null!), Is.False);
        Assert.That(replica.position.X, Is.GreaterThan(before.X));
    }

    [Test]
    public void FamilyFlee_SwimmingYoungTakesParentDirection()
    {
        WaterLandBird parent = new() { State = BehaviorStatus.SwimmingAway };
        parent.RestoreExtras(new BirdState("WaterLandBird", 1, "duck", "", BehaviorStatus.SwimmingAway, Vector2.Zero,
            Vector2.Zero, false, 0, 0, 0, 0, new CurrentAnimatedSprite(new AnimatedSprite()), 0, 0, 0, null, null)
            with { SwimDirection = new Vector2(1, 0) });
        WaterLandBird young = new() { State = BehaviorStatus.SwimmingAway };

        young.MatchGroupFlee(parent);

        Assert.Multiple(() =>
        {
            Assert.That(young.SwimDirection, Is.EqualTo(new Vector2(1, 0)));
            Assert.That(young.flip, Is.True);
        });
    }

    [Test]
    public void ReplicaState_UsesOwnerPoseAndInterpolatesPosition()
    {
        LandBird replica = new() { IsReplica = true, position = new Vector2(0, 0), sprite = new AnimatedSprite(), Services = TestBirds.Services };
        LandBird owner = new()
        {
            BirdID = 23,
            position = new Vector2(100, 50),
            State = BehaviorStatus.FlyingAway,
            flip = true,
            yOffset = -6,
            sprite = new AnimatedSprite(),
        };
        owner.sprite.currentFrame = 7;

        replica.ApplyReplicaState(owner);

        Assert.Multiple(() =>
        {
            Assert.That(replica.position, Is.EqualTo(Vector2.Zero));
            Assert.That(replica.State, Is.EqualTo(BehaviorStatus.FlyingAway));
            Assert.That(replica.flip, Is.True);
            Assert.That(replica.yOffset, Is.EqualTo(-6));
            Assert.That(replica.sprite.currentFrame, Is.EqualTo(7));
        });

        Assert.That(replica.update(new GameTime(TimeSpan.Zero, TimeSpan.FromMilliseconds(16)), null!), Is.False);
        Assert.That(replica.position, Is.EqualTo(new Vector2(46, 20)));
        Assert.That(replica.State, Is.EqualTo(BehaviorStatus.FlyingAway));
    }

    [Test]
    public void ReplicaFlight_KeepsAnimationProgressAcrossSnapshots()
    {
        LandBird owner = new() { State = BehaviorStatus.FlyingAway, position = new Vector2(100, 0), flip = true, sprite = new AnimatedSprite() };
        owner.sprite.setCurrentAnimation(new List<FarmerSprite.AnimationFrame>
        {
            new(6, 70), new(7, 60)
        });
        owner.sprite.loop = true;
        LandBird replica = new() { IsReplica = true, sprite = new AnimatedSprite(), Services = TestBirds.Services };
        GameTime tick = new(TimeSpan.Zero, TimeSpan.FromMilliseconds(75));

        AnimatedSprite firstSprite = new();
        firstSprite.setCurrentAnimation(new List<FarmerSprite.AnimationFrame> { new(6, 70), new(7, 60) });
        firstSprite.loop = true;
        LandBird firstSnapshot = new() { State = owner.State, position = owner.position, flip = owner.flip, sprite = firstSprite };
        replica.ApplyReplicaState(firstSnapshot);
        replica.update(tick, null!);
        Assert.That(replica.sprite.currentFrame, Is.EqualTo(7));

        owner.position = new Vector2(106, 0);
        owner.sprite.currentFrame = 6;
        AnimatedSprite secondSprite = new();
        secondSprite.setCurrentAnimation(new List<FarmerSprite.AnimationFrame> { new(6, 70), new(7, 60) });
        secondSprite.loop = true;
        LandBird secondSnapshot = new() { State = owner.State, position = owner.position, flip = owner.flip, sprite = secondSprite };
        replica.ApplyReplicaState(secondSnapshot);

        Assert.That(replica.sprite.currentFrame, Is.EqualTo(7));
        Assert.That(replica.update(tick, null!), Is.False);
        Assert.That(replica.position.X, Is.GreaterThan(48));
    }

    [Test]
    public void ReplicaIdle_PlaysCueOnceAndRejectsOlderRevision()
    {
        LandBird replica = new() { IsReplica = true, State = BehaviorStatus.Stopped, sprite = new AnimatedSprite(), Services = TestBirds.Services };
        LandBird owner = new() { State = BehaviorStatus.Stopped, IdleTick = 10, IdleRevision = 1,
            IdleCue = new IdleCue(12, "sleep", 4), sprite = new AnimatedSprite() };
        GameTime tick = new(TimeSpan.Zero, TimeSpan.FromMilliseconds(16));

        replica.ApplyReplicaState(owner);
        replica.update(tick, null!);
        Assert.That(replica.State, Is.EqualTo(BehaviorStatus.Stopped));
        replica.update(tick, null!);
        Assert.That(replica.State, Is.EqualTo(BehaviorStatus.Sleeping));

        owner.IdleTick = 11;
        replica.ApplyReplicaState(owner);
        Assert.That(replica.State, Is.EqualTo(BehaviorStatus.Sleeping));
        owner.IdleRevision = 2;
        owner.IdleCue = null;
        replica.ApplyReplicaState(owner);
        Assert.That(replica.IdleCue, Is.Null);
    }

    [Test]
    public void ReplicaIdle_FleeRevokesPendingCue()
    {
        LandBird replica = new() { IsReplica = true, State = BehaviorStatus.Stopped, sprite = new AnimatedSprite(), Services = TestBirds.Services };
        LandBird owner = new() { State = BehaviorStatus.Stopped, IdleTick = 10, IdleRevision = 1,
            IdleCue = new IdleCue(12, "sleep", 4), sprite = new AnimatedSprite() };
        replica.ApplyReplicaState(owner);

        owner.State = BehaviorStatus.FlyingAway;
        owner.IdleRevision = 2;
        owner.IdleCue = null;
        replica.ApplyReplicaState(owner);
        replica.update(new GameTime(TimeSpan.Zero, TimeSpan.FromMilliseconds(16)), null!);

        Assert.That(replica.State, Is.EqualTo(BehaviorStatus.FlyingAway));
        Assert.That(replica.IdleCue, Is.Null);
    }

    [Test]
    public void ReplicaRun_ContinuesWithoutSnapshots()
    {
        BushBird replica = new() { IsReplica = true, State = BehaviorStatus.Stopped, sprite = new AnimatedSprite(), Services = TestBirds.Services };
        BushBird owner = new() { State = BehaviorStatus.Running, position = new Vector2(100, 0), flip = true,
            sprite = new AnimatedSprite() };
        replica.ApplyReplicaState(owner);
        GameTime tick = new(TimeSpan.Zero, TimeSpan.FromMilliseconds(16));

        replica.update(tick, null!);
        float firstPosition = replica.position.X;
        replica.update(tick, null!);

        Assert.That(replica.position.X, Is.GreaterThan(firstPosition));
    }

    [Test]
    public void ReplicaAnimation_DoesNotInvokeGameplayCallback()
    {
        bool callbackCalled = false;
        AnimatedSprite animation = new();
        animation.setCurrentAnimation(new List<FarmerSprite.AnimationFrame>
        {
            new(0, 30), new(1, 30, secondaryArm: false, flip: false, _ => callbackCalled = true)
        });
        animation.loop = true;
        LandBird replica = new() { IsReplica = true, sprite = new AnimatedSprite(), Services = TestBirds.Services };
        LandBird owner = new() { State = BehaviorStatus.FlyingAway, sprite = animation };

        replica.ApplyReplicaState(owner);
        replica.update(new GameTime(TimeSpan.Zero, TimeSpan.FromMilliseconds(35)), null!);

        Assert.That(replica.sprite.currentFrame, Is.EqualTo(1));
        Assert.That(callbackCalled, Is.False);
    }

    [Test]
    public void ReplicaClip_KeepsSoundEventsButDropsEndCallback()
    {
        LandBird owner = new() { State = BehaviorStatus.Pecking, sprite = new AnimatedSprite(), Services = TestBirds.Services };
        LandBird replica = new() { IsReplica = true, State = BehaviorStatus.Pecking, sprite = new AnimatedSprite(), Services = TestBirds.Services };

        owner.StartPeckAnimation();
        replica.StartPeckAnimation();

        static IEnumerable<string> Callbacks(LandBird bird) => bird.sprite.CurrentAnimation
            .SelectMany(frame => frame.frameStartBehavior?.GetInvocationList() ?? Array.Empty<Delegate>()).Select(callback => callback.Method.Name);
        Assert.Multiple(() =>
        {
            Assert.That(Callbacks(owner), Does.Contain("DonePecking").And.Contain("PlayPeck"));
            Assert.That(Callbacks(replica), Does.Not.Contain("DonePecking").And.Contain("PlayPeck"));
        });
    }

    [Test]
    public void BirdSnapshot_RoundTrip_PreservesIdleSchedule()
    {
        LandBird bird = new() { BirdID = 42, State = BehaviorStatus.Stopped, IdleTick = 10, IdleRevision = 3,
            IdleCue = new IdleCue(30, "peck", 12), sprite = new AnimatedSprite() };

        string json = BirdSnapshots.Serialize(new[] { bird });

        Assert.That(json, Does.Contain("\"IdleRevision\":3"));
        Assert.That(json, Does.Contain("\"Tick\":30"));
        Assert.That(json, Does.Contain("\"Seed\":12"));
    }

    [Test]
    public void BirdState_RoundTrip_KeepsTypeSpecificFields()
    {
        TreeTrunkBird trunk = new() { BirdID = 7, BirdName = "black_woodpecker", AnchorTile = new Vector2(12, 34), LocationName = "Forest",
            PeckTimer = 1500, State = BehaviorStatus.Relocating, Relocations = 1, RelocateTarget = new Vector2(15, 30), sprite = new AnimatedSprite() };
        LandBird land = new() { BirdID = 8, FlightOffset = 0.25f, WalkTimer = 700, flip = true, sprite = new AnimatedSprite() };

        List<BirdState> states = Newtonsoft.Json.JsonConvert.DeserializeObject<List<BirdState>>(BirdSnapshots.Serialize(new BirdCritter[] { trunk, land }))!;

        Assert.Multiple(() =>
        {
            Assert.That(states[0] with { Sprite = null! }, Is.EqualTo(BirdSnapshots.Capture(trunk) with { Sprite = null! }));
            Assert.That(states[0].AnchorTile, Is.EqualTo(new Vector2(12, 34)));
            Assert.That(states[0].PeckTimer, Is.EqualTo(1500));
            Assert.That(states[0].Relocations, Is.EqualTo(1));
            Assert.That(states[0].RelocateTarget, Is.EqualTo(new Vector2(15, 30)));
            Assert.That(states[1].FlightOffset, Is.EqualTo(0.25f));
            Assert.That(states[1].WalkTimer, Is.EqualTo(700));
            Assert.That(states[1].Flip, Is.True);
        });
    }

    [Test]
    public void ReplicaTrunk_FollowsOwnerRelocation()
    {
        TreeTrunkBird replica = new() { IsReplica = true, State = BehaviorStatus.Pecking, sprite = new AnimatedSprite(), Services = TestBirds.Services };
        TreeTrunkBird owner = new() { State = BehaviorStatus.Relocating, Relocations = 1, RelocateTarget = new Vector2(3, 0), sprite = new AnimatedSprite() };

        replica.ApplyReplicaState(owner);
        replica.update(new GameTime(TimeSpan.Zero, TimeSpan.FromMilliseconds(16)), null!);

        Assert.Multiple(() =>
        {
            Assert.That(replica.State, Is.EqualTo(BehaviorStatus.Relocating));
            Assert.That(replica.Relocations, Is.EqualTo(1));
            Assert.That(replica.position.X, Is.GreaterThan(0f), "committed flight towards the target tree");
        });
    }

    [Test]
    public void BirdSnapshot_RoundTrip_PreservesVisitAndSequence()
    {
        LocationBirdsState state = new(IMessage.CurrentVersion, "Forest", "visit-1", 42, "[]", "request-1");

        LocationBirdsState restored = MessageJson.Deserialize<LocationBirdsState>(MessageJson.Serialize(state));

        Assert.That(restored, Is.EqualTo(state));
    }

    [Test]
    public void BirdHandoff_RoundTrip_PreservesNewOwnerAndVisit()
    {
        LocationBirdsHandoff handoff = new(IMessage.CurrentVersion, "Forest", 42, "visit-1", 7, "[]");

        LocationBirdsHandoff restored = MessageJson.Deserialize<LocationBirdsHandoff>(MessageJson.Serialize(handoff));

        Assert.That(restored, Is.EqualTo(handoff));
    }

    [Test]
    public void DailySpawnPlan_RoundTrip_KeepsPlacements()
    {
        BirdPlacement tile = new("blue_tit", "spring", "LandBird", "GroundSpawner", new Vector2(4, 5), null,
            new List<IntRange> { new IntRange(600, 2400) }, "blue_tit", 42);
        BirdPlacement tree = tile with { BirdType = "TreeTrunkBird", SpawnPattern = "TreeTrunkSpawner", FeatureTile = new Vector2(7, 8), Times = new List<IntRange>() };
        DailySpawnPlan original = new(new Dictionary<string, IReadOnlyList<BirdPlacement>> { ["Forest"] = new[] { tile, tree } });

        DailySpawnPlan reloaded = MessageJson.Deserialize<DailySpawnPlan>(MessageJson.Serialize(original));
        IReadOnlyList<BirdPlacement> placements = reloaded.Locations["Forest"];

        Assert.That(placements[0] with { Times = tile.Times }, Is.EqualTo(tile));
        Assert.That(placements[0].Times, Is.EqualTo(tile.Times));
        Assert.That(placements[1] with { Times = tree.Times }, Is.EqualTo(tree));
        Assert.That(placements[1].IsActiveAt(100), Is.True);
    }

    [Test]
    public void DefaultRegistries_MatchContentTestIds()
    {
        Assert.That(SpawnPatternRegistry.CreateDefault().Ids, Is.EquivalentTo(ContentTests.Patterns));
        Assert.That(BirdTypeRegistry.CreateDefault().Ids, Is.EquivalentTo(ContentTests.BirdTypes));
    }

    [Test]
    public void SpawnPatternRegistry_CreatesEveryRegisteredPattern()
    {
        SpawnPatternRegistry registry = SpawnPatternRegistry.CreateDefault();

        Assert.That(registry.Ids.All(id => registry.TryCreate(id, out _)), Is.True);
    }

    [TestCase(null)]
    [TestCase("NoSuchSpawner")]
    public void SpawnPatternRegistry_UnknownPattern_ReturnsFalse(string? pattern)
    {
        Assert.That(SpawnPatternRegistry.CreateDefault().TryCreate(new SpawnPatternId(pattern!), out Spawner? spawner), Is.False);
        Assert.That(spawner, Is.Null);
    }

    [Test]
    public void BirdTypeRegistry_UnknownType_ReturnsFalse()
    {
        Assert.That(BirdTypeRegistry.CreateDefault().TryCreate(new BirdTypeId("NoSuchBird"), null!, null!, null!, out BirdCritter? bird), Is.False);
        Assert.That(bird, Is.Null);
    }
}
