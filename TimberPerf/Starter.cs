using System;
using Bindito.Core;
using HarmonyLib;
using Timberborn.ModManagerScene;
using UnityEngine;

namespace TimberPerf
{
    // Each mod that ships this DLL starts it; only the active copy patches the game (Copies)
    public class Starter : IModStarter
    {
        private static bool _started;

        public void StartMod(IModEnvironment modEnvironment)
        {
            if (!Copies.Active)
            {
                Debug.Log($"[TimberPerf] v{Copies.Version} in {modEnvironment.ModPath} stands down: another mod's copy runs (the newest, or the first loaded of the same version)");
                return;
            }
            if (_started) return;
            _started = true;
            PerformanceSettings.Start();
            if (!PerformanceSettings.Enabled)
            {
                Debug.LogWarning("[TimberPerf] Optimizations are off (TimberPerf-off, or T3MP or Performance Plus installed)");
                return;
            }
            Debug.Log($"[TimberPerf] v{Copies.Version} from {modEnvironment.ModPath}");
            var harmony = new Harmony(PerformanceSettings.HarmonyId);
            foreach (Type type in AccessTools.GetTypesFromAssembly(typeof(Starter).Assembly))
            {
                try
                {
                    if (type.HasHarmonyAttribute()) harmony.CreateClassProcessor(type).Patch();
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[TimberPerf] Skipped {type.Name}: {e.GetBaseException().Message}");
                }
            }
        }
    }

    /**
     * The load optimizations check for other mods' patches on the methods they replace, so they're put in place once every
     * mod has started, as the main menu opens.
     */
    [Context("MainMenu")]
    public class LateStart : IConfigurator
    {
        private static bool _installed;

        public void Configure(IContainerDefinition containerDefinition)
        {
            if (_installed || !Copies.Active || !PerformanceSettings.Enabled) return;
            _installed = true;
            DeferredGoodStackModels.Install();
        }
    }
}
