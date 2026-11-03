# TimberPerf

![TimberPerf](Media/cover.png)

Faster Timberborn: big settlements tick faster, saves load sooner, and forests draw as fast as empty land.

It's a mod of its own, and other mods can ship it inside their folder too: BeaverBuddies Co-Op/PvP Edition and Archipelago do. The game loads every DLL in every enabled mod's folder, so with more than one of them on there's more than one copy. Only the newest version runs (the first loaded, when they're the same); the others log that they stand down and do nothing. Bump `<Version>` in `TimberPerf/TimberPerf.csproj` and in `TimberPerf/manifest.json` with every change, or an older build could be the one that runs.

## Numbers

Measured on a MacBook Pro (M1 Max, 32 GB), windowed, each case played with TimberPerf and then with it off (`TimberPerf-off`), Timber Empires disabled.

**A big late-game save** (castori-di-montagna-fabri, a minute at each speed):

| | Without | With | |
|---|---|---|---|
| Paused | 59 fps | 92 fps | +56% |
| Speed 1 | 42 fps | 57 fps | +36% |
| Speed 3 | 41 fps | 55 fps | +34% |
| Load time | 45.8 s | 33.7 s | 26% faster |
| Ticking at speed 3, per frame | 3.7 ms | 1.5 ms | 59% less |

**A 512×512 map in the editor** (65,468 plants, 20 s at the editor's top speed):

| | Without | With | |
|---|---|---|---|
| Frames over 33 ms | 42 | 12 | 71% fewer hitches |
| Water over plants, per tick | 14.3 ms | 6.6 ms | 54% less |

## What it speeds up

Each one gives the same result as the game's own code, only sooner. The ID is how logs, issues and commits refer to it.

### Ticks

- `tick-frontier`: entity buckets skip entities with no enabled tickable component instead of visiting each one.
- `water-objects`: the water depth over every plant and building is read straight from the water map's arrays.
- `site-finish-check`: a construction site checks its build time before its other finishing checks.
- `effect-chances`: an effect's chance for a need is looked up once, not on every check.
- `tube-checks`: whether a new or removed object is a tube is asked once, not once per beaver.
- `remove-by-reference`: removed entities, components and water objects are found in the game's lists by reference.
- `active-alerts`: the warnings panel is rebuilt from the active warnings only.
- `event-delegates`: event handlers are called through typed delegates instead of reflection.
- `behavior-log`: a beaver's behaviour log is turned into text only when a save or the debug panel reads it.

### Paths and reachability

- `path-memo`: a path asked again before the paths changed is answered from memory.
- `accessible-memo`: a path to something reachable from every side is one answer, not one per side.
- `reachability-areas`: which places reach which is kept in stamped arrays instead of a dictionary cleared per area.
- `gate-nodes`: paths aren't checked tile by tile for gates while the map has none.
- `terrain-visits`: a terrain node is checked and marked visited with one lookup instead of two.
- `yielder-reachability`: a search for something to cut or gather skips path checks that can't change its pick.

### Jobs and goods

- `goods-counts`: a district's stock and capacity are updated from the inventories that changed, not recounted every tick.
- `processed-counts`: goods inside workshops are counted the same way.
- `fill-cache`: an inventory's fill percentages are kept until its stock or what it may hold changes.
- `allowed-good-rows`: an inventory's limits come from the row in hand instead of a search by name.
- `full-inputs`: a carrier asked to fill an inventory with no room skips the search through the district.
- `builder-district`: a builder looking for work finds its hub's district once per search.
- `missing-materials`: a site needing goods nobody has shows its warning without searching the district for them.
- `demolish-order`: a builder picking something to demolish checks distance before reachability.
- `planting-spots`: farmers and foresters try their spots nearest first and stop at the first they can plant.
- `full-farmhouses`: a farm house with no room for anything it grows doesn't search its crops.
- `yielder-templates`: the kind of each crop or tree in range is looked up once.

### Movement and animation

- `walker-speed`: a walker's speed delegate is made once per walker, not on every step.
- `movement-substeps`: walkers move corner to corner without writing their transform every 0.1 tile.
- `offscreen-animation`: models nobody can see keep their animation time and get posed when they come into view.
- `offscreen-swimming`: beavers out of sight skip the swimming update.
- `animator-states`: an animated model picks its next animation from parameters read once, not by name per condition.
- `enter-visibility`: a beaver entering a building is shown or hidden once, not twice.

### Drawing

- `tree-patches`: plants are drawn as one mesh per material for every 32-tile patch, so a forest draws like an empty map.
- `water-once-per-frame`: water textures go to the graphics card once a frame, not once a tick.
- `water-upload`: water texture layers whose bytes didn't change aren't uploaded again.
- `shaft-once-per-frame`: power shaft animations update once a frame, not once a tick.
- `coloured-materials`: a recoloured material is looked up once per colouring.
- `sound-listener`: where sounds are heard from is worked out again only when the camera moves.

### Loading a save

- `construction-plans`: singletons and components are built from plans made once per type instead of reflection every time.
- `navigation-shapes`: a building's navmesh shape is built once per kind, orientation and footprint.
- `initial-navigation`: the first navmesh is built from the queued changes in one go, reusing connection decisions for identical inputs.
- `empty-flow-fields`: empty flow fields skip the changed nodes while navigation loads.
- `load-event-routing`: load events go only to the handlers that can use them.
- `block-routing`: block events during the load go only to the subscribers at the block's columns.
- `transput-routing`: a new power connector is handed only to the connectors at the tile it faces.
- `prepared-visuals`: entity models are set up while their entity is still inactive.
- `prepared-status-icons`: status icons are cloned from one prepared copy.
- `deferred-good-stacks`: an empty good pile's model is made only when it has something to show.
- `atlas-pixels`: a texture's pixels are read once while building an atlas.
- `terrain-surface`: each layer of the terrain's surface is worked out once.
- `load-gc-budget`: garbage collection holds off during the load while there's memory to spare, with one full collection at its end.

Every load optimization checks the game's code it relies on against the versions it was reviewed with, and turns itself off after a game update that changed it, or when another mod patches the same methods.

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
