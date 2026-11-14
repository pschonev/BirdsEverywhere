using System.Collections.Immutable;
using Birding.BirdTypes;
using Birding.Domain;
using Birding.Spawners;
using Microsoft.Xna.Framework;
using NUnit.Framework;

namespace Birding.UnitTests;

internal sealed class FlockTests
{
    private sealed class Field : ILocationQuery
    {
        public string Name => "Farm";
        public Point MapSize => new(40, 40);
        public bool IsGroundTile(Vector2 tile) => true;
        public bool IsWaterTile(Vector2 tile) => false;
        public bool IsSpawnableGroundTile(Vector2 tile) => true;
        public string? FishArea(Vector2 tile) => null;
        public bool IsPassable(Vector2 tile) => true;
        public IReadOnlyList<Vector2> FarmerTiles { get; init; } = Array.Empty<Vector2>();
        public IEnumerable<Vector2> MatureTrees() => Array.Empty<Vector2>();
    }

    private static readonly ThreatReaction _ground = new(1, Escape: ImmutableArray.Create(new EscapeOption("ground", 1, 8)), MaxRelocations: 2);
    private static readonly ThreatReaction _mixed = new(1, Escape: ImmutableArray.Create(new EscapeOption("ground", 1, 8), new EscapeOption("offscreen", 1, 8)), MaxRelocations: 2);

    private static AlarmMember[] Line(params float[] xs) => xs.Select((x, i) => new AlarmMember(i + 1, new Vector2(x, 10), true)).ToArray();

    [Test]
    public void Alarm_SpreadZero_InformsNobody() =>
        Assert.That(GroupAlarm.Plan(Line(10, 12), 0, new Cohesion { Spread = 0 }, 1000), Is.Empty);

    [Test]
    public void Alarm_SpreadOne_InformsConnectedChainButNotBeyondLinkRadius()
    {
        Cohesion cohesion = new() { Spread = 1, LinkRadius = 5 };
        List<(int Index, int DelayMs)> informed = GroupAlarm.Plan(Line(10, 14, 18, 30), 0, cohesion, 1000);
        Assert.That(informed.Select(entry => entry.Index), Is.EquivalentTo(new[] { 1, 2 }));
    }

    [Test]
    public void Alarm_DelaysStayInRangeAndAreDeterministic()
    {
        Cohesion cohesion = new() { Spread = 1, LinkRadius = 5, DelayMs = new IntRange(100, 300) };
        AlarmMember[] members = Line(10, 12, 14, 16);
        List<(int Index, int DelayMs)> first = GroupAlarm.Plan(members, 0, cohesion, 1000);
        Assert.Multiple(() =>
        {
            Assert.That(first, Is.EqualTo(GroupAlarm.Plan(members, 0, cohesion, 1000)));
            Assert.That(first.Select(entry => entry.DelayMs), Has.All.InRange(100, 300));
        });
    }

    [Test]
    public void Alarm_SkipsUnavailableBirds()
    {
        AlarmMember[] members = Line(10, 12, 14);
        members[1] = members[1] with { Available = false };
        Assert.That(GroupAlarm.Plan(members, 0, new Cohesion { Spread = 1, LinkRadius = 3 }, 1000).Select(entry => entry.Index), Is.Empty);
    }

    [Test]
    public void Planner_WithoutFlock_AndFanZero_MatchTheSameSeed()
    {
        Field field = new();
        FlockHints hints = new(new Cohesion { Spread = 1, Fan = 0, Rally = 0 }, new Vector2(5, 10), new Vector2(10, 10), null, Array.Empty<Vector2>());
        Assert.That(EscapePlanner.Surface(field, new(10, 10), new(5, 10), _ground, 0, Array.Empty<Vector2>(), new Random(7), hints),
            Is.EqualTo(EscapePlanner.Surface(field, new(10, 10), new(5, 10), _ground, 0, Array.Empty<Vector2>(), new Random(7))));
    }

    [Test]
    public void Planner_RallyLandsNearAnAnchor()
    {
        Field field = new();
        Vector2 anchor = new(16, 10);
        FlockHints hints = new(new Cohesion { Spread = 1, Fan = 0, Rally = 2 }, new Vector2(5, 10), new Vector2(10, 10), null, new[] { anchor });
        for (int seed = 0; seed < 20; seed++)
        {
            Vector2 tile = EscapePlanner.Surface(field, new(10, 10), new(5, 10), _ground, 0, Array.Empty<Vector2>(), new Random(seed), hints)!.Value.Tile;
            Assert.That(Vector2.Distance(tile, anchor), Is.LessThanOrEqualTo(2));
        }
    }

    [Test]
    public void Planner_AMateAlwaysWinsOverTheFan()
    {
        Field field = new();
        Vector2 anchor = new(10, 6);
        FlockHints hints = new(new Cohesion { Spread = 1, Fan = 1, Rally = 2 }, new Vector2(5, 10), new Vector2(10, 10), null, new[] { anchor });
        Vector2 from = new(10, 14);
        for (int seed = 0; seed < 20; seed++)
        {
            Vector2 tile = EscapePlanner.Surface(field, from, new(5, 10), _ground, 0, Array.Empty<Vector2>(), new Random(seed), hints)!.Value.Tile;
            Assert.That(Vector2.Distance(tile, anchor), Is.LessThanOrEqualTo(2), $"seed {seed}: landed on the far side from the mate");
        }
    }

