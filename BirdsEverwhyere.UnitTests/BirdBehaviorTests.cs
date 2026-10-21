using BirdsEverywhere.BirdTypes;
using BirdsEverywhere.BirdTypes.Behaviors;
using NUnit.Framework;

namespace BirdsEverwhyere.UnitTests;

internal sealed class BirdBehaviorTests
{
    private sealed class FixedRandom : Random
    {
        private readonly double _double;
        private readonly int _int;

        public FixedRandom(double nextDouble, int next = 0)
        {
            _double = nextDouble;
            _int = next;
        }

        public override double NextDouble() => _double;
        public override int Next(int maxValue) => _int;
    }

    private static readonly IReadOnlyList<IBirdBehavior> _land = BirdBehaviors.Land(0.003, walkStopChance: null);
    private static readonly IReadOnlyList<IBirdBehavior> _waterLand = BirdBehaviors.WaterLand(0.005);

    private static BirdAction Decide(BehaviorStatus state, bool threat = false, bool idle = true, bool airborne = false, double roll = 0.0, int pick = 0) =>
        BirdBehaviors.Decide(_land, new BirdContext(state, threat, idle, airborne, new FixedRandom(roll, pick)));

    [TestCase(BehaviorStatus.Stopped)]
    [TestCase(BehaviorStatus.Sleeping)]
    [TestCase(BehaviorStatus.Walking)]
    public void Threat_AlwaysFleesFirst(BehaviorStatus state) =>
        Assert.That(Decide(state, threat: true), Is.EqualTo(BirdAction.Flee));

    [Test]
    public void Threat_IgnoredWhileFlyingAway() =>
        Assert.That(Decide(BehaviorStatus.FlyingAway, threat: true), Is.EqualTo(BirdAction.None));

    [TestCase(0.0, true, BirdAction.Stop)]
    [TestCase(0.0, false, BirdAction.None)]
    [TestCase(0.5, true, BirdAction.None)]
    public void Sleeping_WakesOnRollWhenIdle(double roll, bool idle, BirdAction expected) =>
        Assert.That(Decide(BehaviorStatus.Sleeping, idle: idle, roll: roll), Is.EqualTo(expected));

    [TestCase(0.5, true, false)]
    [TestCase(0.0, false, false)]
    [TestCase(0.0, true, true)]
    [TestCase(0.0, true, false)]
    public void Stopped_ListNeverPicksIdle(double roll, bool idle, bool airborne) =>
        Assert.That(Decide(BehaviorStatus.Stopped, idle: idle, airborne: airborne, roll: roll), Is.EqualTo(BirdAction.None));

    [TestCase(BehaviorStatus.Pecking)]
    [TestCase(BehaviorStatus.Walking)]
    public void OtherStates_NoDecision(BehaviorStatus state) =>
        Assert.That(Decide(state), Is.EqualTo(BirdAction.None));

    private static BirdAction DecideAlert(BehaviorStatus state, bool threat = false, bool inAlertRange = true, bool calm = false, bool airborne = false) =>
        BirdBehaviors.Decide(_land, new BirdContext(state, threat, true, airborne, new FixedRandom(0.5), inAlertRange, calm));

    [TestCase(BehaviorStatus.Stopped, BirdAction.Alert)]
    [TestCase(BehaviorStatus.Pecking, BirdAction.Alert)]
    [TestCase(BehaviorStatus.Walking, BirdAction.Alert)]
    [TestCase(BehaviorStatus.Sleeping, BirdAction.Alert)]
    [TestCase(BehaviorStatus.Alert, BirdAction.None)]
    [TestCase(BehaviorStatus.FlyingAway, BirdAction.None)]
    [TestCase(BehaviorStatus.Swimming, BirdAction.None)]
    public void AlertRing_FreezesGroundedBirds(BehaviorStatus state, BirdAction expected) =>
        Assert.That(DecideAlert(state), Is.EqualTo(expected));

    [Test]
    public void AlertRing_FleeWinsAndAirborneIgnores() =>
        Assert.Multiple(() =>
        {
            Assert.That(DecideAlert(BehaviorStatus.Alert, threat: true), Is.EqualTo(BirdAction.Flee));
            Assert.That(DecideAlert(BehaviorStatus.Stopped, airborne: true), Is.EqualTo(BirdAction.None));
        });

