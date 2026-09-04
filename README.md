# StartAllBack Patcher

Patches `StartAllBackX64.dll` to bypass the license check. Can also restore from backup if you want to undo it.

Runs on Windows, requires admin (UAC prompt will appear if you're not already elevated).

---

## Building

```
dotnet publish -c Release -r win-x64
```

Output ends up in `bin/Release/net8.0-windows/win-x64/publish/SynezSAB.exe` — single file, no .NET install needed on the target machine.

---

## Usage

**Patch:**
```
SynezSAB.exe --a
```
Resets the trial reminder in the registry, checks the DLL, creates a timestamped backup, kills the shell processes that hold the file, patches it, and restarts explorer.

**Restore:**
```
SynezSAB.exe --r
```
Finds the most recent `.bak` next to the DLL and restores it. You can also point it at a specific backup:
```
SynezSAB.exe --r "C:\Program Files\StartAllBack\StartAllBackX64.dll.2026-09-04_18-31-00.bak"
```

---

## Supported versions

| Version | Status |
|---------|--------|
| 3.5.5 – 3.5.7 | hash-based patch |
| v3.6.0 – v3.6.5 | hash-based patch |
| anything else | footprint scan fallback |

If your version isn't in the list, the patcher will try to find the license functions by byte pattern and patch them directly. It'll tell you during checkup whether it found them or not.

---

## Notes

- The patcher kills `explorer.exe` and any other shell process that has the DLL open, writes the file, then restarts explorer. Your desktop will flicker for a second.
- Inspired by [PyPass-SAB](https://github.com/GuillaumeMCK/PyPass-SAB).
