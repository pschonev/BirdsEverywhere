using BirdsEverywhere.Domain;
using Newtonsoft.Json.Linq;

namespace BirdsEverywhere.Content;

public static class ReactionLoader
{
    private const int MaxRadius = 20;

    private static readonly HashSet<string> _keys = new(StringComparer.Ordinal) { "threat" };
    private static readonly HashSet<string> _threatKeys = new(StringComparer.Ordinal) { "fleeRadius", "runningMultiplier", "alert", "relocate" };
    private static readonly HashSet<string> _alertKeys = new(StringComparer.Ordinal) { "radius", "clip", "sound", "calmMs" };
    private static readonly HashSet<string> _relocateKeys = new(StringComparer.Ordinal) { "target", "maxDistance", "maxRelocations" };
    private static readonly HashSet<string> _alternativeKeys = new(StringComparer.Ordinal) { "if", "flee" };
    private static readonly HashSet<string> _conditionKeys = new(StringComparer.Ordinal) { "groupHasRole", "nearWater", "season", "timeOfDay", "weather", "stage" };
    private static readonly HashSet<string> _fleeKeys = new(StringComparer.Ordinal) { "radius", "do", "sound", "target", "speedFactor" };

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
                Weather? weather = Name<Weather>(condition["weather"], file, choicePath + ".if.weather");
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
                    defaults.Threat with { FleeRadius = radius },
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

        RelocateReaction? relocate = defaults.Threat.Relocate;
        if (threat["relocate"] is { } relocateToken)
        {
            string relocatePath = threatPath + ".relocate";
            JObject relocateBody = Object(relocateToken, file, relocatePath, _relocateKeys);
            relocate = new RelocateReaction(
                Text(relocateBody["target"], file, relocatePath + ".target") ?? throw new FormatException($"{file}: {relocatePath}.target is required."),
                relocateBody["maxDistance"] is { } distance ? Int(distance, 2, 30, file, relocatePath + ".maxDistance") : 12,
                relocateBody["maxRelocations"] is { } count ? Int(count, 1, 10, file, relocatePath + ".maxRelocations") : 1);
        }

        return new Reactions(new ThreatReaction(fleeRadius, alert, running, relocate));
    }

    private static JObject Object(JToken token, string file, string path, HashSet<string> keys)
    {
        if (token is not JObject body)
        {
            throw new FormatException($"{file}: {path} must be an object.");
        }
        foreach (JProperty property in body.Properties().Where(property => !keys.Contains(property.Name)))
        {
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
