using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewModdingAPI;

namespace BirdsEverywhere
{
    public static class Logging
    {
        public static void PrintSeenBirds(string command, string[] args)
        {
            LogBirdSeenStatus();
        }
        public static void LogBirdSeenStatus()
        {
            foreach (Biome biome in ModEntry.environmentData.biomes)
            {
                ModEntry.modInstance.Monitor.Log($"{biome.name}:", LogLevel.Debug);
                List<string> unseenBirds = biome.birds.Where(x => !ModEntry.saveData.seenBirds.Contains(x)).ToList();
                //unseenBirds = unseenBirds.Select(x => birdDataCollection[x].name).ToList();
                ModEntry.modInstance.Monitor.Log($" Unseen Birds: {String.Join(", ", unseenBirds)}.", LogLevel.Debug);
            }

            ModEntry.modInstance.Monitor.Log($"Seen Birds:", LogLevel.Debug);
            foreach (var kvp in ModEntry.saveData.birdObservations)
            {
                Utils.logObservation(kvp.Key);
            }
        }
    }
}
