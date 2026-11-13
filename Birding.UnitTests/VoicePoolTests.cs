using Birding.BirdTypes;
using Birding.Domain;
using NUnit.Framework;

namespace Birding.UnitTests;

internal sealed class VoicePoolTests
{
    private static readonly VoiceLimit _two = new(2, 100);

    [Test]
    public void CanStart_EnforcesMinGapAndMaxVoices()
    {
        VoicePool pool = new();
        pool.Started("a", 0, () => true);
        Assert.Multiple(() =>
        {
            Assert.That(pool.CanStart("a", _two, 50), Is.False, "inside the gap");
            Assert.That(pool.CanStart("a", _two, 150), Is.True, "second voice");
        });
        pool.Started("a", 150, () => true);
        Assert.That(pool.CanStart("a", _two, 300), Is.False, "two already playing");
    }

    [Test]
    public void CanStart_FreesStoppedAndStaleVoices()
    {
        VoicePool pool = new();
        bool playing = true;
        pool.Started("a", 0, () => playing);
        pool.Started("a", 200, () => true);
        playing = false;
        Assert.That(pool.CanStart("a", _two, 400), Is.True, "the first cue stopped");
        pool.Started("a", 400, () => true);
        Assert.That(pool.CanStart("a", _two, 400 + VoicePool.MaxVoiceMs), Is.True, "voices older than the cap no longer count");
    }

    [Test]
    public void CanStart_KeysAreIndependent()
    {
        VoicePool pool = new();
        pool.Started("Beach/seagulls", 0, () => true);
        Assert.Multiple(() =>
        {
            Assert.That(pool.CanStart("Beach/seagulls", _two, 10), Is.False);
            Assert.That(pool.CanStart("Forest/seagulls", _two, 10), Is.True, "another location");
            Assert.That(pool.CanStart("Beach/crow", _two, 10), Is.True, "another cue");
        });
    }
}
