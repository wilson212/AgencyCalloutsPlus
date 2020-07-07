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

        /// <summary>
        /// The key to open the callout interaction menu
        /// </summary>
        internal static Keys OpenCalloutMenuKey { get; set; } = Keys.F10;

        /// <summary>
        /// The modifier key to open the callout interaction menu
        /// </summary>
        internal static Keys OpenCalloutMenuModifierKey { get; set; } = Keys.None;

        /// <summary>
        /// Gets or sets whether to load the On Duty Status Hud
        /// </summary>
        internal static bool EnableHud { get; set; } = true;

        /// <summary>
        /// Gets or sets the horizontal position of the <see cref="Mod.UI.ContainerElement"/> for the status HUD
        /// </summary>
        internal static float HudPositionX { get; set; } = 320f;

        /// <summary>
        /// Gets or sets the vertical position of the <see cref="Mod.UI.ContainerElement"/> for the status HUD
        /// </summary>
        internal static float HudPositionY { get; set; } = 800f;

        /// <summary>
        /// Gets or sets the text size of the <see cref="Mod.UI.ContainerElement"/> for the status HUD
        /// </summary>
        internal static float HudTextScale { get; set; } = 1.0f;

        /// <summary>
        /// Indicates whether to enable full simulation mode
        /// </summary>
        internal static bool EnableFullSimulation { get; set; } = false;

        /// <summary>
        /// Loads the user settings from the ini file
        /// </summary>
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
            OpenCalloutMenuKey = ini.ReadEnum("KEYBINDINGS", "OpenCalloutMenuKey", Keys.F10);
            OpenCalloutMenuModifierKey = ini.ReadEnum("KEYBINDINGS", "OpenCalloutMenuModifierKey", Keys.None);

            AudioDivision = ini.ReadInt32("GENERAL", "Division", 1);
            AudioUnitType = ini.ReadString("GENERAL", "UnitType", "LINCOLN").ToUpperInvariant();
            AudioBeat = ini.ReadInt32("GENERAL", "Beat", 18);
            TimeScale = ini.ReadInt32("GENERAL", "TimeScale", 30);

            EnableHud = ini.ReadBoolean("HUD", "EnableHUD", true);
            HudPositionX = (float)ini.ReadDouble("HUD", "HudPositionX", 320);
            HudPositionY = (float)ini.ReadDouble("HUD", "HudPositionY", 800);
            HudTextScale = (float)ini.ReadDouble("HUD", "HudTextScale", 1.0);

            EnableFullSimulation = ini.ReadBoolean("SIMULATION", "EnableFullSimulation", false);

            // Log
            Log.Info("Loaded AgencyCalloutsPlus config successfully!");
        }

        /// <summary>
        /// Saves the current settings to the ini file
        /// </summary>
        internal static void Save()
        {
            // Log
            Log.Info("Saving AgencyCalloutsPlus config...");

            // Ensure file exists
            string path = Path.Combine(Main.LSPDFRPluginPath, "AgencyCalloutsPlus.ini");
            EnsureConfigExists(path);

            // Open ini file
            var ini = new InitializationFile(path);
            ini.Create();

            // Save settings
            //ini.Write("KEYBINDINGS", "OpenMenuKey", OpenMenuKey);
            //ini.Write("KEYBINDINGS", "OpenMenuModifierKey", OpenMenuModifierKey);

            // Log
            Log.Info("Saved AgencyCalloutsPlus config successfully!");
        }

        /// <summary>
        /// Ensures the ini file exists. If not, a new ini is created with the default settings.
        /// </summary>
        /// <param name="path"></param>
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
