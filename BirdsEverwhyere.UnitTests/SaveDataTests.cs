using BirdsEverywhere;
using Newtonsoft.Json;
using NUnit.Framework;
using StardewModdingAPI.Toolkit.Serialization;
using StardewModdingAPI.Utilities;
using StardewValley;

namespace BirdsEverwhyere.UnitTests;

public class SaveDataTests
{
    private const string LegacyJson =
        "{\"seenBirds\":[\"blue_tit\"],\"birdObservations\":{\"blue_tit\":{\"observationLocation\":\"Forest\",\"observationDate\":{\"Day\":5,\"Season\":\"spring\",\"Year\":2,\"DayOfWeek\":5},\"observationTime\":630,\"playerName\":\"Farmer\",\"feather\":true,\"egg\":false,\"picture\":false}}}";

    private static readonly JsonSerializerSettings _smapiSettings = JsonHelper.CreateDefaultSettings();

    [Test]
    public void SaveData_LegacySmapi3Json_Loads()
    {
        SaveData data = JsonConvert.DeserializeObject<SaveData>(LegacyJson, _smapiSettings)!;

        ObservationData obs = data.BirdObservations["blue_tit"];
        Assert.That(data.SeenBirds, Does.Contain("blue_tit"));
        Assert.That(obs.ObservationDate, Is.EqualTo(new SDate(5, Season.Spring, 2)));
        Assert.That(obs.ObservationTime, Is.EqualTo(630));
        Assert.That(obs.Feather, Is.True);

        string json = JsonConvert.SerializeObject(data, _smapiSettings);
        SaveData reloaded = JsonConvert.DeserializeObject<SaveData>(json, _smapiSettings)!;

        Assert.That(reloaded.BirdObservations["blue_tit"].ObservationDate, Is.EqualTo(new SDate(5, Season.Spring, 2)));
    }
}
