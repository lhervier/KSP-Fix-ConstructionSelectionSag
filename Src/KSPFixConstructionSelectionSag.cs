using UnityEngine;

namespace com.github.lhervier.ksp.fix.constructionselectionsag
{
    /// <summary>
    /// Reads the settings once, when KSP starts. The fix itself is <see cref="SelectedVesselHeldStill"/>, started with
    /// each flight scene when the settings turn it on.
    /// </summary>
    [KSPAddon(KSPAddon.Startup.Instantly, true)]
    public class KSPFixConstructionSelectionSag : MonoBehaviour
    {
        private void Start()
        {
            Settings.Load();
            Log.Info($"Version {typeof(KSPFixConstructionSelectionSag).Assembly.GetName().Version}, " +
                $"holdVesselStill {Settings.HoldVesselStill}, log level {Log.Level}");
        }
    }
}
