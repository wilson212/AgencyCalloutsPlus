using Rage;
using System.IO;
using System.Windows.Forms;

namespace AgencyDispatchFramework
{
    internal class Settings
    {
        internal static int MaxLocationAttempts { get; set; } = 10;

        internal static int AudioDivision { get; set; } = 1;

        internal static string AudioUnitType { get; set; } = "LINCOLN";

        internal static char AudioUnitTypeLetter => AudioUnitType[0];

        internal static int AudioBeat { get; set; } = 18;

        /// <summary>
        /// Gets or sets the postals file name (without extension) to load on startup
        /// </summary>
        internal static string PostalsFileName { get; set; }

        /// <summary>
        /// Gets or sets the desired logging level of the Game.log
        /// </summary>
        internal static LogLevel LogLevel { get; set; }

        /// <summary>
        /// The key to open the callout interaction menu
        /// </summary>
        internal static Keys OpenMenuKey { get; set; } = Keys.F11;

        /// <summary>
        /// The modifier key to open the callout interaction menu
        /// </summary>
        internal static Keys OpenMenuModifierKey { get; set; } = Keys.None;

        /// <summary>
        /// The key to open the callout interaction menu
        /// </summary>
        internal static Keys OpenCalloutMenuKey { get; set; } = Keys.F11;

        /// <summary>
        /// The modifier key to open the callout interaction menu
        /// </summary>
        internal static Keys OpenCalloutMenuModifierKey { get; set; } = Keys.LShiftKey;

        /// <summary>
        /// The key to open the callout CAD interface
        /// </summary>
        internal static Keys OpenCADMenuKey { get; set; } = Keys.F9;

        /// <summary>
        /// The modifier key to open the callout CAD interface
        /// </summary>
        internal static Keys OpenCADMenuModifierKey { get; set; } = Keys.None;

        /// <summary>
        /// Indicates whether to enable full simulation mode
        /// </summary>
        internal static bool EnableFullSimulation { get; set; } = false;

        /// <summary>
        /// Indicates whether to force a custom timescale in game
        /// </summary>
        internal static bool ForceTimeScale { get; set; } = false;

        /// <summary>
        /// Gets or sets the TimeScale multiplier to set if <see cref="ForceTimeScale"/>
        /// is set to true.
        /// </summary>
        internal static int TimeScaleMultiplier { get; set; } = 30;

        /// <summary>
        /// Gets or sets the time out in seconds between dispatching the player to calls
        /// </summary>
        internal static int TimeoutBetweenCalloutAttempts { get; set; } = 30;

        /// <summary>
        /// Loads the user settings from the ini file
        /// </summary>
        internal static void Initialize()
        {
            // Log
            Log.Info("Loading AgencyDispatchFramework config...");

            // Ensure file exists
            string path = Path.Combine(Main.LSPDFRPluginPath, "AgencyDispatchFramework.ini");
            EnsureConfigExists(path);

            // Open ini file
            var ini = new InitializationFile(path);
            ini.Create();

            // Read key bindings
            OpenMenuKey = ini.ReadEnum("KEYBINDINGS", "OpenMenuKey", Keys.F11);
            OpenMenuModifierKey = ini.ReadEnum("KEYBINDINGS", "OpenMenuModifierKey", Keys.None);
            OpenCalloutMenuKey = ini.ReadEnum("KEYBINDINGS", "OpenCalloutMenuKey", Keys.F11);
            OpenCalloutMenuModifierKey = ini.ReadEnum("KEYBINDINGS", "OpenCalloutMenuModifierKey", Keys.LShiftKey);
            OpenCADMenuKey = ini.ReadEnum("KEYBINDINGS", "OpenCADMenuKey", Keys.F9);
            OpenCADMenuModifierKey = ini.ReadEnum("KEYBINDINGS", "OpenCADMenuModifierKey", Keys.None);

            // Read general settings
            LogLevel = ini.ReadEnum("GENERAL", "LogLevel", LogLevel.DEBUG);
            PostalsFileName = ini.ReadString("GENERAL", "PostalsFilename", "old-postals");
            //EnableFullSimulation = ini.ReadBoolean("SIMULATION", "EnableFullSimulation", false);
            TimeoutBetweenCalloutAttempts = ini.ReadInt32("GENERAL", "TimeoutBetweenCalloutAttempts", 30);

            // Read player settings
            AudioDivision = ini.ReadInt32("PLAYER", "Division", 1);
            AudioUnitType = ini.ReadString("PLAYER", "UnitType", "LINCOLN").ToUpperInvariant();
            AudioBeat = ini.ReadInt32("PLAYER", "Beat", 18);

            // Read timescale settings
            ForceTimeScale = ini.ReadBoolean("TIMESCALE", "ForceTimeScale", false);
            TimeScaleMultiplier = ini.ReadInt32("TIMESCALE", "TimeScaleMultiplier", 30);

            // Log
            Log.Info("Loaded AgencyDispatchFramework config successfully!");
        }

        /// <summary>
        /// Saves the current settings to the ini file
        /// </summary>
        internal static void Save()
        {
            // Log
            Log.Info("Saving AgencyDispatchFramework config...");

            // Ensure file exists
            string path = Path.Combine(Main.LSPDFRPluginPath, "AgencyDispatchFramework.ini");
            EnsureConfigExists(path);

            // Open ini file
            var ini = new InitializationFile(path);
            ini.Create();

            // Save settings
            //ini.Write("KEYBINDINGS", "OpenMenuKey", OpenMenuKey);
            //ini.Write("KEYBINDINGS", "OpenMenuModifierKey", OpenMenuModifierKey);

            // Log
            Log.Info("Saved AgencyDispatchFramework config successfully!");
        }

        /// <summary>
        /// Ensures the ini file exists. If not, a new ini is created with the default settings.
        /// </summary>
        /// <param name="path"></param>
        private static void EnsureConfigExists(string path)
        {
            if (!File.Exists(path))
            {
                Log.Info("Creating new AgencyDispatchFramework config file...");
                Stream resource = typeof(Settings).Assembly.GetManifestResourceStream("IniConfig");
                using (var file = new FileStream(path, FileMode.Create, FileAccess.Write))
                {
                    resource.CopyTo(file);
                }
            }
        }
    }
}
