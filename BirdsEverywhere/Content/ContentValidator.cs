using System.Collections.Immutable;
using BirdsEverywhere.Domain;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BirdsEverywhere.Content;

public static class ContentValidator
{
    /// <summary>Resolves the rule's routine (ref, inline, or bird-type default) and merges its sounds over the bird type's defaults.</summary>
    private static BirdComposition? Composition(SpawnRuleJson rule, string path, string file, IReadOnlyDictionary<string, Routine> routines,
        Action<IssueSeverity, string, string> report)
    {
        if (!BirdTypeProfiles.ByBirdType.TryGetValue(rule.BirdType, out BirdTypeProfile? profile))
        {
            return null;
        }

        Routine? routine = null;
        string routinePath = path + ".routine";
        if (rule.Routine is { Type: JTokenType.String } reference)
        {
            string id = reference.Value<string>()!;
            if (!routines.TryGetValue(id, out routine))
            {
                report(IssueSeverity.Error, routinePath, $"Unknown routine '{id}'.");
            }
        }
        else if (rule.Routine is JObject inline)
        {
            try
            {
                routine = RoutineLoader.Parse(inline, file, routinePath);
            }
            catch (FormatException exception)
            {
                report(IssueSeverity.Error, routinePath, exception.Message);
            }
        }
        else if (rule.Routine is not null)
        {
            report(IssueSeverity.Error, routinePath, "Routine must be a routine id or an inline routine object.");
        }
        else if (profile.DefaultRoutine is { } fallback && !routines.TryGetValue(fallback, out routine))
        {
            report(IssueSeverity.Error, routinePath, $"Default routine '{fallback}' for {rule.BirdType} is missing.");
        }

        if (routine is not null && rule.Routine is not null && profile.DefaultRoutine is null)
        {
            report(IssueSeverity.Error, routinePath, $"{rule.BirdType} has no idle routine.");
        }
        if (routine is not null)
        {
            foreach (string action in routine.Pick.Select(option => option.Id).Distinct().Where(action => !profile.Actions.Contains(action)))
            {
                report(IssueSeverity.Error, routinePath + ".pick", $"{rule.BirdType} cannot perform action '{action}'. Known: {string.Join(", ", profile.Actions)}.");
            }
            foreach ((string action, IReadOnlyList<WeightedId> options) in routine.Next)
            {
                if (!profile.FollowUps.TryGetValue(action, out IReadOnlyList<string>? allowed))
                {
                    report(IssueSeverity.Error, $"{routinePath}.next.{action}", $"{rule.BirdType} has no follow-up after '{action}'. Known: {string.Join(", ", profile.FollowUps.Keys)}.");
                    continue;
                }
                foreach (string target in options.Select(option => option.Id).Where(target => !allowed.Contains(target)))
                {
                    report(IssueSeverity.Error, $"{routinePath}.next.{action}.{target}", $"{rule.BirdType} cannot follow '{action}' with '{target}'. Known: {string.Join(", ", allowed)}.");
                }
            }
        }

        Dictionary<string, string> sounds = new(profile.Sounds, StringComparer.Ordinal);
        foreach ((string role, string cue) in rule.Sounds ?? new Dictionary<string, string>())
        {
            if (!profile.Sounds.ContainsKey(role))
            {
                report(IssueSeverity.Warn, $"{path}.sounds.{role}", $"{rule.BirdType} never plays sound role '{role}'. Known: {string.Join(", ", profile.Sounds.Keys)}.");
            }
            sounds[role] = cue;
        }

        return new BirdComposition(routine, sounds);
    }

