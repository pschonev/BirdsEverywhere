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

    private static BirdAction Decide(BehaviorStatus state, bool threat = false, bool idle = true, bool airborne = false, double roll = 0.0, int pick = 0) =>
        BirdBehaviors.Decide(BirdBehaviors.Land, new BirdContext(state, threat, idle, airborne, new FixedRandom(roll, pick)));

    [TestCase(BehaviorStatus.Stopped)]
    [TestCase(BehaviorStatus.Sleeping)]
    [TestCase(BehaviorStatus.Pecking)]
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
}
