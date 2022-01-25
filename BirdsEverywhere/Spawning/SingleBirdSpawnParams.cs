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
        private Condition.TileSpawnCondition condition;


        public SingleBirdSpawnParamsTile(Condition.TileSpawnCondition condition, Vector2 position, string id, string birdType)
            : base(position, id, birdType)
        {
            this.condition = condition;
        }

        public override bool stillValidSpawnPosition(GameLocation location)
        {
            return condition(location, position, (int)position.X, (int)position.Y);
        }
    }

    public class SingleBirdSpawnParamsTerrainFeature : SingleBirdSpawnParameters
    {
        public int terrainFeatureIndex;

        public SingleBirdSpawnParamsTerrainFeature(int terrainFeatureIndex, Vector2 position, string id, string birdType)
            : base(position, id, birdType)
        {
            this.terrainFeatureIndex = terrainFeatureIndex;
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
        public Vector2 position;
        public string ID;
        public string BirdType;
        public SingleBirdSpawnParameters(Vector2 position, string id, string birdType)
        {
            this.position = position;
            this.ID = id;
            this.BirdType = birdType;
        }

        public abstract bool stillValidSpawnPosition(GameLocation location);
    }
}
