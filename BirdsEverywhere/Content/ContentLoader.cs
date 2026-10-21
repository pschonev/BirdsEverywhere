using BirdsEverywhere.Domain;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BirdsEverywhere.Content;

public static class ContentLoader
{
    private static readonly HashSet<string> _templateFields = new(StringComparer.Ordinal) { "template", "defaults", "rules" };
    private static readonly HashSet<string> _birdFields = new(StringComparer.Ordinal) { "id", "name", "scName", "shorthand", "template", "defaults", "rules" };

    public static (BirdDefinition? Bird, IReadOnlyList<ValidationIssue> Issues, JObject? Effective, IReadOnlyDictionary<string, string> Sources) LoadBird(
        string root,
        string id,
        IReadOnlyDictionary<string, IReadOnlyList<string>> groups,
        IReadOnlyCollection<SpawnPatternId> patterns,
        IReadOnlyCollection<BirdTypeId> birdTypes,
        SharedDefinitions definitions,
        Func<string, string?> read,
        Func<string, bool> textureExists)
    {
        string file = ContentPaths.Bird(root, id);
        List<ValidationIssue> issues = new();
        JObject Read(string path)
        {
            string? text = read(path);
            if (text is null)
            {
                throw new FileNotFoundException($"Missing content file '{path}'.", path);
            }

            return JObject.Parse(text);
        }

        try
        {
            (JObject document, IReadOnlyDictionary<string, string> sources, string chain) =
                TemplateMerger.Resolve(file, name => ContentPaths.Template(root, name), Read);
            foreach (string templateFile in chain.Split(" -> ").SkipLast(1))
            {
                JObject template = Read(templateFile);
                foreach (string field in new[] { "id", "name", "scName", "shorthand" })
                {
                    if (template.ContainsKey(field))
                    {
                        issues.Add(new ValidationIssue(IssueSeverity.Error, templateFile, id, "$." + field, chain, "Identity field is not allowed in a template."));
                    }
                }

                foreach (string key in template.Properties().Select(property => property.Name).Except(_templateFields))
                {
                    issues.Add(new ValidationIssue(IssueSeverity.Error, templateFile, id, "$." + key, chain, "Unknown template field."));
                }
            }

            JObject birdDocument = Read(file);
            foreach (string key in birdDocument.Properties().Select(property => property.Name).Except(_birdFields))
            {
                issues.Add(new ValidationIssue(IssueSeverity.Error, file, id, "$." + key, chain, "Unknown bird field."));
            }

            foreach (JProperty property in (birdDocument["rules"] as JObject ?? new JObject()).Properties())
            {
                if (property.Value is not JObject rule)
                {
                    continue;
                }

                if (HasInheritedRule(root, id, property.Name, read))
                {
                    continue;
                }

                if (rule.ContainsKey("when"))
                {
                    issues.Add(new ValidationIssue(IssueSeverity.Info, file, id, "$.rules." + property.Name, chain, "New rule with conditions."));
                }
                else
                {
                    issues.Add(new ValidationIssue(IssueSeverity.Error, file, id, "$.rules." + property.Name, chain, "New rule requires 'when'."));
                }
            }

            foreach (JProperty field in (birdDocument["defaults"] as JObject ?? new JObject()).Properties())
            {
                foreach (JProperty rule in ((JObject?)document["rules"] ?? new JObject()).Properties())
                {
                    if (rule.Value is JObject effective && sources.TryGetValue($"$.rules.{rule.Name}.{field.Name}", out string? source) && source != file)
                    {
                        issues.Add(new ValidationIssue(IssueSeverity.Warn, file, id, "$.defaults." + field.Name, chain, $"Shadowed by rule '{rule.Name}' from {source}."));
                    }
                }
            }

            (BirdDefinition? bird, IReadOnlyList<ValidationIssue> validation) = ContentValidator.Validate(document, file, chain, sources, groups, patterns, birdTypes, definitions, textureExists);
            issues.AddRange(validation);
            return (issues.Any(issue => issue.Severity == IssueSeverity.Error) ? null : bird, issues, document, sources);
        }
        catch (JsonException exception)
        {
            issues.Add(new ValidationIssue(IssueSeverity.Error, file, id, "$", file, exception.Message));
        }
        catch (FileNotFoundException exception)
        {
            issues.Add(new ValidationIssue(IssueSeverity.Error, exception.FileName ?? file, id, "$", file, exception.Message));
        }
        catch (InvalidOperationException exception)
        {
            issues.Add(new ValidationIssue(IssueSeverity.Error, file, id, "$.template", exception.Message, exception.Message));
        }

        return (null, issues, null, new Dictionary<string, string>());
    }

    private static bool HasInheritedRule(string root, string birdId, string ruleId, Func<string, string?> read)
    {
        JObject bird = JObject.Parse(read(ContentPaths.Bird(root, birdId))!);
        string? template = bird.Value<string>("template");
        while (template is not null)
        {
            JObject document = JObject.Parse(read(ContentPaths.Template(root, template))!);
            if (document["rules"] is JObject rules && rules[ruleId] is JObject)
            {
                return true;
            }

            template = document.Value<string>("template");
        }

        return false;
    }

