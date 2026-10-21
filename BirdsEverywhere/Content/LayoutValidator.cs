using BirdsEverywhere.Domain;

namespace BirdsEverywhere.Content;

public static class LayoutValidator
{
    public static IReadOnlyList<ValidationIssue> ValidateBird(
        BirdDefinition bird,
        string root,
        IReadOnlyDictionary<string, SpriteLayout> layouts,
        Func<string, (int Width, int Height)?> textureSize)
    {
        List<ValidationIssue> issues = new();
        string file = ContentPaths.Bird(root, bird.Id.Value);
        foreach (SpawnRule rule in bird.Rules)
        {
            string path = $"$.rules.{rule.Id}.birdType";
            if (!BirdTypeProfiles.ByBirdType.TryGetValue(rule.BirdType.Value, out BirdTypeProfile? requirement))
            {
                issues.Add(Error(file, bird, path, $"Bird type '{rule.BirdType.Value}' has no layout."));
                continue;
            }
            if (!layouts.TryGetValue(requirement.LayoutId, out SpriteLayout? layout))
            {
                issues.Add(Error(file, bird, path, $"Layout '{requirement.LayoutId}' is missing or invalid."));
                continue;
            }

            issues.AddRange(Requirement(layout, requirement, rule.Composition).Select(message => Error(file, bird, path, message)));
            foreach (string variant in rule.Textures.Keys)
            {
                string texture = ContentPaths.Texture(root, bird.Id.Value, variant);
                if (textureSize(texture) is not { } size)
                {
                    continue;
                }
                int cells = size.Width / layout.FrameWidth * (size.Height / layout.FrameHeight);
                int needed = LayoutSheet.CellCount(layout);
                if (needed > cells)
                {
                    issues.Add(Error(texture, bird, $"$.rules.{rule.Id}.textures.{variant}",
                        $"Layout '{layout.Id}' uses {needed} cells but {Path.GetFileName(texture)} has {cells} ({size.Width}x{size.Height} at {layout.FrameWidth}x{layout.FrameHeight})."));
                }
            }
        }
        return issues.Distinct().ToList();
    }

    public static IEnumerable<string> Requirement(SpriteLayout layout, BirdTypeProfile requirement, BirdComposition? composition)
    {
        IEnumerable<string> clips = requirement.Poses
            .Concat(composition?.Actions?.Values.Select(action => action.Clip).OfType<string>() ?? Enumerable.Empty<string>())
            .Concat(composition?.Reactions?.Threat.Alert is { } alert ? new[] { alert.Clip } : Enumerable.Empty<string>())
            .Distinct();
        foreach (string clip in clips.Where(clip => !layout.Clips.ContainsKey(clip)))
        {
            yield return $"Layout '{layout.Id}' has no clip '{clip}'.";
        }
        foreach ((string name, SpriteClip clip) in layout.Clips)
        {
            IEnumerable<ClipFrame> frames = clip.Frames.Concat(clip.Repeat?.Frames ?? Array.Empty<ClipFrame>()).Concat(clip.End);
            foreach (string unknown in frames.Select(frame => frame.Event).OfType<string>().Distinct().Where(name => !requirement.Events.Contains(name)))
            {
                yield return $"Layout '{layout.Id}' clip '{name}' uses unknown event '{unknown}'.";
            }
        }
    }

    /// <summary>Reads width and height from a PNG header; null if not a PNG.</summary>
    public static (int Width, int Height)? PngSize(ReadOnlySpan<byte> header)
    {
        ReadOnlySpan<byte> signature = stackalloc byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
        if (header.Length < 24 || !header[..8].SequenceEqual(signature))
        {
            return null;
        }
        return (System.Buffers.Binary.BinaryPrimitives.ReadInt32BigEndian(header[16..20]), System.Buffers.Binary.BinaryPrimitives.ReadInt32BigEndian(header[20..24]));
    }

    private static ValidationIssue Error(string file, BirdDefinition bird, string path, string message) =>
        new(IssueSeverity.Error, file, bird.Id.Value, path, file, message);
}
