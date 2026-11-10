namespace Birding.Domain;

public enum Weather
{
    Sun,
    Rain,
    Storm,
    Snow,
    Wind,
    GreenRain,
}

/// <summary>Content-facing weather names: the concrete weathers plus groups expanded at load time.</summary>
public static class WeatherNames
{
    private static readonly Weather[] _all = Enum.GetValues<Weather>();

    private static readonly Dictionary<string, Weather[]> _groups = new(StringComparer.OrdinalIgnoreCase)
    {
        ["any"] = _all,
        ["clear"] = new[] { Weather.Sun, Weather.Wind },
        ["precipitation"] = new[] { Weather.Rain, Weather.Storm, Weather.GreenRain, Weather.Snow },
    };

    public static IEnumerable<string> Known => Enum.GetNames<Weather>().Select(name => name.ToLowerInvariant()).Concat(_groups.Keys).Distinct();

    /// <summary>The concrete weathers named by <paramref name="name"/> (a group or a single weather); false when unknown.</summary>
    public static bool TryExpand(string name, out IReadOnlyList<Weather> weathers)
    {
        if (_groups.TryGetValue(name, out Weather[]? group))
        {
            weathers = group;
            return true;
        }
        if (Enum.TryParse(name, true, out Weather single) && Enum.IsDefined(single))
        {
            weathers = new[] { single };
            return true;
        }
        weathers = Array.Empty<Weather>();
        return false;
    }
}
