using System.Collections.Generic;
using System.Linq;
using BirdsEverywhere;
using BirdsEverywhere.BirdTypes;
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
                    new List<TimeRange> { new TimeRange(600, 2400) }, "blue_tit"),
            };

            DailySpawner reloaded = MessageJson.Deserialize<DailySpawner>(MessageJson.Serialize(original));

            Assert.That(reloaded.LocationBirdPosition["Forest"].Single().spawnPattern, Is.EqualTo("GroundSpawner"));
        }

        [Test]
        public void ShippedBirds_UseKnownPatternsAndTypes()
        {
            List<SpawnData> spawns = new[] { "black_woodpecker", "blue_tit", "coal_tit", "great_tit", "grey_partridge", "herring_gull" }
                .SelectMany(id => ContentTests.LoadBird(id).allSpawnData.Values.SelectMany(list => list))
                .ToList();

            Assert.That(SpawnerFactory.KnownPatterns, Is.SupersetOf(spawns.Select(s => s.spawnPattern).Distinct()));
            Assert.That(BirdFactory.KnownBirdTypes, Is.SupersetOf(spawns.Select(s => s.birdType).Distinct()));
        }

        [TestCase(null)]
        [TestCase("NoSuchSpawner")]
        public void SpawnerFactory_UnknownPattern_ReturnsNull(string? pattern)
        {
            Assert.That(SpawnerFactory.createSpawner(pattern), Is.Null);
        }
    }
}
