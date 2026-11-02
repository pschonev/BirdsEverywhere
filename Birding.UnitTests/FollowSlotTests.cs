using Birding.Domain;
using NUnit.Framework;

namespace Birding.UnitTests;

public class FollowSlotTests
{
    private const int Followers = 12;
    private const float MinSeparation = 32f;
    private const float MinLeaderDistance = 48f;

    [Test]
    public void Offsets_KeepFollowersApartAndOffTheLeader()
    {
        var offsets = Enumerable.Range(0, Followers).Select(FollowSlot.Offset).ToArray();
        Assert.Multiple(() =>
        {
            foreach (var offset in offsets)
            {
                Assert.That(MathF.Sqrt((offset.X * offset.X) + (offset.Y * offset.Y)), Is.GreaterThanOrEqualTo(MinLeaderDistance));
            }
            for (int a = 0; a < Followers; a++)
            {
                for (int b = a + 1; b < Followers; b++)
                {
                    float dx = offsets[a].X - offsets[b].X;
                    float dy = offsets[a].Y - offsets[b].Y;
                    Assert.That(MathF.Sqrt((dx * dx) + (dy * dy)), Is.GreaterThanOrEqualTo(MinSeparation), $"slots {a} and {b}");
                }
            }
        });
    }
}
