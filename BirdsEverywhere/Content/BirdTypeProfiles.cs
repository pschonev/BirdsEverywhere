namespace BirdsEverywhere.Content;

public static class BirdTypeProfiles
{
    public static readonly IReadOnlyDictionary<string, BirdTypeProfile> ByBirdType = new Dictionary<string, BirdTypeProfile>(StringComparer.Ordinal)
    {
        ["LandBird"] = new("land_32",
            new[] { "stand", "walk", "peck", "sleep", "fly" },
            new[] { "peck", "flap" },
            new[] { "sleep", "peck", "hop", "turnAndHop", "walk" },
            FollowUps(("peck", new[] { "peck", "sleep", "stop" })),
            "land_idle",
            Sounds(("alarm", "SpringBirds"), ("flap", "batFlap"), ("peck", "shiny4"))),
        ["WaterLandBird"] = new("waterland_32",
            new[] { "stand", "walk", "peck", "rest", "hop", "swim", "fly" },
            new[] { "hop" },
            new[] { "peck", "rest", "hop", "walk" },
            FollowUps(),
            "waterland_idle",
            Sounds(("alarm", "seagulls"))),
        ["TreeTrunkBird"] = new("trunk_16",
            new[] { "peck", "fly" },
            new[] { "peck", "flap" },
            Array.Empty<string>(),
            FollowUps(),
            null,
            Sounds(("flap", "batFlap"), ("peck", "Cowboy_gunshot"))),
        ["BushBird"] = new("bush_32",
            new[] { "run" },
            Array.Empty<string>(),
            Array.Empty<string>(),
            FollowUps(),
            null,
            Sounds()),
    };

    private static Dictionary<string, IReadOnlyList<string>> FollowUps(params (string Action, string[] Next)[] followUps) =>
        followUps.ToDictionary(entry => entry.Action, entry => (IReadOnlyList<string>)entry.Next, StringComparer.Ordinal);

    private static Dictionary<string, string> Sounds(params (string Role, string Cue)[] sounds) =>
        sounds.ToDictionary(sound => sound.Role, sound => sound.Cue, StringComparer.Ordinal);
}
