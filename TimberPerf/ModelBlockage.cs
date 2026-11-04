using HarmonyLib;
using Timberborn.BlockObjectModelSystem;

namespace TimberPerf
{
    /**
     * The layer slicer blocks or unblocks every model on each level its cut passed, thousands of them, and each call
     * switches all of the model's renderers and colliders again (looked for with GetComponentsInChildren) even when it
     * was blocked or unblocked already. A call that changes nothing is skipped: the model's visibility follows from
     * the same flags, which every other change refreshes by itself.
     */
    [HarmonyPatch(typeof(BlockObjectModelController))]
    static class ModelBlockage
    {
        static bool Prepare() => PerformanceSettings.Enabled;

        [HarmonyPrefix, HarmonyPatch(nameof(BlockObjectModelController.BlockModel))]
        static bool Block(BlockObjectModelController __instance) => !__instance._modelBlocked;

        [HarmonyPrefix, HarmonyPatch(nameof(BlockObjectModelController.UnblockModel))]
        static bool Unblock(BlockObjectModelController __instance) => __instance._modelBlocked;
    }
}
