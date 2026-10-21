using BirdsEverywhere.Domain;
using Newtonsoft.Json.Linq;

namespace BirdsEverywhere.Content;

/// <summary>Shared composition files rules can reference or extend. Action sets stay raw so a rule can override single fields.</summary>
public sealed record SharedDefinitions(IReadOnlyDictionary<string, Routine> Routines, IReadOnlyDictionary<string, JObject> ActionSets)
{
    public static SharedDefinitions Load(string root, out IReadOnlyList<ValidationIssue> issues)
    {
        (IReadOnlyDictionary<string, Routine> routines, IReadOnlyList<ValidationIssue> routineIssues) = RoutineLoader.LoadAll(root);
        (IReadOnlyDictionary<string, JObject> actions, IReadOnlyList<ValidationIssue> actionIssues) = ActionLoader.LoadAll(root);
        issues = routineIssues.Concat(actionIssues).ToList();
        return new SharedDefinitions(routines, actions);
    }

    /// <summary>Parsed action sets for runtime fallbacks (birds without a composition).</summary>
    public IReadOnlyDictionary<string, IReadOnlyDictionary<string, ActionDefinition>> ParsedActions() =>
        ActionSets.ToDictionary(set => set.Key, set => ActionLoader.ParseSet(set.Value, set.Key), StringComparer.Ordinal);
}
