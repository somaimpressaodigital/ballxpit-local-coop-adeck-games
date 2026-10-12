# Loader verification and security reports

`winhttp.dll` is the Unity Doorstop loader distributed with BepInEx. This installation uses it to start BepInEx when the game launches; removing it prevents this loader configuration from starting the mod. It is not the Windows System32 DLL. [BepInEx maintainer explanation](https://github.com/BepInEx/BepInEx/discussions/1172).

The bundled loader was verified byte-for-byte against the original framework ZIP:

```text
SHA-256: 8C6CDBC38836DEE87E3368F5DE1994D7C0CCEBF29E4CE7ABA3C0981F9375412C
```

The project owner reported one VirusTotal engine detection for this DLL (1/71). A local Microsoft Defender custom scan on October 12, 2026 completed without a matching threat record. These results are not a guarantee of safety or a replacement for your own verification. [VirusTotal file report](https://www.virustotal.com/gui/file/8c6cdbc38836dee87e3368f5de1994d7c0ccebf29e4ce7aba3c0981f9375412c).

No installer, antivirus exception, protection-disabling command, or bypass is included. Keep security protections enabled and verify checksums/provenance before installing. Report reproducible security concerns through this repository's issues without including credentials or private saves.

