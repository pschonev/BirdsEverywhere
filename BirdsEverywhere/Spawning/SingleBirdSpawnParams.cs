using Microsoft.Xna.Framework;
using StardewValley;
using BirdsEverywhere.Domain;

namespace BirdsEverywhere.Spawners;

public class SingleBirdSpawnParamsTile : SingleBirdSpawnParameters
{
    public SingleBirdSpawnParamsTile(Vector2 position, string id, string birdType, string spawnPattern, List<IntRange> possibleTimesOfDay, string texture)
        : base(position, id, birdType, spawnPattern, possibleTimesOfDay, texture)
    {
        this.className = "SingleBirdSpawnParamsTile";
    }

    public override bool stillValidSpawnPosition(GameLocation location, Spawner spawner) =>
        spawner is not TileSpawner tile || tile.condition(new GameLocationQuery(location), position);
}

public class SingleBirdSpawnParamsTerrainFeature : SingleBirdSpawnParameters
{
    public int terrainFeatureIndex { get; set; }

    public SingleBirdSpawnParamsTerrainFeature(int terrainFeatureIndex, Vector2 position, string id, string birdType, string spawnPattern, List<IntRange> possibleTimesOfDay, string texture)
        : base(position, id, birdType, spawnPattern, possibleTimesOfDay, texture)
    {
        this.terrainFeatureIndex = terrainFeatureIndex;
        this.className = "SingleBirdSpawnParamsTerrainFeature";
    }

    public override bool stillValidSpawnPosition(GameLocation location, Spawner spawner) =>
        spawner is not TerrainFeatureSpawner feature || feature.condition(location, terrainFeatureIndex, position);
}

public abstract class SingleBirdSpawnParameters
{
    public string className { get; set; } // the kind of SingleBirdSpawmParameter
    public Vector2 position { get; set; }
    public string ID { get; set; } // id of the bird
    public string BirdType { get; set; } // type of bird (behavior)
    public string spawnPattern { get; set; }
    public List<IntRange> possibleTimesOfDay { get; set; }
    public string texture { get; set; }
    public SingleBirdSpawnParameters(Vector2 position, string id, string birdType, string spawnPattern, List<IntRange> possibleTimesOfDay, string texture)
    {
        this.position = position;
        this.ID = id;
        this.BirdType = birdType;
        this.spawnPattern = spawnPattern;
        this.possibleTimesOfDay = possibleTimesOfDay;
        this.texture = texture;
    }

    public abstract bool stillValidSpawnPosition(GameLocation location, Spawner spawner);
    public bool stillValidTime(int time) => possibleTimesOfDay.Count == 0 || possibleTimesOfDay.Any(range => range.Contains(time));
}
