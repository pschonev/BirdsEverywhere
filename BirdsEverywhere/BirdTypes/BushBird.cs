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
		private int index;
		private string locationName;

		private int flightDistance;

		public BushBird(int tileX, int tileY, string birdName, BehaviorStatus state = BehaviorStatus.Pecking)
			: base(0, tileX, tileY, birdName)
		{
			position.Y += 48f;
			sprite.loop = true;
		}

		public override BushBird setTerrainFeature(int index, GameLocation location)
		{
			this.index = index;
			this.locationName = location.Name;

			bush = location.largeTerrainFeatures[index] as Bush;
			flip = bush.tilePosition.X < (position.X / 64);
			startingPosition = position;
			flightDistance = (int)Math.Abs(bush.tilePosition.X - (position.X / 64f)) - 1;

			ModEntry.modInstance.Monitor.Log($"BushBird at {position.X / 64} - {position.Y / 64} will attempt to run to bush at {bush.tilePosition.X} - {bush.tilePosition.Y} (status flipped: {flip}).", LogLevel.Debug);
			return this;
		}

		public override bool update(GameTime time, GameLocation environment)
		{
			characterCheckTimer -= time.ElapsedGameTime.Milliseconds;

			// check if it's time to run away from the farmer
			if (characterCheckTimer <= 0){
				Character f = Utility.isThereAFarmerOrCharacterWithinDistance(position / 64f, flightDistance, environment);

				if (f != null && state != BehaviorStatus.Running)
				{
					if (Utility.isOnScreen(position, -32))
					{
						state = BehaviorStatus.Running;
						addBirdObservation(this.birdName, environment.Name);

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
			}
			// runs away a certain distance
			if (state == BehaviorStatus.Running)
			{
				position.X += (flip ? (-6) : 6);
			}
			// check if it disappears into the bush
			if (state == BehaviorStatus.Running && characterCheckTimer <= 0)
			{
				characterCheckTimer = 200;

				Rectangle tileRect = new Rectangle((int)position.X + 32, (int)position.Y - 32, 4, 192);
				if (bush.getBoundingBox().Intersects(tileRect))
				{
					bush.performUseAction(bush.tilePosition.Value, environment);
					return true;
				}
			}
			return base.update(time, environment);
		}

		// #############
		// # Load/Save #
		// #############

		// this constructor is for loading a bird from saved params 
		public BushBird(Vector2 position, Vector2 startingPosition, string birdName, long birdID, bool flip,
				BehaviorStatus state, CurrentAnimatedSprite currentAnimatedSprite, float gravityAffectedDY, float yOffset, float yJumpOffset, int index, string locationName)
			: base(position, startingPosition, birdName, birdID, flip, state, currentAnimatedSprite, gravityAffectedDY, yOffset, yJumpOffset)
		{
			this.setTerrainFeature(index, Game1.getLocationFromName(locationName));
		}

		public class CurrentBushBirdParams : CurrentBirdParams
		{

			int index;
			string locationName;

			public CurrentBushBirdParams(Vector2 position, Vector2 startingPosition, string birdName, long birdID, bool flip,
				BehaviorStatus state, CurrentAnimatedSprite currentAnimatedSprite, float gravityAffectedDY, float yOffset, float yJumpOffset, int index, string locationName)
				: base(position, startingPosition, birdName, birdID, flip, state, currentAnimatedSprite, gravityAffectedDY, yOffset, yJumpOffset)
			{
				this.birdTypeName = "BushBird";
				this.index = index;
				this.locationName = locationName;
			}

			public override BushBird LoadFromParams()
			{
				return new BushBird(position, startingPosition, birdName, birdID, flip,
				state, currentAnimatedSprite, gravityAffectedDY, yOffset, yJumpOffset, index, locationName);
			}
		}

		public override CurrentBushBirdParams saveParams()
		{
			return new CurrentBushBirdParams(position, startingPosition, birdName, birdID, flip,
				state, getCurrentAnimatedSprite(sprite), gravityAffectedDY, yOffset, yJumpOffset, index, locationName);
		}
	}
}
