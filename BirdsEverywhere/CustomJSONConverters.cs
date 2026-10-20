using System.Reflection;
using BirdsEverywhere.BirdTypes;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using StardewValley;
using StardewValley.BellsAndWhistles;

namespace BirdsEverywhere;

public class CustomBirdTypeConverterWriter : JsonConverter
{
    public override bool CanRead => false;
    public override bool CanConvert(Type objectType)
    {
        return objectType.IsSubclassOf(typeof(BirdCritter));
    }

    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        BirdCritter bird = (BirdCritter)value!;

        JObject o = new()
        {
            { "birdTypeName", bird.BirdTypeName },
            { "position", JToken.FromObject(bird.position) },
            { "startingPosition", JToken.FromObject(bird.startingPosition) },
            { "currentAnimatedSprite", JToken.FromObject(new CurrentAnimatedSprite(bird.sprite)) },
            { "flip", bird.flip },
            { "gravityAffectedDY", bird.gravityAffectedDY },
            { "yOffset", bird.yOffset },
            { "yJumpOffset", bird.yJumpOffset },
            { "baseFrame", bird.baseFrame },
        };

        Type? currentType = bird.GetType();
        while (currentType != null && currentType != typeof(Critter))
        {
            foreach (PropertyInfo prop in currentType.GetProperties(BindingFlags.Public | BindingFlags.DeclaredOnly | BindingFlags.Instance))
            {
                if (prop.GetCustomAttribute<JsonIgnoreAttribute>() != null)
                {
                    continue;
                }
                string name = prop.GetCustomAttribute<JsonPropertyAttribute>()?.PropertyName ?? prop.Name;
                if (o.ContainsKey(name))
                {
                    continue;
                }
                object? propValue = prop.GetValue(bird);
                o.Add(name, propValue == null ? JValue.CreateNull() : JToken.FromObject(propValue));
            }
            currentType = currentType.BaseType;
        }
        o.WriteTo(writer);
    }

    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
}

public class CustomBirdTypeConverterReader : JsonConverter
{
    public override bool CanWrite => false;
    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(BirdCritter);
    }

    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }

    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
        JObject jo = JObject.Load(reader);
        CurrentAnimatedSprite? currentSprite = jo["currentAnimatedSprite"]?.ToObject<CurrentAnimatedSprite>();
        if (currentSprite == null)
        {
            return null;
        }
        AnimatedSprite sprite = CurrentAnimatedSprite.CurrentToAnimatedSprite(currentSprite);
        int index = jo["index"]?.Value<int>() ?? -1;
        string? locationName = jo["locationName"]?.Value<string>();

        return jo["birdTypeName"]?.Value<string>() switch
        {
            "LandBird" => jo.ToObject<LandBird>(serializer)?.SetAnimatedSprite(sprite),
            "WaterLandBird" => jo.ToObject<WaterLandBird>(serializer)?.SetAnimatedSprite(sprite),
            "BushBird" => jo.ToObject<BushBird>(serializer)?.SetAnchor(index, locationName).SetAnimatedSprite(sprite),
            "TreeTrunkBird" => jo.ToObject<TreeTrunkBird>(serializer)?.SetAnchor(index, locationName).SetAnimatedSprite(sprite),
            _ => null,
        };
    }
}
