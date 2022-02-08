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
            CustomBirdType? birdAsCritter = value as CustomBirdType;

            var o = new JObject();
            //o.Add("$type", $"{value.GetType().FullName}, {value.GetType().Assembly.GetName().Name}");
            o.Add("birdTypeName", birdAsCritter.birdTypeName);
            o.Add("position", JToken.FromObject(birdAsCritter.position));
            o.Add("startingPosition", JToken.FromObject(birdAsCritter.startingPosition));

            o.Add("currentAnimatedSprite", JToken.FromObject(new CurrentAnimatedSprite(birdAsCritter.sprite)));
            o.Add("flip", birdAsCritter.flip);
            o.Add("gravityAffectedDY", birdAsCritter.gravityAffectedDY);
            o.Add("yOffset", birdAsCritter.yOffset);
            o.Add("yJumpOffset", birdAsCritter.yJumpOffset);
            o.Add("baseFrame", birdAsCritter.baseFrame);

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
            switch (jo["birdTypeName"].Value<string>())
            {
                case "LandBird":
                    return jo.ToObject<LandBird>(serializer);
                case "BushBird":
                    return jo.ToObject<BushBird>(serializer);
                case "TreeTrunkBird":
                    return jo.ToObject<TreeTrunkBird>(serializer);
                case "WaterLandBird":
                    return jo.ToObject<WaterLandBird>(serializer);
            }
            return null;
        }
    }


    public class CurrentAnimatedSprite
    {
        public List<(int frame, int ms)> currentAnimation { get; set; }
        public int currentFrame { get; set; }
        public float timer { get; set; }
        public bool loop { get; set; }
        public int currentAnimationIndex { get; set; }
        public int spriteWidth { get; set; }
        public int spriteHeight { get; set; }

        public CurrentAnimatedSprite(AnimatedSprite sprite)
        {
            if (currentAnimation != null)
            {
                List<(int frame, int ms)> currentAnimation = new List<(int frame, int ms)>();
                foreach (FarmerSprite.AnimationFrame animFrame in sprite.CurrentAnimation)
                {
                    currentAnimation.Add((animFrame.frame, animFrame.milliseconds));
                }
                this.currentAnimation = currentAnimation;
            }
            else
                this.currentAnimation = null;
            this.currentFrame = sprite.currentFrame;
            this.timer = sprite.timer;
            this.loop = sprite.loop;
            this.currentAnimationIndex = sprite.currentAnimationIndex;
            this.spriteWidth = sprite.spriteWidth.Value;
            this.spriteHeight = sprite.spriteHeight.Value;
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
