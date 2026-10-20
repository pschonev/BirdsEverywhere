using BirdsEverywhere;
using NUnit.Framework;

namespace BirdsEverwhyere.UnitTests;

public class RandomTests
{
    private const int Draws = 1000;

    [TestCase(1)]
    [TestCase(2)]
    [TestCase(5)]
    public void GetRandomElementFromList_ReachesEveryElement(int count)
    {
        Random random = new Random(42);
        List<int> items = Enumerable.Range(0, count).ToList();

        HashSet<int> picked = Enumerable.Range(0, Draws).Select(_ => Utils.GetRandomElementFromList(random, items)).ToHashSet();

        Assert.That(picked, Is.EquivalentTo(items));
    }

    [Test]
    public void GetRandomElementFromList_Empty_Throws()
    {
        Assert.Throws<ArgumentException>(() => Utils.GetRandomElementFromList(new Random(1), new List<int>()));
    }

    [TestCase(1, 1)]
    [TestCase(1, 3)]
    [TestCase(2, 7)]
    public void NextInclusive_CoversWholeRange(int min, int max)
    {
        Random random = new Random(42);

        HashSet<int> drawn = Enumerable.Range(0, Draws).Select(_ => Utils.NextInclusive(random, min, max)).ToHashSet();

        Assert.That(drawn, Is.EquivalentTo(Enumerable.Range(min, max - min + 1)));
    }

    [Test]
    public void NextInclusive_MinAboveMax_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Utils.NextInclusive(new Random(1), 3, 2));
    }
}
