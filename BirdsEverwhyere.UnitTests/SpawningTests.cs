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
    }
}
