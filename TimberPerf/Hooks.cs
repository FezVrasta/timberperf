using System;
using Timberborn.BlockSystem;
using UnityEngine;

namespace TimberPerf
{
    /**
     * What the mods that ship TimberPerf tell it. They never compile against it (two copies of it can be loaded, see
     * Copies): they find the active copy's Hooks by reflection, as TimberPerfBridge in each of them does.
     */
    public static class Hooks
    {
        // BeaverBuddies' Harmony id, for the patches of its that the optimizations know are safe
        public const string BeaverBuddies = "beaverbuddies";
        public const string BeaverBuddiesProfiling = "beaverbuddies.perf";

        /** Types whose constructor arguments BeaverBuddies fills itself (its random generators): built the game's way. */
        public static Func<Type, bool> HandlesParameters;

        /** Whether the tick starting now is the last before the frame is drawn: the ones before skip drawing work. */
        public static void SetLastOfFrame(bool last) => FrameTicks.LastOfFrame = last;

        /** Which island a cell is on (Archipelago), -1 for none: merged plants never cross one. */
        public static Func<Vector2Int, int> IslandAt;

        /** A merged mesh drawing an island's plants, for that island's camera only; and all of them gone, as a game ends. */
        public static Action<int, Renderer> IslandRenderer;
        public static Action IslandRenderersCleared;

        /** A mod that patches how some buildings join the navigation mesh says which, so the shapes the game builds are reused for the others only. */
        public static void AddNavigationShapeBypass(string harmonyId, Func<BlockObject, bool> buildings) =>
            NavigationShapePlans.AddBypass(harmonyId, buildings);
    }
}
