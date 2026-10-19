using Newtonsoft.Json;

namespace BirdsEverywhere;

/// <summary>Serializes mod messages that carry spawn plans or birds, which need the mod's converters.</summary>
internal static class MessageJson
{
    public static JsonSerializerSettings CreateSettings() => new()
    {
        Converters = { new CustomBirdTypeConverterWriter(), new CustomBirdTypeConverterReader() },
    };

    public static string Serialize<T>(T value) => JsonConvert.SerializeObject(value, CreateSettings());

    public static T Deserialize<T>(string json) =>
        JsonConvert.DeserializeObject<T>(json, CreateSettings())
        ?? throw new JsonSerializationException($"Message JSON for {typeof(T).Name} was null.");
}
