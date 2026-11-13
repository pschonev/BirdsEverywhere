---
name: Birding C#
description: Project-specific C# rules for the Birding SMAPI mod (net6.0 / C# 10).
applyTo: "**/*.cs"
---
# Birding

Extends the global C# standards. Existing code is legacy beginner code. Never copy its patterns; new and touched code follows the standards. Target design: [docs/ARCHITECTURE.md](../../docs/ARCHITECTURE.md); migration order: [docs/REFACTOR.md](../../docs/REFACTOR.md).

## Platform limits

- Mod targets net6.0 with C# 10: no `required`, raw string literals, list patterns, generic math, primary constructors on classes, or `TimeProvider`.
- No `async` in game code; SMAPI events and game loop are synchronous.
- JSON uses Newtonsoft.Json (provided by SMAPI), not `System.Text.Json`.

## Game types

- Mutable classes are allowed where the game forces them (subclasses of `Critter`, `TerrainFeature`, `Mod`); behavior may live on them where the game's override model requires it.
- At most one mod-owned abstract layer below a game type: `Critter` → abstract bird base → sealed concrete bird.
- Avoid LINQ and allocations in per-tick update and draw paths.

## SMAPI boundary

- `ModEntry` is the only composition root: build feature objects in `Entry`, subscribe events, delegate. Keep it thin.
- No static mod-instance singleton. Pass `IMonitor` and `IModHelper` explicitly.
- Log via `IMonitor` with the appropriate `LogLevel`; never per tick.
- Prefer `Helper.Data`, `Helper.ModContent`, `Helper.Multiplayer`, and content events over raw I/O. Harmony only as a last resort.
- Build paths with `Path.Combine` or SMAPI `PathUtilities.NormalizePath`.
- Resolve optional mod integrations with `Helper.ModRegistry.GetApi<T>()` in `GameLaunched`; treat null as "not installed".
- Multiplayer messages and save data are versioned, serializable records.
- Domain logic (bird data, spawn rules, conditions, selection) must not reference `Game1`, SMAPI, Newtonsoft, or MonoGame where avoidable. Seed `Random` from game state at the boundary.

## Tests

- True end-to-end is manual in-game testing via SMAPI. Prefer feature-level tests over real JSON fixtures.
- Keep domain logic game-free so its tests run on arm64 (see AGENTS.md).
- NUnit: use `[TestCase]` / `TestCaseSource` tables.
- Game types are concrete and mostly non-virtual; isolate them behind small mod-owned interfaces with hand-written fakes instead of a mocking library.

## Reference patterns

- JSON model `record BirdJson(string? Id, ...)` → `static BirdData ToDomain(BirdJson json, string file)` that throws with file and field on invalid data; the rest of the code only sees `BirdData`.
- Pure `static IReadOnlyList<Spawn> SelectSpawns(Random random, SpawnRules rules, LocationInfo location)`, called from an event handler that gathers game state, logs, and applies results.
