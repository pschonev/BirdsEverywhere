using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley.BellsAndWhistles;
using StardewValley;

namespace BirdsEverywhere.Birds
{
    abstract class CustomBirdType : Critter
    {
		public enum BehaviorStatus
        {
			Pecking,
			FlyingAway,
			Sleeping,
			Stopped,
			Walking,
			Swimming
        }

		protected BehaviorStatus state;

		protected string birdTexture;

		protected string birdName;

		protected CustomBirdType(int baseFrame, int tileX, int tileY, string birdName)
			: base(baseFrame, new Vector2(tileX * 64, tileY * 64))
        {
			this.birdName = birdName;
			string assetPath = $"assets/{birdName}/{birdName}.png";
			Texture2D _texture = ModEntry.modInstance.Helper.Content.Load<Texture2D>(assetPath);
			this.birdTexture = ModEntry.modInstance.Helper.Content.GetActualAssetKey(assetPath);
			this.sprite = new AnimatedSprite(birdTexture, baseFrame, 32, 32);
		}

		public CustomBirdType setState(BehaviorStatus state)
        {
			this.state = state;
			return this;
        }

	}
}
