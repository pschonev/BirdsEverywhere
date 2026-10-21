namespace BirdsEverywhere.Content;

/// <summary>Hardcoded composition of a shipped bird type until bodies move to JSON: layout, clips and events it plays, routine actions, follow-ups allowed in routine <c>next</c>, and default sounds.</summary>
public sealed record BirdTypeProfile(
    string LayoutId,
    IReadOnlyList<string> Clips,
    IReadOnlyList<string> Events,
    IReadOnlyList<string> Actions,
    IReadOnlyDictionary<string, IReadOnlyList<string>> FollowUps,
    string? DefaultRoutine,
    IReadOnlyDictionary<string, string> Sounds);
