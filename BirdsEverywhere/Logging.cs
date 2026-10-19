using StardewModdingAPI;

namespace BirdsEverywhere;

public static class Logging
{
    public static void LogBirdSeenStatus(IMonitor monitor, EnvironmentData environment, SaveData saveData, IReadOnlyDictionary<string, BirdData> catalog)
    {
        foreach (Biome biome in environment.biomes)
        {
            monitor.Log($"{biome.name}:", LogLevel.Debug);
            IEnumerable<string> unseenBirds = biome.birds.Where(x => !saveData.seenBirds.Contains(x));
            monitor.Log($" Unseen Birds: {string.Join(", ", unseenBirds)}.", LogLevel.Debug);
        }

        monitor.Log($"Seen Birds:", LogLevel.Debug);
        foreach (string birdName in saveData.birdObservations.Keys)
        {
            Utils.logObservation(monitor, catalog, saveData, birdName);
        }
    }
}
