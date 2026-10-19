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
        public void SpawnParams_RoundTrip_KeepsSpawnPattern()
        {
            DailySpawner original = new();
            original.LocationBirdPosition["Forest"] = new List<SingleBirdSpawnParameters>
            {
                new SingleBirdSpawnParamsTile(
                    new Vector2(4, 5), "blue_tit", "LandBird", "GroundSpawner",
                    new List<IntRange> { new IntRange(600, 2400) }, "blue_tit"),
            };

            DailySpawner reloaded = MessageJson.Deserialize<DailySpawner>(MessageJson.Serialize(original));

            Assert.That(reloaded.LocationBirdPosition["Forest"].Single().spawnPattern, Is.EqualTo("GroundSpawner"));
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
            Assert.That(BirdTypeRegistry.CreateDefault().TryCreate(new BirdTypeId("NoSuchBird"), null!, null!, null!, out CustomBirdType? bird), Is.False);
            Assert.That(bird, Is.Null);
        }
    }
}
