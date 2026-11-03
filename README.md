# TimberPerf

![TimberPerf](Media/cover.png)

[Steam Workshop](https://steamcommunity.com/sharedfiles/filedetails/?id=3832893232) · [mod.io](https://mod.io/g/timberborn/m/timber-perf)

Faster Timberborn: big settlements tick faster, saves load sooner, and forests draw as fast as empty land.

## Next to T3MP and Performance Plus

- **One mod.** Most of T3MP's optimizations and of the fixes Performance Plus lists, with no other mod needed.
- **Forests that draw like empty land.** Plants are merged into patches: a 512×512 forest goes from 11 to 107 fps.
- **Played in multiplayer.** BeaverBuddies Co-Op/PvP Edition ships it, and plays co-op and PvP with its desync checks on.
- **Ready for game updates.** The save-load optimizations switch off by themselves when an update changes the game's code they rely on, and the game starts as usual.
- **The FPS counter in the map editor.**

BeaverBuddies Co-Op/PvP Edition and Archipelago ship it inside their own folders. With more than one copy installed, only the newest runs: the others log that they stand down and do nothing.

## Numbers

Measured on a MacBook Pro (M1 Max, 32 GB), each case played with TimberPerf and then with it off (`TimberPerf-off`), Timber Empires disabled.

**A late-game colony**: Iron Teeth on a 256×150 map, with 213 beavers, 124 bots, about 650 buildings, 5,500 levees, platforms and other blocks, 3,300 paths, 4,500 trees and 1,900 crops. A minute at each speed, in a 1400×880 window:

| | Without | With | |
|---|---|---|---|
| Paused | 59 fps | 92 fps | +56% |
| Speed 1 | 42 fps | 57 fps | +36% |
| Speed 3 | 41 fps | 55 fps | +34% |
| Load time | 45.8 s | 33.7 s | 26% faster |
| Ticking at speed 3, per frame | 3.7 ms | 1.5 ms | 59% less |

**A 512×512 map in the editor with 65,468 plants**, full-screen at 3456×2168, the camera zoomed out over most of the map. 30 s paused and 30 s at the editor's top speed:

| | Without | With | |
|---|---|---|---|
| Paused | 11 fps | 107 fps | 10× |
| Top speed | 11 fps | 84 fps | 8× |
| 1% low, top speed | 10 fps | 34 fps | 3.4× |
| Checking which plants are flooded, per tick | 14.3 ms | 6.6 ms | 54% less |

## What it speeds up

Each one gives the same result as the game's own code, only sooner. The ID is how logs, issues and commits refer to it, and the badge says where it comes from:

- <img src="Media/badges/T3MP.svg" alt="T3MP" height="18"> ported from T3MP's code
- <img src="Media/badges/PP.svg" alt="Performance Plus" height="18"> written from Performance Plus's description
- <img src="Media/badges/ours.svg" alt="TimberPerf" height="18"> TimberPerf's own

### Ticks

- `tick-frontier`: entity buckets skip entities with no enabled tickable component instead of visiting each one. <img src="Media/badges/T3MP.svg" alt="T3MP" height="18">
- `water-objects`: the water depth over every plant and building is read straight from the water map's arrays. <img src="Media/badges/PP.svg" alt="Performance Plus" height="18">
- `site-finish-check`: a construction site checks its build time before its other finishing checks. <img src="Media/badges/PP.svg" alt="Performance Plus" height="18">
- `effect-chances`: an effect's chance for a need is looked up once, not on every check. <img src="Media/badges/ours.svg" alt="TimberPerf" height="18">
- `tube-checks`: whether a new or removed object is a tube is asked once, not once per beaver. <img src="Media/badges/PP.svg" alt="Performance Plus" height="18">
- `remove-by-reference`: removed entities, components and water objects are found in the game's lists by reference. <img src="Media/badges/PP.svg" alt="Performance Plus" height="18">
- `active-alerts`: the warnings panel is rebuilt from the active warnings only. <img src="Media/badges/PP.svg" alt="Performance Plus" height="18">
- `event-delegates`: event handlers are called through typed delegates instead of reflection. <img src="Media/badges/T3MP.svg" alt="T3MP" height="18">
- `behavior-log`: a beaver's behaviour log is turned into text only when a save or the debug panel reads it. <img src="Media/badges/PP.svg" alt="Performance Plus" height="18">

### Paths and reachability

- `path-memo`: a path asked again before the paths changed is answered from memory. <img src="Media/badges/PP.svg" alt="Performance Plus" height="18">
- `accessible-memo`: a path to something reachable from every side is one answer, not one per side. <img src="Media/badges/PP.svg" alt="Performance Plus" height="18">
- `reachability-areas`: which places reach which is kept in stamped arrays instead of a dictionary cleared per area. <img src="Media/badges/PP.svg" alt="Performance Plus" height="18">
- `gate-nodes`: paths aren't checked tile by tile for gates while the map has none. <img src="Media/badges/PP.svg" alt="Performance Plus" height="18">
- `terrain-visits`: a terrain node is checked and marked visited with one lookup instead of two. <img src="Media/badges/T3MP.svg" alt="T3MP" height="18">
- `yielder-reachability`: a search for something to cut or gather skips path checks that can't change its pick. <img src="Media/badges/T3MP.svg" alt="T3MP" height="18">

### Jobs and goods

- `goods-counts`: a district's stock and capacity are updated from the inventories that changed, not recounted every tick. <img src="Media/badges/PP.svg" alt="Performance Plus" height="18">
- `processed-counts`: goods inside workshops are counted the same way. <img src="Media/badges/PP.svg" alt="Performance Plus" height="18">
- `fill-cache`: an inventory's fill percentages are kept until its stock or what it may hold changes. <img src="Media/badges/PP.svg" alt="Performance Plus" height="18">
- `allowed-good-rows`: an inventory's limits come from the row in hand instead of a search by name. <img src="Media/badges/T3MP.svg" alt="T3MP" height="18">
- `full-inputs`: a carrier asked to fill an inventory with no room skips the search through the district. <img src="Media/badges/PP.svg" alt="Performance Plus" height="18">
- `builder-district`: a builder looking for work finds its hub's district once per search. <img src="Media/badges/PP.svg" alt="Performance Plus" height="18">
- `missing-materials`: a site needing goods nobody has shows its warning without searching the district for them. <img src="Media/badges/PP.svg" alt="Performance Plus" height="18">
- `demolish-order`: a builder picking something to demolish checks distance before reachability. <img src="Media/badges/PP.svg" alt="Performance Plus" height="18">
- `planting-spots`: farmers and foresters try their spots nearest first and stop at the first they can plant. <img src="Media/badges/PP.svg" alt="Performance Plus" height="18">
- `full-farmhouses`: a farm house with no room for anything it grows doesn't search its crops. <img src="Media/badges/PP.svg" alt="Performance Plus" height="18">
- `yielder-templates`: the kind of each crop or tree in range is looked up once. <img src="Media/badges/PP.svg" alt="Performance Plus" height="18">

### Movement and animation

- `walker-speed`: a walker's speed delegate is made once per walker, not on every step. <img src="Media/badges/T3MP.svg" alt="T3MP" height="18">
- `movement-substeps`: walkers move corner to corner without writing their transform every 0.1 tile. <img src="Media/badges/T3MP.svg" alt="T3MP" height="18">
- `offscreen-animation`: models nobody can see keep their animation time and get posed when they come into view; models on screen are checked again every four frames, and a paused one already posed isn't posed again. <img src="Media/badges/PP.svg" alt="Performance Plus" height="18"> <img src="Media/badges/ours.svg" alt="TimberPerf" height="18">
- `offscreen-swimming`: beavers out of sight skip the swimming update. <img src="Media/badges/PP.svg" alt="Performance Plus" height="18">
- `animator-states`: an animated model picks its next animation from parameters read once, not by name per condition. <img src="Media/badges/PP.svg" alt="Performance Plus" height="18">
- `enter-visibility`: a beaver entering a building is shown or hidden once, not twice. <img src="Media/badges/PP.svg" alt="Performance Plus" height="18">

### Drawing

- `tree-patches`: plants are drawn as one mesh per material for every 32-tile patch, so a forest draws like an empty map. <img src="Media/badges/ours.svg" alt="TimberPerf" height="18">
- `water-once-per-frame`: water textures go to the graphics card once a frame, not once a tick. <img src="Media/badges/PP.svg" alt="Performance Plus" height="18">
- `water-upload`: water texture layers whose bytes didn't change aren't uploaded again. <img src="Media/badges/T3MP.svg" alt="T3MP" height="18">
- `shaft-once-per-frame`: power shaft animations update once a frame, not once a tick. <img src="Media/badges/PP.svg" alt="Performance Plus" height="18">
- `coloured-materials`: a recoloured material is looked up once per colouring. <img src="Media/badges/PP.svg" alt="Performance Plus" height="18">
- `sound-listener`: where sounds are heard from is worked out again only when the camera moves. <img src="Media/badges/PP.svg" alt="Performance Plus" height="18">

### Loading a save

- `construction-plans`: singletons and components are built from plans made once per type instead of reflection every time. <img src="Media/badges/T3MP.svg" alt="T3MP" height="18">
- `navigation-shapes`: a building's navmesh shape is built once per kind, orientation and footprint. <img src="Media/badges/T3MP.svg" alt="T3MP" height="18">
- `initial-navigation`: the first navmesh is built from the queued changes in one go, reusing connection decisions for identical inputs. <img src="Media/badges/T3MP.svg" alt="T3MP" height="18">
- `empty-flow-fields`: empty flow fields skip the changed nodes while navigation loads. <img src="Media/badges/T3MP.svg" alt="T3MP" height="18">
- `load-event-routing`: load events go only to the handlers that can use them. <img src="Media/badges/T3MP.svg" alt="T3MP" height="18">
- `block-routing`: block events during the load go only to the subscribers at the block's columns. <img src="Media/badges/T3MP.svg" alt="T3MP" height="18">
- `transput-routing`: a new power connector is handed only to the connectors at the tile it faces. <img src="Media/badges/T3MP.svg" alt="T3MP" height="18">
- `prepared-visuals`: entity models are set up while their entity is still inactive. <img src="Media/badges/T3MP.svg" alt="T3MP" height="18">
- `prepared-status-icons`: status icons are cloned from one prepared copy. <img src="Media/badges/T3MP.svg" alt="T3MP" height="18">
- `deferred-good-stacks`: an empty good pile's model is made only when it has something to show. <img src="Media/badges/T3MP.svg" alt="T3MP" height="18">
- `atlas-pixels`: a texture's pixels are read once while building an atlas. <img src="Media/badges/T3MP.svg" alt="T3MP" height="18">
- `terrain-surface`: each layer of the terrain's surface is worked out once. <img src="Media/badges/T3MP.svg" alt="T3MP" height="18">
- `load-gc-budget`: garbage collection holds off during the load while there's memory to spare, with one full collection at its end. <img src="Media/badges/T3MP.svg" alt="T3MP" height="18">

### Extras

- `editor-fps`: the game's FPS counter shows in the map editor too. <img src="Media/badges/ours.svg" alt="TimberPerf" height="18">

Every load optimization checks the game's code it relies on against the versions it was reviewed with, and turns itself off after a game update that changed it, or when another mod patches the same methods.

## Building

`dotnet build -c Release` in `TimberPerf/` installs it into the Mods folder as the mod. `Tools/Package/package.sh` builds it and zips the installed folder to ~/Downloads. `Tools/Package/release.sh patch|minor|major` cuts a release and `Tools/Package/upload.sh` puts it on the Workshop and mod.io. Release every change with a new version: the version decides which copy runs. `Tools/make_cover.py` draws `Media/cover.png` and the thumbnail, with the brush strokes of Timber Empires' icons (a checkout of it next to this repo).

## Using it from a mod

Never reference `TimberPerf.dll`: the reference could bind to the copy that stood down. Copy `Client/TimberPerfBridge.cs` into the mod, change its namespace, and set or bind what's public in `Hooks`, `Probe`, `PerfCounters` and `Plants.TreePatches` from `StartMod` on.

To ship it, build it with `InstallMod=false` and put the DLL next to the mod's own before the mod installs itself, as `BuildTimberPerf` in BeaverBuddies' and Archipelago's csproj does. They look for this repo next to theirs (`../TimberPerf`), or at `TimberPerfProject`.

## License

GPL-3.0 (`License.txt`), like BeaverBuddies.

## Switches

A file in Documents/Timberborn named `TimberPerf-off` (or the older `BeaverBuddies-noperf`) turns the optimizations off. T3MP or Performance Plus being installed does the same. `BeaverBuddies-perfverify` checks the remembered answers against the game's own and logs any that differ.

Most of the optimizations are ported from T3MP (More More More Performance!) by toropippi, under the Apache License 2.0 (`LICENSE-T3MP.txt`). The Performance Plus fixes were written from its description: none of its code is here.
