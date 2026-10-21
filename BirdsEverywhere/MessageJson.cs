using Newtonsoft.Json;

namespace BirdsEverywhere;

/// <summary>Serializes mod message payloads (plans) with explicit settings instead of process-wide defaults.</summary>
internal static class MessageJson
{
    public static JsonSerializerSettings CreateSettings() => new();

    public static string Serialize<T>(T value) => JsonConvert.SerializeObject(value, CreateSettings());

    public static T Deserialize<T>(string json) =>
        JsonConvert.DeserializeObject<T>(json, CreateSettings())
        ?? throw new JsonSerializationException($"Message JSON for {typeof(T).Name} was null.");
}
