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
    private static readonly JsonSerializerSettings SmapiSettings = JsonHelper.CreateDefaultSettings();

    private static ObservationData Observation(string player) =>
        new("Forest", new SDate(5, Season.Spring, 2), 630, player);

    [Test]
    public void TryApply_NewBird_AddsAndBumpsVersion()
    {
        SaveData log = new();

        bool applied = ObservationLog.TryApply(log, "blue_tit", Observation("A"), out SaveData updated);

        Assert.That(applied, Is.True);
        Assert.That(updated.logVersion, Is.EqualTo(1));
        Assert.That(updated.seenBirds, Does.Contain("blue_tit"));
        Assert.That(updated.birdObservations["blue_tit"].playerName, Is.EqualTo("A"));
    }

    [Test]
    public void TryApply_DoesNotMutateInput()
    {
        SaveData log = new();

        ObservationLog.TryApply(log, "blue_tit", Observation("A"), out _);

        Assert.That(log.logVersion, Is.EqualTo(0));
        Assert.That(log.seenBirds, Is.Empty);
        Assert.That(log.birdObservations, Is.Empty);
    }

    [Test]
    public void TryApply_AlreadySeen_KeepsFirstSightingAndVersion()
    {
        ObservationLog.TryApply(new SaveData(), "blue_tit", Observation("A"), out SaveData first);

        bool applied = ObservationLog.TryApply(first, "blue_tit", Observation("B"), out SaveData second);

        Assert.That(applied, Is.False);
        Assert.That(second, Is.SameAs(first));
        Assert.That(second.birdObservations["blue_tit"].playerName, Is.EqualTo("A"));
    }

    [Test]
    public void TryApply_SimultaneousDistinctBirds_BothPersist()
    {
        ObservationLog.TryApply(new SaveData(), "blue_tit", Observation("A"), out SaveData afterA);
        ObservationLog.TryApply(afterA, "coal_tit", Observation("B"), out SaveData afterB);

        Assert.That(afterB.seenBirds, Is.EquivalentTo(new[] { "blue_tit", "coal_tit" }));
        Assert.That(afterB.logVersion, Is.EqualTo(2));
    }

    [TestCase(0, DeltaStatus.Duplicate)]
    [TestCase(3, DeltaStatus.Duplicate)]
    [TestCase(4, DeltaStatus.Next)]
    [TestCase(5, DeltaStatus.Gap)]
    public void Check_ClassifiesDelta(int deltaVersion, DeltaStatus expected)
    {
        SaveData log = new() { logVersion = 3 };

        Assert.That(ObservationLog.Check(log, deltaVersion), Is.EqualTo(expected));
    }

    [Test]
    public void SaveData_LegacyJson_LoadsWithVersionZero()
    {
        SaveData data = JsonConvert.DeserializeObject<SaveData>("{\"seenBirds\":[\"blue_tit\"],\"birdObservations\":{}}", SmapiSettings)!;

        Assert.That(data.logVersion, Is.EqualTo(0));
    }

    [Test]
    public void ObservationAccepted_RoundTrips()
    {
        ObservationAccepted message = new(IMessage.CurrentVersion, 7, "blue_tit", Observation("A"));

        string json = JsonConvert.SerializeObject(message, SmapiSettings);
        ObservationAccepted? read = JsonConvert.DeserializeObject<ObservationAccepted>(json, SmapiSettings);

        Assert.That(read, Is.Not.Null);
        Assert.That(read!.Version, Is.EqualTo(IMessage.CurrentVersion));
        Assert.That(read.LogVersion, Is.EqualTo(7));
        Assert.That(read.Details.observationDate, Is.EqualTo(new SDate(5, Season.Spring, 2)));
    }
}
