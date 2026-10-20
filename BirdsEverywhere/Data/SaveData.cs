using Newtonsoft.Json;
using StardewModdingAPI.Utilities;

namespace BirdsEverywhere;

public class SaveData
{
    [JsonProperty("seenBirds")]
    public HashSet<string> SeenBirds { get; set; } = new HashSet<string>();

    [JsonProperty("birdObservations")]
    public Dictionary<string, ObservationData> BirdObservations { get; set; } = new Dictionary<string, ObservationData>();

    /// <summary>Counts accepted observations; peers use it to detect missed updates. Saves from before it existed load as 0.</summary>
    [JsonProperty("logVersion")]
    public int LogVersion { get; set; }

    /// <summary>Farm-wide birding experience; saves from before it existed load as 0.</summary>
    [JsonProperty("birdingXp")]
    public long BirdingXp { get; set; }

    /// <summary>Game day of <see cref="Sightings"/>; the set is reset when a new day's first repeat sighting arrives.</summary>
    [JsonProperty("sightingsDay")]
    public int SightingsDay { get; set; }

    [JsonProperty("sightings")]
    public HashSet<string> Sightings { get; set; } = new HashSet<string>();
}

public class ObservationData
{
    [JsonProperty("observationLocation")]
    public string ObservationLocation { get; set; }

    [JsonProperty("observationDate")]
    public SDate ObservationDate { get; set; }

    [JsonProperty("observationTime")]
    public int ObservationTime { get; set; }

    [JsonProperty("playerName")]
    public string PlayerName { get; set; }

    [JsonProperty("feather")]
    public bool Feather { get; set; }

    [JsonProperty("egg")]
    public bool Egg { get; set; }

    [JsonProperty("picture")]
    public bool Picture { get; set; }

    public ObservationData(string observationLocation, SDate observationDate, int observationTime, string playerName)
    {
        this.ObservationLocation = observationLocation;
        this.ObservationDate = observationDate;
        this.ObservationTime = observationTime;
        this.PlayerName = playerName;
    }
}
