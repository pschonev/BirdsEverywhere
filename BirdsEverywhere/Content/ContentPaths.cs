namespace BirdsEverywhere.Content;

public static class ContentPaths
{
    private static readonly HashSet<string> _nonBirdDirectories = new(StringComparer.Ordinal) { "templates", "compat", "layouts", "routines" };

    public static IEnumerable<string> BirdIds(string root) =>
        Directory.GetDirectories(root).Select(path => Path.GetFileName(path)).Where(name => !_nonBirdDirectories.Contains(name));

    public static string LayoutsDirectory(string root) => Path.Combine(root, "layouts");

    public static string Layout(string root, string id) => Path.Combine(LayoutsDirectory(root), $"{id}.json");

    public static string RoutinesDirectory(string root) => Path.Combine(root, "routines");

    public static string Regions(string root) => Path.Combine(root, "regions.json");

    public static string CompatDirectory(string root) => Path.Combine(root, "compat");

    public static string CompatRegions(string root, string modId) => Path.Combine(CompatDirectory(root), $"{modId}.json");

    public static string Bird(string root, string id) => Path.Combine(root, id, $"{id}.json");

    public static string Template(string root, string id) => Path.Combine(root, "templates", $"{id}.json");

    public static string Texture(string root, string id, string variant) =>
        Path.Combine(root, id, variant.Length == 0 ? $"{id}.png" : $"{id}_{variant}.png");
}