using Birding.Spawners;
using Microsoft.Xna.Framework;
using NUnit.Framework;

namespace Birding.UnitTests;

public class DepartureTests
{
    private const string Location = "Beach";
    private const int Seed = 7;

    private sealed class OpenGround : ILocationQuery
    {
        public string Name => Location;
        public Point MapSize => new(40, 40);
        public bool IsGroundTile(Vector2 tile) => true;
        public bool IsWaterTile(Vector2 tile) => false;
        public bool IsSpawnableGroundTile(Vector2 tile) => true;
        public string? FishArea(Vector2 tile) => null;
        public bool IsPassable(Vector2 tile) => true;
        public IReadOnlyList<Vector2> FarmerTiles { get; init; } = Array.Empty<Vector2>();
        public IEnumerable<Vector2> MatureTrees() => Array.Empty<Vector2>();
    }

    private static DailySpawnPlan PlanWith(int seed) => new(new Dictionary<string, IReadOnlyList<BirdPlacement>>
    {
        [Location] = new[] { new BirdPlacement("tit", "rule", "land", "ground", new Vector2(5, 5), null, Array.Empty<Birding.Domain.IntRange>(), "", seed) },
    });

    [Test]
    public void Table_KeepsBirdAwayUntilDelayPassed()
    {
        DepartureTable table = new();
        table.Record(new Departure(Location, Seed, DepartureTable.ToMinutes(900)));
        Assert.Multiple(() =>
        {
            Assert.That(table.IsDeparted(Location, Seed), Is.True);
            Assert.That(table.CanReturn(Location, Seed, 1058), Is.False);
            Assert.That(table.CanReturn(Location, Seed, 1100), Is.True);
            Assert.That(table.IsDeparted(Location, Seed + 1), Is.False);
            Assert.That(table.CanReturn(Location, Seed + 1, 2000), Is.False);
        });
    }

    [Test]
    public void Table_ClearAndMarkReturnedForgetBird()
    {
        DepartureTable table = new();
        table.Record(new Departure(Location, Seed, 600));
        table.MarkReturned(Location, Seed);
        Assert.That(table.IsDeparted(Location, Seed), Is.False);
        table.Record(new Departure(Location, Seed, 600));
        table.Clear();
        Assert.That(table.Snapshot(), Is.Empty);
    }

    [Test]
    public void Table_ReplaceRestoresSnapshot()
    {
        DepartureTable source = new();
        source.Record(new Departure(Location, Seed, 600));
        DepartureTable target = new();
        target.Replace(source.Snapshot());
        Assert.That(target.Snapshot(), Is.EqualTo(source.Snapshot()));
    }

    [Test]
    public void Departures_SurviveJsonRoundTrip()
    {
        List<Departure> departures = new() { new Departure(Location, Seed, 600) };
        Assert.That(MessageJson.Deserialize<List<Departure>>(MessageJson.Serialize(departures)), Is.EqualTo(departures));
    }

    [TestCase(Location, Seed, Location, true)]
    [TestCase(Location, Seed, "Town", false)]
    [TestCase(Location, Seed, null, false)]
    [TestCase(Location, 999, Location, false)]
    [TestCase("Town", Seed, "Town", false)]
    public void IsValid_RequiresSenderInLocationAndPlannedBird(string location, int seed, string? senderLocation, bool expected) =>
        Assert.That(DepartureTable.IsValid(PlanWith(Seed), new Departure(location, seed, 600), senderLocation), Is.EqualTo(expected));

    [Test]
    public void ReturnTiles_AreNearOriginNotExcludedAndFarFromFarmers()
    {
        Vector2 origin = new(20, 20);
        Vector2 farmer = new(17, 20);
        OpenGround location = new() { FarmerTiles = new[] { farmer } };
        Vector2 old = new(21, 21);
        for (int seed = 0; seed < 50; seed++)
        {
            Vector2? tile = ReturnTiles.Find(location, (_, _) => true, origin, new[] { old }, new Random(seed), radius: 12);
            Assert.That(tile, Is.Not.Null);
            Assert.That(tile, Is.Not.EqualTo(old));
            Assert.That(Vector2.Distance(tile!.Value, farmer), Is.GreaterThanOrEqualTo(ReturnTiles.FarmerDistance));
        }
    }

    [Test]
    public void ReturnTiles_ReturnNullWhenNoTileQualifies() =>
        Assert.That(ReturnTiles.Find(new OpenGround(), (_, _) => false, new Vector2(20, 20), Array.Empty<Vector2>(), new Random(1)), Is.Null);
}
