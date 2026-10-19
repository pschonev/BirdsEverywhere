using Newtonsoft.Json;

namespace BirdsEverywhere.Content;

public sealed record BirdJson
{
    [JsonProperty("id", Required = Required.Always)]
    public string Id { get; init; } = "";

    [JsonProperty("name", Required = Required.Always)]
    public string Name { get; init; } = "";

    [JsonProperty("scName", Required = Required.Always)]
    public string ScientificName { get; init; } = "";

    [JsonProperty("description", Required = Required.Always)]
    public string Description { get; init; } = "";

    [JsonProperty("template")]
    public string? Template { get; init; }

    [JsonProperty("rules", Required = Required.Always)]
    public Dictionary<string, SpawnRuleJson> Rules { get; init; } = new();
}