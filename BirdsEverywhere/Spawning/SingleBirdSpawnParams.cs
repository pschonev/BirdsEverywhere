using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StardewValley;
using StardewModdingAPI;
using Microsoft.Xna.Framework;
using StardewValley.TerrainFeatures;

namespace BirdsEverywhere.Spawners
{
    public class SingleBirdSpawnParamsTile : SingleBirdSpawnParameters
    {
        public SingleBirdSpawnParamsTile(Vector2 position, string id, string birdType, string spawnPattern, List<TimeRange> possibleTimesOfDay, string texture)
            : base(position, id, birdType, spawnPattern, possibleTimesOfDay, texture)
        {
            this.className = "SingleBirdSpawnParamsTile";
        }

        public override bool stillValidSpawnPosition(GameLocation location, IMonitor monitor)
        {
            if (SpawnerFactory.createSpawner(spawnPattern) is not TileSpawner spawner)
                return logUnknownPattern(monitor);
            return spawner.condition(new GameLocationQuery(location), position);
        }
    }

    public class SingleBirdSpawnParamsTerrainFeature : SingleBirdSpawnParameters
    {
        public int terrainFeatureIndex { get; set; }

        public SingleBirdSpawnParamsTerrainFeature(int terrainFeatureIndex, Vector2 position, string id, string birdType, string spawnPattern, List<TimeRange> possibleTimesOfDay, string texture)
            : base(position, id, birdType, spawnPattern, possibleTimesOfDay, texture)
        {
            this.terrainFeatureIndex = terrainFeatureIndex;
            this.className = "SingleBirdSpawnParamsTerrainFeature";
        }

        public override bool stillValidSpawnPosition(GameLocation location, IMonitor monitor)
        {
            if (SpawnerFactory.createSpawner(spawnPattern) is not TerrainFeatureSpawner spawner)
                return logUnknownPattern(monitor);
            return spawner.condition(location, terrainFeatureIndex, position);
        }
    }

    public abstract class SingleBirdSpawnParameters
    {
        public string className { get; set; } // the kind of SingleBirdSpawmParameter
        public Vector2 position { get; set; }
        public string ID { get; set; } // id of the bird
        public string BirdType { get; set; } // type of bird (behavior)
        public string spawnPattern { get; set; }
        public List<TimeRange> possibleTimesOfDay { get; set; }
        public string texture { get; set; }
        public SingleBirdSpawnParameters(Vector2 position, string id, string birdType, string spawnPattern, List<TimeRange> possibleTimesOfDay, string texture)
        {
            this.position = position;
            this.ID = id;
            this.BirdType = birdType;
            this.spawnPattern = spawnPattern;
            this.possibleTimesOfDay = possibleTimesOfDay;
            this.texture = texture;
        }

        public abstract bool stillValidSpawnPosition(GameLocation location, IMonitor monitor);

        protected bool logUnknownPattern(IMonitor monitor)
        {
            monitor.LogOnce($"Bird '{ID}' has unknown spawn pattern '{spawnPattern}'; keeping it in place.", LogLevel.Warn);
            return true;
        }
        public bool stillValidTime(int time) => possibleTimesOfDay.Any(x => x.isInRange(time));
    }
}
