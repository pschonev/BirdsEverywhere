using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley.BellsAndWhistles;
using StardewValley;
using StardewModdingAPI;
using StardewModdingAPI.Utilities;
using System;
using System.Collections.Generic;

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

		protected BehaviorStatus state;

		protected string birdTexture;

		protected string birdName;

		protected int characterCheckTimer = 200;

		protected long birdID;

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

		// constructor that doesn't get new bird ID
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
			AnimatedSprite currentSprite = new AnimatedSprite(birdTexture, baseFrame, spriteWidth, spriteHeight);
			List<FarmerSprite.AnimationFrame> currentAnim = new List<FarmerSprite.AnimationFrame>();
			foreach((int frame, int ms) frameWithTime in sprite.currentAnimation)
            {
				currentAnim.Add(new FarmerSprite.AnimationFrame((short)(baseFrame + frameWithTime.frame), frameWithTime.ms));
			}
			currentSprite.setCurrentAnimation(currentAnim);
			currentSprite.currentFrame = sprite.currentFrame;
			currentSprite.timer = sprite.timer;
			currentSprite.loop = sprite.loop;
			currentSprite.currentAnimationIndex = sprite.currentAnimationIndex;
			this.sprite = currentSprite;

			// set random again
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

			protected Vector2 position;
			protected Vector2 startingPosition;
			protected BehaviorStatus state;
			protected string birdName;
			protected int characterCheckTimer;
			protected long birdID;
			protected bool flip;
			protected CurrentAnimatedSprite currentAnimatedSprite;
			protected float gravityAffectedDY;
			protected float yOffset;
			protected float yJumpOffset;

			protected int baseFrame;
			protected int spriteWidth;
			protected int spriteHeight;

			protected CurrentBirdParams(Vector2 position, Vector2 startingPosition, string birdName, long birdID, bool flip, 
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
			public List<(int frame, int ms)> currentAnimation;
			public int currentFrame;
			public float timer;
			public bool loop;
			public int currentAnimationIndex;

			public CurrentAnimatedSprite(AnimatedSprite sprite)
            {
				List<(int frame, int ms)> currentAnimation = new List<(int frame, int ms)>();
				foreach(FarmerSprite.AnimationFrame animFrame in sprite.CurrentAnimation)
                {
					currentAnimation.Add((animFrame.frame, animFrame.milliseconds));
                }

				this.currentAnimation = currentAnimation;
				this.currentFrame = sprite.currentFrame;
				this.timer = sprite.timer;
				this.loop = sprite.loop;
				this.currentAnimationIndex = sprite.currentAnimationIndex;
			}
		}

		public abstract CurrentBirdParams saveParams();
	}
}
