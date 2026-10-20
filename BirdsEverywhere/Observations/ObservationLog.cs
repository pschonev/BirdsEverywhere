namespace BirdsEverywhere.Observations;

public static class ObservationLog
{
    public static DeltaStatus Check(SaveData log, int deltaVersion) =>
        deltaVersion <= log.LogVersion ? DeltaStatus.Duplicate
        : deltaVersion == log.LogVersion + 1 ? DeltaStatus.Next
        : DeltaStatus.Gap;

    /// <summary>Adds the first sighting of a bird to a copy of the log and bumps its version; false if the bird was already seen.</summary>
    public static bool TryApply(SaveData log, string birdId, ObservationData details, out SaveData updated)
    {
        if (log.SeenBirds.Contains(birdId))
        {
            updated = log;
            return false;
        }

        updated = new SaveData
        {
            SeenBirds = new HashSet<string>(log.SeenBirds) { birdId },
            BirdObservations = new Dictionary<string, ObservationData>(log.BirdObservations) { [birdId] = details },
            LogVersion = log.LogVersion + 1,
            BirdingXp = log.BirdingXp + BirdingProgress.NewSpeciesXp,
            SightingsDay = log.SightingsDay,
            Sightings = new HashSet<string>(log.Sightings),
        };
        return true;
    }
}
