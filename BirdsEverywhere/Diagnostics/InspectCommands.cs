using BirdsEverywhere.BirdTypes;
using BirdsEverywhere.Diagnostics;
using BirdsEverywhere.Domain;
using BirdsEverywhere.Spawners;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.TerrainFeatures;

namespace BirdsEverywhere;

public partial class ModEntry
{
    private const string CheckUsage = "Usage: birds_check <birdId> [<ruleId>] [-l <location>]";

    private void RegisterInspectCommands(IModHelper helper)
    {
        var commands = helper.ConsoleCommands;
        commands.Add("birds_rules", "Show a bird's rules with today's eligibility: birds_rules <birdId>", ShowRules);
        commands.Add("birds_check", "Check why a rule can or cannot spawn: " + CheckUsage, CheckRule);
        commands.Add("birds_live", "List live birds in this location.", ShowLiveBirds);
        helper.Events.Display.RenderedWorld += DrawDebugOverlay;
    }

    private bool TryGetBird(string id, out BirdDefinition bird)
    {
        if (_catalog.TryGetValue(id, out BirdDefinition? found))
        {
            bird = found;
            return true;
        }
        Monitor.Log($"Unknown bird '{id}'.", LogLevel.Warn);
        bird = null!;
        return false;
    }

    private bool IsEligibleToday(SpawnRule rule) =>
        rule.When.MatchesDay(Enum.Parse<Domain.Season>(Game1.currentSeason, true), CurrentWeather(), Game1.dayOfMonth);

    private void ShowRules(string command, string[] args)
    {
        if (!CheckDebugCommand())
        {
            return;
        }
        if (args.Length != 1)
        {
            Monitor.Log("Usage: birds_rules <birdId>", LogLevel.Warn);
            return;
        }
        if (!TryGetBird(args[0], out BirdDefinition bird))
        {
            return;
        }

        Region? region = _regions.FirstOrDefault(r => r.Species.Contains(bird.Id.Value));
        List<string> lines = new() { RuleText.Header(bird, bird.Rules.Count(IsEligibleToday), region?.Id ?? "none", region is null ? null : region.Species.ToList().IndexOf(bird.Id.Value)) };
        lines.AddRange(bird.Rules.SelectMany(rule => RuleText.Lines(rule, IsEligibleToday(rule))));
        Monitor.Log(string.Join(Environment.NewLine, lines), LogLevel.Info);
    }

    private void CheckRule(string command, string[] args)
    {
        if (!CheckDebugCommand(host: true))
        {
            return;
        }
        if (!BirdSelector.TryParse(args, out BirdSelector? selector, out _) || selector!.Force)
        {
            Monitor.Log(CheckUsage, LogLevel.Warn);
            return;
        }
        if (!TryGetBird(selector.BirdId, out BirdDefinition bird))
        {
            return;
        }

        List<SpawnRule> rules = bird.Rules.Where(rule => selector.RuleId is null || rule.Id == selector.RuleId).ToList();
        if (rules.Count == 0)
        {
            Monitor.Log($"Bird '{bird.Id.Value}' has no rule '{selector.RuleId}'.", LogLevel.Warn);
            return;
        }

        List<string> lines = new();
        foreach (SpawnRule rule in rules)
        {
            lines.Add($"{(IsEligibleToday(rule) ? "✅" : "❌")} {rule.Id}: {(IsEligibleToday(rule) ? "eligible today" : "not eligible today (season/weather/day)")}");
            if (!_spawnPatterns.TryCreate(rule.SpawnPattern, out Spawner? spawner))
            {
                lines.Add($"   ❌ unknown pattern '{rule.SpawnPattern.Value}'");
                continue;
            }
            IEnumerable<Habitat> habitats = rule.Locations.Select(Habitat.Parse)
                .Where(habitat => selector.Location is null || habitat.Location == selector.Location);
            foreach (Habitat habitat in habitats)
            {
                lines.Add("   " + CheckHabitat(bird, rule, spawner, habitat));
            }
        }
        Monitor.Log(string.Join(Environment.NewLine, lines), LogLevel.Info);
    }

