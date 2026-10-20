namespace BirdsEverywhere.Observations;

public static class ObservationLog
{
    public static DeltaStatus Check(SaveData log, int deltaVersion) =>
        deltaVersion <= log.logVersion ? DeltaStatus.Duplicate
        : deltaVersion == log.logVersion + 1 ? DeltaStatus.Next
        : DeltaStatus.Gap;

    /// <summary>Adds the first sighting of a bird to a copy of the log and bumps its version; false if the bird was already seen.</summary>
    public static bool TryApply(SaveData log, string birdId, ObservationData details, out SaveData updated)
    {
        if (log.seenBirds.Contains(birdId))
        {
            updated = log;
            return false;
        }

        updated = new SaveData
        {
            seenBirds = new HashSet<string>(log.seenBirds) { birdId },
            birdObservations = new Dictionary<string, ObservationData>(log.birdObservations) { [birdId] = details },
            logVersion = log.logVersion + 1,
        };
        return true;
    }
}
