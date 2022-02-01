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
        public SingleBirdSpawnParamsTile(Condition.TileSpawnCondition condition, Vector2 position, string id, string birdType)
            : base(position, id, birdType)
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

        public SingleBirdSpawnParamsTerrainFeature(int terrainFeatureIndex, Vector2 position, string id, string birdType)
            : base(position, id, birdType)
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
        public string className { get; set; }
        public Vector2 position { get; set; }
        public string ID { get; set; }
        public string BirdType { get; set; }
        public SingleBirdSpawnParameters(Vector2 position, string id, string birdType)
        {
            this.position = position;
            this.ID = id;
            this.BirdType = birdType;
        }

        public abstract bool stillValidSpawnPosition(GameLocation location);
    }
}
