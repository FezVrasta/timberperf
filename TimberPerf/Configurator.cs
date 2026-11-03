using Bindito.Core;

namespace TimberPerf
{
    // Only the active copy binds anything: the game finds the configurators of every copy (Copies)
    [Context("Game")]
    [Context("MapEditor")]
    public class Configurator : IConfigurator
    {
        public void Configure(IContainerDefinition containerDefinition)
        {
            if (!Copies.Active || !PerformanceSettings.Enabled) return;
            containerDefinition.Bind<Plants.TreePatches>().AsSingleton();
        }
    }

    // The game only shows its FPS counter in games, though the counter behind it also runs in the map editor
    [Context("MapEditor")]
    public class MapEditorConfigurator : IConfigurator
    {
        public void Configure(IContainerDefinition containerDefinition)
        {
            if (Copies.Active) containerDefinition.Bind<Timberborn.DiagnosticsUI.FramesPerSecondPanel>().AsSingleton();
        }
    }
}
