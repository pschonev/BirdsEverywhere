using BirdsEverywhere.Domain;
using Newtonsoft.Json.Linq;

namespace BirdsEverywhere.Content;

public static class ActionLoader
{
    public static readonly IReadOnlyList<string> Primitives = new[] { "stay", "walk", "hop", "flyOff", "runToBush", "swim", "dive" };

    private static readonly HashSet<string> _keys = new(StringComparer.Ordinal)
    {
        "do", "clip", "sound", "soundChance", "durationMs", "endChance", "speed", "climb", "impulse", "turn", "pauseMs", "hiddenMs", "radius",
    };

    /// <summary>Loads <c>actions/*.json</c> sets (action id → definition) as raw objects so rules can override single fields; each set is parsed once to fail fast.</summary>
    public static (IReadOnlyDictionary<string, JObject> Sets, IReadOnlyList<ValidationIssue> Issues) LoadAll(string root) =>
        DefinitionFiles.LoadAll(ContentPaths.ActionsDirectory(root), (_, json, file) =>
        {
            JObject set = JObject.Parse(json);
            ParseSet(set, file);
            return set;
        });

    public static IReadOnlyDictionary<string, ActionDefinition> ParseSet(JObject set, string file) =>
        set.Properties().ToDictionary(entry => entry.Name, entry => Parse(entry.Value, file, $"$.{entry.Name}"), StringComparer.Ordinal);

    public static ActionDefinition Parse(JToken token, string file, string path)
    {
        if (token is not JObject body)
        {
            throw new FormatException($"{file}: {path} must be an object.");
        }
        foreach (JProperty property in body.Properties().Where(property => !_keys.Contains(property.Name)))
        {
            throw new FormatException($"{file}: {path}.{property.Name} is not a known action field.");
        }

        string primitive = body.Value<string>("do") ?? "";
        if (!Primitives.Contains(primitive))
        {
            throw new FormatException($"{file}: {path}.do must be one of {string.Join(", ", Primitives)}.");
        }

        ActionDefinition action = new(
            primitive,
            Text(body, "clip", file, path),
            Text(body, "sound", file, path),
            Chance(body, "soundChance", file, path) ?? 1,
            body["durationMs"] is { } duration ? LayoutLoader.ParseRange(duration, file, $"{path}.durationMs") : null,
            Chance(body, "endChance", file, path),
            Number(body, "speed", file, path),
            Number(body, "climb", file, path),
            Number(body, "impulse", file, path),
            body["turn"] is { } turn ? turn.Type == JTokenType.Boolean ? turn.Value<bool>() : throw new FormatException($"{file}: {path}.turn must be true or false.") : false,
            body["pauseMs"] is { } pause ? LayoutLoader.ParseRange(pause, file, $"{path}.pauseMs") : null,
            body["hiddenMs"] is { } hidden ? LayoutLoader.ParseRange(hidden, file, $"{path}.hiddenMs") : null,
            body["radius"] is { } radius
                ? radius.Type == JTokenType.Integer && radius.Value<int>() is >= 1 and <= 10 ? radius.Value<int>() : throw new FormatException($"{file}: {path}.radius must be an integer from 1 to 10.")
                : null);

        string? missing = action.Do switch
        {
            "stay" or "swim" when action.Clip is null => "clip",
            "dive" when action.Clip is null || action.HiddenMs is null => "clip and hiddenMs",
            "walk" when action.Clip is null || action.Speed is null => "clip and speed",
            "walk" when action.DurationMs is null && action.EndChance is null => "durationMs or endChance",
            "hop" when action.Impulse is null => "impulse",
            "flyOff" when action.Clip is null || action.Speed is null || action.Climb is null => "clip, speed and climb",
            "runToBush" when action.Clip is null || action.Speed is null => "clip and speed",
            _ => null,
        };
        return missing is null ? action : throw new FormatException($"{file}: {path} ('{action.Do}') needs {missing}.");
    }

    private static string? Text(JObject body, string key, string file, string path) => body[key] switch
    {
        null => null,
        { Type: JTokenType.String } value when value.Value<string>()!.Length > 0 => value.Value<string>(),
        _ => throw new FormatException($"{file}: {path}.{key} must be a non-empty string."),
    };

    private static double? Chance(JObject body, string key, string file, string path) => body[key] switch
    {
        null => null,
        { Type: JTokenType.Float or JTokenType.Integer } value when value.Value<double>() is >= 0 and <= 1 => value.Value<double>(),
        _ => throw new FormatException($"{file}: {path}.{key} must be a number in [0, 1]."),
    };

    private static float? Number(JObject body, string key, string file, string path) => body[key] switch
    {
        null => null,
        { Type: JTokenType.Float or JTokenType.Integer } value when value.Value<float>() >= 0 => value.Value<float>(),
        _ => throw new FormatException($"{file}: {path}.{key} must be a non-negative number."),
    };
}
