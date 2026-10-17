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
        public SingleBirdSpawnParamsTile(Condition.TileSpawnCondition condition, Vector2 position, string id, string birdType, List<TimeRange> possibleTimesOfDay, string texture)
            : base(position, id, birdType, possibleTimesOfDay, texture)
        {
            this.className = "SingleBirdSpawnParamsTile";
        }

        public override bool stillValidSpawnPosition(GameLocation location)
        {
            BirdData data = ModEntry.birdDataCollection[this.ID];
            Condition.TileSpawnCondition condition = (SpawnerFactory.createSpawner(location, data) as TileSpawner).condition;
            return condition(location, position, (int)position.X, (int)position.Y);
        }
    }

    public class SingleBirdSpawnParamsTerrainFeature : SingleBirdSpawnParameters
    {
        public int terrainFeatureIndex { get; set; }

        public SingleBirdSpawnParamsTerrainFeature(int terrainFeatureIndex, Vector2 position, string id, string birdType, List<TimeRange> possibleTimesOfDay, string texture)
            : base(position, id, birdType, possibleTimesOfDay, texture)
        {
            this.terrainFeatureIndex = terrainFeatureIndex;
            this.className = "SingleBirdSpawnParamsTerrainFeature";
        }

        public override bool stillValidSpawnPosition(GameLocation location)
        {
            BirdData data = ModEntry.birdDataCollection[this.ID];
            Condition.TerrainSpawnCondition condition = (SpawnerFactory.createSpawner(location, data) as TerrainFeatureSpawner).condition;
            return condition(location, terrainFeatureIndex, position);
        }
    }

    public abstract class SingleBirdSpawnParameters
    {
        public string className { get; set; } // the kind of SingleBirdSpawmParameter
        public Vector2 position { get; set; }
        public string ID { get; set; } // id of the bird
        public string BirdType { get; set; } // type of bird (behavior)
        public List<TimeRange> possibleTimesOfDay { get; set; }
        public string texture { get; set; }
        public SingleBirdSpawnParameters(Vector2 position, string id, string birdType, List<TimeRange> possibleTimesOfDay, string texture)
        {
            this.position = position;
            this.ID = id;
            this.BirdType = birdType;
            this.possibleTimesOfDay = possibleTimesOfDay;
            this.texture = texture;
        }

        public abstract bool stillValidSpawnPosition(GameLocation location);
        public bool stillValidTime(int time) => possibleTimesOfDay.Any(x => x.isInRange(time));
    }
}
