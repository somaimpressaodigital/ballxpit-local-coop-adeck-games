# Third-party components

| Component | Included material | Source / attribution |
| --- | --- | --- |
| BepInEx IL2CPP 6.0.0-be.788 | Unmodified upstream framework archive | [Builds](https://builds.bepinex.dev/projects/bepinex_be), [corresponding source revision](https://github.com/BepInEx/BepInEx/tree/5b766a3b7f6c164d4798924a93f3acf4db769d06) |
| BALLxPIT Local Coop 0.1.0 | Unmodified original DLL, redistributed with author permission confirmed by the project owner | [sparrowsj / Nexus Mods](https://www.nexusmods.com/ballxpit/mods/24) |
| .NET 6.0.7 | Runtime included in the upstream framework archive | [Source](https://github.com/dotnet/runtime/tree/v6.0.7), license and third-party notices in `vendor/licenses/` |
| Unity Doorstop | `winhttp.dll` included in the upstream framework archive | [Source and license](https://github.com/NeighTools/UnityDoorstop) |
| Il2CppInterop / Harmony / MonoMod | Framework libraries included in the upstream archive | [Il2CppInterop](https://github.com/BepInEx/Il2CppInterop), [HarmonyX](https://github.com/BepInEx/HarmonyX), [MonoMod](https://github.com/MonoMod/MonoMod) |

BepInEx's LGPL 2.1 license and .NET's license/notices are preserved in `vendor/licenses/` and in complete release packages. The upstream framework archive is kept intact; its checksum is in `vendor/framework.sha256`.

No game executable, game assets, `GameAssembly.dll`, or generated game interop assemblies are distributed. Users generate those reference assemblies from their own game installation.

Redistribution permission for the original base is specific to its author's authorization; this repository does not grant additional third-party rights.

