using System.Diagnostics.CodeAnalysis;
using BirdsEverywhere.BirdTypes;
using BirdsEverywhere.Domain;
using BirdsEverywhere.Spawners;
using Microsoft.Xna.Framework;
using StardewValley;

namespace BirdsEverywhere.Registries;

internal sealed class BirdTypeRegistry
{
    private readonly IReadOnlyDictionary<BirdTypeId, Func<BirdPlacement, GameLocation, BirdServices, BirdCritter?>> _factories;

    public BirdTypeRegistry(IReadOnlyDictionary<BirdTypeId, Func<BirdPlacement, GameLocation, BirdServices, BirdCritter?>> factories) => _factories = factories;

    public IReadOnlyCollection<BirdTypeId> Ids => _factories.Keys.ToArray();

    public static BirdTypeRegistry CreateDefault() => new(new Dictionary<BirdTypeId, Func<BirdPlacement, GameLocation, BirdServices, BirdCritter?>>
    {
        [new BirdTypeId("LandBird")] = (p, _, s) => new LandBird((int)p.Tile.X, (int)p.Tile.Y, p.BirdId, p.Texture, s),
        [new BirdTypeId("WaterLandBird")] = (p, _, s) => new WaterLandBird((int)p.Tile.X, (int)p.Tile.Y, p.BirdId, p.Texture, s),
        [new BirdTypeId("TreeTrunkBird")] = (p, l, s) => p.FeatureTile is Vector2 anchor
            ? new TreeTrunkBird((int)p.Tile.X, (int)p.Tile.Y, p.BirdId, p.Texture, s).SetAnchor(anchor, l.Name)
            : null,
        [new BirdTypeId("BushBird")] = (p, l, s) => p.FeatureTile is Vector2 anchor
            ? new BushBird((int)p.Tile.X, (int)p.Tile.Y, p.BirdId, p.Texture, s).SetAnchor(anchor, l.Name)
            : null,
    });

    public bool TryCreate(BirdTypeId id, BirdPlacement placement, GameLocation location, BirdServices services, [NotNullWhen(true)] out BirdCritter? bird)
    {
        bird = _factories.TryGetValue(id, out var create) ? create(placement, location, services) : null;
        return bird is not null;
    }
}
