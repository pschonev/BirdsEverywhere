using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Birding.Content;

public sealed record SpawnRuleJson
{
    [JsonProperty("when")]
    public JToken? When { get; init; }

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
    public double Weight { get; init; } = 1;

    [JsonProperty("spawnParams")]
    public SpawnParamsJson? SpawnParams { get; init; }

    [JsonProperty("routine")]
    public JToken? Routine { get; init; }

    [JsonProperty("sounds")]
    public Dictionary<string, string>? Sounds { get; init; }

    [JsonProperty("soundCooldownMs")]
    public Dictionary<string, int>? SoundCooldownMs { get; init; }

    [JsonProperty("soundVoices")]
    public Dictionary<string, VoiceLimitJson>? SoundVoices { get; init; }

    [JsonProperty("reactions")]
    public JToken? Reactions { get; init; }

    [JsonProperty("actions")]
    public JObject? Actions { get; init; }

    [JsonProperty("stages")]
    public Dictionary<string, StageJson>? Stages { get; init; }
}

public sealed record StageJson
{
    [JsonProperty("layout")]
    public string? Layout { get; init; }

    [JsonProperty("routine")]
    public JToken? Routine { get; init; }

    [JsonProperty("reactions")]
    public JToken? Reactions { get; init; }

    [JsonProperty("actions")]
    public JObject? Actions { get; init; }

    [JsonProperty("sounds")]
    public Dictionary<string, string>? Sounds { get; init; }
}

public sealed record SpawnParamsJson
{
    [JsonProperty("shoreDistance")]
    public int[]? ShoreDistance { get; init; }

    [JsonProperty("group")]
    public GroupJson? Group { get; init; }

    [JsonProperty("speciesDistance")]
    public int? SpeciesDistance { get; init; }
}

public sealed record GroupJson
{
    [JsonProperty("formation", Required = Required.Always)]
    public string Formation { get; init; } = "";

    [JsonProperty("radius")]
    public int Radius { get; init; } = 3;

    [JsonProperty("spacing")]
    public int Spacing { get; init; } = 1;

    [JsonProperty("groupSpacing")]
    public int GroupSpacing { get; init; }

    [JsonProperty("members")]
    public Dictionary<string, GroupMemberJson>? Members { get; init; }
}

public sealed record GroupMemberJson
{
    [JsonProperty("count", Required = Newtonsoft.Json.Required.Always)]
    public int[] Count { get; init; } = Array.Empty<int>();

    [JsonProperty("stage", Required = Newtonsoft.Json.Required.Always)]
    public string Stage { get; init; } = "";

    [JsonProperty("required")]
    public bool Required { get; init; }

    [JsonProperty("textures")]
    public Dictionary<string, int>? Textures { get; init; }
}

public sealed record VoiceLimitJson
{
    [JsonProperty("max", Required = Required.Always)]
    public int Max { get; init; }

    [JsonProperty("minGapMs")]
    public int MinGapMs { get; init; }
}
