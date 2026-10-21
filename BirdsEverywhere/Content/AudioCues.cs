namespace BirdsEverywhere.Content;

/// <summary>Mod audio: <c>assets/audio/&lt;name&gt;.ogg|.wav</c> is referenced in sound maps as <c>audio:&lt;name&gt;</c> and registered as cue <c>&lt;ModId&gt;_&lt;name&gt;</c>.</summary>
public static class AudioCues
{
    public const string Prefix = "audio:";

    private static readonly string[] _extensions = { ".ogg", ".wav" };

    public static IEnumerable<string> Candidates(string root, string name) =>
        _extensions.Select(extension => Path.Combine(ContentPaths.AudioDirectory(root), name + extension));

    public static IReadOnlyDictionary<string, string> Files(string root) =>
        Directory.Exists(ContentPaths.AudioDirectory(root))
            ? Directory.GetFiles(ContentPaths.AudioDirectory(root))
                .Where(path => _extensions.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase))
                .GroupBy(path => Path.GetFileNameWithoutExtension(path), StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal)
            : new Dictionary<string, string>();
}
