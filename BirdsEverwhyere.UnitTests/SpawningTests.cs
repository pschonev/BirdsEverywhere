using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using BirdsEverywhere;
using BirdsEverywhere.Domain;
using BirdsEverywhere.Spawners;
using Microsoft.Xna.Framework;
using NUnit.Framework;
using StardewModdingAPI;

namespace BirdsEverwhyere.UnitTests
{
    public class SpawningTests
    {
        private sealed class FakeLocation : ILocationQuery
        {
            public string Name => "Forest";
            public Point MapSize => new(40, 30);
            public bool IsGroundTile(Vector2 tile) => tile.X < 20;
            public bool IsWaterTile(Vector2 tile) => tile.X >= 20;
            public bool IsSpawnableGroundTile(Vector2 tile) => IsGroundTile(tile);
            public string? FishArea(Vector2 tile) => tile.Y < 15 ? "River" : "Lake";
        }

        private sealed class NullMonitor : IMonitor
        {
            public bool IsVerbose => false;
            public void Log(string message, LogLevel level = LogLevel.Trace) { }
            public void LogOnce(string message, LogLevel level = LogLevel.Trace) { }
            public void VerboseLog(string message) { }
            public void VerboseLog(ref StardewModdingAPI.Framework.Logging.VerboseLogStringHandler message) { }
        }

        private static (BirdDefinition Bird, SpawnRule Rule) MakeBird(string pattern = "GroundSpawner", SpawnParams? spawnParams = null)
        {
            SpawnRule rule = new("spring", new SpawnConditions(ImmutableArray<Season>.Empty, ImmutableArray<Weather>.Empty,
                ImmutableArray<IntRange>.Empty, ImmutableArray<IntRange>.Empty), ImmutableArray.Create("Forest"),
                new SpawnPatternId(pattern), new BirdTypeId("LandBird"), new IntRange(2, 2), new IntRange(3, 3),
                new Dictionary<string, int> { [""] = 1 }, 1, spawnParams ?? SpawnParams.None);
            return (new BirdDefinition(new BirdId("blue_tit"), "Blue Tit", "Cyanistes caeruleus", "", ImmutableArray.Create(rule)), rule);
        }

        private static List<Vector2> Spawn(int seed)
        {
            (BirdDefinition bird, SpawnRule rule) = MakeBird();
            return new GroundSpawner()
                .spawnBirds(new FakeLocation(), bird, rule, null, new Random(seed), new NullMonitor())
                .Select(p => p.Tile)
                .ToList();
        }

        private static List<Vector2> SpawnWith(TileSpawner spawner, SpawnParams spawnParams, string? fishArea, string pattern, int seed)
        {
            (BirdDefinition bird, SpawnRule rule) = MakeBird(pattern, spawnParams);
            return spawner.spawnBirds(new FakeLocation(), bird, rule, fishArea, new Random(seed), new NullMonitor()).Select(p => p.Tile).ToList();
        }

        [Test]
        public void ShoreDistance_MeasuresToOppositeKind()
        {
            Assert.That(ShoreDistance.From(new FakeLocation(), new Vector2(19, 10)), Is.EqualTo(1));
            Assert.That(ShoreDistance.From(new FakeLocation(), new Vector2(15, 10)), Is.EqualTo(5));
            Assert.That(ShoreDistance.From(new FakeLocation(), new Vector2(22, 10)), Is.EqualTo(3));
        }

        [Test]
        public void GroundSpawner_ShoreDistance_KeepsOnlyTilesNearWater()
        {
            for (int seed = 0; seed < 20; seed++)
            {
                List<Vector2> tiles = SpawnWith(new GroundSpawner(), new SpawnParams(new IntRange(1, 3)), null, "GroundSpawner", seed);

                Assert.That(tiles.All(t => t.X >= 17 && t.X < 20), Is.True);
            }
        }

        [Test]
        public void WaterSpawner_FishArea_KeepsOnlyThatArea()
        {
            for (int seed = 0; seed < 20; seed++)
            {
                List<Vector2> tiles = SpawnWith(new WaterSpawner(), SpawnParams.None, "Lake", "WaterSpawner", seed);

                Assert.That(tiles.All(t => t.Y >= 15 && t.X >= 20), Is.True);
            }
        }

