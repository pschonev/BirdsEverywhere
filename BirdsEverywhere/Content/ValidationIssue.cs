namespace BirdsEverywhere.Content;

public enum IssueSeverity
{
    Error,
    Warn,
    Info,
}

public sealed record ValidationIssue(IssueSeverity Severity, string File, string Id, string Path, string Chain, string Message)
{
    public override string ToString() => $"{Severity}: {File}: {Path} ({Id}; {Chain}): {Message}";
}