    [TestCase(true, BirdAction.Stop)]
    [TestCase(false, BirdAction.None)]
    public void Alert_ResumesOnlyWhenCalm(bool calm, BirdAction expected) =>
        Assert.That(DecideAlert(BehaviorStatus.Alert, inAlertRange: false, calm: calm), Is.EqualTo(expected));

    [Test]
    public void Threat_IgnoredWhileRelocating() =>
        Assert.That(BirdBehaviors.Decide(BirdBehaviors.FleeOnly, new BirdContext(BehaviorStatus.Relocating, true, true, false, new FixedRandom(0))),
            Is.EqualTo(BirdAction.None));

    private static readonly Microsoft.Xna.Framework.Vector2[] _trees =
    {
        new(10, 10), new(12, 10), new(14, 10), new(20, 10), new(30, 10), new(10, 14),
    };

    [TestCase(0, 14f, 10f)]
    [TestCase(1, 20f, 10f)]
    public void Relocation_PicksAFreeTreeInReachAwayFromTheThreat(int pick, float x, float y) =>
        Assert.That(Relocation.Pick(_trees, new(10, 10), threat: new(9, 13), fleeRadius: 1, maxDistance: 12,
                occupied: new[] { new Microsoft.Xna.Framework.Vector2(12, 10) }, new FixedRandom(0, pick)),
            Is.EqualTo(new Microsoft.Xna.Framework.Vector2(x, y)));

    [Test]
    public void Relocation_NoCandidate_ReturnsNull() =>
        Assert.That(Relocation.Pick(_trees, new(10, 10), threat: new(20, 10), fleeRadius: 30, maxDistance: 12,
            Array.Empty<Microsoft.Xna.Framework.Vector2>(), new FixedRandom(0)), Is.Null);

    [TestCase(0f, 0f)]
    [TestCase(0.5f, -48f)]
    [TestCase(1f, 0f)]
    public void Relocation_ArcPeaksHalfway(float progress, float expected) =>
        Assert.That(Relocation.ArcOffset(progress, 48f), Is.EqualTo(expected).Within(0.001f));

    private static readonly BirdsEverywhere.Domain.SpawnConditions _rainOnly = new(
        System.Collections.Immutable.ImmutableArray<BirdsEverywhere.Domain.Season>.Empty,
        System.Collections.Immutable.ImmutableArray.Create(BirdsEverywhere.Domain.Weather.Rain),
        System.Collections.Immutable.ImmutableArray<BirdsEverywhere.Domain.IntRange>.Empty,
        System.Collections.Immutable.ImmutableArray<BirdsEverywhere.Domain.IntRange>.Empty);

    private static readonly BirdsEverywhere.Domain.Routine _forager = new(0.01,
        new[] { new BirdsEverywhere.Domain.WeightedId("peck", 3), new BirdsEverywhere.Domain.WeightedId("dive", 1) },
        new Dictionary<string, IReadOnlyList<BirdsEverywhere.Domain.WeightedId>>(),
        new[] { new BirdsEverywhere.Domain.RareAction("wormPull", 0.1, _rainOnly) });

    private static readonly BirdsEverywhere.Domain.IdleWorld _rain = new(BirdsEverywhere.Domain.Season.Spring, BirdsEverywhere.Domain.Weather.Rain, 1, 900);
    private static readonly BirdsEverywhere.Domain.IdleWorld _sun = _rain with { Weather = BirdsEverywhere.Domain.Weather.Sun };

    [TestCase(0.05, true, "wormPull")]
    [TestCase(0.05, false, "peck")]
    [TestCase(0.5, true, "peck")]
    public void Routine_RareActionNeedsItsConditionAndChance(double roll, bool raining, string expected) =>
        Assert.That(_forager.Choose(new FixedRandom(roll, 0), id => id != "dive", raining ? _rain : _sun), Is.EqualTo(expected));

    [Test]
    public void Routine_ConditionalRareNeverFiresWithoutAWorld() =>
        Assert.That(_forager.Choose(new FixedRandom(0, 0), _ => true, null), Is.EqualTo("peck"));

