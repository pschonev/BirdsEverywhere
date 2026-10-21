namespace BirdsEverywhere.BirdTypes.Behaviors;

/// <summary>Routine action ids used in JSON; the enum stays the runtime and wire format until actions become data.</summary>
internal static class BirdActionIds
{
    private static readonly IReadOnlyDictionary<string, BirdAction> _byId = new Dictionary<string, BirdAction>(StringComparer.Ordinal)
    {
        ["stop"] = BirdAction.Stop,
        ["sleep"] = BirdAction.Sleep,
        ["peck"] = BirdAction.Peck,
        ["rest"] = BirdAction.Rest,
        ["hop"] = BirdAction.Hop,
        ["turnAndHop"] = BirdAction.TurnAndHop,
        ["walk"] = BirdAction.Walk,
    };

    public static IEnumerable<string> All => _byId.Keys;

    public static bool TryParse(string id, out BirdAction action) => _byId.TryGetValue(id, out action);

    public static BirdAction Parse(string id) =>
        _byId.TryGetValue(id, out BirdAction action) ? action : throw new ArgumentOutOfRangeException(nameof(id), id, "Unknown routine action.");
}