    [Test]
    public void Planner_WithNoSpotNearAMateTakesTheClosestOneInsteadOfAnyRandomSpot()
    {
        Field field = new();
        Vector2 anchor = new(30, 30);
        FlockHints hints = new(new Cohesion { Spread = 1, Fan = 0, Rally = 2 }, new Vector2(5, 10), new Vector2(10, 10), null, new[] { anchor });
        List<float> distances = Enumerable.Range(0, 30)
            .Select(seed => Vector2.Distance(EscapePlanner.Surface(field, new(10, 10), new(5, 10), _ground, 0, Array.Empty<Vector2>(), new Random(seed), hints)!.Value.Tile, anchor)).ToList();
        Assert.That(distances.Max() - distances.Min(), Is.LessThanOrEqualTo(0.01f), "every bird picks the same closest spot distance");
    }

    [Test]
    public void Planner_FanKeepsTheBirdsSide()
    {
        Field field = new();
        FlockHints hints = new(new Cohesion { Spread = 1, Fan = 1, Rally = 0 }, new Vector2(5, 10), new Vector2(10, 10), null, Array.Empty<Vector2>());
        Vector2 from = new(10, 13);
        for (int seed = 0; seed < 20; seed++)
        {
            Vector2 tile = EscapePlanner.Surface(field, from, new(5, 10), _ground, 0, Array.Empty<Vector2>(), new Random(seed), hints)!.Value.Tile;
            Assert.That(tile.Y, Is.GreaterThan(10));
        }
    }

    [TestCase(1.0, true)]
    [TestCase(0.0, false)]
    public void Planner_SameOutcome_FollowsTheTriggerOnlyWhenSet(double sameOutcome, bool offscreen)
    {
        FlockHints hints = new(new Cohesion { Spread = 1, SameOutcome = sameOutcome, Fan = 0, Rally = 0 }, new Vector2(5, 10), new Vector2(10, 10),
            EscapeOption.Offscreen, Array.Empty<Vector2>());
        EscapeChoice? choice = EscapePlanner.Surface(new Field(), new(10, 10), new(5, 10), _mixed, 0, Array.Empty<Vector2>(), new FixedDouble(0.1), hints);
        Assert.That(choice is null, Is.EqualTo(offscreen));
    }

    [Test]
    public void Rejoin_LandsNearTheMateAndClearOfEveryFarmer()
    {
        Field field = new() { FarmerTiles = new[] { new Vector2(22, 10), new Vector2(8, 10) } };
        for (int seed = 0; seed < 20; seed++)
        {
            Vector2 tile = EscapePlanner.Rejoin(field, new(10, 10), new(20, 10), 2, 3, Array.Empty<Vector2>(), new Random(seed))!.Value;
            Assert.Multiple(() =>
            {
                Assert.That(Vector2.Distance(tile, new Vector2(20, 10)), Is.LessThanOrEqualTo(3));
                Assert.That(field.FarmerTiles.All(farmer => Vector2.Distance(tile, farmer) > 3));
            });
        }
    }

    [Test]
    public void Rejoin_NoFreeTile_StaysPut()
    {
        Field field = new() { FarmerTiles = new[] { new Vector2(20, 10) } };
        Assert.That(EscapePlanner.Rejoin(field, new(10, 10), new(20, 10), 8, 3, Array.Empty<Vector2>(), new Random(1)), Is.Null);
    }

    [Test]
    public void Retarget_SafeSpot_KeepsFlying()
    {
        Field field = new() { FarmerTiles = new[] { new Vector2(2, 10) } };
        Assert.That(EscapePlanner.NeedsRetarget(field, new(10, 10), new(20, 10), 4, Array.Empty<Vector2>()), Is.False);
    }

    [Test]
    public void Retarget_FarmerNearSpot_Replans()
    {
        Field field = new() { FarmerTiles = new[] { new Vector2(22, 10) } };
        Assert.That(EscapePlanner.NeedsRetarget(field, new(10, 10), new(20, 10), 4, Array.Empty<Vector2>()), Is.True);
    }

    [Test]
    public void Retarget_FarmerOnFlightLine_Replans()
    {
        Field field = new() { FarmerTiles = new[] { new Vector2(15, 11) } };
        Assert.That(EscapePlanner.NeedsRetarget(field, new(10, 10), new(20, 10), 4, Array.Empty<Vector2>()), Is.True);
    }

    [Test]
    public void Retarget_SpotTaken_Replans() =>
        Assert.That(EscapePlanner.NeedsRetarget(new Field(), new(10, 10), new(20, 10), 4, new[] { new Vector2(20, 10) }), Is.True);

    [Test]
    public void Planner_FarmerMidGroup_BlocksLandingBehindThem()
    {
        Field field = new() { FarmerTiles = new[] { new Vector2(12, 10) } };
        EscapeChoice? choice = EscapePlanner.Surface(field, new(10, 10), new(12, 10), _ground, 0, Array.Empty<Vector2>(), new Random(1));
        Assert.That(choice is null || choice.Value.Tile.X < 12);
    }

    private sealed class FixedDouble : Random
    {
        private readonly double _value;

        public FixedDouble(double value) => _value = value;

        public override double NextDouble() => _value;

        public override int Next(int maxValue) => 0;
    }
}
