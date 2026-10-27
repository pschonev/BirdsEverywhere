using System.Collections.Immutable;

namespace BirdsEverywhere.Domain;

public sealed record SpawnRule(
    string Id,
    SpawnConditions When,
    ImmutableArray<string> Locations,
    SpawnPatternId SpawnPattern,
    BirdTypeId BirdType,
    IntRange GroupCount,
    IntRange GroupSize,
    IReadOnlyDictionary<string, int> Textures,
    double Weight,
    SpawnParams Params,
    BirdComposition? Composition = null,
    IReadOnlyDictionary<string, BirdComposition>? Stages = null)
{
    /// <summary>Weighted pick over <paramref name="textures"/> (default: the rule's); one <c>Next</c> call either way.</summary>
    public string PickTexture(BirdId bird, Random random, IReadOnlyDictionary<string, int>? textures = null)
    {
        textures ??= Textures;
        int choice = random.Next(textures.Values.Sum());
        foreach (KeyValuePair<string, int> texture in textures)
        {
            choice -= texture.Value;
            if (choice < 0)
            {
                return texture.Key.Length == 0 ? bird.Value : $"{bird.Value}_{texture.Key}";
            }
        }

        throw new InvalidOperationException("A validated rule must have a positive texture weight.");
    }
}
