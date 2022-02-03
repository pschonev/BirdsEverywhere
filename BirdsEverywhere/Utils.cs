using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewModdingAPI;

namespace BirdsEverywhere
{
    public static class Utils
    {
        private static double transform(double p)
        {
            return Math.Pow(Game1.random.NextDouble(), 1.0 / p);
        }

        public static List<T> shuffleByWeights<T>(List<T> items, List<double> weights)
        {
            List<int> indexes = Enumerable.Range(0, items.Count).ToList();
            indexes.Sort((x, y) =>
                transform(weights[x]).CompareTo(transform(weights[y]))
            );
            return indexes.Select(x => items[x]).ToList();
        }

        public static List<T> shuffleListByOrder<T>(List<T> items, double luckLevel = 2.0)
        {
            luckLevel = Clamp(luckLevel, 0.0, 10.0);
            List<double> birdWeights = Enumerable.Range(1, items.Count).Select(x => (double)x).ToList();
            birdWeights = birdWeights.Select(x => Math.Pow(x, luckLevel) / Math.Log(items.Count, 2)).ToList();
            return shuffleByWeights(items, birdWeights);
        }

        public static T Clamp<T>(this T val, T min, T max) where T : IComparable<T>
        {
            if (val.CompareTo(min) < 0) return min;
            else if (val.CompareTo(max) > 0) return max;
            else return val;
        }

        public static T getRandomElementFromList<T>(List<T> listToPickFrom)
        {
            int index = Game1.random.Next(0, Math.Max(listToPickFrom.Count - 1, 0));
            return listToPickFrom[index];
        }

        public static double getRandomBoundedDoublePosOrNeg(double minDistance, double maxDistance)
        {
            int sign = Game1.random.NextDouble() > 0.5 ? 1 : -1;
            return (Game1.random.NextDouble() * (maxDistance - minDistance) + minDistance) * sign;
        }

        public static bool isCoordInsideScreenBounds(float coord, int sizeDimension, int padding)
        {
            return (coord >= padding - 1 && coord <= sizeDimension - padding);
        }

        public static List<Vector2> getRandomPositionsStartingFromThisTile(Vector2 startTile, GameLocation location, int maxAttempts=100, double minDistance=1.0, double maxDistance=4.0, double branchChance=0.2)
		{
			List<Vector2> tiles = new List<Vector2>();
            tiles.Add(startTile);
            Vector2 currentTile2 = startTile;

			int attempts = 0;

			while (attempts < maxAttempts)
			{
                float xDistance = (float)Math.Round(getRandomBoundedDoublePosOrNeg(minDistance, maxDistance));
                xDistance = isCoordInsideScreenBounds(currentTile2.X + xDistance, location.map.DisplayWidth / 64, 4) ? xDistance : xDistance * -1;
                float yDistance = (float)Math.Round(getRandomBoundedDoublePosOrNeg(minDistance, maxDistance));
                yDistance = isCoordInsideScreenBounds(currentTile2.Y + yDistance, location.map.DisplayHeight / 64, 4) ? yDistance : yDistance * -1;

                Vector2 tile = new Vector2(currentTile2.X + xDistance, currentTile2.Y + yDistance);
                if (!tiles.Contains(tile))
                {
                    tiles.Add(tile);
                    if (Game1.random.NextDouble() <= branchChance)
                        currentTile2 = tile;
                }
                attempts++;
			}
			return tiles;
		}

        public static void logList(IEnumerable<string> list, string description = "-")
        {
            ModEntry.modInstance.Monitor.Log($" {description}: {String.Join(" - ", list)}.", LogLevel.Debug);
        }

        public static void logObservation(string birdName)
        {
            BirdData data = ModEntry.birdDataCollection[birdName];
            ObservationData obs = ModEntry.saveData.birdObservations[birdName];
            ModEntry.modInstance.Monitor.Log($"{data.name} - {data.scName} (ID: {birdName}) was seen by {obs.playerName} at {obs.observationLocation} at {Game1.getTimeOfDayString(obs.observationTime)} on {obs.observationDate.ToLocaleString()}", LogLevel.Debug);
        }

        public static bool isEligibleLocation(GameLocation location)
        {
            return ModEntry.eligibleLocations.Contains(location.Name);
        }
    }
}
