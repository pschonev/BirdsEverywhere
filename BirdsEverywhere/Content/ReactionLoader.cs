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
}
