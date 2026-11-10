using System.Collections.Immutable;
using Birding.Domain;
using Newtonsoft.Json.Linq;

namespace Birding.Content;

public static class ReactionLoader
{
    private const int MaxRadius = 20;

    private static readonly HashSet<string> _keys = new(StringComparer.Ordinal) { "threat" };
    private static readonly HashSet<string> _threatKeys = new(StringComparer.Ordinal) { "fleeRadius", "runningMultiplier", "alert", "escape", "maxRelocations" };
    private static readonly HashSet<string> _alertKeys = new(StringComparer.Ordinal) { "radius", "clip", "sound", "calmMs" };
    private static readonly HashSet<string> _escapeKeys = new(StringComparer.Ordinal) { "target", "weight", "maxDistance", "hideForMs" };
    private static readonly HashSet<string> _escapeTargets = new(StringComparer.Ordinal) { "ground", "water", "otherTree", EscapeOption.Tree, EscapeOption.Offscreen };
    private static readonly HashSet<string> _alternativeKeys = new(StringComparer.Ordinal) { "if", "flee" };
    private static readonly HashSet<string> _conditionKeys = new(StringComparer.Ordinal) { "groupHasRole", "nearWater", "season", "timeOfDay", "weather", "stage" };
    private static readonly HashSet<string> _fleeKeys = new(StringComparer.Ordinal) { "radius", "do", "sound", "target", "speedFactor", "escape", "maxRelocations" };

    /// <summary>Loads <c>reactions/*.json</c> raw so rules can override single fields; each file is parsed once against a neutral default to fail fast.</summary>
    public static (IReadOnlyDictionary<string, JObject> Sets, IReadOnlyList<ValidationIssue> Issues) LoadAll(string root) =>
        DefinitionFiles.LoadAll(ContentPaths.ReactionsDirectory(root), (_, json, file) =>
        {
            JObject body = JObject.Parse(json);
            Parse(body, new Reactions(new ThreatReaction(1)), file, "$");
            return body;
        });

