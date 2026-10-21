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

    private static readonly IReadOnlyList<IBirdBehavior> _land = BirdBehaviors.Land(TestBirds.Services.Routines["land_idle"], 0.003, walkStopChance: null);
    private static readonly IReadOnlyList<IBirdBehavior> _waterLand = BirdBehaviors.WaterLand(TestBirds.Services.Routines["waterland_idle"], 0.005);

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

    [TestCase(0, BirdAction.Sleep)]
    [TestCase(1, BirdAction.Peck)]
    [TestCase(2, BirdAction.Hop)]
    [TestCase(3, BirdAction.TurnAndHop)]
    [TestCase(4, BirdAction.Walk)]
    [TestCase(5, BirdAction.Walk)]
    public void Stopped_PicksIdleRoutine(int pick, BirdAction expected) =>
        Assert.That(Decide(BehaviorStatus.Stopped, pick: pick), Is.EqualTo(expected));

    [TestCase(0.5, true, false)]
    [TestCase(0.0, false, false)]
    [TestCase(0.0, true, true)]
    public void Stopped_NoRoutineWhenRollFailsBusyOrAirborne(double roll, bool idle, bool airborne) =>
        Assert.That(Decide(BehaviorStatus.Stopped, idle: idle, airborne: airborne, roll: roll), Is.EqualTo(BirdAction.None));

    [TestCase(BehaviorStatus.Pecking)]
    [TestCase(BehaviorStatus.Walking)]
    public void OtherStates_NoDecision(BehaviorStatus state) =>
        Assert.That(Decide(state), Is.EqualTo(BirdAction.None));

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

    [TestCase(0, BirdAction.Peck)]
    [TestCase(1, BirdAction.Rest)]
    [TestCase(2, BirdAction.Hop)]
    [TestCase(3, BirdAction.Walk)]
    public void WaterLand_StoppedPicksIdleRoutine(int pick, BirdAction expected) =>
        Assert.That(DecideWaterLand(BehaviorStatus.Stopped, pick: pick), Is.EqualTo(expected));

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
