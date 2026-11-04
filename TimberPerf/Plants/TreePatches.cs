using System.Collections.Generic;
using HarmonyLib;
using Timberborn.BlockObjectModelSystem;
using Timberborn.BlockSystem;
using Timberborn.EntitySystem;
using Timberborn.LevelVisibilitySystem;
using Timberborn.NaturalResources;
using Timberborn.NaturalResourcesModelSystem;
using Timberborn.SelectionSystem;
using Timberborn.SingletonSystem;
using UnityEngine;
using UnityEngine.Rendering;

namespace TimberPerf.Plants
{
    /**
     * Plants drawn as one mesh per material for each patch of the map, instead of one draw each: a big map full of
     * trees draws as fast as an empty one, in the map editor and in a game. Each plant keeps its own renderers (it's
     * only their drawing that's switched off), so whatever the game does with them still works; a patch is merged
     * again the frame after one of its plants changed model, came or went. A highlighted plant is left out and draws
     * itself, so the game's highlight shows on it. Patches don't cross islands (Archipelago's, through
     * Hooks.IslandAt): each is drawn only by its own island's camera. A plant hidden in the fog of war (Timber Empires,
     * through PlantFogged) is left out and left to the fog. A patch's meshes are split by the plants' level, so the
     * layer slicer only switches the ones above its cut to shadows, as the game does with each plant, with nothing merged
     * again.
     */
    public class TreePatches : IPostLoadableSingleton, ILateUpdatableSingleton, IUnloadableSingleton
    {
        // Tiles a side
        private const int PatchSize = 32;

        public static TreePatches Current { get; private set; }

        private sealed class Plant
        {
            public EntityComponent Entity;
            public MeshRenderer[] Renderers;
            public BlockObjectModelController Model;
            public int Level;
            public (int, Vector2Int) Patch;
            public bool Released;
            public bool Fogged;
        }

        private sealed class Part
        {
            public GameObject Root;
            public MeshFilter Filter;
            public MeshRenderer Renderer;
            public ShadowCastingMode Mode;
            public int Level;
            public readonly List<CombineInstance> Pieces = new();
        }

        private sealed class Patch
        {
            public int Island;
            public readonly List<Plant> Plants = new();
            public readonly Dictionary<(Material, ShadowCastingMode, int, int), Part> Parts = new();
        }

        private readonly EntityRegistry _entityRegistry;
        private readonly EventBus _eventBus;
        private readonly ILevelVisibilityService _levels;
        private readonly Dictionary<EntityComponent, Plant> _plants = new();
        private readonly Dictionary<(int, Vector2Int), Patch> _patches = new();
        private readonly HashSet<(int, Vector2Int)> _dirty = new();
        // A merged material for each of the game's: the game gives every plant a copy of its own
        private readonly Dictionary<(string, Shader, bool), Material> _materials = new();
        private static readonly int EnableDetail = Shader.PropertyToID("_EnableDetail");
        // The plants' renderers switched off here, so other mods can tell them from hidden ones
        private readonly HashSet<Renderer> _merged = new();
        private readonly List<MeshRenderer> _shown = new();
        private readonly HashSet<EntityComponent> _fogged = new();
        private readonly List<(int, Vector2Int)> _merging = new();
        private Transform _parent;
        private bool _off;
        private bool _logged;

        public TreePatches(EntityRegistry entityRegistry, EventBus eventBus, ILevelVisibilityService levels)
        {
            _entityRegistry = entityRegistry;
            _eventBus = eventBus;
            _levels = levels;
        }

        // For the mods that ship TimberPerf, by reflection (TimberPerfBridge): a plant in the fog of war or out of it,
        // and whether a renderer is off only because its plant is drawn merged with others (it shows all the same)
        public static void PlantFogged(Timberborn.BaseComponentSystem.BaseComponent plant, bool fogged) => Current?.Fogged(plant ? plant.GetComponent<EntityComponent>() : null, fogged);
        public static bool RendererMerged(Renderer renderer) => Current?.IsMerged(renderer) ?? false;
        // Off, every plant draws itself again, to compare; and what's merged, for a perf command
        public static void SwitchAll(bool on) => Current?.Switch(on);
        public static string DescribeAll() => Current?.Describe() ?? "none";

        public void PostLoad()
        {
            _parent = new GameObject("TimberPerf plant patches").transform;
            Current = this;
            foreach (EntityComponent entity in _entityRegistry.Entities) Add(entity);
            _eventBus.Register(this);
        }

        [OnEvent]
        public void OnMaxVisibleLevelChanged(MaxVisibleLevelChangedEvent changed)
        {
            foreach (Patch patch in _patches.Values) foreach (Part part in patch.Parts.Values) Slice(part);
        }

        // Shadows only above the slicer's cut
        private void Slice(Part part) =>
            part.Renderer.shadowCastingMode = part.Mode == ShadowCastingMode.On && part.Level > _levels.MaxVisibleLevel ? ShadowCastingMode.ShadowsOnly : part.Mode;

