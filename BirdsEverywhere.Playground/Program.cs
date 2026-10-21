using BirdsEverywhere.Content;
using BirdsEverywhere.Diagnostics;
using BirdsEverywhere.Domain;
using BirdsEverywhere.Spawners;
using System.Text.RegularExpressions;

namespace BirdsEverywhere.Playground;

internal sealed record PlaygroundState(Season Season, Weather Weather, int Day, double Luck, int Seed,
    string Farm, int Level, IReadOnlySet<string> Seen);

internal static class Program
{
    private static void Main()
    {
        string assets = Path.Combine(AppContext.BaseDirectory, "assets");
        string? Read(string path) => File.Exists(path) ? File.ReadAllText(path) : null;
        (IReadOnlyList<Region> regions, _) = ContentLoader.LoadRegions(assets, Array.Empty<string>(), Read);
        IReadOnlyDictionary<string, IReadOnlyList<string>> groups = Region.AllGroups(regions);
        string[] patternNames = { "GroundSpawner", "WaterSpawner", "WaterOrGroundSpawner", "SpawnableGroundSpawner", "TreeTrunkSpawner", "BushSpawner" };
        SpawnPatternId[] patterns = patternNames.Select(id => new SpawnPatternId(id)).ToArray();
        string[] typeNames = { "LandBird", "WaterLandBird", "TreeTrunkBird", "BushBird" };
        BirdTypeId[] types = typeNames.Select(id => new BirdTypeId(id)).ToArray();
        SharedDefinitions definitions = SharedDefinitions.Load(assets, out _);
        Dictionary<string, BirdDefinition> catalog = new(StringComparer.Ordinal);
        foreach (string id in ContentPaths.BirdIds(assets))
        {
            (BirdDefinition? bird, IReadOnlyList<ValidationIssue> issues, _, _) = ContentLoader.LoadBird(assets, id, groups, patterns, types, definitions, Read, File.Exists);
            foreach (ValidationIssue issue in issues.Where(issue => issue.Severity == IssueSeverity.Error))
            {
                Console.Error.WriteLine(issue);
            }
            if (bird is not null)
            {
                catalog[id] = bird;
            }
        }

        PlaygroundState state = new(Season.Spring, Weather.Sun, 1, 0, 1, "Standard", 0, new HashSet<string>());
        Console.WriteLine($"Birds Everywhere | Spawn playground\nLoaded {catalog.Count} birds. Type 'help' for commands.");
        while (true)
        {
            Console.Write("> ");
            string? input = Console.ReadLine();
            if (input is null || input.Trim() is "quit" or "exit")
            {
                return;
            }
            string[] args = Regex.Matches(input, "\"([^\"]*)\"|\\S+")
                .Select(match => match.Groups[1].Success ? match.Groups[1].Value : match.Value).ToArray();
            if (args.Length == 0)
            {
                continue;
            }

            switch (args[0])
            {
                case "help" when args.Length == 1:
                    PrintHelp();
                    break;
                case "state":
                    Console.WriteLine($"Level={state.Level} Season={state.Season} Day={state.Day} Weather={state.Weather} Luck={state.Luck} Seed={state.Seed} Farm={state.Farm} Seen=[{string.Join(", ", state.Seen.OrderBy(id => id, StringComparer.Ordinal))}]");
                    break;
                case "set" when args.Length == 3:
                    if (!TrySet(state, args[1], args[2], out PlaygroundState updated))
                    {
                        Console.WriteLine("Invalid setting or value.");
                    }
                    else
                    {
                        state = updated;
                    }
                    break;
                case "observe" when args.Length == 2 && BirdLookup.FindMatches(catalog.Values, args[1]) is { Count: 1 } observed:
                    state = state with { Seen = state.Seen.Append(observed[0].Id.Value).ToHashSet(StringComparer.Ordinal) };
                    break;
                case "next":
                    state = state with
                    {
                        Day = state.Day == 28 ? 1 : state.Day + 1,
                        Season = state.Day == 28 ? (Season)(((int)state.Season + 1) % 4) : state.Season,
                        Seed = state.Seed + 1
                    };
                    break;
                case "plan":
                    List<SpawnAssignment> planned = Simulate(regions, catalog, state, state.Seed).ToList();
                    foreach (bool unseen in new[] { true, false })
                    {
                        Console.WriteLine(unseen ? "Unseen:" : "Seen:");
                        foreach (SpawnAssignment assignment in planned.Where(assignment => assignment.Unseen == unseen))
                        {
                            Console.WriteLine("  " + Describe(assignment));
                        }
                    }
                    break;
                case "weights" when args.Length == 2 && regions.FirstOrDefault(r => r.Id == args[1]) is { } weighted:
                    foreach (BirdWeight weight in SpawnPlanner.Weights(weighted, catalog.Values, state.Season, state.Weather, state.Day, state.Luck, state.Level))
                    {
                        Console.WriteLine($"{weight.Rank + 1}. {weight.Bird.Id.Value}: {weight.Weight:F3} ({(state.Seen.Contains(weight.Bird.Id.Value) ? "seen" : "unseen")})");
                    }
                    Console.WriteLine($"exponent {SpawnPlanner.Exponent(state.Luck, state.Level):F2}");
                    break;
                case "rules" when args.Length == 2 && BirdLookup.FindMatches(catalog.Values, args[1]) is { Count: 1 } found:
                    BirdDefinition ruleBird = found[0];
                    Console.WriteLine(RuleText.Header(ruleBird, ruleBird.EligibleRules(state.Season, state.Weather, state.Day).Count));
                    foreach (SpawnRule rule in ruleBird.Rules)
                    {
                        foreach (string line in RuleText.Lines(rule, rule.When.MatchesDay(state.Season, state.Weather, state.Day)))
                        {
                            Console.WriteLine(line);
                        }
                    }
                    break;
                case "layout":
                    ShowLayout(assets, args.Skip(1).ToArray());
                    break;
                case "odds":
                    ShowOdds(regions, catalog, state, args.Skip(1).ToArray());
                    break;
                default:
                    Console.WriteLine("Unknown command or bird. Type 'help' for commands and examples.");
                    break;
            }
        }
    }