    public static (IReadOnlyList<Region> Regions, IReadOnlyList<ValidationIssue> Issues) LoadRegions(
        string root,
        IEnumerable<string> compatModIds,
        Func<string, string?> read,
        IReadOnlyCollection<string>? birdIds = null)
    {
        List<ValidationIssue> issues = new();
        Dictionary<string, double> ratios = new(StringComparer.Ordinal);
        Dictionary<string, List<string>> species = new(StringComparer.Ordinal);
        Dictionary<string, Dictionary<string, List<string>>> groups = new(StringComparer.Ordinal);

        void Error(string file, string path, string message) =>
            issues.Add(new ValidationIssue(IssueSeverity.Error, file, "", path, file, message));

        string[] files = compatModIds.Select(modId => ContentPaths.CompatRegions(root, modId)).Prepend(ContentPaths.Regions(root)).ToArray();
        foreach (string file in files)
        {
            bool isBase = file == files[0];
            try
            {
                JObject document = JObject.Parse(read(file) ?? throw new FileNotFoundException("Regions missing.", file));
                foreach (JProperty region in document.Properties())
                {
                    string path = "$." + region.Name;
                    if (region.Value is not JObject body)
                    {
                        Error(file, path, "Region must be an object.");
                        continue;
                    }

                    if (!isBase && !species.ContainsKey(region.Name))
                    {
                        Error(file, path, $"Unknown region '{region.Name}'; compat files extend existing regions.");
                        continue;
                    }

                    if (isBase)
                    {
                        species[region.Name] = new List<string>();
                        groups[region.Name] = new Dictionary<string, List<string>>(StringComparer.Ordinal);
                        ratios[region.Name] = Region.DefaultRatio;
                    }

                    foreach (JProperty field in body.Properties())
                    {
                        string fieldPath = path + "." + field.Name;
                        switch (field.Name)
                        {
                            case "ratio" when isBase:
                                if (field.Value.Type is JTokenType.Float or JTokenType.Integer && field.Value.Value<double>() is > 0 and < 1)
                                {
                                    ratios[region.Name] = field.Value.Value<double>();
                                }
                                else
                                {
                                    Error(file, fieldPath, "Ratio must be a number between 0 and 1 (exclusive).");
                                }

                                break;
                            case "species":
                                if (field.Value is JArray names && names.All(name => name.Type == JTokenType.String && !string.IsNullOrWhiteSpace(name.Value<string>())))
                                {
                                    species[region.Name].AddRange(names.Values<string>().Select(name => name!).Except(species[region.Name], StringComparer.Ordinal));
                                }
                                else
                                {
                                    Error(file, fieldPath, "Expected an array of bird ids.");
                                }

                                break;
                            case "groups":
                                if (field.Value is not JObject groupObject)
                                {
                                    Error(file, fieldPath, "Expected an object of location groups.");
                                    break;
                                }

                                foreach (JProperty group in groupObject.Properties())
                                {
                                    if (group.Value is not JArray locations || locations.Count == 0 || locations.Any(location => location.Type != JTokenType.String || string.IsNullOrWhiteSpace(location.Value<string>()) || location.Value<string>()!.StartsWith('@')))
                                    {
                                        Error(file, fieldPath + "." + group.Name, "Expected a nonempty array of location names (no nested groups).");
                                        continue;
                                    }

                                    if (!groups[region.Name].TryGetValue(group.Name, out List<string>? members))
                                    {
                                        members = new List<string>();
                                        groups[region.Name].Add(group.Name, members);
                                    }

                                    members.AddRange(locations.Values<string>().Select(location => location!).Except(members, StringComparer.Ordinal));
                                }

                                break;
                            default:
                                Error(file, fieldPath, $"Unknown region field '{field.Name}'.");
                                break;
                        }
                    }
                }
            }
            catch (Exception exception) when (exception is JsonException or FileNotFoundException)
            {
                Error(file, "$", exception.Message);
            }
        }

        string regionsFile = files[0];
        foreach (IGrouping<string, string> duplicate in species.SelectMany(region => region.Value.Select(bird => (Region: region.Key, Bird: bird))).GroupBy(entry => entry.Bird, entry => entry.Region).Where(group => group.Count() > 1))
        {
            Error(regionsFile, "$", $"Species '{duplicate.Key}' is listed in several regions: {string.Join(", ", duplicate)}.");
        }

        foreach (IGrouping<string, string> duplicate in groups.SelectMany(region => region.Value.Keys.Select(group => (Region: region.Key, Group: group))).GroupBy(entry => entry.Group, entry => entry.Region).Where(group => group.Count() > 1))
        {
            Error(regionsFile, "$", $"Location group '{duplicate.Key}' is defined in several regions: {string.Join(", ", duplicate)}.");
        }

        if (birdIds is not null)
        {
            foreach (string unknown in species.Values.SelectMany(list => list).Where(bird => !birdIds.Contains(bird)))
            {
                Error(regionsFile, "$", $"Region lists unknown bird '{unknown}'.");
            }

            foreach (string missing in birdIds.Except(species.Values.SelectMany(list => list)))
            {
                issues.Add(new ValidationIssue(IssueSeverity.Warn, regionsFile, missing, "$", regionsFile, "Bird is not listed in any region and will never spawn."));
            }
        }

        List<Region> regions = species.Keys
            .Select(id => new Region(id, ratios[id], species[id], groups[id].ToDictionary(group => group.Key, group => (IReadOnlyList<string>)group.Value, StringComparer.Ordinal)))
            .ToList();
        return (regions, issues);
    }
}
