namespace BirdsEverywhere.Diagnostics;

public sealed record BirdSelector(string BirdId, string? RuleId, string? Location, bool Force)
{
    public static bool TryParse(IReadOnlyList<string> args, out BirdSelector? selector, out string error)
    {
        selector = null;
        error = "Usage: birds_spawn <birdId> [<ruleId>] [-l <location>] [-f]";
        string? birdId = null;
        string? ruleId = null;
        string? location = null;
        bool force = false;

        for (int index = 0; index < args.Count; index++)
        {
            string arg = args[index];
            if (arg == "-l" && index + 1 < args.Count && !args[index + 1].StartsWith('-'))
            {
                if (location is not null)
                {
                    return false;
                }
                location = args[++index];
            }
            else if (arg == "-f" && !force)
            {
                force = true;
            }
            else if (arg.StartsWith('-'))
            {
                return false;
            }
            else if (birdId is null)
            {
                birdId = arg;
            }
            else if (ruleId is null)
            {
                ruleId = arg;
            }
            else
            {
                return false;
            }
        }

        if (birdId is null)
        {
            return false;
        }
        selector = new BirdSelector(birdId, ruleId, location, force);
        error = "";
        return true;
    }
}