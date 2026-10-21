namespace BirdsEverywhere.Domain;

/// <summary>Sprite-sheet cell size and the clips played from it; cells count left to right, top to bottom.</summary>
public sealed record SpriteLayout(string Id, int FrameWidth, int FrameHeight, IReadOnlyDictionary<string, SpriteClip> Clips);
