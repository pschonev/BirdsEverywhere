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
            {"SpawnableGroundSpawner", () => new SpawnableGroundSpawner()}
        };

        public static Spawner createSpawner(GameLocation location, SpawnData data, string id, string spawner)
        {
            return spawnerTypes[spawner]();
        }
    }
}

