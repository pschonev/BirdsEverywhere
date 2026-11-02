# AGENTS.md

## Toolchain
- .NET SDK 10.0.x (pinned via `global.json`, `rollForward: latestFeature`).
- Mod: `net6.0` (game runtime). Tests: `net10.0`, NUnit 5, Microsoft.NET.Test.Sdk 18.
- Stardew Valley 1.6.15, SMAPI 4.5.2, Pathoschild.Stardew.ModBuildConfig 4.4.0.
- Version lives only in `Birding/Birding.csproj` (`<Version>`); `manifest.json` uses `%ProjectVersion%`.
- Newtonsoft.Json and SMAPI.Toolkit are referenced from `$(GamePath)/smapi-internal` (ModBuildConfig 4.x no longer adds Newtonsoft).

## Coding standards
Follow the global C# standards (`~/.copilot/instructions/csharp.instructions.md`) plus [.github/instructions/birding.instructions.md](.github/instructions/birding.instructions.md). Existing code is legacy and not a style reference. Target design: [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md); migration order and known defects: [docs/REFACTOR.md](docs/REFACTOR.md). Analyzer and style settings live in `Directory.Build.props` and `.editorconfig`; Release builds treat warnings as errors.

## Game path
ModBuildConfig auto-detects the Steam install. To override, create an uncommitted `~/stardewvalley.targets`:
```xml
<Project>
  <PropertyGroup>
    <GamePath>/path/to/Stardew Valley/Contents/MacOS</GamePath>
  </PropertyGroup>
</Project>
```
Never commit machine-specific paths.

## Commands
```sh
dotnet restore Birding.sln
dotnet build Birding.sln                       # also deploys to $(GamePath)/Mods/Birding and zips
dotnet build Birding/Birding.csproj -p:EnableModDeploy=false -p:EnableModZip=false   # compile only
dotnet test Birding.sln                        # see Apple Silicon note
```

## Apple Silicon
Game/SMAPI assemblies are x64-only. Tests that touch game or SMAPI types (`SaveDataTests`, `JSONConverterTests`) fail on an arm64 test host with "assembly architecture is not compatible". Install the x64 .NET 10 SDK (lands in `/usr/local/share/dotnet/x64`), then:
```sh
dotnet test Birding.sln --arch x64
```
Pure-logic tests (`BirdDataTests`, `ClampTests`, `ReflectionTests`) run on arm64.

## Test map
Each rule is asserted in one layer only: schemas own JSON shape, loaders/`ContentValidator` own semantics, so don't add a schema row and a validator row for the same rule.
- `SchemaTests`: shipped assets match schemas; schema and loader agree on routine/action/reaction/layout; schema enums match code.
- `ConditionTests`, `TemplateMergerTests`, `CompositionTests`, `ContentTests`, `LayoutSheetTests`: semantics of `when`, template merging, composition resolution, validation messages, shipped content.
- `SpawningTests`, `FollowSlotTests`, `BirdBehaviorTests`, `AnimationCharacterizationTests`: spawn planning and determinism, group placement, bird decisions, a few RNG/animation goldens (update on purpose only).
- `GameTypeRegressionTests`, `SaveDataTests`, `ObservationLogTests`, `BirdingProgressTests`, `RandomTests`: snapshots, save format, multiplayer messages, progress.

## Verification
Keep compile, test, deploy, and in-game checks separate. Only in-game testing (launch via SMAPI) verifies runtime behavior; never claim it from build/test results.
