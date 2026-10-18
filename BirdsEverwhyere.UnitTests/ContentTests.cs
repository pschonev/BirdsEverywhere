using System.Collections.Generic;
using System.IO;
using System.Linq;
using BirdsEverywhere;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace BirdsEverwhyere.UnitTests
{
    public class ContentTests
    {
        private static readonly string AssetsDir = MockBirdData.AssetsDir;
        private static readonly JObject Environment = JObject.Parse(File.ReadAllText(Path.Combine(AssetsDir, "environmentData.json")));

        private static IEnumerable<string> BirdIds() =>
            Directory.GetDirectories(AssetsDir)
                .Select(Path.GetFileName)
                .Where(name => name != "templates")
                .Select(name => name!)
                .OrderBy(name => name);

        private static IEnumerable<string> BiomeBirds() =>
            Environment["biomes"]!.SelectMany(b => b["birds"]!.Values<string>()).Select(id => id!);

        private static IEnumerable<string> BiomeLocations() =>
            Environment["biomes"]!.SelectMany(b => b["locations"]!.Values<string>()).Select(id => id!);

        internal static BirdData LoadBird(string id) =>
            System.Text.Json.JsonSerializer.Deserialize<MockBirdData>(File.ReadAllText(Path.Combine(AssetsDir, id, $"{id}.json")))!;

        [TestCaseSource(nameof(BirdIds))]
        public void Bird_JsonIsValid(string id)
        {
            JObject json = JObject.Parse(File.ReadAllText(Path.Combine(AssetsDir, id, $"{id}.json")));

            Assert.That(json.Value<string>("id"), Is.EqualTo(id));
            Assert.That(json.Value<string>("name"), Is.Not.Empty);
            string? template = json.Value<string>("template");
            if (!string.IsNullOrEmpty(template))
                Assert.That(Path.Combine(AssetsDir, "templates", $"{template}.json"), Does.Exist);
        }

        [TestCaseSource(nameof(BirdIds))]
        public void Bird_TexturesExist(string id)
        {
            BirdData bird = LoadBird(id);
            IEnumerable<string> textureKeys = bird.allSpawnData.Values.SelectMany(list => list).SelectMany(s => s.textures.Keys).Distinct();

            Assert.That(Path.Combine(AssetsDir, id, $"{id}_icon.png"), Does.Exist);
            Assert.That(Path.Combine(AssetsDir, id, $"{id}.png"), Does.Exist);
            foreach (string key in textureKeys.Where(k => k != ""))
                Assert.That(Path.Combine(AssetsDir, id, $"{id}_{key}.png"), Does.Exist);
        }

        [TestCaseSource(nameof(BirdIds))]
        public void Bird_IsInSomeBiome(string id)
        {
            Assert.That(BiomeBirds(), Does.Contain(id));
        }

        [TestCaseSource(nameof(BirdIds))]
        public void Bird_LocationsAreInSomeBiome(string id)
        {
            IEnumerable<string> locations = LoadBird(id).allSpawnData.Values.SelectMany(list => list).SelectMany(s => s.locations).Distinct();

            Assert.That(BiomeLocations(), Is.SupersetOf(locations));
        }

        [Test]
        public void Environment_BirdsHaveFolders()
        {
            Assert.That(BirdIds(), Is.SupersetOf(BiomeBirds()));
        }

        // Characterizes current merge behavior. Birds without defaultSpawnData get global defaults
        // for locations/group sizes, shadowing the template values (see REFACTOR.md defects).
        [TestCase("black_woodpecker", "TreeTrunkSpawner", "TreeTrunkBird", "Forest,Farm,Backwoods", 1, 4, 1, 7)]
        [TestCase("blue_tit", "SpawnableGroundSpawner", "LandBird", "Forest", 1, 2, 1, 5)]
        [TestCase("coal_tit", "SpawnableGroundSpawner", "LandBird", "Forest", 1, 2, 1, 5)]
        [TestCase("great_tit", "SpawnableGroundSpawner", "LandBird", "Forest", 1, 2, 1, 5)]
        [TestCase("grey_partridge", "BushSpawner", "BushBird", "Forest,Farm,Backwoods", 1, 3, 1, 7)]
        [TestCase("herring_gull", "WaterOrGroundSpawner", "WaterLandBird", "Beach", 1, 3, 1, 7)]
        public void Bird_MergedSpawnData(string id, string pattern, string birdType, string locations, int minGroupCount, int maxGroupCount, int minGroupSize, int maxGroupSize)
        {
            BirdData bird = LoadBird(id);

            Assert.That(bird.allSpawnData.Keys, Is.EquivalentTo(new[] { "spring", "summer", "fall", "winter" }));
            foreach (SpawnData s in bird.allSpawnData.Values.SelectMany(list => list))
            {
                Assert.That(s.spawnPattern, Is.EqualTo(pattern));
                Assert.That(s.birdType, Is.EqualTo(birdType));
                Assert.That(s.locations, Is.EqualTo(locations.Split(',')));
                Assert.That((s.minGroupCount, s.maxGroupCount, s.minGroupSize, s.maxGroupSize), Is.EqualTo((minGroupCount, maxGroupCount, minGroupSize, maxGroupSize)));
            }
        }
    }
}
