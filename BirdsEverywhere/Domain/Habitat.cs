namespace BirdsEverywhere.Domain;

public sealed record Habitat(string Key, string Location, string? FarmType, string? FishArea)
{
    private const string FarmPrefix = "Farm_";

    public static Habitat Parse(string key)
    {
        int colon = key.IndexOf(':');
        string place = colon < 0 ? key : key.Substring(0, colon);
        string? fishArea = colon < 0 ? null : key.Substring(colon + 1);
        return place.StartsWith(FarmPrefix, StringComparison.Ordinal)
            ? new Habitat(key, "Farm", place.Substring(FarmPrefix.Length), fishArea)
            : new Habitat(key, place, null, fishArea);
    }
}
