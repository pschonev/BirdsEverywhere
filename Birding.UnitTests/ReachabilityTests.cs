using Birding.Content;
using Birding.Domain;
using Birding.Spawners;
using Microsoft.Xna.Framework;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace Birding.UnitTests;

public class ReachabilityTests
{
    // Row 0..9, 30 wide: land x<10 (west), water x 10..29 row 4 only is a bridge gap; east land x>=20.
    private static readonly Point _size = new(40, 10);

    private static bool Land(Vector2 tile, bool bridge) =>
        tile.X < 10 || tile.X >= 30 || (bridge && tile.Y == 4 && tile.X >= 10 && tile.X < 30);

    private static ReachableTiles Compute(bool bridge) =>
        Reachability.Compute(_size, tile => Land(tile, bridge), tile => !Land(tile, bridge), new[] { new Vector2(2, 4) });

    [Test]
    public void Compute_ExcludesIslandLandAndKeepsNearbyWater()
    {
        ReachableTiles reach = Compute(bridge: false);
        Assert.Multiple(() =>
        {
            Assert.That(reach.Contains(new Vector2(5, 5)), Is.True);
            Assert.That(reach.Contains(new Vector2(12, 5)), Is.True, "water within reach of reachable land");
            Assert.That(reach.Contains(new Vector2(35, 5)), Is.False, "island land");
            Assert.That(reach.Contains(new Vector2(-1, 5)), Is.False);
            Assert.That(reach.Contains(new Vector2(5, 20)), Is.False);
        });
    }

    private sealed class Reach : ILocationQuery
    {
        public ReachableTiles Tiles { get; init; } = null!;
        public string Name => "Beach";
        public Point MapSize => _size;
        public bool IsGroundTile(Vector2 tile) => true;
        public bool IsWaterTile(Vector2 tile) => false;
        public bool IsSpawnableGroundTile(Vector2 tile) => true;
        public string? FishArea(Vector2 tile) => null;
        public bool IsPassable(Vector2 tile) => true;
        public IReadOnlyList<Vector2> FarmerTiles => Array.Empty<Vector2>();
        public IEnumerable<Vector2> MatureTrees() => Array.Empty<Vector2>();
        public bool IsReachable(Vector2 tile) => Tiles.Contains(tile);
    }

    [Test]
    public void IsReachableNear_AcceptsFeaturesNextToReachableTilesAndRejectsIslandOnes()
    {
        Reach query = new() { Tiles = Compute(bridge: false) };
        Assert.Multiple(() =>
        {
            Assert.That(TileRules.IsReachableNear(query, new Vector2(10, 5)), Is.True, "tree on an unreachable tile beside reachable land");
            Assert.That(TileRules.IsReachableNear(query, new Vector2(35, 5)), Is.False, "island tree");
        });
    }

    [Test]
    public void Compute_BridgeConnectsIsland()
    {
        Assert.That(Compute(bridge: true).Contains(new Vector2(35, 5)), Is.True);
    }

    [Test]
    public void Compute_NoSeedOnLandReachesNothing()
    {
        ReachableTiles reach = Reachability.Compute(_size, tile => Land(tile, false), tile => !Land(tile, false), new[] { new Vector2(15, 5) });
        Assert.That(reach.Count, Is.EqualTo(0));
    }

    [Test]
    public void GatedAreas_DefinesOnlyBeachEast()
    {
        Assert.Multiple(() =>
        {
            Assert.That(GatedAreas.Defines("Beach", "East"), Is.True);
            Assert.That(GatedAreas.Defines("Beach", "ocean"), Is.False);
            Assert.That(GatedAreas.Defines("Forest", "East"), Is.False);
            Assert.That(GatedAreas.IsBridge("Beach", new Vector2(58, 13)), Is.True);
            Assert.That(GatedAreas.IsBridge("Beach", new Vector2(62, 13)), Is.False);
        });
    }

    [Test]
    public void BridgeFixedCondition_ParsesMatchesAndRoundTrips()
    {
        List<string> errors = new();
        SpawnConditions conditions = ConditionParser.Parse(JToken.Parse(@"{""bridgeFixed"":true}"), "$.when", (path, message) => errors.Add(path + message));
        Assert.Multiple(() =>
        {
            Assert.That(errors, Is.Empty);
            Assert.That(conditions.MatchesDay(Season.Spring, Weather.Sun, 1, bridgeFixed: false), Is.False);
            Assert.That(conditions.MatchesDay(Season.Spring, Weather.Sun, 1, bridgeFixed: true), Is.True);
            Assert.That(ConditionParser.ToJson(conditions)["bridgeFixed"]!.Value<bool>(), Is.True);
            Assert.That(ConditionParser.Describe(conditions), Is.EqualTo("bridgeFixed true"));
        });
    }

    [Test]
    public void BridgeFixedCondition_RejectsNonBoolean()
    {
        List<string> errors = new();
        ConditionParser.Parse(JToken.Parse(@"{""bridgeFixed"":""yes""}"), "$.when", (path, message) => errors.Add(path + message));
        Assert.That(errors, Is.EqualTo(new[] { "$.when.bridgeFixed" + "must be true or false." }));
    }

    [Test]
    public void BridgeFixedFalse_MatchesOnlyWhileBroken()
    {
        List<string> errors = new();
        SpawnConditions conditions = ConditionParser.Parse(JToken.Parse(@"{""bridgeFixed"":false}"), "$.when", (path, message) => errors.Add(path));
        Assert.Multiple(() =>
        {
            Assert.That(conditions.MatchesDay(Season.Spring, Weather.Sun, 1, bridgeFixed: false), Is.True);
            Assert.That(conditions.MatchesDay(Season.Spring, Weather.Sun, 1, bridgeFixed: true), Is.False);
            Assert.That(conditions.IsUnconditional, Is.False);
        });
    }
}
