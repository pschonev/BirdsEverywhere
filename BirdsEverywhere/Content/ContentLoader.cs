using BirdsEverywhere.Domain;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BirdsEverywhere.Content;

public static class ContentLoader
{
    private static readonly HashSet<string> TemplateFields = new(StringComparer.Ordinal) { "template", "defaults", "rules" };
    private static readonly HashSet<string> BirdFields = new(StringComparer.Ordinal) { "id", "name", "scName", "description", "template", "defaults", "rules" };

    public static (BirdDefinition? Bird, IReadOnlyList<ValidationIssue> Issues, JObject? Effective, IReadOnlyDictionary<string, string> Sources) LoadBird(
        string root,
        string id,
        IReadOnlyDictionary<string, IReadOnlyList<string>> groups,
        IReadOnlyCollection<SpawnPatternId> patterns,
        IReadOnlyCollection<BirdTypeId> birdTypes,
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
                foreach (string field in new[] { "id", "name", "scName", "description" })
                {
                    if (template.ContainsKey(field))
                    {
                        issues.Add(new ValidationIssue(IssueSeverity.Error, templateFile, id, "$." + field, chain, "Identity field is not allowed in a template."));
                    }
                }

                foreach (string key in template.Properties().Select(property => property.Name).Except(TemplateFields))
                {
                    issues.Add(new ValidationIssue(IssueSeverity.Error, templateFile, id, "$." + key, chain, "Unknown template field."));
                }
            }

            JObject birdDocument = Read(file);
            foreach (string key in birdDocument.Properties().Select(property => property.Name).Except(BirdFields))
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

            (BirdDefinition? bird, IReadOnlyList<ValidationIssue> validation) = ContentValidator.Validate(document, file, chain, sources, groups, patterns, birdTypes, textureExists);
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

    public static (IReadOnlyDictionary<string, IReadOnlyList<string>> Groups, IReadOnlyList<ValidationIssue> Issues) LoadGroups(string root, Func<string, string?> read)
    {
        string file = Path.Combine(root, "locationGroups.json");
        List<ValidationIssue> issues = new();
        Dictionary<string, IReadOnlyList<string>> groups = new(StringComparer.Ordinal);
        try
        {
            JObject document = JObject.Parse(read(file) ?? throw new FileNotFoundException("Location groups missing.", file));
            foreach (JProperty property in document.Properties())
            {
                if (property.Value is not JArray locations || locations.Count == 0 || locations.Any(location => location.Type != JTokenType.String || string.IsNullOrWhiteSpace(location.Value<string>()) || location.Value<string>()!.StartsWith('@')))
                {
                    issues.Add(new ValidationIssue(IssueSeverity.Error, file, property.Name, "$." + property.Name, file, "Expected a nonempty array of location names (no nested groups)."));
                    continue;
                }

                groups[property.Name] = locations.Values<string>().Select(location => location!).ToArray();
            }
        }
        catch (Exception exception) when (exception is JsonException or FileNotFoundException)
        {
            issues.Add(new ValidationIssue(IssueSeverity.Error, file, "", "$", file, exception.Message));
        }

        return (groups, issues);
    }
}
