using Birding.BirdTypes;
using NUnit.Framework;

namespace Birding.UnitTests;

internal sealed class SoundCarryTests
{
    [Test]
    public void Volume_IsFullOnScreenFadesWithDistanceAndNeverSilent()
    {
        Assert.Multiple(() =>
        {
            Assert.That(SoundCarry.Volume(0f), Is.EqualTo(1f));
            Assert.That(SoundCarry.Volume(SoundCarry.FadeTiles / 2f), Is.EqualTo((1f + SoundCarry.FloorVolume) / 2f).Within(1e-6));
            Assert.That(SoundCarry.Volume(SoundCarry.FadeTiles), Is.EqualTo(SoundCarry.FloorVolume).Within(1e-6));
            Assert.That(SoundCarry.Volume(500f), Is.EqualTo(SoundCarry.FloorVolume));
        });
    }
}
