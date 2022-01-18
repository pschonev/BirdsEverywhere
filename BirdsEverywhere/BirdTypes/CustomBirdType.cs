using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley.BellsAndWhistles;
using StardewValley;
using StardewModdingAPI;
using StardewModdingAPI.Utilities;

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
			Texture2D texture = ModEntry.modInstance.Helper.Content.Load<Texture2D>(assetPath);
			this.birdTexture = ModEntry.modInstance.Helper.Content.GetActualAssetKey(assetPath);
			this.sprite = new AnimatedSprite(birdTexture, baseFrame, 32, 32);
		}

		public CustomBirdType setState(BehaviorStatus state)
        {
			this.state = state;
			return this;
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
				// send observation
				ModEntry.modInstance.Helper.Multiplayer.SendMessage(ModEntry.saveData, "SaveNewObservation", modIDs: new[] { ModEntry.modInstance.ModManifest.UniqueID });
			}
		}
	}
}
