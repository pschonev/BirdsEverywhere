using Newtonsoft.Json;

namespace BirdsEverywhere.Content;

public sealed record SpawnRuleJson
{
    [JsonProperty("when")]
    public SpawnWhenJson? When { get; init; }

    [JsonProperty("locations", Required = Required.Always)]
    public List<string> Locations { get; init; } = new();

    [JsonProperty("spawnPattern", Required = Required.Always)]
    public string SpawnPattern { get; init; } = "";

    [JsonProperty("birdType", Required = Required.Always)]
    public string BirdType { get; init; } = "";

    [JsonProperty("groupCount", Required = Required.Always)]
    public int[] GroupCount { get; init; } = Array.Empty<int>();

    [JsonProperty("groupSize", Required = Required.Always)]
    public int[] GroupSize { get; init; } = Array.Empty<int>();

    [JsonProperty("textures", Required = Required.Always)]
    public Dictionary<string, int> Textures { get; init; } = new();

    [JsonProperty("weight")]
    public int Weight { get; init; } = 1;

    [JsonProperty("spawnParams")]
    public SpawnParamsJson? SpawnParams { get; init; }

    [JsonProperty("routineWeights")]
    public Dictionary<string, int>? RoutineWeights { get; init; }

    [JsonProperty("threatResponse")]
    public string? ThreatResponse { get; init; }
}

public sealed record SpawnParamsJson
{
    [JsonProperty("shoreDistance")]
    public int[]? ShoreDistance { get; init; }
}

public sealed record SpawnWhenJson
{
    [JsonProperty("seasons")]
    public List<string>? Seasons { get; init; }

    [JsonProperty("weather")]
    public List<string>? Weather { get; init; }

    [JsonProperty("days")]
    public List<int[]>? Days { get; init; }

    [JsonProperty("times")]
    public List<int[]>? Times { get; init; }
}