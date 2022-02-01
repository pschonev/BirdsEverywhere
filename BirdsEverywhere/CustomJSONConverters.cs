using BirdsEverywhere.BirdTypes;
using BirdsEverywhere.Spawners;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BirdsEverywhere
{
    public class SpawnConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return (objectType == typeof(SingleBirdSpawnParameters));
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            JObject jo = JObject.Load(reader);
            if (jo["className"].Value<string>() == "SingleBirdSpawnParamsTerrainFeature")
                return jo.ToObject<SingleBirdSpawnParamsTerrainFeature>(serializer);

            if (jo["className"].Value<string>() == "SingleBirdSpawnParamsTile")
                return jo.ToObject<SingleBirdSpawnParamsTile>(serializer);

            return null;
        }

        public override bool CanWrite
        {
            get { return false; }
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }

    public class CurrentBirdParamsConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return (objectType == typeof(CustomBirdType.CurrentBirdParams));
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            JObject jo = JObject.Load(reader);
            switch(jo["birdTypeName"].Value<string>())
            {
                case "LandBird":
                    return jo.ToObject<LandBird.CurrentLandBirdParams>(serializer);
                case "BushBird":
                    return jo.ToObject<BushBird.CurrentBushBirdParams>(serializer);
                case "TreeTrunkBird":
                    return jo.ToObject<TreeTrunkBird.CurrentTreeTrunkBirdParams>(serializer);
                case "WaterLandBird":
                    return jo.ToObject<WaterLandBird.CurrentwaterLandParams>(serializer);
            }
            

            return null;
        }

        public override bool CanWrite
        {
            get { return false; }
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
