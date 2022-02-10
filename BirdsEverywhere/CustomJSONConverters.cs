using BirdsEverywhere.BirdTypes;
using BirdsEverywhere.Spawners;
using StardewValley.TerrainFeatures;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using StardewValley;
using System.Reflection;
using StardewValley.BellsAndWhistles;

namespace BirdsEverywhere
{
    public class CustomBirdTypeConverterWriter : JsonConverter
    {
        public override bool CanRead => false;
        public override bool CanConvert(Type objectType)
        {
            return objectType.IsSubclassOf(typeof(CustomBirdType));
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            CustomBirdType birdAsCritter = value as CustomBirdType;

            var o = new JObject();
            //o.Add("$type", $"{value.GetType().FullName}, {value.GetType().Assembly.GetName().Name}");

            // properties of Critter
            o.Add("birdTypeName", birdAsCritter.birdTypeName);
            o.Add("position", JToken.FromObject(birdAsCritter.position));
            o.Add("startingPosition", JToken.FromObject(birdAsCritter.startingPosition));

            // AnimatedSpirit has to be handled specifically
            o.Add("currentAnimatedSprite", JToken.FromObject(new CurrentAnimatedSprite(birdAsCritter.sprite)));
            o.Add("flip", birdAsCritter.flip);
            o.Add("gravityAffectedDY", birdAsCritter.gravityAffectedDY);
            o.Add("yOffset", birdAsCritter.yOffset);
            o.Add("yJumpOffset", birdAsCritter.yJumpOffset);
            o.Add("baseFrame", birdAsCritter.baseFrame);

            // properties of CustomBirdType and derivatives
            Type currentType = value.GetType();
            do {
                foreach (PropertyInfo prop in currentType.GetProperties(BindingFlags.Public | BindingFlags.DeclaredOnly | BindingFlags.Instance))
                {
                    if (prop.GetCustomAttribute(typeof(JsonIgnoreAttribute)) != null)
                        continue;
                    if (o.ContainsKey(prop.Name))
                        continue;
                    o.Add(prop.Name, JToken.FromObject(prop.GetValue(value)));
                }
                currentType = currentType.BaseType;
            } while (currentType != null && currentType != typeof(Critter));
                o.WriteTo(writer);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }

    public class CustomBirdTypeConverterReader : JsonConverter
    {
        public override bool CanWrite => false;
        public override bool CanConvert(Type objectType)
        {
            return (objectType == typeof(CustomBirdType));
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var jo = JObject.Load(reader);
            CurrentAnimatedSprite currentSprite = jo["currentAnimatedSprite"].ToObject<CurrentAnimatedSprite>();
            AnimatedSprite sprite = CurrentAnimatedSprite.CurrentToAnimatedSprite(currentSprite);

            switch (jo["birdTypeName"].Value<string>())
            {
                case "LandBird":
                    return jo.ToObject<LandBird>(serializer).setAnimatedSprite(sprite);
                case "WaterLandBird":
                    return jo.ToObject<WaterLandBird>(serializer).setAnimatedSprite(sprite);
                case "BushBird":
                    return jo.ToObject<BushBird>(serializer).setTerrainFeature(jo["index"].Value<int>(), jo["locationName"].Value<string>()).setAnimatedSprite(sprite);
                case "TreeTrunkBird":
                    return jo.ToObject<TreeTrunkBird>(serializer).setTerrainFeature(jo["index"].Value<int>(), jo["locationName"].Value<string>()).setAnimatedSprite(sprite);
            }
            return null;
        }
    }

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
}
