namespace Birding.Spawners;

/// <summary>What one populate pass does with one planned bird.</summary>
public enum PopulateAction
{
    Skip,
    Spawn,
    Return,
    Arrive,
}

public static class PopulateDecision
{
    /// <summary>A bird already spawned this visit is skipped, unless it has since left the location (departed): then it may come back once its delay passed, on a new visit at its tile and during a visit by flying in.</summary>
    public static PopulateAction For(bool active, bool inSpawned, bool departed, bool canReturn, bool visitStart, bool hasLiveBird)
    {
        if (!active || hasLiveBird)
        {
            return PopulateAction.Skip;
        }
        if (departed)
        {
            return !canReturn ? PopulateAction.Skip : visitStart ? PopulateAction.Return : PopulateAction.Arrive;
        }
        return inSpawned ? PopulateAction.Skip : PopulateAction.Spawn;
    }
}
