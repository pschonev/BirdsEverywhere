using System;
using System.Collections.Generic;
using System.Linq;
using BirdsEverywhere;
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

        private static BirdData MakeBird()
        {
            SpawnData spawn = SpawnData.getDefaultSpawnData();
            spawn.birdType = "LandBird";
            spawn.spawnPattern = "GroundSpawner";
            spawn.minGroupCount = 2;
            spawn.maxGroupCount = 2;
            spawn.minGroupSize = 3;
            spawn.maxGroupSize = 3;
            spawn.initTextureList();
            return new BirdData { id = "blue_tit", name = "Blue Tit", currentSpawnData = spawn };
        }

        private static List<Vector2> Spawn(int seed) =>
            new GroundSpawner()
                .spawnBirds(new FakeLocation(), MakeBird(), new Random(seed), new NullMonitor())
                .Select(p => p.position)
                .ToList();

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
