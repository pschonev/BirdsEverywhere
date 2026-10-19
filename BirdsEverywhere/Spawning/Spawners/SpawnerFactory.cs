using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StardewValley;
using StardewModdingAPI;
using Microsoft.Xna.Framework;

namespace BirdsEverywhere.Spawners
{   /// <summary>
/// Implements multiple strategies to check a location for viable spawn tiles and spawn a variable number of birds
/// </summary>
    public static class SpawnerFactory
    {
        public delegate Spawner SpawnPatternDelegate();

        private static readonly Dictionary<string, SpawnPatternDelegate> spawnerTypes = new Dictionary<string, SpawnPatternDelegate>() {
            {"GroundSpawner", () => new GroundSpawner()},
            {"WaterSpawner", () => new WaterSpawner()},
            {"WaterOrGroundSpawner", () => new WaterOrGroundSpawner()},
            {"SpawnableGroundSpawner", () => new SpawnableGroundSpawner()},
            {"TreeTrunkSpawner", () => new TreeTrunkSpawner() },
            {"BushSpawner", () => new BushSpawner() }
        };

        public static IReadOnlyCollection<string> KnownPatterns => spawnerTypes.Keys;

        public static Spawner createSpawner(GameLocation location, BirdData data, IMonitor monitor)
        {
            string pattern = data.currentSpawnData.spawnPattern;
            monitor.Log($"For location {location} {data.name}s will be created with {pattern}", LogLevel.Debug);
            return createSpawner(pattern) ?? throw new KeyNotFoundException($"Unknown spawn pattern '{pattern}' for bird '{data.id}'.");
        }

        public static Spawner? createSpawner(string? pattern)
        {
            if (pattern is null || !spawnerTypes.TryGetValue(pattern, out SpawnPatternDelegate? create))
                return null;
            return create();
        }
    }
}

