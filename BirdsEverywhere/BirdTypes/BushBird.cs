using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.TerrainFeatures;
using StardewValley.BellsAndWhistles;

namespace BirdsEverywhere.BirdTypes
{
    class BushBird : CustomBirdTypeTerrainFeature
    {
		private Bush bush;

		public BushBird(int tileX, int tileY, string birdName, BehaviorStatus state = BehaviorStatus.Pecking)
			: base(0, tileX, tileY, birdName)
		{
			//position.Y += 48f;
			flip = (Game1.random.NextDouble() < 0.5);
			sprite.loop = true;
		}

		public override BushBird setTerrainFeature(int index, GameLocation location)
		{
			bush = location.largeTerrainFeatures[index] as Bush;
			flip = bush.tilePosition.X < position.X;
			startingPosition = position;
			return this;
		}

		public override bool update(GameTime time, GameLocation environment)
		{
			characterCheckTimer -= time.ElapsedGameTime.Milliseconds;
			if (characterCheckTimer <= 0 && state != BehaviorStatus.Running)
			{
				if (Utility.isOnScreen(position, -32))
				{
					state = BehaviorStatus.Running;
					sprite.setCurrentAnimation(new List<FarmerSprite.AnimationFrame>
					{
						new FarmerSprite.AnimationFrame(baseFrame, 40),
						new FarmerSprite.AnimationFrame(baseFrame + 1, 40),
						new FarmerSprite.AnimationFrame(baseFrame + 2, 40),
						new FarmerSprite.AnimationFrame(baseFrame + 3, 100),
						new FarmerSprite.AnimationFrame(baseFrame + 5, 70),
						new FarmerSprite.AnimationFrame(baseFrame + 5, 40)
					});
					sprite.loop = true;
				}
				characterCheckTimer = 200;
			}
			if (state == BehaviorStatus.Running)
			{
				position.X += (flip ? (-6) : 6);
			}
			if (state == BehaviorStatus.Running && characterCheckTimer <= 0)
			{
				characterCheckTimer = 200;
				if (environment.largeTerrainFeatures != null)
				{
					Rectangle tileRect = new Rectangle((int)position.X + 32, (int)position.Y - 32, 4, 192);
					foreach (LargeTerrainFeature f in environment.largeTerrainFeatures)
					{
						if (f is Bush && f.getBoundingBox().Intersects(tileRect))
						{
							(f as Bush).performUseAction(f.tilePosition, environment);
							return true;
						}
					}
				}
			}
			return base.update(time, environment);
		}
	}
}
