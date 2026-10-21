using BirdsEverywhere.Domain;
using StardewModdingAPI;

namespace BirdsEverywhere.BirdList;

public static class BirdText
{
    public static string Name(ITranslationHelper translation, BirdDefinition bird)
    {
        Translation name = translation.Get($"bird.{bird.Id.Value}.name");
        return name.HasValue() ? name.ToString() : bird.Name;
    }

    public static string Description(ITranslationHelper translation, BirdDefinition bird) =>
        translation.Get($"bird.{bird.Id.Value}.description").ToString();
}
