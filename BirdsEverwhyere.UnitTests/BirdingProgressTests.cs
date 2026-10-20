using BirdsEverywhere;
using BirdsEverywhere.Observations;
using Newtonsoft.Json;
using NUnit.Framework;
using StardewModdingAPI.Utilities;

namespace BirdsEverwhyere.UnitTests;

public class BirdingProgressTests
{
    [TestCase(0, 0)]
    [TestCase(99, 0)]
    [TestCase(100, 1)]
    [TestCase(399, 1)]
    [TestCase(400, 2)]
    [TestCase(900, 3)]
    public void Level_FromXp(long xp, int level) => Assert.That(BirdingProgress.Level(xp), Is.EqualTo(level));

    [TestCase(0, "level 0, 0/100")]
    [TestCase(500, "level 2, 100/500")]
    public void Describe_ShowsProgressWithinLevel(long xp, string text) => Assert.That(BirdingProgress.Describe(xp), Is.EqualTo(text));

    [Test]
    public void TryAwardSighting_OncePerSpeciesAndDay()
    {
        SaveData log = new() { SeenBirds = { "a", "b" } };

        Assert.That(BirdingProgress.TryAwardSighting(log, "unseen", 1, out _), Is.False);
        Assert.That(BirdingProgress.TryAwardSighting(log, "a", 1, out SaveData first), Is.True);
        Assert.That(first.BirdingXp, Is.EqualTo(BirdingProgress.RepeatSightingXp));
        Assert.That(BirdingProgress.TryAwardSighting(first, "a", 1, out _), Is.False);
        Assert.That(BirdingProgress.TryAwardSighting(first, "b", 1, out SaveData second), Is.True);
        Assert.That(second.BirdingXp, Is.EqualTo(2 * BirdingProgress.RepeatSightingXp));
        Assert.That(BirdingProgress.TryAwardSighting(second, "a", 2, out SaveData nextDay), Is.True);
        Assert.That(nextDay.Sightings, Is.EquivalentTo(new[] { "a" }));
    }

    [Test]
    public void NewObservation_AwardsSpeciesXp_AndSaveDataRoundTrips()
    {
        ObservationData details = new("Forest", new SDate(1, "spring"), 600, "P");

        Assert.That(ObservationLog.TryApply(new SaveData(), "a", details, out SaveData updated), Is.True);
        Assert.That(updated.BirdingXp, Is.EqualTo(BirdingProgress.NewSpeciesXp));

        SaveData loaded = JsonConvert.DeserializeObject<SaveData>(JsonConvert.SerializeObject(updated))!;
        Assert.That(loaded.BirdingXp, Is.EqualTo(updated.BirdingXp));
        Assert.That(JsonConvert.DeserializeObject<SaveData>("{\"seenBirds\":[\"a\"]}")!.BirdingXp, Is.EqualTo(0));
    }
}
