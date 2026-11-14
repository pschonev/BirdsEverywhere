using Birding.BirdTypes;
using Microsoft.Xna.Framework;
using NUnit.Framework;

namespace Birding.UnitTests;

internal sealed class WaterDriftTests
{
    private static readonly Func<Vector2, Vector2, bool> _free = (_, _) => true;
    private static readonly Func<Vector2, Vector2, bool> _blocked = (_, _) => false;

    private static int Flips(Vector2 position, bool flip, Vector2 anchor, Func<Vector2, Vector2, bool> canMove, int steps)
    {
        int flips = 0;
        for (int i = 0; i < steps; i++)
        {
            (position, bool next, anchor) = WaterDrift.Step(position, flip, anchor, canMove);
            flips += next != flip ? 1 : 0;
            flip = next;
        }
        return flips;
    }

    [Test]
    public void Step_ReAnchorsAfterTheBirdSwamAwayInsteadOfFlippingEveryStep()
    {
        Vector2 far = new(1000, 500);
        Vector2 staleAnchor = new(100, 500);
        (Vector2 position, bool flip, Vector2 anchor) = WaterDrift.Step(far, true, staleAnchor, _free);
        Assert.Multiple(() =>
        {
            Assert.That(anchor, Is.EqualTo(far));
            Assert.That(position.X, Is.EqualTo(far.X + WaterDrift.StepPx));
            Assert.That(flip, Is.True);
            Assert.That(Flips(far, true, staleAnchor, _free, 250), Is.Zero, "an unobstructed bird drifts without turning until it reaches its range");
        });
    }

    [Test]
    public void Step_TurnsOnceAtTheEdgeOfOpenWaterAndMovesBack()
    {
        Func<Vector2, Vector2, bool> openWaterToTheEast = (_, next) => next.X < 105f;
        Vector2 start = new(104.5f, 300);
        Assert.That(Flips(start, true, start, openWaterToTheEast, 100), Is.EqualTo(1));
    }

    [Test]
    public void Step_ABirdThatCannotMoveEitherWayStaysStillAndDoesNotFlip()
    {
        Vector2 position = new(300, 300);
        (Vector2 next, bool flip, _) = WaterDrift.Step(position, true, position, _blocked);
        Assert.Multiple(() =>
        {
            Assert.That(next, Is.EqualTo(position));
            Assert.That(flip, Is.True);
            Assert.That(Flips(position, true, position, _blocked, 500), Is.Zero);
        });
    }

    [Test]
    public void Step_TurnsAtTheRangeLimitNotBeyondIt()
    {
        Vector2 anchor = new(500, 300);
        Vector2 position = anchor + new Vector2(WaterDrift.RangePx, 0f);
        (Vector2 next, bool flip, _) = WaterDrift.Step(position, true, anchor, _free);
        Assert.Multiple(() =>
        {
            Assert.That(next, Is.EqualTo(position));
            Assert.That(flip, Is.False);
        });
    }
}
