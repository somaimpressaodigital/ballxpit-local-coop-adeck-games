# BALL x PIT Local Co-op — Adeck Games

Two-player local co-op companion mod by **Extermineg — Adeck Games**.

This repository contains the companion source code, build scripts, and the two required bases: BepInEx IL2CPP and the original BALLxPIT Local Coop plugin. The game and generated game assemblies are not included.

## Features

- Start **Local co-op** from the main menu and play without city management.
- Independent character selection, equipment, health, upgrades, and permanent character progression.
- Shared character and stage unlocks with visible challenge requirements.
- Individual HUD panels and player markers in pink and cyan.
- One automatic revival per player per run, costing half of the surviving partner's current health.
- Upgrade and fusion descriptions, character advantages/disadvantages, and run statistics.
- Controller assignment and reconnection handling for Rewired-compatible Xbox/PS5 controllers.
- Automatic language: **Portuguese when the game is in Portuguese; English for every other game language**.

## Install a release

1. Download `ballxpit-coop-2.1.10-complete.zip` from [Releases](https://github.com/somaimpressaodigital/ballxpit-local-coop-adeck-games/releases).
2. Close BALL x PIT. Back up existing mod configurations before updating.
3. Extract the **contents** directly into the folder containing `Balls.exe`. `BepInEx`, `dotnet`, `winhttp.dll`, and `doorstop_config.ini` belong next to the executable.
4. Launch the game. The first launch may take time and require internet access while BepInEx downloads Unity libraries and generates interop assemblies.
5. Connect two controllers and select **Local co-op**.

No separate installer or administrator launch is required by default. Do not extract into an extra folder named after the ZIP.

The smaller `companion` archive only contains the Adeck Games plugins and their configuration; it requires both bases to be installed already.

## Build from source

See [BUILDING.md](BUILDING.md) for prerequisites, dependency installation, compilation, tests, and packaging.

```powershell
.\InstallDependencies.ps1 -GamePath "D:\Games\BALL x PIT"
# Launch the game once to generate BepInEx/interop, then close it.
.\Build.ps1 -GamePath "D:\Games\BALL x PIT"
.\Package.ps1 -Complete
```

## Compatibility and validation

Baseline: Windows x64, BALL x PIT using Unity **6000.0.50f1**, BepInEx IL2CPP **6.0.0-be.788**, and BALLxPIT Local Coop **0.1.0**. The companion is **2.1.10**; the balance plugin is **1.0.1**. Other game or base versions may require changes because the companion integrates with native game methods and private base-plugin fields.

Version 2.1.10 fixes the extra reload wait, isolates solo play, and resets co-op state between runs. See [CHANGELOG.md](CHANGELOG.md) for the changes and [VALIDATION.md](VALIDATION.md) for the test evidence and its limits. Automated rule tests and native scenario fixtures are useful regression checks, not a substitute for real physical-controller, boss, and full-run playtesting.

Co-op progression is stored separately in `%LOCALAPPDATA%\BALLxPITCoop`. Do not commit or distribute personal saves. Read [SECURITY.md](SECURITY.md) for the loader-file antivirus report and its verification limits.

## Credits and dependencies

[CREDITS.md](CREDITS.md) and [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md)