        [TestCase("Town", "Town", null, null)]
        [TestCase("Farm_Forest", "Farm", "Forest", null)]
        [TestCase("Farm_Meadowlands:River", "Farm", "Meadowlands", "River")]
        [TestCase("Forest:Lake", "Forest", null, "Lake")]
        public void Habitat_Parse(string key, string location, string? farmType, string? fishArea)
        {
            Assert.That(Habitat.Parse(key), Is.EqualTo(new Habitat(key, location, farmType, fishArea)));
        }

        [Test]
        public void GroundSpawner_SameSeed_SamePositions()
        {
            Assert.That(Spawn(7), Is.EqualTo(Spawn(7)));
        }

        [Test]
        public void GroundSpawner_OnlyGroundTiles_AtMostGroupTotal()
        {
            List<Vector2> positions = Spawn(7);

            Assert.That(positions, Is.Not.Empty);
            Assert.That(positions.Count, Is.LessThanOrEqualTo(6));
            Assert.That(positions.All(p => p.X < 20), Is.True);
        }

        private sealed class TinyLocation : ILocationQuery
        {
            public string Name => "Tiny";
            public Point MapSize => new(5, 5);
            public bool IsGroundTile(Vector2 tile) => true;
            public bool IsWaterTile(Vector2 tile) => false;
            public bool IsSpawnableGroundTile(Vector2 tile) => true;
            public string? FishArea(Vector2 tile) => null;
        }

        [Test]
        public void TileSelection_TinyMap_ReturnsNothing()
        {
            Assert.That(TileSelection.Tiles(new TinyLocation(), Condition.isValidGroundTile, new IntRange(1, 1), new IntRange(3, 3), new Random(1)), Is.Empty);
        }

        [Test]
        public void TileSelection_Tiles_AreUnique()
        {
            for (int seed = 0; seed < 20; seed++)
            {
                Assert.That(TileSelection.Tiles(new FakeLocation(), Condition.isValidGroundTile, new IntRange(3, 3), new IntRange(4, 4), new Random(seed)), Is.Unique);
            }
        }

        [Test]
        public void TileSelection_Features_EmptyCandidates_ReturnsNothing()
        {
            Assert.That(TileSelection.Features(Array.Empty<int>(), new IntRange(2, 2), new Random(1), (_, _) => Vector2.Zero), Is.Empty);
        }

        [Test]
        public void TileSelection_Features_SkipsUnplaceableAndRespectsCount()
        {
            for (int seed = 0; seed < 20; seed++)
            {
                var placed = TileSelection.Features(Enumerable.Range(0, 10), new IntRange(3, 3), new Random(seed),
                    (index, _) => index % 2 == 0 ? new Vector2(index, 0) : null);

                Assert.That(placed.Count, Is.EqualTo(3));
                Assert.That(placed.All(p => p.Candidate % 2 == 0 && p.Position.X == p.Candidate), Is.True);
                Assert.That(placed.Select(p => p.Candidate), Is.Unique);
            }
        }

        [Test]
        public void Shuffle_SameSeed_IsPermutation()
        {
            List<int> items = Enumerable.Range(0, 10).ToList();

            Assert.That(Utils.Shuffle(new Random(3), items), Is.EqualTo(Utils.Shuffle(new Random(3), items)));
            Assert.That(Utils.Shuffle(new Random(3), items), Is.EquivalentTo(items));
        }

        [Test]
        public void ShuffleByWeights_FavoursHeavyItems()
        {
            List<int> items = new() { 0, 1 };
            List<double> weights = new() { 1, 20 };
            int heavyFirst = Enumerable.Range(0, 200).Count(seed => Utils.shuffleByWeights(new Random(seed), items, weights)[0] == 1);

            Assert.That(heavyFirst, Is.GreaterThan(150));
        }

        [Test]
        public void StableHash_IsStable()
        {
            Assert.That(Utils.StableHash(""), Is.EqualTo(unchecked((int)2166136261)));
            Assert.That(Utils.StableHash("a"), Is.EqualTo(unchecked((int)0xE40C292C)));
            Assert.That(Utils.StableHash("abc|1|day"), Is.Not.EqualTo(Utils.StableHash("abc|2|day")));
        }

