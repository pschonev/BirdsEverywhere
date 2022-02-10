using NUnit.Framework;
using BirdsEverywhere;

namespace BirdsEverwhyere.UnitTests
{
    public class ClampTests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void Clamp_SetsToMinimum()
        {
            int value = -10;
            int min = 5;
            int max = 15;
            int clampedValue = Utils.Clamp<int>(value, min, max);
            Assert.That(clampedValue == min);
        }
        [Test]
        public void Clamp_SetsToMaximum()
        {
            int value = 50;
            int min = 5;
            int max = 15;
            int clampedValue = Utils.Clamp<int>(value, min, max);
            Assert.That(clampedValue == max);
        }
        [Test]
        public void Clamp_KeepValue()
        {
            int value = 7;
            int min = 5;
            int max = 15;
            int clampedValue = Utils.Clamp<int>(value, min, max);
            Assert.That(clampedValue == value);
        }
    }
}