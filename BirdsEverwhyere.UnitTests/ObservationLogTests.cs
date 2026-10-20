using BirdsEverywhere;
using BirdsEverywhere.Multiplayer;
using BirdsEverywhere.Observations;
using Newtonsoft.Json;
using NUnit.Framework;
using StardewModdingAPI.Toolkit.Serialization;
using StardewModdingAPI.Utilities;
using StardewValley;

namespace BirdsEverwhyere.UnitTests;

public class ObservationLogTests
{
    private static readonly JsonSerializerSettings _smapiSettings = JsonHelper.CreateDefaultSettings();

    private static ObservationData Observation(string player) =>
        new("Forest", new SDate(5, Season.Spring, 2), 630, player);

    [Test]
    public void TryApply_NewBird_AddsAndBumpsVersion()
    {
        SaveData log = new();

        bool applied = ObservationLog.TryApply(log, "blue_tit", Observation("A"), out SaveData updated);

        Assert.That(applied, Is.True);
        Assert.That(updated.LogVersion, Is.EqualTo(1));
        Assert.That(updated.SeenBirds, Does.Contain("blue_tit"));
        Assert.That(updated.BirdObservations["blue_tit"].PlayerName, Is.EqualTo("A"));
    }

    [Test]
    public void TryApply_DoesNotMutateInput()
    {
        SaveData log = new();

        ObservationLog.TryApply(log, "blue_tit", Observation("A"), out _);

        Assert.That(log.LogVersion, Is.EqualTo(0));
        Assert.That(log.SeenBirds, Is.Empty);
        Assert.That(log.BirdObservations, Is.Empty);
    }

    [Test]
    public void TryApply_AlreadySeen_KeepsFirstSightingAndVersion()
    {
        ObservationLog.TryApply(new SaveData(), "blue_tit", Observation("A"), out SaveData first);

        bool applied = ObservationLog.TryApply(first, "blue_tit", Observation("B"), out SaveData second);

        Assert.That(applied, Is.False);
        Assert.That(second, Is.SameAs(first));
        Assert.That(second.BirdObservations["blue_tit"].PlayerName, Is.EqualTo("A"));
    }

    [Test]
    public void TryApply_SimultaneousDistinctBirds_BothPersist()
    {
        ObservationLog.TryApply(new SaveData(), "blue_tit", Observation("A"), out SaveData afterA);
        ObservationLog.TryApply(afterA, "coal_tit", Observation("B"), out SaveData afterB);

        Assert.That(afterB.SeenBirds, Is.EquivalentTo(new[] { "blue_tit", "coal_tit" }));
        Assert.That(afterB.LogVersion, Is.EqualTo(2));
    }

    [TestCase(0, DeltaStatus.Duplicate)]
    [TestCase(3, DeltaStatus.Duplicate)]
    [TestCase(4, DeltaStatus.Next)]
    [TestCase(5, DeltaStatus.Gap)]
    public void Check_ClassifiesDelta(int deltaVersion, DeltaStatus expected)
    {
        SaveData log = new() { LogVersion = 3 };

        Assert.That(ObservationLog.Check(log, deltaVersion), Is.EqualTo(expected));
    }

    [Test]
    public void SaveData_LegacyJson_LoadsWithVersionZero()
    {
        SaveData data = JsonConvert.DeserializeObject<SaveData>("{\"seenBirds\":[\"blue_tit\"],\"birdObservations\":{}}", _smapiSettings)!;

        Assert.That(data.LogVersion, Is.EqualTo(0));
    }

    [Test]
    public void ObservationAccepted_RoundTrips()
    {
        ObservationAccepted message = new(IMessage.CurrentVersion, 7, "blue_tit", Observation("A"));

        string json = JsonConvert.SerializeObject(message, _smapiSettings);
        ObservationAccepted? read = JsonConvert.DeserializeObject<ObservationAccepted>(json, _smapiSettings);

        Assert.That(read, Is.Not.Null);
        Assert.That(read!.Version, Is.EqualTo(IMessage.CurrentVersion));
        Assert.That(read.LogVersion, Is.EqualTo(7));
        Assert.That(read.Details.ObservationDate, Is.EqualTo(new SDate(5, Season.Spring, 2)));
    }
}
