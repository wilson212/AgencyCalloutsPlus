using Rage;
using System.IO;
using System.Windows.Forms;

namespace AgencyCalloutsPlus
{
    internal class Settings
    {
        internal static int MaxLocationAttempts { get; set; } = 10;

        internal static int AudioDivision { get; set; } = 1;

        internal static string AudioUnitType { get; set; } = "LINCOLN";

        internal static char AudioUnitTypeLetter => AudioUnitType[0];

        internal static int AudioBeat { get; set; } = 18;

        internal static int TimeScale { get; set; } = 30;


        internal static string GetUnitString => $"{AudioDivision}-{AudioUnitTypeLetter}-{AudioBeat}";

        /// <summary>
        /// The key to open the callout interaction menu
        /// </summary>
        internal static Keys OpenMenuKey { get; set; } = Keys.F11;

        /// <summary>
        /// The modifier key to open the callout interaction menu
        /// </summary>
        internal static Keys OpenMenuModifierKey { get; set; } = Keys.None;

        internal static bool EnableFullSimulation { get; set; } = false;

        internal static void Initialize()
        {
            // Log
            Log.Info("Loading AgencyCalloutsPlus config...");

            // Ensure file exists
            string path = Path.Combine(Main.LSPDFRPluginPath, "AgencyCalloutsPlus.ini");
            EnsureConfigExists(path);

            // Open ini file
            var ini = new InitializationFile(path);
            ini.Create();

            // Read key bindings
            OpenMenuKey = ini.ReadEnum("KEYBINDINGS", "OpenMenuKey", Keys.F11);
            OpenMenuModifierKey = ini.ReadEnum("KEYBINDINGS", "OpenMenuModifierKey", Keys.None);
            EnableFullSimulation = ini.ReadBoolean("SIMULATION", "EnableFullSimulation", false);
            AudioDivision = ini.ReadInt32("GENERAL", "Division", 1);
            AudioUnitType = ini.ReadString("GENERAL", "UnitType", "LINCOLN").ToUpperInvariant();
            AudioBeat = ini.ReadInt32("GENERAL", "Beat", 18);
            TimeScale = ini.ReadInt32("GENERAL", "TimeScale", 30);

            // Log
            Log.Info("Loaded AgencyCalloutsPlus successfully!");
        }

        private static void EnsureConfigExists(string path)
        {
            if (!File.Exists(path))
            {
                Log.Info("Creating new AgencyCalloutsPlus config file...");
                Stream resource = typeof(Settings).Assembly.GetManifestResourceStream("IniConfig");
                using (var file = new FileStream(path, FileMode.Create, FileAccess.Write))
                {
                    resource.CopyTo(file);
                }
            }
        }
    }
}
