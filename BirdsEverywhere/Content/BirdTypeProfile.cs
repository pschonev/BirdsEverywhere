namespace BirdsEverywhere.Content;

/// <summary>Hardcoded composition of a shipped bird type until bodies move to JSON. <c>Poses</c> are single-cell clips shown directly; <c>ActionPrimitives</c> maps each action id the class runs to its primitive; <c>Actions</c> are the ids a routine may pick. <c>Reactions</c> null means the type has its own threat logic (bush).</summary>
public sealed record BirdTypeProfile(
    string LayoutId,
    IReadOnlyList<string> Poses,
    IReadOnlyList<string> Events,
    IReadOnlyDictionary<string, string> ActionPrimitives,
    string DefaultActions,
    IReadOnlyList<string> Actions,
    IReadOnlyDictionary<string, IReadOnlyList<string>> FollowUps,
    string? DefaultRoutine,
    IReadOnlyDictionary<string, string> Sounds,
    Domain.Reactions? Reactions,
    bool CanAlert,
    IReadOnlyList<string> RelocateTargets,
    IReadOnlyList<string> CustomPrimitives);
