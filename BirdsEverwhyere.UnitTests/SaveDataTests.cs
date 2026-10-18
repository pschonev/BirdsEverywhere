using BirdsEverywhere;
using Newtonsoft.Json;
using NUnit.Framework;
using StardewModdingAPI.Toolkit.Serialization;
using StardewModdingAPI.Utilities;
using StardewValley;

namespace BirdsEverwhyere.UnitTests
{
    public class SaveDataTests
    {
        private const string LegacyJson =
            "{\"seenBirds\":[\"blue_tit\"],\"birdObservations\":{\"blue_tit\":{\"observationLocation\":\"Forest\",\"observationDate\":{\"Day\":5,\"Season\":\"spring\",\"Year\":2,\"DayOfWeek\":5},\"observationTime\":630,\"playerName\":\"Farmer\",\"feather\":true,\"egg\":false,\"picture\":false}}}";

        private static readonly JsonSerializerSettings SmapiSettings = JsonHelper.CreateDefaultSettings();

        [Test]
        public void SaveData_LegacySmapi3Json_Loads()
        {
            SaveData data = JsonConvert.DeserializeObject<SaveData>(LegacyJson, SmapiSettings);

            ObservationData obs = data.birdObservations["blue_tit"];
            Assert.That(data.seenBirds, Does.Contain("blue_tit"));
            Assert.That(obs.observationDate, Is.EqualTo(new SDate(5, Season.Spring, 2)));
            Assert.That(obs.observationTime, Is.EqualTo(630));
            Assert.That(obs.feather, Is.True);
        }

        [Test]
        public void SaveData_RoundTrip_PreservesDate()
        {
            SaveData data = JsonConvert.DeserializeObject<SaveData>(LegacyJson, SmapiSettings);

            string json = JsonConvert.SerializeObject(data, SmapiSettings);
            SaveData reloaded = JsonConvert.DeserializeObject<SaveData>(json, SmapiSettings);

            Assert.That(reloaded.birdObservations["blue_tit"].observationDate, Is.EqualTo(new SDate(5, Season.Spring, 2)));
        }
    }
}
