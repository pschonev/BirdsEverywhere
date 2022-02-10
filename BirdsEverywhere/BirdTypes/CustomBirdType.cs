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

		private long _birdID;

		public abstract string birdTypeName { get; }
		public BehaviorStatus state { get; set; }

		public string birdName { get; set; }

		public int characterCheckTimer { get; set; } = 200;

		public long birdID {
			get => _birdID;
			set {
				_birdID = value;
				seedRandom();
			} }

		public CurrentAnimatedSprite currentAnimatedSprite { set
            {
				this.sprite = CurrentAnimatedSprite.CurrentToAnimatedSprite(value);
			} }

		[JsonIgnore]
		protected Random random;

		protected CustomBirdType(int baseFrame, int tileX, int tileY, string birdName, int spriteWidth=32, int spriteHeight=32)
			: base(baseFrame, new Vector2(tileX * 64, tileY * 64))
        {
			this.birdName = birdName;
			string textureKey = getTextureName(birdName);
			this.sprite = new AnimatedSprite(textureKey, baseFrame, spriteWidth, spriteHeight);
			this.birdID = ModEntry.modInstance.Helper.Multiplayer.GetNewID();
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
	}
}
