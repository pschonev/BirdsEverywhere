using BirdsEverywhere.BirdTypes;
using BirdsEverywhere.Content;

namespace BirdsEverwhyere.UnitTests;

internal static class TestBirds
{
    private static readonly string _assets = Path.Combine(AppContext.BaseDirectory, "assets");

    public static readonly SharedDefinitions Definitions = SharedDefinitions.Load(_assets, out _);

    public static readonly BirdServices Services = new(null!, null!, () => 1, (_, _) => { },
        LayoutLoader.LoadAll(_assets).Layouts, Definitions.Routines, Definitions.ParsedActions());
}