        public void Unload()
        {
            Current = null;
            Hooks.IslandRenderersCleared?.Invoke();
            foreach (Material material in _materials.Values) Object.Destroy(material);
            _materials.Clear();
        }

        [OnEvent]
        public void OnEntityInitialized(EntityInitializedEvent initialized) => Add(initialized.Entity);

        [OnEvent]
        public void OnEntityDeleted(EntityDeletedEvent deleted)
        {
            _fogged.Remove(deleted.Entity);
            if (!_plants.Remove(deleted.Entity, out Plant plant)) return;
            _patches[plant.Patch].Plants.Remove(plant);
            Draw(plant, true);
            _dirty.Add(plant.Patch);
        }

        private void Add(EntityComponent entity)
        {
            if (!entity || _plants.ContainsKey(entity) || !entity.HasComponent<NaturalResource>()) return;
            Vector3 at = entity.Transform.position;
            var cell = new Vector2Int(Mathf.FloorToInt(at.x), Mathf.FloorToInt(at.z));
            int island = Hooks.IslandAt?.Invoke(cell) ?? -1;
            var plant = new Plant
            {
                Entity = entity,
                Renderers = entity.GameObject.GetComponentsInChildren<MeshRenderer>(true),
                Model = entity.GetComponent<BlockObjectModelController>(),
                Level = entity.TryGetComponent(out BlockObject block) ? block.CoordinatesAtBaseZ.z : 0,
                Patch = (island, new Vector2Int(Mathf.FloorToInt(at.x / PatchSize), Mathf.FloorToInt(at.z / PatchSize))),
                Fogged = _fogged.Contains(entity),
            };
            _plants[entity] = plant;
            if (!_patches.TryGetValue(plant.Patch, out Patch patch)) _patches[plant.Patch] = patch = new Patch { Island = island };
            patch.Plants.Add(plant);
            NaturalResourceModel model = entity.GetComponent<NaturalResourceModel>();
            if (model) model.ModelChanged += (_, _) => Changed(entity);
            _dirty.Add(plant.Patch);
        }

        internal void Changed(EntityComponent entity)
        {
            if (entity && _plants.TryGetValue(entity, out Plant plant)) _dirty.Add(plant.Patch);
        }

        internal void Highlighted(EntityComponent entity, bool highlighted)
        {
            if (!entity || !_plants.TryGetValue(entity, out Plant plant) || plant.Released == highlighted) return;
            plant.Released = highlighted;
            _dirty.Add(plant.Patch);
        }

        // The fog took the plant's renderers, or gave them back; it's merged again only once out of the fog
        public void Fogged(EntityComponent entity, bool fogged)
        {
            if (!entity) return;
            // Told before it was known here (the fog comes up as the game loads)
            if (fogged) _fogged.Add(entity);
            else _fogged.Remove(entity);
            if (!_plants.TryGetValue(entity, out Plant plant) || plant.Fogged == fogged) return;
            plant.Fogged = fogged;
            if (fogged) foreach (MeshRenderer renderer in plant.Renderers) _merged.Remove(renderer);
            _dirty.Add(plant.Patch);
        }

        // Whether a renderer is off because its plant is drawn in a patch (it shows, only not by itself)
        public bool IsMerged(Renderer renderer) => renderer && _merged.Contains(renderer);

        // Off, every plant draws itself again: to compare
        public void Switch(bool on)
        {
            _off = !on;
            _parent.gameObject.SetActive(on);
            foreach (Plant plant in _plants.Values) Draw(plant, !on);
            if (on) _dirty.UnionWith(_patches.Keys);
        }

        public string Describe()
        {
            int parts = 0, fogged = 0, released = 0;
            foreach (Patch patch in _patches.Values) foreach (Part part in patch.Parts.Values) if (part.Root.activeSelf) parts++;
            foreach (Plant plant in _plants.Values)
            {
                if (plant.Fogged) fogged++;
                if (plant.Released) released++;
            }
            return $"{_plants.Count} plants in {_patches.Count} patches, {parts} merged meshes, {fogged} in the fog, {released} highlighted";
        }

        public void LateUpdateSingleton()
        {
            if (_off || _dirty.Count == 0) return;
            bool first = !_logged;
            // A few milliseconds a frame: a colony's crops and bushes change all the time, and a patch left for the next
            // frame shows its plants as they were a moment ago. All at once the first time, so nothing shows twice
            long until = _logged ? System.Diagnostics.Stopwatch.GetTimestamp() + System.Diagnostics.Stopwatch.Frequency * 3 / 1000 : long.MaxValue;
            _merging.Clear();
            _merging.AddRange(_dirty);
            foreach (var key in _merging)
            {
                if (System.Diagnostics.Stopwatch.GetTimestamp() > until) break;
                _dirty.Remove(key);
                if (_patches.TryGetValue(key, out Patch patch)) Merge(patch);
            }
            if (first)
            {
                _logged = true;
                Debug.Log($"[TimberPerf] Plant patches: {Describe()}");
            }
        }

