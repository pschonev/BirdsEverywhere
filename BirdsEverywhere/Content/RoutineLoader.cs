using BirdsEverywhere.Domain;
using Newtonsoft.Json.Linq;

namespace BirdsEverywhere.Content;

public static class RoutineLoader
{
    private static readonly HashSet<string> _keys = new(StringComparer.Ordinal) { "chance", "pick", "next" };

    public static (IReadOnlyDictionary<string, Routine> Routines, IReadOnlyList<ValidationIssue> Issues) LoadAll(string root) =>
        DefinitionFiles.LoadAll(ContentPaths.RoutinesDirectory(root), (_, json, file) => Parse(JToken.Parse(json), file, "$"));

    /// <summary>Parses a routine file or an inline rule <c>routine</c> object; throws <see cref="FormatException"/> naming the field.</summary>
    public static Routine Parse(JToken token, string file, string path)
    {
        if (token is not JObject body)
        {
            throw new FormatException($"{file}: {path} must be an object.");
        }
        foreach (JProperty property in body.Properties().Where(property => !_keys.Contains(property.Name)))
        {
            throw new FormatException($"{file}: {path}.{property.Name} is not a known routine field.");
        }
        if (body["chance"] is not { Type: JTokenType.Float or JTokenType.Integer } chanceToken || chanceToken.Value<double>() is not (> 0 and <= 1))
        {
            throw new FormatException($"{file}: {path}.chance must be a number in (0, 1].");
        }

        Dictionary<string, IReadOnlyList<WeightedId>> next = new(StringComparer.Ordinal);
        if (body["next"] is JObject nextBody)
        {
            foreach (JProperty entry in nextBody.Properties())
            {
                next[entry.Name] = Weights(entry.Value, file, $"{path}.next.{entry.Name}");
            }
        }
        else if (body["next"] is not null)
        {
            throw new FormatException($"{file}: {path}.next must be an object.");
        }

        return new Routine(chanceToken.Value<double>(), Weights(body["pick"], file, $"{path}.pick"), next);
    }

    private static List<WeightedId> Weights(JToken? token, string file, string path)
    {
        if (token is not JObject weights || weights.Count == 0)
        {
            throw new FormatException($"{file}: {path} must be a non-empty object of action weights.");
        }

        List<WeightedId> result = new();
        foreach (JProperty entry in weights.Properties())
        {
            if (entry.Value.Type != JTokenType.Integer || entry.Value.Value<int>() < 1)
            {
                throw new FormatException($"{file}: {path}.{entry.Name} must be a positive integer weight.");
            }
            result.Add(new WeightedId(entry.Name, entry.Value.Value<int>()));
        }
        return result;
    }
}