    /// <summary>Parses a merged reactions object over the bird type's defaults; throws <see cref="FormatException"/> naming the field.</summary>
    public static Reactions Parse(JToken token, Reactions defaults, string file, string path)
    {
        JObject body = Object(token, file, path, _keys);
        if (body["threat"] is not { } threatToken)
        {
            return defaults;
        }

        string threatPath = path + ".threat";
        if (threatToken is JArray alternatives)
        {
            if (alternatives.Count == 0)
            {
                throw new FormatException($"{file}: {threatPath} needs at least one alternative.");
            }
            List<ThreatAlternative> choices = new();
            for (int index = 0; index < alternatives.Count; index++)
            {
                string choicePath = $"{threatPath}[{index}]";
                JObject choice = Object(alternatives[index], file, choicePath, _alternativeKeys);
                JObject condition = choice["if"] is { } conditionToken
                    ? Object(conditionToken, file, choicePath + ".if", _conditionKeys) : new JObject();
                JObject fleeBody = Object(choice["flee"] ?? throw new FormatException($"{file}: {choicePath}.flee is required."), file, choicePath + ".flee", _fleeKeys);
                string primitive = Text(fleeBody["do"], file, choicePath + ".flee.do")
                    ?? throw new FormatException($"{file}: {choicePath}.flee.do is required.");
                if (primitive is not ("swimAway" or "flyOff"))
                {
                    throw new FormatException($"{file}: {choicePath}.flee.do must be swimAway or flyOff.");
                }
                if (fleeBody["target"] is not null || fleeBody["speedFactor"] is not null)
                {
                    throw new FormatException($"{file}: {choicePath}.flee.target and speedFactor require a follow reaction (not supported yet).");
                }
                int radius = fleeBody["radius"] is { } radiusToken ? Int(radiusToken, 1, MaxRadius, file, choicePath + ".flee.radius") : defaults.Threat.FleeRadius;
                bool? nearWater = condition["nearWater"] switch
                {
                    null => null,
                    { Type: JTokenType.Boolean } value => value.Value<bool>(),
                    _ => throw new FormatException($"{file}: {choicePath}.if.nearWater must be a boolean."),
                };
                Season? season = Name<Season>(condition["season"], file, choicePath + ".if.season");
                ImmutableArray<Weather> weather = Weathers(condition["weather"], file, choicePath + ".if.weather");
                IntRange? time = condition["timeOfDay"] is { } range ? new IntRange(
                    Int(range is JArray { Count: 2 } pair ? pair[0] : range, 600, 2600, file, choicePath + ".if.timeOfDay"),
                    Int(range is JArray { Count: 2 } times ? times[1] : range, 600, 2600, file, choicePath + ".if.timeOfDay")) : null;
                if (time is { } hours && hours.Min > hours.Max)
                {
                    throw new FormatException($"{file}: {choicePath}.if.timeOfDay must be ordered.");
                }
                choices.Add(new ThreatAlternative(
                    new ReactionCondition(Text(condition["groupHasRole"], file, choicePath + ".if.groupHasRole"), nearWater, season, time, weather,
                        Text(condition["stage"], file, choicePath + ".if.stage")),
                    WithEscape(defaults.Threat with { FleeRadius = radius }, fleeBody, file, choicePath + ".flee"),
                    new ActionDefinition(primitive, null, Text(fleeBody["sound"], file, choicePath + ".flee.sound"), 1, null, null, null, null, null, false, null)));
            }
            return new Reactions(defaults.Threat, choices);
        }
        JObject threat = Object(threatToken, file, threatPath, _threatKeys);
        int fleeRadius = threat["fleeRadius"] is { } flee ? Int(flee, 1, MaxRadius, file, threatPath + ".fleeRadius") : defaults.Threat.FleeRadius;
        double running = threat["runningMultiplier"] is { } multiplier
            ? multiplier.Type is JTokenType.Float or JTokenType.Integer && multiplier.Value<double>() is >= 1 and <= 4
                ? multiplier.Value<double>()
                : throw new FormatException($"{file}: {threatPath}.runningMultiplier must be a number from 1 to 4.")
            : defaults.Threat.RunningMultiplier;

        AlertReaction? alert = defaults.Threat.Alert;
        if (threat["alert"] is { } alertToken)
        {
            string alertPath = threatPath + ".alert";
            JObject alertBody = Object(alertToken, file, alertPath, _alertKeys);
            int radius = alertBody["radius"] is { } r ? Int(r, 1, MaxRadius, file, alertPath + ".radius") : throw new FormatException($"{file}: {alertPath}.radius is required.");
            if (radius <= fleeRadius)
            {
                throw new FormatException($"{file}: {alertPath}.radius must be larger than fleeRadius ({fleeRadius}).");
            }
            alert = new AlertReaction(
                radius,
                Text(alertBody["clip"], file, alertPath + ".clip") ?? throw new FormatException($"{file}: {alertPath}.clip is required."),
                Text(alertBody["sound"], file, alertPath + ".sound"),
                alertBody["calmMs"] is { } calm ? Int(calm, 0, 60000, file, alertPath + ".calmMs") : 1500);
        }
        else if (alert is not null && alert.Radius <= fleeRadius)
        {
            throw new FormatException($"{file}: {threatPath}.fleeRadius must be smaller than the alert radius ({alert.Radius}).");
        }

        return new Reactions(WithEscape(new ThreatReaction(fleeRadius, alert, running, defaults.Threat.Escape, defaults.Threat.MaxRelocations), threat, file, threatPath));
    }

    private static ThreatReaction WithEscape(ThreatReaction threat, JObject body, string file, string path)
    {
        if (body["escape"] is { } escapeToken)
        {
            if (escapeToken is not JArray { Count: > 0 } options)
            {
                throw new FormatException($"{file}: {path}.escape must be a non-empty array.");
            }
            threat = threat with { Escape = options.Select((option, index) => ParseEscape(option, file, $"{path}.escape[{index}]")).ToImmutableArray() };
        }
        if (body["maxRelocations"] is { } count)
        {
            threat = threat with { MaxRelocations = Int(count, 1, 10, file, path + ".maxRelocations") };
        }
        return threat;
    }

