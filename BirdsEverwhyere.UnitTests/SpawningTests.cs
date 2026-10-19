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
        }

        private sealed class NullMonitor : IMonitor
        {
            public bool IsVerbose => false;
            public void Log(string message, LogLevel level = LogLevel.Trace) { }
            public void LogOnce(string message, LogLevel level = LogLevel.Trace) { }
            public void VerboseLog(string message) { }
            public void VerboseLog(ref StardewModdingAPI.Framework.Logging.VerboseLogStringHandler message) { }
        }

        private static (BirdDefinition Bird, SpawnRule Rule) MakeBird()
        {
            SpawnRule rule = new("spring", new SpawnConditions(ImmutableArray<Season>.Empty, ImmutableArray<Weather>.Empty,
                ImmutableArray<IntRange>.Empty, ImmutableArray<IntRange>.Empty), ImmutableArray.Create("Forest"),
                new SpawnPatternId("GroundSpawner"), new BirdTypeId("LandBird"), new IntRange(2, 2), new IntRange(3, 3),
                new Dictionary<string, int> { [""] = 1 }, 1);
            return (new BirdDefinition(new BirdId("blue_tit"), "Blue Tit", "Cyanistes caeruleus", "", ImmutableArray.Create(rule)), rule);
        }

        private static List<Vector2> Spawn(int seed)
        {
            (BirdDefinition bird, SpawnRule rule) = MakeBird();
            return new GroundSpawner()
                .spawnBirds(new FakeLocation(), bird, rule, new Random(seed), new NullMonitor())
                .Select(p => p.position)
                .ToList();
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
        public void ShuffleListByOrder_SameSeed_SameOrder()
        {
            List<int> items = Enumerable.Range(0, 10).ToList();

            Assert.That(Utils.shuffleListByOrder(new Random(3), items), Is.EqualTo(Utils.shuffleListByOrder(new Random(3), items)));
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
                new Dictionary<string, int> { [""] = 1 }, 1);
            return new BirdDefinition(new BirdId(id), id, "", "", ImmutableArray.Create(rule));
        }

        private static IReadOnlyList<SpawnAssignment> Plan(IReadOnlySet<string> seen, int seed, Func<string, bool>? exists = null)
        {
            Dictionary<string, BirdDefinition> catalog = new[]
            {
                PlannerBird("a", Season.Spring, "Forest", "Town"),
                PlannerBird("b", Season.Spring, "Forest", "Beach", "Mountain"),
                PlannerBird("c", Season.Winter, "Desert", "Woods"),
            }.ToDictionary(bird => bird.Id.Value);
            return SpawnPlanner.Plan(catalog, seen, Season.Spring, Weather.Sun, 1, new Random(seed), exists ?? (_ => true));
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
        public void SpawnPlanner_SkipsMissingLocations()
        {
            IReadOnlyList<SpawnAssignment> plan = Plan(new HashSet<string> { "a", "b" }, 1, name => name != "Forest");

            Assert.That(plan.Select(p => p.Location), Has.None.EqualTo("Forest"));
        }
    }
}
