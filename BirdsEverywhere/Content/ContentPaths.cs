namespace BirdsEverywhere.Content;

public static class ContentPaths
{
    public static string Bird(string root, string id) => Path.Combine(root, id, $"{id}.json");

    public static string Template(string root, string id) => Path.Combine(root, "templates", $"{id}.json");

    public static string Texture(string root, string id, string variant) =>
        Path.Combine(root, id, variant.Length == 0 ? $"{id}.png" : $"{id}_{variant}.png");
}