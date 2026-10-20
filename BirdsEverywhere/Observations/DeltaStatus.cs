namespace BirdsEverywhere.Observations;

/// <summary>How an incoming observation delta relates to the local log version.</summary>
public enum DeltaStatus
{
    Duplicate,
    Next,
    Gap,
}
