using Newtonsoft.Json;

namespace BirdsEverywhere.Content;

public static class DefinitionFiles
{
    /// <summary>Loads every <c>&lt;directory&gt;/*.json</c> keyed by file name; a broken file is reported and skipped.</summary>
    public static (IReadOnlyDictionary<string, T> Items, IReadOnlyList<ValidationIssue> Issues) LoadAll<T>(string directory, Func<string, string, string, T> parse)
    {
        Dictionary<string, T> items = new(StringComparer.Ordinal);
        List<ValidationIssue> issues = new();
        if (!Directory.Exists(directory))
        {
            return (items, issues);
        }

        foreach (string file in Directory.GetFiles(directory, "*.json").OrderBy(path => path, StringComparer.Ordinal))
        {
            string id = Path.GetFileNameWithoutExtension(file);
            try
            {
                items[id] = parse(id, File.ReadAllText(file), file);
            }
            catch (Exception exception) when (exception is FormatException or JsonException)
            {
                issues.Add(new ValidationIssue(IssueSeverity.Error, file, id, "$", file, exception.Message));
            }
        }
        return (items, issues);
    }
}
