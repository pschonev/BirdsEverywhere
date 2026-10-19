namespace BirdsEverywhere.Content;

public static class ContentPaths
{
    private static readonly HashSet<string> NonBirdDirectories = new(StringComparer.Ordinal) { "templates", "compat" };

    public static IEnumerable<string> BirdIds(string root) =>
        Directory.GetDirectories(root).Select(path => Path.GetFileName(path)).Where(name => !NonBirdDirectories.Contains(name));

    public static string Groups(string root) => Path.Combine(root, "locationGroups.json");

    public static string CompatDirectory(string root) => Path.Combine(root, "compat");

    public static string CompatGroups(string root, string modId) => Path.Combine(CompatDirectory(root), $"{modId}.json");

    public static string Bird(string root, string id) => Path.Combine(root, id, $"{id}.json");

    public static string Template(string root, string id) => Path.Combine(root, "templates", $"{id}.json");

    public static string Texture(string root, string id, string variant) =>
        Path.Combine(root, id, variant.Length == 0 ? $"{id}.png" : $"{id}_{variant}.png");
}