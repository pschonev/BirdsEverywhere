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
    class TreeTrunkBird : CustomBirdTypeTerrainFeature
    {
		public const int flyingSpeed = 6;

		private Tree tree;
		private int index;
		private string locationName;

		private int peckTimer;

		public TreeTrunkBird(int tileX, int tileY, string birdName, BehaviorStatus state = BehaviorStatus.Pecking)
			: base(0, tileX, tileY, birdName, 16, 16)
		{
			base.position.X += 32f;
			base.position.Y += 0f;
			startingPosition = position;
		}

		public override TreeTrunkBird setTerrainFeature(int index, GameLocation location)
        {
			this.tree = location.terrainFeatures.Pairs.ElementAt(index).Value as Tree;
			this.index = index;
			this.locationName = location.Name;
			return this;
		}

		public override void drawAboveFrontLayer(SpriteBatch b)
		{
			sprite.draw(b, Game1.GlobalToLocal(Game1.viewport, position + new Vector2(-80f, -64f + yJumpOffset + yOffset)), 1f, 0, 0, Color.White, flip, 4f);
		}

		public override void draw(SpriteBatch b)
		{
			b.Draw(Game1.shadowTexture, Game1.GlobalToLocal(Game1.viewport, position + new Vector2(0f, -4f)), Game1.shadowTexture.Bounds, Color.White, 0f, new Vector2(Game1.shadowTexture.Bounds.Center.X, Game1.shadowTexture.Bounds.Center.Y), 3f + Math.Max(-3f, (yJumpOffset + yOffset) / 16f), SpriteEffects.None, (position.Y - 1f) / 10000f);
		}

		private void donePecking(Farmer who)
		{
			peckTimer = random.Next(1000, 3000);
		}

		private void playFlap(Farmer who)
		{
			if (Utility.isOnScreen(position, 64))
			{
				Game1.playSound("batFlap");
			}
		}

		private void playPeck(Farmer who)
		{
			if (Utility.isOnScreen(position, 64))
			{
				Game1.playSound("Cowboy_gunshot");
			}
		}

		public override bool update(GameTime time, GameLocation environment)
		{
			if (environment == null || sprite == null || tree == null)
			{
				return true;
			}
			if (yJumpOffset < 0f && state != BehaviorStatus.FlyingAway)
			{
				if (!flip && !environment.isCollidingPosition(getBoundingBox(-2, 0), Game1.viewport, isFarmer: false, 0, glider: false, null, pathfinding: false, projectile: false, ignoreCharacterRequirement: true))
				{
					position.X -= 2f;
				}
				else if (!environment.isCollidingPosition(getBoundingBox(2, 0), Game1.viewport, isFarmer: false, 0, glider: false, null, pathfinding: false, projectile: false, ignoreCharacterRequirement: true))
				{
					position.X += 2f;
				}
			}
			peckTimer -= time.ElapsedGameTime.Milliseconds;
			if (state != BehaviorStatus.FlyingAway && peckTimer <= 0 && sprite.CurrentAnimation == null)
			{
				int nibbles = random.Next(2, 8);
				List<FarmerSprite.AnimationFrame> anim = new List<FarmerSprite.AnimationFrame>();
				for (int i = 0; i < nibbles; i++)
				{
					anim.Add(new FarmerSprite.AnimationFrame(baseFrame, 100));
					anim.Add(new FarmerSprite.AnimationFrame(baseFrame + 1, 100, secondaryArm: false, flip: false, playPeck));
				}
				anim.Add(new FarmerSprite.AnimationFrame(baseFrame, 200, secondaryArm: false, flip: false, donePecking));
				sprite.setCurrentAnimation(anim);
				sprite.loop = false;
			}
			characterCheckTimer -= time.ElapsedGameTime.Milliseconds;
			if (characterCheckTimer < 0)
			{
				Character f = Utility.isThereAFarmerOrCharacterWithinDistance(position / 64f, 6, environment);
				characterCheckTimer = 200;
				if ((f != null || (bool)tree.stump.Value) && state != BehaviorStatus.FlyingAway)
				{
					state = BehaviorStatus.FlyingAway;
					addBirdObservation(this.birdName, environment.Name);

					if (f != null && f.Position.X > position.X)
					{
						flip = false;
					}
					else
					{
						flip = true;
					}
					sprite.setCurrentAnimation(new List<FarmerSprite.AnimationFrame>
					{
						new FarmerSprite.AnimationFrame((short)(baseFrame + 2), 70),
						new FarmerSprite.AnimationFrame((short)(baseFrame + 3), 60, secondaryArm: false, flip, playFlap),
						new FarmerSprite.AnimationFrame((short)(baseFrame + 4), 70),
						new FarmerSprite.AnimationFrame((short)(baseFrame + 3), 60)
					});
					sprite.loop = true;
				}
			}
			if (state == BehaviorStatus.FlyingAway)
			{
				if (!flip)
				{
					position.X -= 6f;
				}
				else
				{
					position.X += 6f;
				}
				yOffset -= 1f;
			}
			return base.update(time, environment);
		}


		// #############
		// # Load/Save #
		// #############

		// this constructor is for loading a bird from saved params 
		public TreeTrunkBird(Vector2 position, Vector2 startingPosition, string birdName, long birdID, bool flip,
				BehaviorStatus state, CurrentAnimatedSprite currentAnimatedSprite, float gravityAffectedDY, float yOffset, float yJumpOffset, int index, string locationName, int peckTimer)
			: base(position, startingPosition, birdName, birdID, flip, state, currentAnimatedSprite, gravityAffectedDY, yOffset, yJumpOffset, spriteWidth: 16, spriteHeight: 16)
		{
			this.setTerrainFeature(index, Game1.getLocationFromName(locationName));
			this.peckTimer = peckTimer;
		}

		public class CurrentTreeTrunkBirdParams : CurrentBirdParams
		{

			public int index { get; set; }
			public string locationName { get; set; }
			public int peckTimer { get; set; }

			public CurrentTreeTrunkBirdParams(Vector2 position, Vector2 startingPosition, string birdName, long birdID, bool flip,
				BehaviorStatus state, CurrentAnimatedSprite currentAnimatedSprite, float gravityAffectedDY, float yOffset, float yJumpOffset, int index, string locationName, int peckTimer)
				: base(position, startingPosition, birdName, birdID, flip, state, currentAnimatedSprite, gravityAffectedDY, yOffset, yJumpOffset)
			{
				this.birdTypeName = "TreeTrunkBird";
				this.index = index;
				this.locationName = locationName;
				this.peckTimer = peckTimer;
			}

			public override TreeTrunkBird LoadFromParams()
			{
				return new TreeTrunkBird(position, startingPosition, birdName, birdID, flip,
				state, currentAnimatedSprite, gravityAffectedDY, yOffset, yJumpOffset, index, locationName, peckTimer);
			}
		}

		public override CurrentTreeTrunkBirdParams saveParams()
		{
			return new CurrentTreeTrunkBirdParams(position, startingPosition, birdName, birdID, flip,
				state, getCurrentAnimatedSprite(sprite), gravityAffectedDY, yOffset, yJumpOffset, index, locationName, peckTimer);
		}
	}
}
