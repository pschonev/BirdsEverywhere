using System.Collections.Generic;
using System.IO;
using System.Linq;
using BirdsEverywhere.Content;
using BirdsEverywhere.Domain;
using NUnit.Framework;

namespace BirdsEverwhyere.UnitTests
{
    public class ContentTests
    {
        private static readonly string AssetsDir = Path.Combine(System.AppContext.BaseDirectory, "assets");

        internal static readonly SpawnPatternId[] Patterns = new[] { "GroundSpawner", "WaterSpawner", "WaterOrGroundSpawner", "SpawnableGroundSpawner", "TreeTrunkSpawner", "BushSpawner" }
            .Select(id => new SpawnPatternId(id)).ToArray();

        internal static readonly BirdTypeId[] BirdTypes = new[] { "LandBird", "WaterLandBird", "TreeTrunkBird", "BushBird" }
            .Select(id => new BirdTypeId(id)).ToArray();

        private static IEnumerable<string> BirdIds() => ContentPaths.BirdIds(AssetsDir).OrderBy(name => name);

        private static IEnumerable<string> CompatModIds() =>
            Directory.GetFiles(ContentPaths.CompatDirectory(AssetsDir), "*.json").Select(Path.GetFileNameWithoutExtension).Select(name => name!);

        internal static BirdDefinition LoadBird(string id)
        {
            string? Read(string path) => File.Exists(path) ? File.ReadAllText(path) : null;
            var (groups, groupIssues) = ContentLoader.LoadGroups(AssetsDir, System.Array.Empty<string>(), Read);
            var (bird, issues, _, _) = ContentLoader.LoadBird(AssetsDir, id, groups, Patterns, BirdTypes, Read, File.Exists);
            Assert.That(groupIssues.Concat(issues).Where(issue => issue.Severity != IssueSeverity.Info), Is.Empty);
            Assert.That(bird, Is.Not.Null);
            return bird!;
        }

        [TestCaseSource(nameof(BirdIds))]
        public void Bird_JsonIsValid(string id)
        {
            Assert.That(LoadBird(id).Id.Value, Is.EqualTo(id));
        }

        [TestCaseSource(nameof(BirdIds))]
        public void Bird_TexturesExist(string id)
        {
            BirdDefinition bird = LoadBird(id);
            IEnumerable<string> textureKeys = bird.Rules.SelectMany(rule => rule.Textures.Keys).Distinct();

            Assert.That(Path.Combine(AssetsDir, id, $"{id}_icon.png"), Does.Exist);
            Assert.That(Path.Combine(AssetsDir, id, $"{id}.png"), Does.Exist);
            foreach (string key in textureKeys.Where(k => k != ""))
                Assert.That(Path.Combine(AssetsDir, id, $"{id}_{key}.png"), Does.Exist);
        }

        [TestCaseSource(nameof(BirdIds))]
        public void Bird_HasResolvedLocations(string id)
        {
            Assert.That(LoadBird(id).Rules.SelectMany(rule => rule.Locations), Is.Not.Empty);
        }

        [Test]
        public void LocationGroups_HaveLocations()
        {
            var (groups, issues) = ContentLoader.LoadGroups(AssetsDir, CompatModIds(), File.ReadAllText);
            Assert.That(issues, Is.Empty);
            Assert.That(groups.Values.SelectMany(locations => locations), Is.Not.Empty);
        }

        [Test]
        public void LocationGroups_MergeCompatFiles()
        {
            Dictionary<string, string> files = new()
            {
                [ContentPaths.Groups("/a")] = @"{""coast"":[""Beach""]}",
                [ContentPaths.CompatGroups("/a", "Some.Mod")] = @"{""coast"":[""Beach"",""Custom_Reef""],""river"":[""Custom_River""]}",
            };
            var (groups, issues) = ContentLoader.LoadGroups("/a", new[] { "Some.Mod" }, path => files.GetValueOrDefault(path));
            Assert.That(issues, Is.Empty);
            Assert.That(groups["coast"], Is.EqualTo(new[] { "Beach", "Custom_Reef" }));
            Assert.That(groups["river"], Is.EqualTo(new[] { "Custom_River" }));
        }

        [TestCase("black_woodpecker", "TreeTrunkSpawner", "TreeTrunkBird", "Forest,Farm,Backwoods", 1, 4, 1, 7)]
        [TestCase("blue_tit", "SpawnableGroundSpawner", "LandBird", "Forest,Farm,Backwoods", 1, 3, 1, 7)]
        [TestCase("coal_tit", "SpawnableGroundSpawner", "LandBird", "Forest,Farm,Backwoods", 1, 3, 1, 7)]
        [TestCase("great_tit", "SpawnableGroundSpawner", "LandBird", "Forest,Farm,Backwoods", 1, 3, 1, 7)]
        [TestCase("grey_partridge", "BushSpawner", "BushBird", "Forest,Farm,Backwoods", 1, 3, 1, 7)]
        [TestCase("herring_gull", "WaterOrGroundSpawner", "WaterLandBird", "Beach", 1, 3, 1, 7)]
        public void Bird_MergedSpawnData(string id, string pattern, string birdType, string locations, int minGroupCount, int maxGroupCount, int minGroupSize, int maxGroupSize)
        {
            BirdDefinition bird = LoadBird(id);
            Assert.That(bird.Rules.Select(rule => rule.Id), Is.EquivalentTo(new[] { "spring", "summer", "fall", "winter" }));
            foreach (SpawnRule rule in bird.Rules)
            {
                Assert.That(rule.SpawnPattern.Value, Is.EqualTo(pattern));
                Assert.That(rule.BirdType.Value, Is.EqualTo(birdType));
                Assert.That(rule.Locations, Is.EqualTo(locations.Split(',')));
                Assert.That((rule.GroupCount.Min, rule.GroupCount.Max, rule.GroupSize.Min, rule.GroupSize.Max), Is.EqualTo((minGroupCount, maxGroupCount, minGroupSize, maxGroupSize)));
            }
        }
    }
}
