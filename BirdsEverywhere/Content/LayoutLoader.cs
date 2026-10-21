using BirdsEverywhere.Domain;
using Newtonsoft.Json.Linq;

namespace BirdsEverywhere.Content;

public static class LayoutLoader
{
    public const string FlipTag = "flip";

    private static readonly HashSet<string> _layoutKeys = new(StringComparer.Ordinal) { "frameSize", "clips" };
    private static readonly HashSet<string> _clipKeys = new(StringComparer.Ordinal) { "frames", "repeat", "end", "loop" };
    private static readonly HashSet<string> _repeatKeys = new(StringComparer.Ordinal) { "frames", "count" };

    public static (IReadOnlyDictionary<string, SpriteLayout> Layouts, IReadOnlyList<ValidationIssue> Issues) LoadAll(string root) =>
        DefinitionFiles.LoadAll(ContentPaths.LayoutsDirectory(root), Parse);

    /// <summary>Parses a layout file; throws <see cref="FormatException"/> naming the file and field.</summary>
    public static SpriteLayout Parse(string id, string json, string file)
    {
        JObject root = JObject.Parse(json);
        RejectUnknown(root, _layoutKeys, file, "$");
        if (root["frameSize"] is not JArray { Count: 2 } size || size.Any(item => item.Type != JTokenType.Integer || item.Value<int>() < 1))
        {
            throw new FormatException($"{file}: frameSize must be [width, height].");
        }
        if (root["clips"] is not JObject clips || clips.Count == 0)
        {
            throw new FormatException($"{file}: clips must be a non-empty object.");
        }

        Dictionary<string, SpriteClip> parsed = new(StringComparer.Ordinal);
        foreach (JProperty clip in clips.Properties())
        {
            parsed[clip.Name] = ParseClip(clip.Value, file, $"clips.{clip.Name}");
        }
        return new SpriteLayout(id, size[0].Value<int>(), size[1].Value<int>(), parsed);
    }

    private static SpriteClip ParseClip(JToken token, string file, string path)
    {
        if (token is JArray list)
        {
            return new SpriteClip(ParseFrames(list, file, path, required: true), null, Array.Empty<ClipFrame>(), false);
        }
        if (token is not JObject body)
        {
            throw new FormatException($"{file}: {path} must be a frame list or an object.");
        }

        RejectUnknown(body, _clipKeys, file, path);
        ClipRepeat? repeat = null;
        if (body["repeat"] is JObject repeatBody)
        {
            RejectUnknown(repeatBody, _repeatKeys, file, $"{path}.repeat");
            repeat = new ClipRepeat(
                ParseFrames(repeatBody["frames"], file, $"{path}.repeat.frames", required: true),
                ParseRange(repeatBody["count"], file, $"{path}.repeat.count"));
        }
        else if (body["repeat"] is not null)
        {
            throw new FormatException($"{file}: {path}.repeat must be an object.");
        }
        if (body["loop"] is { } loop && loop.Type != JTokenType.Boolean)
        {
            throw new FormatException($"{file}: {path}.loop must be true or false.");
        }

        return new SpriteClip(
            ParseFrames(body["frames"], file, $"{path}.frames", required: repeat is null),
            repeat,
            ParseFrames(body["end"], file, $"{path}.end", required: false),
            body["loop"]?.Value<bool>() ?? false);
    }

    private static IReadOnlyList<ClipFrame> ParseFrames(JToken? token, string file, string path, bool required)
    {
        if (token is null && !required)
        {
            return Array.Empty<ClipFrame>();
        }
        if (token is not JArray frames || (required && frames.Count == 0))
        {
            throw new FormatException($"{file}: {path} must be a {(required ? "non-empty " : "")}frame list.");
        }

        List<ClipFrame> result = new(frames.Count);
        for (int i = 0; i < frames.Count; i++)
        {
            result.Add(ParseFrame(frames[i], file, $"{path}[{i}]"));
        }
        return result;
    }

    private static ClipFrame ParseFrame(JToken token, string file, string path)
    {
        if (token is not JArray { Count: >= 2 } frame || frame[0].Type != JTokenType.Integer || frame[0].Value<int>() < 0)
        {
            throw new FormatException($"{file}: {path} must be [cell, ms, ...tags].");
        }

        string? eventName = null;
        bool flip = false;
        foreach (JToken tag in frame.Skip(2))
        {
            string? name = tag.Type == JTokenType.String ? tag.Value<string>() : null;
            if (string.IsNullOrEmpty(name))
            {
                throw new FormatException($"{file}: {path} tags must be non-empty strings.");
            }
            if (name == FlipTag)
            {
                flip = true;
            }
            else if (eventName is null)
            {
                eventName = name;
            }
            else
            {
                throw new FormatException($"{file}: {path} has more than one event.");
            }
        }
        return new ClipFrame(frame[0].Value<int>(), ParseRange(frame[1], file, path), eventName, flip);
    }

    private static StepRange ParseRange(JToken? token, string file, string path)
    {
        if (token?.Type == JTokenType.Integer && token.Value<int>() >= 0)
        {
            int value = token.Value<int>();
            return new StepRange(value, value, 1);
        }
        if (token is JArray { Count: 2 or 3 } range && range.All(item => item.Type == JTokenType.Integer))
        {
            int step = range.Count == 3 ? range[2].Value<int>() : 1;
            int min = range[0].Value<int>();
            int max = range[1].Value<int>();
            if (step >= 1 && min >= 0 && max >= min)
            {
                return new StepRange(min, max, step);
            }
        }
        throw new FormatException($"{file}: {path} must be an integer or [min, max] / [min, max, step].");
    }

    private static void RejectUnknown(JObject body, HashSet<string> allowed, string file, string path)
    {
        foreach (JProperty property in body.Properties().Where(property => !allowed.Contains(property.Name)))
        {
            throw new FormatException($"{file}: {path}.{property.Name} is not a known field.");
        }
    }
}
