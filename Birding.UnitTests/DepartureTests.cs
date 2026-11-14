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
        public IReadOnlyList<Vector2> NpcTiles { get; init; } = Array.Empty<Vector2>();
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
            Assert.That(table.CanReturn(Location, Seed, 930), Is.False);
            Assert.That(table.CanReturn(Location, Seed, 940), Is.True, "40 in-game minutes, about 30 real seconds");
            Assert.That(table.IsDeparted(Location, Seed + 1), Is.False);
            Assert.That(table.CanReturn(Location, Seed + 1, 2000), Is.False);
        });
    }

    [Test]
    public void ArrivalStart_IsBeyondTheScreenOnTheFarSideOfTheFarmer()
    {
        Vector2 start = ReturnTiles.ArrivalStart(new Point(100, 60), new Vector2(50, 30), new[] { new Vector2(38, 30) }, point => point.X < 50 * 64f + 10 * 64f);

        Assert.Multiple(() =>
        {
            Assert.That(start.X, Is.GreaterThan(50 * 64f + 10 * 64f), "past the visible edge");
            Assert.That(start.Y, Is.EqualTo(30 * 64f + 32f).Within(1f), "straight away from the farmer");
        });
    }

    [Test]
    public void ArrivalStart_StopsAtTheMapEdgeWhenNothingIsOffScreen()
    {
        Vector2 start = ReturnTiles.ArrivalStart(new Point(60, 40), new Vector2(50, 20), new[] { new Vector2(30, 20) }, _ => true);

        Assert.That(start.X, Is.LessThanOrEqualTo(60 * 64f - 1f).And.GreaterThan(55 * 64f));
    }

    [Test]
    public void ArrivalStart_WithoutFarmersStillReturnsAPointInsideTheMap()
    {
        Vector2 start = ReturnTiles.ArrivalStart(new Point(60, 40), new Vector2(10, 10), Array.Empty<Vector2>(), _ => false);

        Assert.That(start.X, Is.InRange(0f, 60 * 64f));
    }

    [TestCase(true, false, false, false, false, false, PopulateAction.Spawn)]
    [TestCase(true, true, false, false, false, false, PopulateAction.Skip, TestName = "already spawned this visit")]
    [TestCase(false, false, false, false, false, false, PopulateAction.Skip, TestName = "outside its time window")]
    [TestCase(true, false, false, false, false, true, PopulateAction.Skip, TestName = "a live bird of that seed exists")]
    [TestCase(true, true, true, false, false, false, PopulateAction.Skip, TestName = "departed, delay not over")]
    [TestCase(true, true, true, true, true, false, PopulateAction.Return, TestName = "departed, delay over, new visit")]
    [TestCase(true, true, true, true, false, false, PopulateAction.Arrive, TestName = "regression: departed birds were skipped for the rest of the visit because they stayed in the spawned set")]
    [TestCase(true, false, true, true, false, false, PopulateAction.Arrive)]
    [TestCase(true, true, true, true, false, true, PopulateAction.Skip, TestName = "departed record but already flew back in")]
    public void PopulateDecision_ChoosesTheRightAction(bool active, bool inSpawned, bool departed, bool canReturn, bool visitStart, bool live, PopulateAction expected) =>
        Assert.That(PopulateDecision.For(active, inSpawned, departed, canReturn, visitStart, live), Is.EqualTo(expected));

    [Test]
    public void ThreatSight_CountsOnlyNpcsWithinSightOfAFarmer()
    {
        Vector2[] farmers = { new(10, 10) };
        Assert.Multiple(() =>
        {
            Assert.That(ThreatSight.InSight(new Vector2(20, 10), farmers), Is.True);
            Assert.That(ThreatSight.InSight(new Vector2(40, 10), farmers), Is.False, "nobody can see this villager");
            Assert.That(ThreatSight.InSight(new Vector2(10, 10), Array.Empty<Vector2>()), Is.False);
            Assert.That(ThreatSight.Near(new Vector2(12, 10), new[] { new Vector2(10, 10) }, ThreatSight.NpcSpawnClearanceTiles), Is.True);
            Assert.That(ThreatSight.Near(new Vector2(20, 10), new[] { new Vector2(10, 10) }, ThreatSight.NpcSpawnClearanceTiles), Is.False);
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
    public void ReturnTiles_KeepClearOfVillagers()
    {
        OpenGround location = new() { NpcTiles = new[] { new Vector2(20, 20) } };
        for (int seed = 0; seed < 30; seed++)
        {
            Vector2? tile = ReturnTiles.Find(location, (_, _) => true, new Vector2(20, 20), Array.Empty<Vector2>(), new Random(seed), radius: 12);
            Assert.That(tile is null || Vector2.Distance(tile.Value, new Vector2(20, 20)) > ThreatSight.NpcSpawnClearanceTiles, $"seed {seed}");
        }
    }

    [Test]
    public void ReturnTiles_ReturnNullWhenNoTileQualifies() =>
        Assert.That(ReturnTiles.Find(new OpenGround(), (_, _) => false, new Vector2(20, 20), Array.Empty<Vector2>(), new Random(1)), Is.Null);
}
