using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;

namespace BirdsEverywhere;

public static class Utils
{
    public static List<T> shuffleByWeights<T>(Random random, List<T> items, List<double> weights)
    {
        double[] keys = weights.Select(w => Math.Pow(random.NextDouble(), 1.0 / w)).ToArray();
        return Enumerable.Range(0, items.Count).OrderBy(i => keys[i]).Select(i => items[i]).ToList();
    }

    public static List<T> shuffleListByOrder<T>(Random random, List<T> items, double luckLevel = 2.0)
    {
        luckLevel = Clamp(luckLevel, 0.0, 10.0);
        List<double> birdWeights = Enumerable.Range(1, items.Count).Select(x => (double)x).ToList();
        birdWeights = birdWeights.Select(x => Math.Pow(x, luckLevel) / Math.Log(items.Count, 2)).ToList();
        return shuffleByWeights(random, items, birdWeights);
    }

    public static T Clamp<T>(this T val, T min, T max) where T : IComparable<T>
    {
        if (val.CompareTo(min) < 0)
        {
            return min;
        }
        else if (val.CompareTo(max) > 0)
        {
            return max;
        }
        else
        {
            return val;
        }
    }

    public static T getRandomElementFromList<T>(Random random, IReadOnlyList<T> listToPickFrom)
    {
        if (listToPickFrom.Count == 0)
        {
            throw new ArgumentException("Cannot pick from an empty list.", nameof(listToPickFrom));
        }

        return listToPickFrom[random.Next(listToPickFrom.Count)];
    }

    public static int NextInclusive(Random random, int min, int max)
    {
        if (min > max)
        {
            throw new ArgumentOutOfRangeException(nameof(min), min, $"min must not exceed max ({max}).");
        }

        return random.Next(min, max + 1);
    }

    public static double getRandomBoundedDoublePosOrNeg(Random random, double minDistance, double maxDistance)
    {
        int sign = random.NextDouble() > 0.5 ? 1 : -1;
        return (random.NextDouble() * (maxDistance - minDistance) + minDistance) * sign;
    }

    public static bool isCoordInsideScreenBounds(float coord, int sizeDimension, int padding)
    {
        return (coord >= padding - 1 && coord <= sizeDimension - padding);
    }

    public static List<Vector2> getRandomPositionsStartingFromThisTile(Random random, Vector2 startTile, Point mapSize, int maxAttempts = 100, double minDistance = 1.0, double maxDistance = 4.0, double branchChance = 0.2)
    {
        List<Vector2> tiles = new List<Vector2>();
        tiles.Add(startTile);
        Vector2 currentTile2 = startTile;

        int attempts = 0;

        while (attempts < maxAttempts)
        {
            float xDistance = (float)Math.Round(getRandomBoundedDoublePosOrNeg(random, minDistance, maxDistance));
            xDistance = isCoordInsideScreenBounds(currentTile2.X + xDistance, mapSize.X, 4) ? xDistance : xDistance * -1;
            float yDistance = (float)Math.Round(getRandomBoundedDoublePosOrNeg(random, minDistance, maxDistance));
            yDistance = isCoordInsideScreenBounds(currentTile2.Y + yDistance, mapSize.Y, 4) ? yDistance : yDistance * -1;

            Vector2 tile = new Vector2(currentTile2.X + xDistance, currentTile2.Y + yDistance);
            if (!tiles.Contains(tile))
            {
                tiles.Add(tile);
                if (random.NextDouble() <= branchChance)
                {
                    currentTile2 = tile;
                }
            }
            attempts++;
        }
        return tiles;
    }

    public static void logObservation(IMonitor monitor, IReadOnlyDictionary<string, BirdData> catalog, SaveData saveData, string birdName)
    {
        BirdData data = catalog[birdName];
        ObservationData obs = saveData.birdObservations[birdName];
        monitor.Log($"{data.name} - {data.scName} (ID: {birdName}) was seen by {obs.playerName} at {obs.observationLocation} at {Game1.getTimeOfDayString(obs.observationTime)} on {obs.observationDate.ToLocaleString()}", LogLevel.Debug);
    }

    public static int StableHash(string text)
    {
        unchecked
        {
            int hash = (int)2166136261;
            foreach (char c in text)
            {
                hash = (hash ^ c) * 16777619;
            }

            return hash;
        }
    }

    public static void MergeDictionaries<OBJ1, OBJ2>(this IDictionary<OBJ1, List<OBJ2>> dict1, IDictionary<OBJ1, List<OBJ2>> dict2)
    {
        foreach (var kvp2 in dict2)
        {
            // If the dictionary already contains the key then merge them
            if (dict1.ContainsKey(kvp2.Key))
            {
                dict1[kvp2.Key].AddRange(kvp2.Value);
                continue;
            }
            dict1.Add(kvp2);
        }
    }
}