    private string CheckHabitat(BirdDefinition bird, SpawnRule rule, Spawner spawner, Habitat habitat)
    {
        if (!GameHabitat.Exists(habitat))
        {
            return $"❌ {habitat.Key}: location, farm type, or fish area not in this save";
        }

        GameLocation location = Game1.getLocationFromName(habitat.Location);
        string candidates = spawner switch
        {
            TileSpawner tiles => $"{CountTiles(location, tiles.Restrict(rule, habitat.FishArea))} candidate tiles",
            TreeTrunkSpawner => $"{Enumerable.Range(0, location.terrainFeatures.Count()).Count(index => Condition.IsEligibleTree(location, index))} eligible trees",
            BushSpawner => $"{location.largeTerrainFeatures.Count(feature => feature is Bush)} bushes (needs a free straight path 5-11 tiles beside)",
            _ => "unknown candidates",
        };
        int dryRun = spawner.SpawnBirds(location, bird, rule, habitat.FishArea, new Random(), Monitor).Count;
        return $"{(dryRun > 0 ? "✅" : "❌")} {habitat.Key}: {candidates}; dry run placed {dryRun} birds";
    }

    private static int CountTiles(GameLocation location, Condition.TileSpawnCondition condition)
    {
        GameLocationQuery query = new(location);
        int count = 0;
        for (int x = 0; x < query.MapSize.X; x++)
        {
            for (int y = 0; y < query.MapSize.Y; y++)
            {
                if (condition(query, new Vector2(x, y)))
                {
                    count++;
                }
            }
        }
        return count;
    }

    private void ShowLiveBirds(string command, string[] args)
    {
        if (!CheckDebugCommand())
        {
            return;
        }
        GameLocation location = Game1.currentLocation;
        List<BirdCritter> birds = location.critters?.OfType<BirdCritter>().ToList() ?? new List<BirdCritter>();
        if (birds.Count == 0)
        {
            Monitor.Log($"No live birds in {location.Name}.", LogLevel.Info);
            return;
        }

        _plan.Locations.TryGetValue(location.Name, out IReadOnlyList<BirdPlacement>? placements);
        GameLocationQuery query = new(location);
        IEnumerable<string> lines = birds.Select(bird =>
        {
            Vector2 tile = new((int)(bird.position.X / 64f), (int)(bird.position.Y / 64f));
            string surface = query.IsWaterTile(tile) ? "water" : "ground";
            string anchor = bird switch
            {
                TreeTrunkBird tree => $" tree #{tree.Index}",
                BushBird bush => $" bush #{bush.Index}",
                _ => "",
            };
            BirdPlacement? placement = placements?.FirstOrDefault(p => p.Seed == bird.BirdID);
            string rule = placement is null ? "not in plan" : $"rule {placement.RuleId} via {placement.SpawnPattern}";
            return $"{bird.BirdName} [{bird.BirdTypeName}] tex {bird.Texture} at ({(int)tile.X}, {(int)tile.Y}) on {surface}, {bird.State}{anchor}, {rule}";
        });
        Monitor.Log(string.Join(Environment.NewLine, lines), LogLevel.Info);
    }

    private void DrawDebugOverlay(object? sender, RenderedWorldEventArgs e)
    {
        if (!_config.Debug || !Context.IsWorldReady || Game1.currentLocation?.critters is not { } critters)
        {
            return;
        }
        foreach (BirdCritter bird in critters.OfType<BirdCritter>())
        {
            Vector2 screen = Game1.GlobalToLocal(Game1.viewport, bird.position + new Vector2(-32f, -112f + bird.yJumpOffset + bird.yOffset));
            Utility.drawTextWithShadow(e.SpriteBatch, $"{bird.BirdName} / {bird.State}", Game1.smallFont, screen, Color.White);
        }
    }
}
