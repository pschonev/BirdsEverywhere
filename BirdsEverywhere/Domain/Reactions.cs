namespace BirdsEverywhere.Domain;

/// <summary>Resolved reactions of a rule. Only the <c>threat</c> trigger exists so far.</summary>
public sealed record Reactions(ThreatReaction Threat, IReadOnlyList<ThreatAlternative>? Alternatives = null)
{
	public ThreatAlternative Select(ReactionContext context) =>
		Alternatives?.FirstOrDefault(alternative => alternative.Condition.Matches(context))
		?? new ThreatAlternative(new ReactionCondition(), Threat, null);
}

public sealed record ReactionContext(IReadOnlyCollection<string> GroupRoles, bool NearWater, Season Season, Weather Weather, int Time, string? Stage);

public sealed record ReactionCondition(string? GroupHasRole = null, bool? NearWater = null, Season? Season = null,
	IntRange? TimeOfDay = null, Weather? Weather = null, string? Stage = null)
{
	public bool Matches(ReactionContext context) =>
		(GroupHasRole is null || context.GroupRoles.Contains(GroupHasRole))
		&& (NearWater is null || context.NearWater == NearWater)
		&& (Season is null || context.Season == Season)
		&& (TimeOfDay is null || TimeOfDay.Value.Contains(context.Time))
		&& (Weather is null || context.Weather == Weather)
		&& (Stage is null || context.Stage == Stage);
}

public sealed record ThreatAlternative(ReactionCondition Condition, ThreatReaction Threat, ActionDefinition? Flee);

/// <summary>A threat within <c>FleeRadius</c> tiles runs the bird's <c>flee</c> action, or relocates first; a running farmer counts <c>RunningMultiplier</c> times closer.</summary>
public sealed record ThreatReaction(int FleeRadius, AlertReaction? Alert = null, double RunningMultiplier = 1, RelocateReaction? Relocate = null);

/// <summary>Instead of fleeing, fly to a <c>Target</c> (e.g. <c>otherTree</c>) within <c>MaxDistance</c> tiles, up to <c>MaxRelocations</c> times; then flee as usual.</summary>
public sealed record RelocateReaction(string Target, int MaxDistance, int MaxRelocations);

/// <summary>A threat between the flee and alert radius freezes the bird on <c>Clip</c>; it resumes after <c>CalmMs</c> without a threat in the alert radius.</summary>
public sealed record AlertReaction(int Radius, string Clip, string? Sound, int CalmMs);