        private static BirdDefinition PlannerBird(string id, Season season, params string[] locations)
        {
            SpawnRule rule = new("r", new SpawnConditions(ImmutableArray.Create(season), ImmutableArray<Weather>.Empty,
                ImmutableArray<IntRange>.Empty, ImmutableArray<IntRange>.Empty), locations.ToImmutableArray(),
                new SpawnPatternId("GroundSpawner"), new BirdTypeId("LandBird"), new IntRange(1, 1), new IntRange(1, 1),
                new Dictionary<string, int> { [""] = 1 }, 1, SpawnParams.None);
            return new BirdDefinition(new BirdId(id), id, "", "", ImmutableArray.Create(rule));
        }

        private static IReadOnlyList<SpawnAssignment> Plan(IReadOnlySet<string> seen, int seed, Func<string, bool>? exists = null,
            Func<SpawnAssignment, bool>? place = null, double luck = 0)
        {
            Dictionary<string, BirdDefinition> catalog = new[]
            {
                PlannerBird("a", Season.Spring, "Forest", "Town"),
                PlannerBird("b", Season.Spring, "Forest", "Beach", "Mountain"),
                PlannerBird("c", Season.Winter, "Desert", "Woods"),
            }.ToDictionary(bird => bird.Id.Value);
            return SpawnPlanner.Plan(catalog, seen, Season.Spring, Weather.Sun, 1, luck, new Random(seed),
                habitat => (exists ?? (_ => true))(habitat.Location), place ?? (_ => true));
        }

        [Test]
        public void SpawnPlanner_SameSeed_SamePlan()
        {
            HashSet<string> seen = new() { "a" };

            Assert.That(Plan(seen, 5).Select(p => (p.Location, p.Bird.Id.Value)), Is.EqualTo(Plan(seen, 5).Select(p => (p.Location, p.Bird.Id.Value))));
        }

        [Test]
        public void SpawnPlanner_RespectsSeasonLimitAndUniqueLocations()
        {
            for (int seed = 0; seed < 20; seed++)
            {
                IReadOnlyList<SpawnAssignment> plan = Plan(new HashSet<string> { "a", "b" }, seed);

                Assert.That(plan.Count, Is.EqualTo(3));
                Assert.That(plan.Select(p => p.Location), Is.Unique);
                Assert.That(plan.Any(p => p.Bird.Id.Value == "c"), Is.False);
            }
        }

        [Test]
        public void SpawnPlanner_UnseenBirdGetsOneLocationFirst()
        {
            for (int seed = 0; seed < 20; seed++)
            {
                IReadOnlyList<SpawnAssignment> plan = Plan(new HashSet<string> { "a" }, seed);

                Assert.That(plan[0].Bird.Id.Value, Is.EqualTo("b"));
                Assert.That(plan.Count(p => p.Bird.Id.Value == "b"), Is.EqualTo(1));
            }
        }

        [Test]
        public void SpawnPlanner_InfeasibleLocation_FallsBackToNextOne()
        {
            for (int seed = 0; seed < 20; seed++)
            {
                IReadOnlyList<SpawnAssignment> plan = Plan(new HashSet<string>(), seed, place: assignment => assignment.Location == "Town");

                Assert.That(plan, Has.Count.EqualTo(1));
                Assert.That(plan[0].Location, Is.EqualTo("Town"));
                Assert.That(plan[0].Bird.Id.Value, Is.EqualTo("a"));
            }
        }

        [Test]
        public void SpawnPlanner_NothingFeasible_PlansNothing()
        {
            Assert.That(Plan(new HashSet<string>(), 1, place: _ => false), Is.Empty);
        }

        [Test]
        public void SpawnPlanner_UnseenCount_LuckRaisesChances()
        {
            double Average(double luck) => Enumerable.Range(0, 2000).Average(seed => SpawnPlanner.UnseenCount(luck, new Random(seed)));

            Assert.That(Average(0), Is.InRange(1.05, 1.3));
            Assert.That(Average(0.1), Is.GreaterThan(Average(0)));
            Assert.That(Average(-0.1), Is.LessThan(Average(0)));
        }

        [Test]
        public void SpawnPlanner_SkipsMissingLocations()
        {
            IReadOnlyList<SpawnAssignment> plan = Plan(new HashSet<string> { "a", "b" }, 1, name => name != "Forest");

            Assert.That(plan.Select(p => p.Location), Has.None.EqualTo("Forest"));
        }
    }
}
