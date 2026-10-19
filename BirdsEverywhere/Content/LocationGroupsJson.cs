namespace BirdsEverywhere.Content;

public sealed record LocationGroupsJson(IReadOnlyDictionary<string, IReadOnlyList<string>> Groups);