    private static EscapeOption ParseEscape(JToken token, string file, string path)
    {
        JObject body = Object(token, file, path, _escapeKeys);
        string target = Text(body["target"], file, path + ".target") ?? throw new FormatException($"{file}: {path}.target is required.");
        if (!_escapeTargets.Contains(target))
        {
            throw new FormatException($"{file}: {path}.target has unknown value '{target}'. Use {string.Join(", ", _escapeTargets)}.");
        }
        double weight = body["weight"] switch
        {
            null => 1,
            { Type: JTokenType.Float or JTokenType.Integer } value when value.Value<double>() > 0 => value.Value<double>(),
            _ => throw new FormatException($"{file}: {path}.weight must be a number above 0."),
        };
        if (body["hideForMs"] != null && target != EscapeOption.Tree)
        {
            throw new FormatException($"{file}: {path}.hideForMs only applies to target 'tree'.");
        }
        return new EscapeOption(target, weight, body["maxDistance"] is { } distance ? Int(distance, 2, 30, file, path + ".maxDistance") : 12,
            body["hideForMs"] is { } hide ? Int(hide, 1000, 120000, file, path + ".hideForMs") : EscapeOption.DefaultHideForMs);
    }

    private static JObject Object(JToken token, string file, string path, HashSet<string> keys)
    {
        if (token is not JObject body)
        {
            throw new FormatException($"{file}: {path} must be an object.");
        }
        foreach (JProperty property in body.Properties().Where(property => !keys.Contains(property.Name)))
        {
            if (property.Name == "relocate")
            {
                throw new FormatException($"{file}: {path}.relocate was replaced by escape: [{{ target, weight, maxDistance }}] plus maxRelocations.");
            }
            throw new FormatException($"{file}: {path}.{property.Name} is not a known field. Known: {string.Join(", ", keys)}.");
        }
        return body;
    }

    private static int Int(JToken token, int min, int max, string file, string path) =>
        token.Type == JTokenType.Integer && token.Value<int>() >= min && token.Value<int>() <= max
            ? token.Value<int>()
            : throw new FormatException($"{file}: {path} must be an integer from {min} to {max}.");

    private static string? Text(JToken? token, string file, string path) => token switch
    {
        null => null,
        { Type: JTokenType.String } value when value.Value<string>()!.Length > 0 => value.Value<string>(),
        _ => throw new FormatException($"{file}: {path} must be a non-empty string."),
    };

    private static ImmutableArray<Weather> Weathers(JToken? token, string file, string path)
    {
        string[] names = token switch
        {
            null => Array.Empty<string>(),
            JArray list => list.Select(item => Text(item, file, path) ?? throw new FormatException($"{file}: {path} must contain names.")).ToArray(),
            _ => new[] { Text(token, file, path)! },
        };
        HashSet<Weather> result = new();
        foreach (string name in names)
        {
            result.UnionWith(WeatherNames.TryExpand(name, out IReadOnlyList<Weather> expanded)
                ? expanded : throw new FormatException($"{file}: {path} has unknown value '{name}'. Use {string.Join(", ", WeatherNames.Known)}."));
        }
        return result.Count == Enum.GetValues<Weather>().Length ? ImmutableArray<Weather>.Empty : result.OrderBy(weather => weather).ToImmutableArray();
    }

    private static T? Name<T>(JToken? token, string file, string path) where T : struct, Enum
    {
        string? name = Text(token, file, path);
        if (name is null)
        {
            return null;
        }
        return Enum.TryParse(name, true, out T parsed) && Enum.IsDefined(parsed)
            ? parsed : throw new FormatException($"{file}: {path} has unknown value '{name}'.");
    }
}
