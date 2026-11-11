using Birding.BirdTypes;
using NUnit.Framework;

namespace Birding.UnitTests;

internal sealed class ExclusiveLocksTests
{
    [Test]
    public void SecondBirdIsBlockedWhileTheFirstHoldsTheLock()
    {
        ExclusiveLocks locks = new();

        Assert.Multiple(() =>
        {
            Assert.That(locks.TryAcquire("song", 1, 0), Is.True);
            Assert.That(locks.IsBlocked("song", 100), Is.True);
            Assert.That(locks.TryAcquire("song", 2, 100), Is.False);
            Assert.That(locks.TryAcquire("other", 2, 100), Is.True, "locks are independent by name");
        });
    }

    [Test]
    public void ReleaseBlocksForTheCooldownThenFrees()
    {
        ExclusiveLocks locks = new();
        locks.TryAcquire("song", 1, 0);
        locks.Release("song", 1, 1000, 5000);

        Assert.Multiple(() =>
        {
            Assert.That(locks.IsBlocked("song", 5999), Is.True);
            Assert.That(locks.IsBlocked("song", 6000), Is.False);
            Assert.That(locks.TryAcquire("song", 2, 6000), Is.True);
        });
    }

    [Test]
    public void ReleaseByANonHolderIsIgnored()
    {
        ExclusiveLocks locks = new();
        locks.TryAcquire("song", 1, 0);
        locks.Release("song", 2, 10, 0);

        Assert.That(locks.IsBlocked("song", 20), Is.True);
    }

    [Test]
    public void LockHeldByAVanishedBirdExpires()
    {
        ExclusiveLocks locks = new();
        locks.TryAcquire("song", 1, 0);

        Assert.That(locks.IsBlocked("song", ExclusiveLocks.MaxHoldMs), Is.False);
    }
}
