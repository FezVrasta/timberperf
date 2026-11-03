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
}
