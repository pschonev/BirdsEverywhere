using Birding.Spawners;
using Microsoft.Xna.Framework;
using NUnit.Framework;

namespace Birding.UnitTests;

internal sealed class TrunkVisibilityTests
{
    private static readonly Vector2 _trunk = new(10, 10);

    [TestCase(10, 13, true, TestName = "a tree three tiles south, directly in front")]
    [TestCase(11, 12, true, TestName = "a tree two tiles south and one east, its crown overlaps")]
    [TestCase(9, 15, true, TestName = "a tree five tiles south, the edge of its crown")]
    [TestCase(10, 16, false, TestName = "too far south for its crown to reach")]
    [TestCase(12, 12, false, TestName = "two tiles to the side, beyond the crown width")]
    [TestCase(10, 8, false, TestName = "a tree north of the trunk is behind it")]
    [TestCase(10, 10, false, TestName = "the trunk's own tree")]
    public void IsCovered_FollowsTheCrownOfTreesInFront(float x, float y, bool covered) =>
        Assert.That(TrunkVisibility.IsCovered(_trunk, new[] { new Vector2(x, y) }), Is.EqualTo(covered));

    [Test]
    public void IsCovered_WithNoOtherTreesIsFalse() =>
        Assert.That(TrunkVisibility.IsCovered(_trunk, Array.Empty<Vector2>()), Is.False);
}
