using BirdsEverywhere.Domain;
using StardewModdingAPI;

namespace BirdsEverywhere;

public static class Logging
{
    public static void LogBirdSeenStatus(IMonitor monitor, SaveData saveData, IReadOnlyDictionary<string, BirdDefinition> catalog)
    {
        IEnumerable<string> unseenBirds = catalog.Keys.Where(id => !saveData.seenBirds.Contains(id));
        monitor.Log($"Unseen Birds: {string.Join(", ", unseenBirds)}.", LogLevel.Debug);

        monitor.Log($"Seen Birds:", LogLevel.Debug);
        foreach (string birdName in saveData.birdObservations.Keys)
        {
            Utils.logObservation(monitor, catalog, saveData, birdName);
        }
    }
}
