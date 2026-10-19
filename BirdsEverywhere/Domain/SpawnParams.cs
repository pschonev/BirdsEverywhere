namespace BirdsEverywhere.Domain;

public sealed record SpawnParams(IntRange? ShoreDistance)
{
    public const int MaxShoreDistance = 20;

    public static SpawnParams None { get; } = new((IntRange?)null);
}
