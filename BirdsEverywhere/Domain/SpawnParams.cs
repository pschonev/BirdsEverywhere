namespace BirdsEverywhere.Domain;

/// <summary><c>Group</c>: how a group's members are laid out (null: legacy random walk); <c>SpeciesDistance</c>: minimum tiles to other species' placements (null: 8).</summary>
public sealed record SpawnParams(IntRange? ShoreDistance, GroupLayout? Group = null, int? SpeciesDistance = null)
{
    public const int MaxShoreDistance = 20;

    public static SpawnParams None { get; } = new((IntRange?)null);
}

public enum Formation
{
    Cluster,
    Scattered,
    Line,
}

/// <summary>Members within <c>Radius</c> tiles of the group's first tile, at least <c>Spacing</c> apart; groups at least <c>GroupSpacing</c> apart.</summary>
public sealed record GroupLayout(Formation Formation, int Radius, int Spacing, int GroupSpacing,
    IReadOnlyDictionary<string, GroupMember>? Members = null);

public sealed record GroupMember(IntRange Count, string Stage);
