using System.Collections.Immutable;

namespace Birding.Domain;

/// <summary>Resolved reactions of a rule. Only the <c>threat</c> trigger exists so far.</summary>
public sealed record Reactions(ThreatReaction Threat, IReadOnlyList<ThreatAlternative>? Alternatives = null)
{
    public ThreatAlternative Select(ReactionContext context) =>
        Alternatives?.FirstOrDefault(alternative => alternative.Condition.Matches(context))
        ?? new ThreatAlternative(new ReactionCondition(), Threat, null);
}

public sealed record ReactionContext(IReadOnlyCollection<string> GroupRoles, bool NearWater, Season Season, Weather Weather, int Time, string? Stage);

public sealed record ReactionCondition(string? GroupHasRole = null, bool? NearWater = null, Season? Season = null,
    IntRange? TimeOfDay = null, ImmutableArray<Weather> Weathers = default, string? Stage = null)
{
    public bool Matches(ReactionContext context) =>
        (GroupHasRole is null || context.GroupRoles.Contains(GroupHasRole))
        && (NearWater is null || context.NearWater == NearWater)
        && (Season is null || context.Season == Season)
        && (TimeOfDay is null || TimeOfDay.Value.Contains(context.Time))
        && (Weathers.IsDefaultOrEmpty || Weathers.Contains(context.Weather))
        && (Stage is null || context.Stage == Stage);
}

public sealed record ThreatAlternative(ReactionCondition Condition, ThreatReaction Threat, ActionDefinition? Flee);

/// <summary>A threat within <c>FleeRadius</c> tiles runs the bird's <c>flee</c> action, or escapes to a nearby spot first while fewer than <c>MaxRelocations</c> were used; a running farmer counts <c>RunningMultiplier</c> times closer.</summary>
public sealed record ThreatReaction(int FleeRadius, AlertReaction? Alert = null, double RunningMultiplier = 1,
    ImmutableArray<EscapeOption> Escape = default, int MaxRelocations = 1)
{
    private readonly ImmutableArray<EscapeOption> _escape = Escape.IsDefault ? ImmutableArray<EscapeOption>.Empty : Escape;

    public ImmutableArray<EscapeOption> Escape
    {
        get => _escape;
        init => _escape = value.IsDefault ? ImmutableArray<EscapeOption>.Empty : value;
    }

    /// <summary>Whether an escape option names <paramref name="target"/>.</summary>
    public bool CanEscapeTo(string target) => Escape.Any(option => option.Target == target);
}

/// <summary>One weighted escape choice: a spot on <c>Target</c> within <c>MaxDistance</c> tiles, or off screen for <see cref="Offscreen"/>; <c>tree</c> hides the bird for <c>HideForMs</c>.</summary>
public sealed record EscapeOption(string Target, double Weight, int MaxDistance, int HideForMs = EscapeOption.DefaultHideForMs)
{
    public const string Offscreen = "offscreen";
    public const string Tree = "tree";
    public const int DefaultHideForMs = 10000;
}

/// <summary>A threat between the flee and alert radius freezes the bird on <c>Clip</c>; it resumes after <c>CalmMs</c> without a threat in the alert radius, or flees after <c>FleeAfterMs</c> of continuous alert (never when null).</summary>
public sealed record AlertReaction(int Radius, string Clip, string? Sound, int CalmMs, int? FleeAfterMs = null);