    public static (BirdDefinition? Bird, IReadOnlyList<ValidationIssue> Issues) Validate(
        JObject document,
        string file,
        string chain,
        IReadOnlyDictionary<string, string> sources,
        IReadOnlyDictionary<string, IReadOnlyList<string>> groups,
        IReadOnlyCollection<SpawnPatternId> patterns,
        IReadOnlyCollection<BirdTypeId> birdTypes,
        IReadOnlyDictionary<string, Routine> routines,
        Func<string, bool> textureExists)
    {
        List<ValidationIssue> issues = new();
        string id = Path.GetFileNameWithoutExtension(file);

        void Report(IssueSeverity severity, string path, string message)
        {
            string source = sources.TryGetValue(path, out string? value) ? value : file;
            issues.Add(new ValidationIssue(severity, source, id, path, chain, message));
        }

        BirdJson? json;
        try
        {
            json = document.ToObject<BirdJson>(JsonSerializer.Create(new JsonSerializerSettings
            {
                MissingMemberHandling = MissingMemberHandling.Error,
            }));
        }
        catch (JsonException exception)
        {
            Report(IssueSeverity.Error, "$" + (exception is JsonSerializationException serialization && serialization.Path is { Length: > 0 } ? "." + serialization.Path : ""), exception.Message);
            return (null, issues);
        }

        if (json is null)
        {
            Report(IssueSeverity.Error, "$", "Bird must be an object.");
            return (null, issues);
        }

        if (json.Id != id)
        {
            Report(IssueSeverity.Error, "$.id", $"Expected id '{id}', got '{json.Id}'.");
        }

        foreach ((string value, string path) in new[] { (json.Name, "$.name"), (json.ScientificName, "$.scName") })
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                Report(IssueSeverity.Error, path, "Must not be empty.");
            }
        }

        if (json.Shorthand is not null && string.IsNullOrWhiteSpace(json.Shorthand))
        {
            Report(IssueSeverity.Error, "$.shorthand", "Must not be empty when specified.");
        }

        if (json.Rules.Count == 0)
        {
            Report(IssueSeverity.Warn, "$.rules", "Bird has no rules.");
        }

        ImmutableArray<SpawnRule>.Builder rules = ImmutableArray.CreateBuilder<SpawnRule>();
        foreach (KeyValuePair<string, SpawnRuleJson> entry in json.Rules)
        {
            string path = $"$.rules.{entry.Key}";
            SpawnRuleJson rule = entry.Value;
            if (rule is null)
            {
                Report(IssueSeverity.Error, path, "Rule must be an object.");
                continue;
            }

            if (rule.Weight <= 0)
            {
                Report(IssueSeverity.Error, path + ".weight", "Weight must be positive.");
            }

            if (!patterns.Contains(new SpawnPatternId(rule.SpawnPattern)))
            {
                Report(IssueSeverity.Error, path + ".spawnPattern", $"Unknown spawn pattern '{rule.SpawnPattern}'.");
            }

            if (!birdTypes.Contains(new BirdTypeId(rule.BirdType)))
            {
                Report(IssueSeverity.Error, path + ".birdType", $"Unknown bird type '{rule.BirdType}'.");
            }

            IntRange count = ValidateRange(rule.GroupCount, path + ".groupCount", 1, int.MaxValue, Report);
            IntRange size = ValidateRange(rule.GroupSize, path + ".groupSize", 1, int.MaxValue, Report);
            List<string> locations = new();
            if (rule.Locations is null || rule.Locations.Count == 0)
            {
                Report(IssueSeverity.Error, path + ".locations", "At least one location is required.");
            }
            else
            {
                foreach (string location in rule.Locations)
                {
                    if (location is null || location.Length == 0)
                    {
                        Report(IssueSeverity.Error, path + ".locations", "Location must not be empty.");
                    }
                    else if (location.StartsWith('@'))
                    {
                        if (groups.TryGetValue(location.Substring(1), out IReadOnlyList<string>? names))
                        {
                            locations.AddRange(names);
                        }
                        else
                        {
                            Report(IssueSeverity.Error, path + ".locations", $"Unknown location group '{location}'.");
                        }
                    }
                    else
                    {
                        locations.Add(location);
                    }
                }
            }

            if (locations.Count == 0)
            {
                Report(IssueSeverity.Error, path + ".locations", "No locations resolved.");
            }

            Dictionary<string, int> textures = new(StringComparer.Ordinal);
            if (rule.Textures is null || rule.Textures.Count == 0)
            {
                Report(IssueSeverity.Error, path + ".textures", "At least one texture is required.");
            }
            else
            {
                foreach (KeyValuePair<string, int> texture in rule.Textures)
                {
                    string texturePath = ContentPaths.Texture(Path.GetDirectoryName(Path.GetDirectoryName(file)!)!, id, texture.Key);
                    if (texture.Value <= 0)
                    {
                        Report(IssueSeverity.Error, path + ".textures." + texture.Key, "Texture weight must be positive.");
                    }

                    if (!textureExists(texturePath))
                    {
                        Report(IssueSeverity.Error, path + ".textures." + texture.Key, $"Texture not found: {texturePath}.");
                    }

                    textures[texture.Key] = texture.Value;
                }
            }

            SpawnWhenJson when = rule.When ?? new SpawnWhenJson();
            ImmutableArray<Season>.Builder seasons = ImmutableArray.CreateBuilder<Season>();
            foreach (string season in when.Seasons ?? new List<string>())
            {
                if (Enum.TryParse(season, ignoreCase: true, out Season parsed) && Enum.IsDefined(parsed))
                {
                    seasons.Add(parsed);
                }
                else
                {
                    Report(IssueSeverity.Error, path + ".when.seasons", $"Unknown season '{season}'.");
                }
            }

            ImmutableArray<Weather>.Builder weather = ImmutableArray.CreateBuilder<Weather>();
            foreach (string condition in when.Weather ?? new List<string>())
            {
                if (Enum.TryParse(condition, ignoreCase: true, out Weather parsed) && Enum.IsDefined(parsed))
                {
                    weather.Add(parsed);
                }
                else
                {
                    Report(IssueSeverity.Error, path + ".when.weather", $"Unknown weather '{condition}'.");
                }
            }

            ImmutableArray<IntRange> days = ValidateRanges(when.Days, path + ".when.days", 1, 28, Report);
            ImmutableArray<IntRange> times = ValidateRanges(when.Times, path + ".when.times", 600, 2600, Report);
            IntRange? shoreDistance = rule.SpawnParams?.ShoreDistance is int[] shore
                ? ValidateRange(shore, path + ".spawnParams.shoreDistance", 0, SpawnParams.MaxShoreDistance, Report)
                : null;
            rules.Add(new SpawnRule(entry.Key, new SpawnConditions(seasons.ToImmutable(), weather.ToImmutable(), days, times),
                locations.Distinct(StringComparer.Ordinal).ToImmutableArray(), new SpawnPatternId(rule.SpawnPattern), new BirdTypeId(rule.BirdType),
                count, size, textures.ToImmutableDictionary(StringComparer.Ordinal), rule.Weight, new SpawnParams(shoreDistance),
                Composition(rule, path, file, routines, Report)));
        }

        foreach (Season season in Enum.GetValues<Season>())
        {
            foreach (Weather weather in Enum.GetValues<Weather>())
            {
                if (!rules.Any(rule => rule.When.MatchesDay(season, weather, 1)))
                {
                    Report(IssueSeverity.Info, "$.rules", $"No eligible rule for {season}/{weather} on day 1.");
                }
            }
        }

        return issues.Any(issue => issue.Severity == IssueSeverity.Error)
            ? (null, issues)
            : (new BirdDefinition(new BirdId(json.Id), json.Name, json.ScientificName, rules.ToImmutable()) { Shorthand = json.Shorthand }, issues);
    }

    private static IntRange ValidateRange(int[]? values, string path, int minimum, int maximum, Action<IssueSeverity, string, string> report)
    {
        if (values is not { Length: 2 } || values[0] < minimum || values[1] > maximum || values[0] > values[1])
        {
            report(IssueSeverity.Error, path, $"Expected [min, max] with {minimum} <= min <= max <= {maximum}.");
            return default;
        }

        return new IntRange(values[0], values[1]);
    }

    private static ImmutableArray<IntRange> ValidateRanges(List<int[]>? values, string path, int minimum, int maximum, Action<IssueSeverity, string, string> report) =>
        values is null ? ImmutableArray<IntRange>.Empty : values.Select((range, index) => ValidateRange(range, $"{path}[{index}]", minimum, maximum, report)).ToImmutableArray();
}
