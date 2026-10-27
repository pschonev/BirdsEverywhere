namespace BirdsEverywhere.Domain;

/// <summary>Clock times (HHMM) of the day's light changes at one location.</summary>
public readonly record struct TimeAnchors(int Sunrise, int Dusk, int Dark)
{
    /// <summary>Used where no location is known (coverage checks, tests).</summary>
    public static readonly TimeAnchors Default = new(600, 1800, 2000);

    public int Of(AnchorKind kind) => kind switch
    {
        AnchorKind.Sunrise => Sunrise,
        AnchorKind.Dusk => Dusk,
        AnchorKind.Dark => Dark,
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Fixed times have no anchor."),
    };
}
