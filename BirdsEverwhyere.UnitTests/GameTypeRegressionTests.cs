using System.Collections.Generic;
using System.Linq;
using BirdsEverywhere;
using BirdsEverywhere.BirdTypes;
using BirdsEverywhere.Content;
using BirdsEverywhere.Domain;
using BirdsEverywhere.Registries;
using BirdsEverywhere.Spawners;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using NUnit.Framework;
using StardewModdingAPI.Toolkit.Serialization;
using StardewValley;

namespace BirdsEverwhyere.UnitTests
{
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

            Assert.That(current.currentAnimation, Is.EqualTo(new List<(int, int)> { (3, 100), (4, 200) }));
        }

        [Test]
        public void WaterSpawner_HasCondition()
        {
            Assert.That(new WaterSpawner().condition, Is.Not.Null);
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
}
