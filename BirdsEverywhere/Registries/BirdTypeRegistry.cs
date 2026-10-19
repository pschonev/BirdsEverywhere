using System.Diagnostics.CodeAnalysis;
using BirdsEverywhere.BirdTypes;
using BirdsEverywhere.Domain;
using BirdsEverywhere.Spawners;
using StardewValley;

namespace BirdsEverywhere.Registries;

internal sealed class BirdTypeRegistry
{
    private readonly IReadOnlyDictionary<BirdTypeId, Func<SingleBirdSpawnParameters, GameLocation, BirdServices, BirdCritter?>> _factories;

    public BirdTypeRegistry(IReadOnlyDictionary<BirdTypeId, Func<SingleBirdSpawnParameters, GameLocation, BirdServices, BirdCritter?>> factories) => _factories = factories;

    public IReadOnlyCollection<BirdTypeId> Ids => _factories.Keys.ToArray();

    public static BirdTypeRegistry CreateDefault() => new(new Dictionary<BirdTypeId, Func<SingleBirdSpawnParameters, GameLocation, BirdServices, BirdCritter?>>
    {
        [new BirdTypeId("LandBird")] = (p, _, s) => new LandBird((int)p.position.X, (int)p.position.Y, p.ID, p.texture, s),
        [new BirdTypeId("WaterLandBird")] = (p, _, s) => new WaterLandBird((int)p.position.X, (int)p.position.Y, p.ID, p.texture, s),
        [new BirdTypeId("TreeTrunkBird")] = (p, l, s) => p is SingleBirdSpawnParamsTerrainFeature f
            ? new TreeTrunkBird((int)p.position.X, (int)p.position.Y, p.ID, p.texture, s).SetAnchor(f.terrainFeatureIndex, l.Name)
            : null,
        [new BirdTypeId("BushBird")] = (p, l, s) => p is SingleBirdSpawnParamsTerrainFeature f
            ? new BushBird((int)p.position.X, (int)p.position.Y, p.ID, p.texture, s).SetAnchor(f.terrainFeatureIndex, l.Name)
            : null,
    });

    public bool TryCreate(BirdTypeId id, SingleBirdSpawnParameters placement, GameLocation location, BirdServices services, [NotNullWhen(true)] out BirdCritter? bird)
    {
        bird = _factories.TryGetValue(id, out var create) ? create(placement, location, services) : null;
        return bird is not null;
    }
}
