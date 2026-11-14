using Microsoft.Xna.Framework;

namespace Birding.Spawners;

/// <summary>When a villager or pet counts as a threat to birds: only while it is within sight of a farmer, so characters nobody can see never scare birds away.</summary>
public static class ThreatSight
{
    /// <summary>About the half-width of the screen plus a margin, in tiles.</summary>
    public const float NpcSightTiles = 14f;

    /// <summary>Tiles around an NPC that new or returning birds keep clear of.</summary>
    public const float NpcSpawnClearanceTiles = 5f;

    /// <summary>Tiles around an NPC a walking bird will not step into.</summary>
    public const float NpcWalkClearanceTiles = 2f;

    public static bool InSight(Vector2 npc, IEnumerable<Vector2> farmers) =>
        farmers.Any(farmer => Vector2.Distance(npc, farmer) <= NpcSightTiles);

    public static bool Near(Vector2 tile, IEnumerable<Vector2> npcs, float clearance) =>
        npcs.Any(npc => Vector2.Distance(npc, tile) <= clearance);
}
