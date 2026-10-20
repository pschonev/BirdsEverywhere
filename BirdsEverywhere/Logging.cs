using BirdsEverywhere.Domain;
using StardewModdingAPI;

namespace BirdsEverywhere;

public static class Logging
{
    public static void LogBirdSeenStatus(IMonitor monitor, SaveData saveData, IReadOnlyDictionary<string, BirdDefinition> catalog)
    {
        IEnumerable<string> unseenBirds = catalog.Keys.Where(id => !saveData.SeenBirds.Contains(id));
        monitor.Log($"Unseen Birds: {string.Join(", ", unseenBirds)}.", LogLevel.Debug);

        monitor.Log($"Seen Birds:", LogLevel.Debug);
        foreach (string birdName in saveData.BirdObservations.Keys)
        {
            Utils.LogObservation(monitor, catalog, saveData, birdName);
        }
    }
}