    [Test]
    public void Routine_FilterRestrictsPicksAndHasOptions() =>
        Assert.Multiple(() =>
        {
            Assert.That(_forager.Choose(new FixedRandom(0.9, 0), id => id == "dive", _rain), Is.EqualTo("dive"));
            Assert.That(_forager.HasOptions(id => id == "swim"), Is.False);
            Assert.That(_forager.Choose(new FixedRandom(0.9, 0), id => id == "swim", _rain), Is.Null);
        });

    [Test]
    public void Routine_ChooseWithoutRareOrFilter_DrawsLikeRoll()
    {
        BirdsEverywhere.Domain.Routine plain = _forager with { Rare = null };
        Random a = new(7);
        Random b = new(7);
        for (int i = 0; i < 50; i++)
        {
            Assert.That(plain.Choose(a, _ => true, null), Is.EqualTo(BirdsEverywhere.Domain.Routine.Roll(plain.Pick, b)));
        }
    }

    [TestCase(BehaviorStatus.Diving)]
    public void Threat_IgnoredWhileDiving(BehaviorStatus state) =>
        Assert.That(BirdBehaviors.Decide(_waterLand, new BirdContext(state, true, true, false, new FixedRandom(0), true, false)), Is.EqualTo(BirdAction.None));

    /// <summary>An approach timeline: far, alert ring, still close, leaves, calm, then the flee radius.</summary>
    [Test]
    public void ApproachAndRetreat_Timeline()
    {
        (bool Threat, bool InRing, bool Calm)[] ticks = { (false, false, true), (false, true, false), (false, false, false), (false, false, true), (false, true, false), (true, false, false) };
        BehaviorStatus state = BehaviorStatus.Stopped;
        List<BirdAction> actions = new();
        foreach ((bool threat, bool inRing, bool calm) in ticks)
        {
            BirdAction action = BirdBehaviors.Decide(_land, new BirdContext(state, threat, true, false, new FixedRandom(0.5), inRing, calm));
            actions.Add(action);
            state = action switch
            {
                BirdAction.Alert => BehaviorStatus.Alert,
                BirdAction.Stop => BehaviorStatus.Stopped,
                BirdAction.Flee => BehaviorStatus.FlyingAway,
                _ => state,
            };
        }

        Assert.That(actions, Is.EqualTo(new[] { BirdAction.None, BirdAction.Alert, BirdAction.None, BirdAction.Stop, BirdAction.Alert, BirdAction.Flee }));
    }

    private static BirdAction DecideWaterLand(BehaviorStatus state, bool threat = false, bool idle = true, double roll = 0.0, int pick = 0) =>
        BirdBehaviors.Decide(_waterLand, new BirdContext(state, threat, idle, false, new FixedRandom(roll, pick)));

    [TestCase(BehaviorStatus.Stopped)]
    [TestCase(BehaviorStatus.Swimming)]
    public void WaterLand_ThreatFleesFirst(BehaviorStatus state) =>
        Assert.That(DecideWaterLand(state, threat: true), Is.EqualTo(BirdAction.Flee));

    [TestCase(0.0, BirdAction.Stop)]
    [TestCase(0.5, BirdAction.None)]
    public void WaterLand_WalkingStopsOnRoll(double roll, BirdAction expected) =>
        Assert.That(DecideWaterLand(BehaviorStatus.Walking, roll: roll), Is.EqualTo(expected));

    [Test]
    public void WaterLand_StoppedListNeverPicksIdle() =>
        Assert.That(DecideWaterLand(BehaviorStatus.Stopped), Is.EqualTo(BirdAction.None));

    [Test]
    public void WaterLand_SwimmingHasNoRoutine() =>
        Assert.That(DecideWaterLand(BehaviorStatus.Swimming), Is.EqualTo(BirdAction.None));

    [TestCase(BehaviorStatus.Pecking, true, BirdAction.Flee)]
    [TestCase(BehaviorStatus.Stopped, true, BirdAction.Flee)]
    [TestCase(BehaviorStatus.Stopped, false, BirdAction.None)]
    [TestCase(BehaviorStatus.FlyingAway, true, BirdAction.None)]
    [TestCase(BehaviorStatus.Running, true, BirdAction.None)]
    public void FleeOnly_FleesOnceOnThreat(BehaviorStatus state, bool threat, BirdAction expected) =>
        Assert.That(BirdBehaviors.Decide(BirdBehaviors.FleeOnly, new BirdContext(state, threat, true, false, new FixedRandom(0.0))), Is.EqualTo(expected));
}
