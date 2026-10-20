using BirdsEverywhere;
using BirdsEverywhere.BirdTypes;
using BirdsEverywhere.Domain;
using BirdsEverywhere.Multiplayer;
using BirdsEverywhere.Registries;
using BirdsEverywhere.Spawners;
using Microsoft.Xna.Framework;
using NUnit.Framework;
using StardewValley;

namespace BirdsEverwhyere.UnitTests;

// Touches game types: run with `dotnet test --arch x64` on Apple Silicon.
public class GameTypeRegressionTests
{
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
    public void ReplicaState_UsesOwnerPoseAndInterpolatesPosition()
    {
        LandBird replica = new() { IsReplica = true, position = new Vector2(0, 0), sprite = new AnimatedSprite() };
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

        Assert.That(replica.update(null!, null!), Is.False);
        Assert.That(replica.position, Is.EqualTo(new Vector2(40, 20)));
        Assert.That(replica.State, Is.EqualTo(BehaviorStatus.FlyingAway));
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
        BirdPlacement tree = tile with { BirdType = "TreeTrunkBird", SpawnPattern = "TreeTrunkSpawner", FeatureIndex = 3, Times = new List<IntRange>() };
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