        // A plant's renderers drawn by themselves, or left to its patch. One in the fog is the fog's
        private void Draw(Plant plant, bool itself)
        {
            if (plant.Fogged) return;
            foreach (MeshRenderer renderer in plant.Renderers)
            {
                if (!renderer) continue;
                renderer.forceRenderingOff = !itself;
                if (itself) _merged.Remove(renderer);
                else _merged.Add(renderer);
            }
        }

        private void Merge(Patch patch)
        {
            foreach (Part part in patch.Parts.Values) part.Pieces.Clear();
            foreach (Plant plant in patch.Plants)
            {
                if (plant.Fogged) continue;
                Draw(plant, plant.Released);
                if (plant.Released) continue;
                _shown.Clear();
                foreach (MeshRenderer renderer in plant.Renderers)
                {
                    if (renderer && renderer.enabled && renderer.gameObject.activeInHierarchy) _shown.Add(renderer);
                }
                foreach (MeshRenderer renderer in _shown)
                {
                    Mesh mesh = renderer.TryGetComponent(out MeshFilter filter) ? filter.sharedMesh : null;
                    if (!mesh) continue;
                    Material[] materials = renderer.sharedMaterials;
                    for (int i = 0; i < mesh.subMeshCount && i < materials.Length; i++)
                    {
                        if (!materials[i]) continue;
                        Material shared = Shared(materials[i]);
                        // Merged as the plant shows uncut, the slicer's shadows put back by Slice
                        ShadowCastingMode mode = renderer.shadowCastingMode;
                        if (mode == ShadowCastingMode.ShadowsOnly && plant.Model && plant.Model.ModelBlocked) mode = ShadowCastingMode.On;
                        var key = (shared, mode, renderer.gameObject.layer, plant.Level);
                        if (!patch.Parts.TryGetValue(key, out Part part)) patch.Parts[key] = part = NewPart(shared, renderer, mode, plant.Level, patch.Island);
                        part.Pieces.Add(new CombineInstance { mesh = mesh, subMeshIndex = i, transform = renderer.localToWorldMatrix });
                    }
                }
            }
            foreach (Part part in patch.Parts.Values)
            {
                Mesh merged = part.Filter.sharedMesh;
                merged.Clear();
                part.Root.SetActive(part.Pieces.Count > 0);
                if (part.Pieces.Count > 0) merged.CombineMeshes(part.Pieces.ToArray(), true, true);
                Slice(part);
            }
        }

        private Material Shared(Material material)
        {
            // Fruit on a bush is a switch in the plant's own copy (GatherableModel), so with and without are two
            bool detail = material.HasFloat(EnableDetail) && material.GetFloat(EnableDetail) > 0.5f;
            var key = (material.name.Replace(" (Instance)", ""), material.shader, detail);
            if (!_materials.TryGetValue(key, out Material shared))
            {
                shared = new Material(material) { name = key.Item1 + (detail ? " detail" : "") + " (patches)" };
                _materials[key] = shared;
            }
            return shared;
        }

        private Part NewPart(Material material, MeshRenderer like, ShadowCastingMode mode, int level, int island)
        {
            var root = new GameObject($"{material.name} patch") { layer = like.gameObject.layer };
            root.transform.SetParent(_parent, false);
            var part = new Part { Root = root, Filter = root.AddComponent<MeshFilter>(), Renderer = root.AddComponent<MeshRenderer>(), Mode = mode, Level = level };
            part.Filter.sharedMesh = new Mesh { name = root.name, indexFormat = IndexFormat.UInt32 };
            part.Renderer.sharedMaterial = material;
            part.Renderer.receiveShadows = like.receiveShadows;
            part.Renderer.renderingLayerMask = like.renderingLayerMask & ~(uint)(1 << RenderingLayerMask.NameToRenderingLayer("Selection"));
            part.Renderer.lightProbeUsage = like.lightProbeUsage;
            if (island >= 0) Hooks.IslandRenderer?.Invoke(island, part.Renderer);
            return part;
        }
    }

    [HarmonyPatch]
    static class TreePatchesHooks
    {
        // Shown or hidden as a whole (SetVisibility), without a model change
        [HarmonyPostfix, HarmonyPatch(typeof(NaturalResourceModel), nameof(NaturalResourceModel.Show))]
        static void Shown(NaturalResourceModel __instance) => TreePatches.Current?.Changed(__instance.GetComponent<EntityComponent>());

        [HarmonyPostfix, HarmonyPatch(typeof(NaturalResourceModel), nameof(NaturalResourceModel.Hide))]
        static void Hidden(NaturalResourceModel __instance) => TreePatches.Current?.Changed(__instance.GetComponent<EntityComponent>());

        [HarmonyPostfix, HarmonyPatch(typeof(HighlightableObject), nameof(HighlightableObject.UpdateColorAndHighlight))]
        static void Highlight(HighlightableObject __instance) =>
            TreePatches.Current?.Highlighted(__instance.GetComponent<EntityComponent>(), __instance._isHighlighted);
    }
}
