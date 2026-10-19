using System.Diagnostics.CodeAnalysis;
using BirdsEverywhere.Domain;
using BirdsEverywhere.Spawners;

namespace BirdsEverywhere.Registries;

internal sealed class SpawnPatternRegistry
{
    private readonly IReadOnlyDictionary<SpawnPatternId, Func<Spawner>> _factories;

    public SpawnPatternRegistry(IReadOnlyDictionary<SpawnPatternId, Func<Spawner>> factories) => _factories = factories;

    public IReadOnlyCollection<SpawnPatternId> Ids => _factories.Keys.ToArray();

    public static SpawnPatternRegistry CreateDefault() => new(new Dictionary<SpawnPatternId, Func<Spawner>>
    {
        [new SpawnPatternId("GroundSpawner")] = () => new GroundSpawner(),
        [new SpawnPatternId("WaterSpawner")] = () => new WaterSpawner(),
        [new SpawnPatternId("WaterOrGroundSpawner")] = () => new WaterOrGroundSpawner(),
        [new SpawnPatternId("SpawnableGroundSpawner")] = () => new SpawnableGroundSpawner(),
        [new SpawnPatternId("TreeTrunkSpawner")] = () => new TreeTrunkSpawner(),
        [new SpawnPatternId("BushSpawner")] = () => new BushSpawner(),
    });

    public bool TryCreate(SpawnPatternId id, [NotNullWhen(true)] out Spawner? spawner)
    {
        spawner = _factories.TryGetValue(id, out Func<Spawner>? create) ? create() : null;
        return spawner is not null;
    }
}
