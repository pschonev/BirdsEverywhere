namespace BirdsEverywhere.Observations;

/// <summary>Farm-wide birding XP: a new species is worth far more than re-sighting a known one, once per species and day.</summary>
public static class BirdingProgress
{
    public const int NewSpeciesXp = 100;
    public const int RepeatSightingXp = 10;
    private const int XpPerLevelSquared = 100;

    public static int Level(long xp) => (int)Math.Floor(Math.Sqrt(Math.Max(xp, 0) / (double)XpPerLevelSquared));

    public static long LevelStart(int level) => (long)XpPerLevelSquared * level * level;

    /// <summary>Text like "level 2, 100/500": XP gained within the level over the XP the level spans.</summary>
    public static string Describe(long xp)
    {
        int level = Level(xp);
        long start = LevelStart(level);
        return $"level {level}, {xp - start}/{LevelStart(level + 1) - start}";
    }

    /// <summary>Awards repeat-sighting XP on a copy of the log; false if the bird is unseen or was already counted for <paramref name="day"/>.</summary>
    public static bool TryAwardSighting(SaveData log, string birdId, int day, out SaveData updated)
    {
        updated = log;
        if (!log.SeenBirds.Contains(birdId) || (log.SightingsDay == day && log.Sightings.Contains(birdId)))
        {
            return false;
        }

        HashSet<string> sightings = log.SightingsDay == day ? new HashSet<string>(log.Sightings) : new HashSet<string>();
        sightings.Add(birdId);
        updated = new SaveData
        {
            SeenBirds = log.SeenBirds,
            BirdObservations = log.BirdObservations,
            LogVersion = log.LogVersion,
            BirdingXp = log.BirdingXp + RepeatSightingXp,
            SightingsDay = day,
            Sightings = sightings,
        };
        return true;
    }
}
