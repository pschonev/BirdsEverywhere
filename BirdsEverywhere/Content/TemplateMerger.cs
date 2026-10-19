using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BirdsEverywhere.Content;

public static class TemplateMerger
{
    public static (JObject Document, IReadOnlyDictionary<string, string> Sources, string Chain) Resolve(
        string birdFile, Func<string, string> templatePath, Func<string, JObject> read)
    {
        List<(JObject Document, string File)> layers = new();
        List<string> chain = new();
        string file = birdFile;
        while (true)
        {
            if (chain.Contains(file))
            {
                throw new InvalidOperationException($"Template cycle: {string.Join(" -> ", chain.Append(file))}");
            }

            chain.Add(file);
            JObject document = read(file);
            layers.Add((document, file));
            string? parent = document.Value<string>("template");
            if (parent is null)
            {
                break;
            }

            file = templatePath(parent);
        }

        layers.Reverse();
        JObject merged = new();
        Dictionary<string, string> sources = new();
        foreach ((JObject document, string source) in layers)
        {
            MergeSources(merged, document, source, sources, "$");
            merged = Merge(merged, document);
        }

        if (merged["defaults"] is not null and not JObject)
        {
            throw new JsonSerializationException("Expected an object at $.defaults.");
        }

        if (merged["rules"] is not null and not JObject)
        {
            throw new JsonSerializationException("Expected an object at $.rules.");
        }

        JObject defaults = (JObject?)merged["defaults"] ?? new JObject();
        JObject rules = (JObject?)merged["rules"] ?? new JObject();
        foreach (JProperty property in rules.Properties().ToArray())
        {
            if (property.Value is not JObject)
            {
                continue;
            }

            string rulePath = $"$.rules.{property.Name}";
            Dictionary<string, string> effectiveSources = new();
            foreach (KeyValuePair<string, string> entry in sources.Where(entry => entry.Key.StartsWith("$.defaults.", StringComparison.Ordinal)))
            {
                effectiveSources[$"{rulePath}.{entry.Key.Substring("$.defaults.".Length)}"] = entry.Value;
            }

            Dictionary<string, string> defaultSources = new(effectiveSources);
            JObject effective = (JObject)defaults.DeepClone();
            foreach ((JObject document, string source) in layers)
            {
                if (document["rules"] is not JObject layerRules)
                {
                    if (document.ContainsKey("rules"))
                    {
                        effective = (JObject)defaults.DeepClone();
                        effectiveSources = new(defaultSources);
                    }

                    continue;
                }

                if (!layerRules.TryGetValue(property.Name, out JToken? layerValue))
                {
                    continue;
                }

                if (layerValue is not JObject layerRule)
                {
                    effective = (JObject)defaults.DeepClone();
                    effectiveSources = new(defaultSources);
                    continue;
                }

                MergeSources(effective, layerRule, source, effectiveSources, rulePath);
                effective = Merge(effective, layerRule);
            }

            foreach (string key in sources.Keys.Where(key => key.StartsWith(rulePath + ".", StringComparison.Ordinal)).ToArray())
            {
                sources.Remove(key);
            }

            foreach (KeyValuePair<string, string> entry in effectiveSources)
            {
                sources[entry.Key] = entry.Value;
            }

            property.Value = effective;
        }

        merged.Remove("defaults");
        return (merged, sources, string.Join(" -> ", chain.AsEnumerable().Reverse()));
    }

    private static void MergeSources(JObject inherited, JObject replacement, string file, Dictionary<string, string> sources, string path)
    {
        foreach (JProperty property in replacement.Properties())
        {
            string childPath = $"{path}.{property.Name}";
            if (property.Value.Type == JTokenType.Null)
            {
                foreach (string key in sources.Keys.Where(key => key == childPath || key.StartsWith(childPath + ".", StringComparison.Ordinal)).ToArray())
                {
                    sources.Remove(key);
                }
            }
            else if (property.Value is JObject child)
            {
                if (inherited[property.Name] is not JObject)
                {
                    foreach (string key in sources.Keys.Where(key => key.StartsWith(childPath + ".", StringComparison.Ordinal)).ToArray())
                    {
                        sources.Remove(key);
                    }
                }

                MergeSources(inherited[property.Name] as JObject ?? new JObject(), child, file, sources, childPath);
            }
            else
            {
                foreach (string key in sources.Keys.Where(key => key.StartsWith(childPath + ".", StringComparison.Ordinal)).ToArray())
                {
                    sources.Remove(key);
                }

                sources[childPath] = file;
            }
        }
    }

    public static JObject Merge(JObject inherited, JObject overrideValues)
    {
        JObject result = (JObject)inherited.DeepClone();
        foreach (KeyValuePair<string, JToken?> entry in overrideValues)
        {
            if (entry.Value is null || entry.Value.Type == JTokenType.Null)
            {
                result.Remove(entry.Key);
            }
            else if (result[entry.Key] is JObject existing && entry.Value is JObject replacement)
            {
                result[entry.Key] = Merge(existing, replacement);
            }
            else
            {
                result[entry.Key] = entry.Value.DeepClone();
            }
        }

        return result;
    }
}
