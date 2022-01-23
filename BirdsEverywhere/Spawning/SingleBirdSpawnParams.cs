﻿using System;
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
        private Condition.SpawnCondition condition;


        public SingleBirdSpawnParamsTile(Condition.SpawnCondition condition, Vector2 position, string id, string birdType)
            : base(position, id, birdType)
        {
            this.condition = condition;
        }

        public override bool stillValidSpawnPosition(GameLocation location)
        {
            return condition(location, Position, (int)Position.X, (int)Position.Y);
        }
    }

    public class SingleBirdSpawnParamsTree : SingleBirdSpawnParameters
    {
        public int treeIndex;

        public SingleBirdSpawnParamsTree(int treeIndex, Vector2 position, string id, string birdType)
            : base(position, id, birdType)
        {
            this.treeIndex = treeIndex;
        }

        public override bool stillValidSpawnPosition(GameLocation location)
        {
            return Condition.isEligibleTree(location, treeIndex);
        }
    }

    public abstract class SingleBirdSpawnParameters
    {
        public Vector2 Position;
        public string ID;
        public string BirdType;
        public SingleBirdSpawnParameters(Vector2 position, string id, string birdType)
        {
            this.Position = position;
            this.ID = id;
            this.BirdType = birdType;
        }

        public abstract bool stillValidSpawnPosition(GameLocation location);
    }
}