# TimberPerf

Faster Timberborn: big settlements tick faster, saves load sooner, and forests draw as fast as empty land.

It's a mod of its own, and other mods can ship it inside their folder too: BeaverBuddies Co-Op/PvP Edition and Archipelago do. The game loads every DLL in every enabled mod's folder, so with more than one of them on there's more than one copy. Only the newest version runs (the first loaded, when they're the same); the others log that they stand down and do nothing. Bump `<Version>` in `TimberPerf/TimberPerf.csproj` and in `TimberPerf/manifest.json` with every change, or an older build could be the one that runs.

## Building

`dotnet build -c Release` in `TimberPerf/` installs it into the Mods folder as the mod. `Tools/Package/package.sh` builds it and zips the installed folder to ~/Downloads. `Tools/make_cover.py` draws `Media/cover.png` and the thumbnail, with the brush strokes of Timber Empires' icons (a checkout of it next to this repo).

## Using it from a mod

Never reference `TimberPerf.dll`: the reference could bind to the copy that stood down. Copy `Client/TimberPerfBridge.cs` into the mod, change its namespace, and set or bind what's public in `Hooks`, `Probe`, `PerfCounters` and `Plants.TreePatches` from `StartMod` on.

To ship it, build it with `InstallMod=false` and put the DLL next to the mod's own before the mod installs itself, as `BuildTimberPerf` in BeaverBuddies' and Archipelago's csproj does. They look for this repo next to theirs (`../TimberPerf`), or at `TimberPerfProject`.

## License

GPL-3.0 (`License.txt`), like BeaverBuddies.

## Switches

A file in Documents/Timberborn named `TimberPerf-off` (or the older `BeaverBuddies-noperf`) turns the optimizations off. T3MP or Performance Plus being installed does the same. `BeaverBuddies-perfverify` checks the remembered answers against the game's own and logs any that differ.

Most of the optimizations are ported from T3MP (More More More Performance!) by toropippi, under the Apache License 2.0 (`LICENSE-T3MP.txt`).
