namespace Birding.BirdTypes;

/// <summary>How far the <c>song</c> sound role carries: full volume while the bird is on screen, then a linear fade to <see cref="FloorVolume"/> that never goes silent anywhere in the location.</summary>
public static class SoundCarry
{
    public const string Role = "song";
    public const float FloorVolume = 0.2f;
    public const float FadeTiles = 30f;

    public static float Volume(float tilesFromScreen) =>
        tilesFromScreen <= 0f ? 1f : MathF.Max(FloorVolume, 1f - (tilesFromScreen / FadeTiles * (1f - FloorVolume)));
}
