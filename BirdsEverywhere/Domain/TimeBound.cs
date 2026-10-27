using System.Globalization;

namespace BirdsEverywhere.Domain;

/// <summary>One end of a time window: a fixed HHMM clock time, or an anchor plus a clock-minute offset.</summary>
/// <param name="Kind">Fixed, or the anchor the offset applies to.</param>
/// <param name="Value">HHMM for <see cref="AnchorKind.Fixed"/>, otherwise the offset in minutes.</param>
public readonly record struct TimeBound(AnchorKind Kind, int Value)
{
    public const int MinTime = 600;
    public const int MaxTime = 2600;
    public const int MaxOffsetMinutes = 999;
    private const int SnapMinutes = 10;

    public static TimeBound At(int clock) => new(AnchorKind.Fixed, clock);

    /// <summary>Parses <c>sunrise</c>, <c>dusk</c> or <c>dark</c> with an optional <c>+minutes</c> / <c>-minutes</c> suffix.</summary>
    public static bool TryParse(string text, out TimeBound bound)
    {
        bound = default;
        foreach (AnchorKind kind in new[] { AnchorKind.Sunrise, AnchorKind.Dusk, AnchorKind.Dark })
        {
            string name = kind.ToString().ToLowerInvariant();
            if (!text.StartsWith(name, StringComparison.Ordinal))
            {
                continue;
            }
            string rest = text[name.Length..];
            if (rest.Length == 0)
            {
                bound = new TimeBound(kind, 0);
                return true;
            }
            if (rest.Length is >= 2 and <= 4 && rest[0] is '+' or '-' && rest[1..].All(character => character is >= '0' and <= '9')
                && int.Parse(rest[1..], CultureInfo.InvariantCulture) is var minutes && minutes <= MaxOffsetMinutes)
            {
                bound = new TimeBound(kind, rest[0] == '-' ? -minutes : minutes);
                return true;
            }
            return false;
        }
        return false;
    }

    /// <summary>The HHMM time at these anchors; anchored bounds add clock minutes, snap to 10 minutes and stay within 0600–2600.</summary>
    public int Resolve(TimeAnchors anchors)
    {
        if (Kind == AnchorKind.Fixed)
        {
            return Value;
        }
        int minutes = ToMinutes(anchors.Of(Kind)) + Value;
        minutes = Math.Max(0, (minutes + SnapMinutes / 2) / SnapMinutes * SnapMinutes);
        return Math.Clamp(minutes / 60 * 100 + minutes % 60, MinTime, MaxTime);
    }

    public override string ToString() => Kind == AnchorKind.Fixed ? Value.ToString(CultureInfo.InvariantCulture)
        : Kind.ToString().ToLowerInvariant() + (Value == 0 ? "" : Value > 0 ? $"+{Value}" : Value.ToString(CultureInfo.InvariantCulture));

    private static int ToMinutes(int clock) => clock / 100 * 60 + clock % 100;
}
