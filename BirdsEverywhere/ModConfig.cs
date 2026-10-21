namespace BirdsEverywhere;

public sealed class ModConfig
{
    public bool Debug { get; set; }

    /// <summary>Scales every bird sound, 0 (mute) to 1.</summary>
    public float SoundVolume { get; set; } = 1f;
}