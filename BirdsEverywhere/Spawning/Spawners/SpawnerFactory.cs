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
    class SpawnerFactory
    {
        public delegate Spawner SpawnPatternDelegate();

        private static readonly Dictionary<string, SpawnPatternDelegate> spawnerTypes = new Dictionary<string, SpawnPatternDelegate>() {
            {"GroundSpawner", () => new GroundSpawner()},
            {"WaterSpawner", () => new WaterSpawner()},
            {"WaterOrGroundSpawner", () => new WaterOrGroundSpawner()},
            {"SpawnableGroundSpawner", () => new SpawnableGroundSpawner()},
            {"TreeTrunkSpawner", () => new TreeTrunkSpawner() }
        };

        public static Spawner createSpawner(GameLocation location, BirdData data)
        {
            ModEntry.modInstance.Monitor.Log($"For location {location} {data.name}s will be created with {data.spawnData.spawnPattern}", LogLevel.Debug);
            return spawnerTypes[data.spawnData.spawnPattern]();
        }
    }
}

