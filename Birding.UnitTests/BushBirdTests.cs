using Birding.BirdTypes;
using Birding.BirdTypes.Behaviors;
using Birding.Content;
using Birding.Spawners;
using Microsoft.Xna.Framework;
using NUnit.Framework;

namespace Birding.UnitTests;

internal sealed class BushBirdTests
{
    private static readonly Vector2 _bush = new(50, 20);

    [Test]
    public void Place_FacesNearestArrival()
    {
        Vector2? east = BushPlacement.Place(_bush, 3, new[] { new Vector2(90, 20) }, 12, new Random(1));
        Vector2? west = BushPlacement.Place(_bush, 3, new[] { new Vector2(10, 20) }, 12, new Random(1));
        Assert.That(east, Is.EqualTo(new Vector2(53, 20)));
        Assert.That(west, Is.EqualTo(new Vector2(47, 20)));
    }

    [Test]
    public void Place_RejectsArrivalInsideAlertRadius() =>
        Assert.That(BushPlacement.Place(_bush, 3, new[] { new Vector2(60, 20) }, 12, new Random(1)), Is.Null);

    [Test]
    public void Place_WithoutArrivals_PicksEitherSide()
    {
        Vector2 position = BushPlacement.Place(_bush, 4, Array.Empty<Vector2>(), 12, new Random(3))!.Value;
        Assert.That(Math.Abs(position.X - _bush.X), Is.EqualTo(4));
    }

    [Test]
    public void Profile_AlertsThenFleesAfterDelay()
    {
        Domain.AlertReaction alert = BirdTypeProfiles.ByBirdType["BushBird"].Reactions!.Threat.Alert!;
        Assert.That(alert.Radius, Is.GreaterThan(BirdTypeProfiles.ByBirdType["BushBird"].Reactions!.Threat.FleeRadius));
        Assert.That(alert.FleeAfterMs, Is.EqualTo(3000));
    }

    private static BirdAction Decide(BehaviorStatus state, bool threat = false, bool inAlertRange = false, bool calm = false) =>
        BirdBehaviors.Decide(BirdBehaviors.Bush, new BirdContext(state, threat, true, false, new Random(1), inAlertRange, calm));

    [Test]
    public void Behavior_FleesWhenClose() =>
        Assert.That(Decide(BehaviorStatus.Stopped, threat: true, inAlertRange: true), Is.EqualTo(BirdAction.Flee));

    [Test]
    public void Behavior_AlertsInRing() =>
        Assert.That(Decide(BehaviorStatus.Stopped, inAlertRange: true), Is.EqualTo(BirdAction.Alert));

    [Test]
    public void Behavior_CalmsAfterRingEmpties() =>
        Assert.That(Decide(BehaviorStatus.Alert, calm: true), Is.EqualTo(BirdAction.Stop));

    [Test]
    public void Behavior_StaysRunning() =>
        Assert.That(Decide(BehaviorStatus.Running, threat: true, inAlertRange: true), Is.EqualTo(BirdAction.None));
}