    private static void PrintHelp()
    {
        Console.WriteLine(@"
    Commands
      help                         Show this guide. Example: help
      state                        Show settings and seen birds. Example: state
      set <setting> <value>        Change season, weather, day, luck, level, seed, or farm.
                                   Examples: set season summer
                                             set weather rain
                                             set day 14
                                             set luck 0.05
                                             set level 3
                                             set seed 42
                                             set farm Standard
      weights <region>             Show species weights today. Example: weights valley
      plan                         Simulate a day, split into unseen and seen birds. Example: plan
      observe <bird>               Mark a bird as seen. Example: observe great_tit
      next                         Advance day and seed; roll over the season. Example: next
      rules <bird>                 Show each rule: ✅/❌ today, conditions, count bounds,
                                   start state. Example: rules great_tit
      odds [bird [rule] [-l location]] [-n samples]
                                   Estimate odds across seeds (default: 1000).
                                   Examples: odds
                                             odds great_tit -n 500
                                             odds great_tit spring -l Forest -n 500
      layout [id] [cols <n>] [svg <path>]
                                   List layouts, or print a layout's cell -> pose reference and
                                   optionally write a numbered SVG template. cols = cells per row
                                   of the final sheet (default: all in one row).
                                   Examples: layout
                                             layout land_32
                                             layout land_32 cols 9 svg land_32.svg
      quit                         Leave (also: exit). Example: quit

    A <bird> is its id, English name, scientific name or shorthand (ignores case, spaces, underscores).
    Quote names with spaces. Examples: observe gretit1
                                       rules ""Great Tit""
                                       odds ""Parus major""

    Odds assume every tile is feasible; they are upper bounds, not in-game spawn rates.");
    }

    private static void ShowLayout(string assets, string[] args)
    {
        const string usage = "Usage: layout [id] [cols <n>] [svg <path>]";
        string dir = ContentPaths.LayoutsDirectory(assets);
        if (args.Length == 0)
        {
            Console.WriteLine(Directory.Exists(dir) ? string.Join(", ", Directory.GetFiles(dir, "*.json").Select(Path.GetFileNameWithoutExtension).Order()) : "No layouts.");
            return;
        }

        string id = args[0];
        int? cols = null;
        string? svgPath = null;
        for (int i = 1; i < args.Length; i += 2)
        {
            if (i + 1 >= args.Length)
            {
                Console.WriteLine(usage);
                return;
            }
            switch (args[i])
            {
                case "cols" when int.TryParse(args[i + 1], out int parsed) && parsed >= 1:
                    cols = parsed;
                    break;
                case "svg":
                    svgPath = args[i + 1];
                    break;
                default:
                    Console.WriteLine(usage);
                    return;
            }
        }

        string file = ContentPaths.Layout(assets, id);
        if (!File.Exists(file))
        {
            Console.WriteLine($"Unknown layout '{id}'.");
            return;
        }

        try
        {
            SpriteLayout layout = LayoutLoader.Parse(id, File.ReadAllText(file), file);
            int columns = cols ?? Math.Max(1, LayoutSheet.CellCount(layout));
            foreach (string line in LayoutSheet.Text(layout, columns))
            {
                Console.WriteLine(line);
            }
            if (svgPath is not null)
            {
                File.WriteAllText(svgPath, LayoutSheet.Svg(layout, columns));
                Console.WriteLine($"Wrote {svgPath}");
            }
        }
        catch (FormatException error)
        {
            Console.WriteLine(error.Message);
        }
    }

    private static bool TrySet(PlaygroundState state, string key, string value, out PlaygroundState updated)
    {
        updated = state;
        switch (key)
        {
            case "season" when Enum.TryParse(value, true, out Season season): updated = state with { Season = season }; return true;
            case "weather" when Enum.TryParse(value, true, out Weather weather): updated = state with { Weather = weather }; return true;
            case "day" when int.TryParse(value, out int day) && day is >= 1 and <= 28: updated = state with { Day = day }; return true;
            case "seed" when int.TryParse(value, out int seed): updated = state with { Seed = seed }; return true;
            case "luck" when double.TryParse(value, out double luck) && luck is >= -0.1 and <= 0.1: updated = state with { Luck = luck }; return true;
            case "level" when int.TryParse(value, out int level) && level is >= 0 and <= 20: updated = state with { Level = level }; return true;
            case "farm" when value.Length > 0: updated = state with { Farm = value }; return true;
            default: return false;
        }
    }

    private static IReadOnlyList<SpawnAssignment> Simulate(IReadOnlyList<Region> regions, IReadOnlyDictionary<string, BirdDefinition> catalog, PlaygroundState state, int seed) =>
        SpawnPlanner.Plan(regions, catalog, state.Seen, state.Season, state.Weather, state.Day, state.Luck, state.Level, new Random(seed),
            habitat => habitat.FarmType is null || habitat.FarmType.Equals(state.Farm, StringComparison.OrdinalIgnoreCase), _ => true, _ => 3000);

    private static string Describe(SpawnAssignment assignment) =>
        $"{assignment.Bird.Id.Value} / {assignment.Rule.Id} at {assignment.Habitat.Key}";

    private static void ShowOdds(IReadOnlyList<Region> regions, Dictionary<string, BirdDefinition> catalog, PlaygroundState state, string[] args)
    {
        int count = 1000;
        List<string> selectorArgs = args.ToList();
        int index = selectorArgs.IndexOf("-n");
        if (index >= 0)
        {
            if (index + 1 >= selectorArgs.Count || !int.TryParse(selectorArgs[index + 1], out count) || count < 1)
            {
                Console.WriteLine("Usage: odds [bird [rule] [-l location]] [-n positiveSamples]");
                return;
            }
            selectorArgs.RemoveRange(index, 2);
        }

        BirdSelector? selector = null;
        if (selectorArgs.Count > 0 && (!BirdSelector.TryParse(selectorArgs, out selector, out string error) || selector!.Force))
        {
            Console.WriteLine("Usage: odds [bird [rule] [-l location]] [-n positiveSamples]");
            return;
        }
        if (selector is not null)
        {
            IReadOnlyList<BirdDefinition> matches = BirdLookup.FindMatches(catalog.Values, selector.BirdId);
            if (matches.Count != 1)
            {
                Console.WriteLine(matches.Count == 0
                    ? $"Unknown bird '{selector.BirdId}'."
                    : $"Ambiguous bird '{selector.BirdId}'. Matches: {string.Join(", ", matches.Select(match => match.Id.Value))}.");
                return;
            }
            selector = selector with { BirdId = matches[0].Id.Value };
        }

        Dictionary<string, int> hits = new(StringComparer.Ordinal);
        for (int sample = 0; sample < count; sample++)
        {
            HashSet<string> sampleKeys = new(StringComparer.Ordinal);
            foreach (SpawnAssignment assignment in Simulate(regions, catalog, state, unchecked(state.Seed + sample))
                .Where(assignment => selector is null || assignment.Bird.Id.Value == selector.BirdId
                    && (selector.RuleId is null || assignment.Rule.Id == selector.RuleId)
                    && (selector.Location is null || assignment.Location == selector.Location)))
            {
                string key = (selector is null ? assignment.Bird.Id.Value : Describe(assignment)) + (assignment.Unseen ? " [unseen]" : " [seen]");
                if (sampleKeys.Add(key))
                {
                    hits[key] = hits.GetValueOrDefault(key) + 1;
                }
            }
        }

        Console.WriteLine($"Upper-bound odds (tiles assumed feasible), {count} seeds:");
        foreach ((string key, int hitsCount) in hits.OrderByDescending(hit => hit.Value).Take(selector is null ? 10 : int.MaxValue))
        {
            Console.WriteLine($"{key}: {100.0 * hitsCount / count:F1}%");
        }
    }
}