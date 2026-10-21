namespace BirdsEverywhere.Content;

public static class BirdTypeProfiles
{
    public static readonly IReadOnlyDictionary<string, BirdTypeProfile> ByBirdType = new Dictionary<string, BirdTypeProfile>(StringComparer.Ordinal)
    {
        ["LandBird"] = new("land_32",
            new[] { "stand" },
            new[] { "peck", "flap" },
            Map(("sleep", "stay"), ("peck", "stay"), ("hop", "hop"), ("turnAndHop", "hop"), ("walk", "walk"), ("flee", "flyOff")),
            "land_actions",
            new[] { "sleep", "peck", "hop", "turnAndHop", "walk" },
            FollowUps(("peck", new[] { "peck", "sleep", "stop" })),
            "land_idle",
            Map(("alarm", "SpringBirds"), ("flap", "batFlap"), ("peck", "shiny4"))),
        ["WaterLandBird"] = new("waterland_32",
            new[] { "stand" },
            new[] { "hop" },
            Map(("peck", "stay"), ("rest", "stay"), ("hop", "hop"), ("walk", "walk"), ("swim", "swim"), ("flee", "flyOff")),
            "waterland_actions",
            new[] { "peck", "rest", "hop", "walk" },
            FollowUps(),
            "waterland_idle",
            Map(("alarm", "seagulls"))),
        ["TreeTrunkBird"] = new("trunk_16",
            Array.Empty<string>(),
            new[] { "peck", "flap" },
            Map(("peck", "stay"), ("flee", "flyOff")),
            "trunk_actions",
            Array.Empty<string>(),
            FollowUps(),
            null,
            Map(("flap", "batFlap"), ("peck", "Cowboy_gunshot"))),
        ["BushBird"] = new("bush_32",
            Array.Empty<string>(),
            Array.Empty<string>(),
            Map(("flee", "runToBush")),
            "bush_actions",
            Array.Empty<string>(),
            FollowUps(),
            null,
            Map()),
    };

    private static Dictionary<string, IReadOnlyList<string>> FollowUps(params (string Action, string[] Next)[] followUps) =>
        followUps.ToDictionary(entry => entry.Action, entry => (IReadOnlyList<string>)entry.Next, StringComparer.Ordinal);

    private static Dictionary<string, string> Map(params (string Key, string Value)[] entries) =>
        entries.ToDictionary(entry => entry.Key, entry => entry.Value, StringComparer.Ordinal);
}
