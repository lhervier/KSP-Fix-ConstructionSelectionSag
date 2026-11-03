using System;
using System.IO;

namespace com.github.lhervier.ksp.fix.constructionselectionsag
{
    /// <summary>
    /// What the player can set in PluginData/settings.cfg, next to the DLL. Read once, when KSP starts.
    /// </summary>
    internal static class Settings
    {
        /// <summary>
        /// Whether a landed vessel is held still while one of its parts is selected with the Move or Rotate tool of
        /// EVA construction mode (see <see cref="SelectedVesselHeldStill"/>).
        /// </summary>
        public static bool HoldVesselStill { get; private set; } = true;

        /// <summary>
        /// Reads the settings file and applies its log level. A missing file, a missing value or an unknown
        /// value leaves the default: the fix on, logging at Info.
        /// </summary>
        public static void Load()
        {
            string folder = Path.GetDirectoryName(typeof(Settings).Assembly.Location);
            string path = Path.Combine(Path.Combine(folder, "PluginData"), "settings.cfg");
            if (!File.Exists(path))
            {
                Log.Warning($"No settings file at {path}, using the defaults");
                return;
            }
            ConfigNode node = ConfigNode.Load(path);
            if (node == null)
            {
                Log.Warning($"Could not read {path}, using the defaults");
                return;
            }

            string level = node.GetValue("logLevel");
            LogLevel parsedLevel;
            if (Enum.TryParse(level, true, out parsedLevel) && Enum.IsDefined(typeof(LogLevel), parsedLevel))
            {
                Log.Level = parsedLevel;
            }
            else
            {
                Log.Warning($"Unknown logLevel '{level}' in {path}, logging at {Log.Level}");
            }

            HoldVesselStill = ReadBool(node, "holdVesselStill", HoldVesselStill, path);
        }

        /// <summary>
        /// Returns the boolean value <paramref name="name"/> of <paramref name="node"/>, or
        /// <paramref name="fallback"/> when it is missing or not a boolean (the latter with a warning).
        /// </summary>
        private static bool ReadBool(ConfigNode node, string name, bool fallback, string path)
        {
            string value = node.GetValue(name);
            if (value == null)
            {
                return fallback;
            }
            bool parsed;
            if (bool.TryParse(value, out parsed))
            {
                return parsed;
            }
            Log.Warning($"Unknown {name} '{value}' in {path}, using {fallback}");
            return fallback;
        }
    }
}
