using System.Collections.Immutable;
using BirdsEverywhere.Domain;
using Newtonsoft.Json.Linq;

namespace BirdsEverywhere.Content;

public static class RoutineLoader
{
    private static readonly HashSet<string> _keys = new(StringComparer.Ordinal) { "chance", "pick", "next", "rare" };
    private static readonly HashSet<string> _rareKeys = new(StringComparer.Ordinal) { "chance", "when" };
    private static readonly HashSet<string> _whenKeys = new(StringComparer.Ordinal) { "seasons", "weather", "days", "times" };

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

        return new Routine(chanceToken.Value<double>(), Weights(body["pick"], file, $"{path}.pick"), next, Rare(body["rare"], file, $"{path}.rare"));
    }

    private static List<RareAction>? Rare(JToken? token, string file, string path)
    {
        if (token is null)
        {
            return null;
        }
        if (token is not JObject entries)
        {
            throw new FormatException($"{file}: {path} must be an object of rare actions.");
        }

        List<RareAction> rare = new();
        foreach (JProperty entry in entries.Properties())
        {
            string entryPath = $"{path}.{entry.Name}";
            if (entry.Value is not JObject body)
            {
                throw new FormatException($"{file}: {entryPath} must be an object.");
            }
            foreach (JProperty property in body.Properties().Where(property => !_rareKeys.Contains(property.Name)))
            {
                throw new FormatException($"{file}: {entryPath}.{property.Name} is not a known field. Known: {string.Join(", ", _rareKeys)}.");
            }
            if (body["chance"] is not { Type: JTokenType.Float or JTokenType.Integer } chance || chance.Value<double>() is not (> 0 and <= 1))
            {
                throw new FormatException($"{file}: {entryPath}.chance must be a number in (0, 1].");
            }
            rare.Add(new RareAction(entry.Name, chance.Value<double>(), When(body["when"], file, entryPath + ".when")));
        }
        return rare;
    }

    private static SpawnConditions When(JToken? token, string file, string path)
    {
        if (token is null)
        {
            return new SpawnConditions(ImmutableArray<Season>.Empty, ImmutableArray<Weather>.Empty, ImmutableArray<IntRange>.Empty, ImmutableArray<IntRange>.Empty);
        }
        if (token is not JObject body)
        {
            throw new FormatException($"{file}: {path} must be an object.");
        }
        foreach (JProperty property in body.Properties().Where(property => !_whenKeys.Contains(property.Name)))
        {
            throw new FormatException($"{file}: {path}.{property.Name} is not a known condition. Known: {string.Join(", ", _whenKeys)}.");
        }
        return new SpawnConditions(
            Names<Season>(body["seasons"], file, path + ".seasons"),
            Names<Weather>(body["weather"], file, path + ".weather"),
            Ranges(body["days"], 1, 28, file, path + ".days"),
            Ranges(body["times"], 600, 2600, file, path + ".times"));
    }

    private static ImmutableArray<T> Names<T>(JToken? token, string file, string path) where T : struct, Enum =>
        token is null ? ImmutableArray<T>.Empty
        : token is JArray names && names.All(name => name.Type == JTokenType.String && Enum.TryParse(name.Value<string>(), true, out T parsed) && Enum.IsDefined(parsed))
            ? names.Select(name => Enum.Parse<T>(name.Value<string>()!, true)).ToImmutableArray()
            : throw new FormatException($"{file}: {path} must be a list of {string.Join(", ", Enum.GetNames<T>())}.");

    private static ImmutableArray<IntRange> Ranges(JToken? token, int min, int max, string file, string path) =>
        token is null ? ImmutableArray<IntRange>.Empty
        : token is JArray ranges && ranges.All(range => range is JArray { Count: 2 } pair && pair.All(value => value.Type == JTokenType.Integer)
            && pair[0].Value<int>() >= min && pair[0].Value<int>() <= pair[1].Value<int>() && pair[1].Value<int>() <= max)
            ? ranges.Select(range => new IntRange(range[0]!.Value<int>(), range[1]!.Value<int>())).ToImmutableArray()
            : throw new FormatException($"{file}: {path} must be a list of [min, max] within {min}–{max}.");

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
