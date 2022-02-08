using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley.BellsAndWhistles;
using StardewValley;
using StardewModdingAPI;
using StardewModdingAPI.Utilities;
using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace BirdsEverywhere.BirdTypes
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
			Running,
			Swimming
		}
		public abstract string birdTypeName { get; }
		public BehaviorStatus state { get; set; }

		[JsonIgnore]
		public string birdTexture { get; set; }

		public string birdName { get; set; }

		public int characterCheckTimer { get; set; } = 200;

		public long birdID { get; set; }

		[JsonIgnore]
		protected Random random;

		protected CustomBirdType(int baseFrame, int tileX, int tileY, string birdName, int spriteWidth=32, int spriteHeight=32)
			: base(baseFrame, new Vector2(tileX * 64, tileY * 64))
        {
			this.birdName = birdName;
			this.birdTexture = getTextureName(birdName);
			this.sprite = new AnimatedSprite(birdTexture, baseFrame, spriteWidth, spriteHeight);
			this.birdID = ModEntry.modInstance.Helper.Multiplayer.GetNewID();
			seedRandom();
		}

		// constructor that doesn't create new bird ID
		protected CustomBirdType(int baseFrame, int tileX, int tileY, string birdName, long birdID, int spriteWidth = 32, int spriteHeight = 32)
			: base(baseFrame, new Vector2(tileX * 64, tileY * 64))
		{
			this.birdName = birdName;
			this.birdTexture = getTextureName(birdName);
			this.sprite = new AnimatedSprite(birdTexture, baseFrame, spriteWidth, spriteHeight);
			this.birdID = birdID;
			seedRandom();
		}

		// this is for loading a bird from saved params
		protected CustomBirdType(Vector2 position, Vector2 startingPosition, string birdName, long birdID, bool flip,
				BehaviorStatus state, CurrentAnimatedSprite sprite, float gravityAffectedDY, float yOffset, float yJumpOffset,
				int characterCheckTimer = 200, int baseFrame = 0, int spriteWidth = 32, int spriteHeight = 32)
			: this(baseFrame, (int)startingPosition.X / 64, (int)startingPosition.Y / 64, birdName, birdID, spriteWidth, spriteHeight)
        {
			this.position = position;
			this.flip = flip;
			this.state = state;
			this.characterCheckTimer = characterCheckTimer;
			this.gravityAffectedDY = gravityAffectedDY;
			this.yOffset = yOffset;
			this.yJumpOffset = yJumpOffset;

			// setting the sprite
			this.birdTexture = getTextureName(birdName);
			if (sprite != null)
			{
				AnimatedSprite currentSprite = new AnimatedSprite(birdTexture, baseFrame, spriteWidth, spriteHeight);
				if (sprite.currentAnimation != null)
				{
					List<FarmerSprite.AnimationFrame> currentAnim = new List<FarmerSprite.AnimationFrame>();
					foreach ((int frame, int ms) frameWithTime in sprite.currentAnimation)
					{
						currentAnim.Add(new FarmerSprite.AnimationFrame((short)(baseFrame + frameWithTime.frame), frameWithTime.ms));
					}
					currentSprite.setCurrentAnimation(currentAnim);
				}
				currentSprite.currentFrame = sprite.currentFrame;
				currentSprite.timer = sprite.timer;
				currentSprite.loop = sprite.loop;
				currentSprite.currentAnimationIndex = sprite.currentAnimationIndex;
				this.sprite = currentSprite;
			}
			else
				this.sprite = null;

			// set random again with birdID seed
			seedRandom();
		}

		public CustomBirdType setState(BehaviorStatus state)
        {
			this.state = state;
			return this;
        }

		public void seedRandom()
        {
			this.random = new Random((int)(this.birdID % Int32.MaxValue));
		}

		private string getTextureName(string birdName)
        {
			string assetPath = $"assets/{birdName}/{birdName}.png";
			Texture2D texture = ModEntry.modInstance.Helper.Content.Load<Texture2D>(assetPath);
			return ModEntry.modInstance.Helper.Content.GetActualAssetKey(assetPath);
		}

		public override void drawAboveFrontLayer(SpriteBatch b)
		{
			if (state == BehaviorStatus.FlyingAway)
			{
				base.draw(b);
			}
		}

		public void addBirdObservation(string birdName, string location)
		{
			if (ModEntry.saveData.seenBirds.Add(birdName))
			{
				// create observation and add to save file
				ObservationData observation = new ObservationData(location, new SDate(Game1.dayOfMonth, Game1.currentSeason), Game1.timeOfDay, Game1.player.Name);
				ModEntry.saveData.birdObservations[birdName] = observation;
				// log observation
				ModEntry.modInstance.Monitor.Log($"New bird was added to seen birds:", LogLevel.Debug);
				Utils.logObservation(birdName);
				// send observation multiplayer
				ModEntry.modInstance.Helper.Multiplayer.SendMessage(ModEntry.saveData, "SaveNewObservation", modIDs: new[] { ModEntry.modInstance.ModManifest.UniqueID });
				// show global message
				string ObservationMessage = $"{Game1.player.Name} discovered a new bird: {ModEntry.birdDataCollection[birdName].name}!";
				Game1.showGlobalMessage(ObservationMessage);
				ModEntry.modInstance.Helper.Multiplayer.SendMessage(ObservationMessage, "GlobalObservationMessage", modIDs: new[] { ModEntry.modInstance.ModManifest.UniqueID });
			}
		}

		public abstract class CurrentBirdParams
        {
			public string birdTypeName { get; set; }

			public Vector2 position { get; set; }
			public Vector2 startingPosition { get; set; }
			public BehaviorStatus state { get; set; }
			public string birdName { get; set; }
			public int characterCheckTimer { get; set; }
			public long birdID { get; set; }
			public bool flip { get; set; }
			public CurrentAnimatedSprite currentAnimatedSprite { get; set; }
			public float gravityAffectedDY { get; set; }
			public float yOffset { get; set; }
			public float yJumpOffset { get; set; }

			public int baseFrame { get; set; }
			public int spriteWidth { get; set; }
			public int spriteHeight { get; set; }

			public CurrentBirdParams(Vector2 position, Vector2 startingPosition, string birdName, long birdID, bool flip, 
				BehaviorStatus state, CurrentAnimatedSprite currentAnimatedSprite, float gravityAffectedDY, float yOffset, float yJumpOffset,
				int characterCheckTimer = 200, int baseFrame=0,  int spriteWidth = 32, int spriteHeight = 32)
            {
				this.position = position;
				this.startingPosition = startingPosition;
				this.state = state;
				this.birdName = birdName;
				this.characterCheckTimer = characterCheckTimer;
				this.birdID = birdID;
				this.flip = flip;
				this.currentAnimatedSprite = currentAnimatedSprite;
				this.gravityAffectedDY = gravityAffectedDY;
				this.yOffset = yOffset;
				this.yJumpOffset = yJumpOffset;

				this.baseFrame = baseFrame;
				this.spriteWidth = spriteWidth;
				this.spriteHeight = spriteHeight;
            }

			public abstract CustomBirdType LoadFromParams();



		}

		public class CurrentAnimatedSprite
        {
			public List<(int frame, int ms)> currentAnimation { get; set; }
			public int currentFrame { get; set; }
			public float timer { get; set; }
			public bool loop { get; set; }
			public int currentAnimationIndex { get; set; }

			public CurrentAnimatedSprite(List<(int frame, int ms)> currentAnimation,int currentFrame, float timer,  bool loop, int currentAnimationIndex)
            {
				this.currentAnimation = currentAnimation;
				this.currentFrame = currentFrame;
				this.timer = timer;
				this.loop = loop;
				this.currentAnimationIndex = currentAnimationIndex;
            }

			internal CurrentAnimatedSprite(AnimatedSprite sprite)
            {
				if (currentAnimation != null)
				{
					List<(int frame, int ms)> currentAnimation = new List<(int frame, int ms)>();
					foreach (FarmerSprite.AnimationFrame animFrame in sprite.CurrentAnimation)
					{
						currentAnimation.Add((animFrame.frame, animFrame.milliseconds));
					}
					this.currentAnimation = currentAnimation;
				}
				else
					this.currentAnimation = null;
				this.currentFrame = sprite.currentFrame;
				this.timer = sprite.timer;
				this.loop = sprite.loop;
				this.currentAnimationIndex = sprite.currentAnimationIndex;
			}
		}

		public abstract CurrentBirdParams saveParams();

		public CurrentAnimatedSprite getCurrentAnimatedSprite(AnimatedSprite sprite)
        {
			if (sprite != null)
				return new CurrentAnimatedSprite(sprite);
			else
				return null;
		}
	}
}
