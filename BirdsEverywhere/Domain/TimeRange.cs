namespace BirdsEverywhere.Domain;

/// <summary>An inclusive time window whose ends may be anchored to the day's light.</summary>
public readonly record struct TimeRange(TimeBound From, TimeBound To)
{
    /// <summary>The window at these anchors; <c>Min &gt; Max</c> when the anchors leave it empty.</summary>
    public IntRange Resolve(TimeAnchors anchors) => new(From.Resolve(anchors), To.Resolve(anchors));

    public override string ToString() => $"{From}–{To}";
}
