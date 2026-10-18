# AGENTS.md

## Toolchain
- .NET SDK 10.0.x (pinned via `global.json`, `rollForward: latestFeature`).
- Mod: `net6.0` (game runtime). Tests: `net10.0`, NUnit 5, Microsoft.NET.Test.Sdk 18.
- Stardew Valley 1.6.15, SMAPI 4.5.2, Pathoschild.Stardew.ModBuildConfig 4.4.0.
- Version lives only in `BirdsEverywhere/BirdsEverywhere.csproj` (`<Version>`); `manifest.json` uses `%ProjectVersion%`.
- Newtonsoft.Json and SMAPI.Toolkit are referenced from `$(GamePath)/smapi-internal` (ModBuildConfig 4.x no longer adds Newtonsoft).

## Coding standards
Follow the global C# standards (`~/.copilot/instructions/csharp.instructions.md`) plus [.github/instructions/birdseverywhere.instructions.md](.github/instructions/birdseverywhere.instructions.md). Existing code is legacy and not a style reference. Target design: [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md); migration order and known defects: [docs/REFACTOR.md](docs/REFACTOR.md). Analyzer and style settings live in `Directory.Build.props` and `.editorconfig`; Release builds treat warnings as errors.

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
dotnet restore BirdsEverywhere.sln
dotnet build BirdsEverywhere.sln                       # also deploys to $(GamePath)/Mods/BirdsEverywhere and zips
dotnet build BirdsEverywhere/BirdsEverywhere.csproj -p:EnableModDeploy=false -p:EnableModZip=false   # compile only
dotnet test BirdsEverywhere.sln                        # see Apple Silicon note
```

## Apple Silicon
Game/SMAPI assemblies are x64-only. Tests that touch game or SMAPI types (`SaveDataTests`, `JSONConverterTests`) fail on an arm64 test host with "assembly architecture is not compatible". Install the x64 .NET 10 SDK (lands in `/usr/local/share/dotnet/x64`), then:
```sh
dotnet test BirdsEverywhere.sln --arch x64
```
Pure-logic tests (`BirdDataTests`, `ClampTests`, `ReflectionTests`) run on arm64.

## Verification
Keep compile, test, deploy, and in-game checks separate. Only in-game testing (launch via SMAPI) verifies runtime behavior; never claim it from build/test results.
