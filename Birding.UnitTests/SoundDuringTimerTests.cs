using Birding.BirdTypes;
using Birding.Domain;
using NUnit.Framework;

namespace Birding.UnitTests;

internal sealed class SoundDuringTimerTests
{
    private static readonly SoundDuring _always = new("call", 1.0, 400);
    private static readonly SoundDuring _never = new("call", 0.0, 400);

    [Test]
    public void Advance_RollsOncePerInterval()
    {
        SoundDuringTimer timer = new();
        Random random = new(1);
        bool[] results = Enumerable.Range(0, 8).Select(_ => timer.Advance(_always, 100, random)).ToArray();

        Assert.That(results, Is.EqualTo(new[] { false, false, false, true, false, false, false, true }));
    }

    [Test]
    public void Advance_ChanceZeroNeverPlays()
    {
        SoundDuringTimer timer = new();
        Random random = new(1);
        Assert.That(Enumerable.Range(0, 20).Any(_ => timer.Advance(_never, 100, random)), Is.False);
    }

    [Test]
    public void Advance_RestartsWhenTheActionStopsOrChanges()
    {
        SoundDuringTimer timer = new();
        Random random = new(1);
        timer.Advance(_always, 300, random);
        timer.Advance(null, 100, random);
        Assert.Multiple(() =>
        {
            Assert.That(timer.Advance(_always, 300, random), Is.False, "the stop reset the clock");
            Assert.That(timer.Advance(_always, 100, random), Is.True);
            Assert.That(timer.Advance(new SoundDuring("call", 1.0, 400), 300, random), Is.False, "a new action starts from zero");
        });
    }

    [Test]
    public void Advance_ADrawOnlyHappensAtTheInterval()
    {
        SoundDuringTimer timer = new();
        CountingRandom random = new();
        for (int i = 0; i < 3; i++)
        {
            timer.Advance(_always, 100, random);
        }
        Assert.That(random.Draws, Is.Zero);
        timer.Advance(_always, 100, random);
        Assert.That(random.Draws, Is.EqualTo(1));
    }

    private sealed class CountingRandom : Random
    {
        public int Draws { get; private set; }

        public override double NextDouble()
        {
            Draws++;
            return 0.0;
        }
    }
}
