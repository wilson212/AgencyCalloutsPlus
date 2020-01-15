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

        internal static int AudioBeat { get; set; } = 18;

        /// <summary>
        /// The key to open the callout interaction menu
        /// </summary>
        internal static Keys OpenCalloutInteractionMenuKey { get; set; } = Keys.I;

        /// <summary>
        /// The modifier key to open the callout interaction menu
        /// </summary>
        internal static Keys OpenCalloutInteractionMenuModifierKey { get; set; } = Keys.None;

        internal static bool EnableFullSimulation { get; set; } = false;

        internal static void Initialize()
        {
            /// === Load AgencyCalloutsPlus settings === ///
            Game.LogTrivial("[TRACE] AgencyCalloutsPlus: Loading AgencyCalloutsPlus config...");

            string path = Path.Combine(Main.LSPDFRPluginPath, "AgencyCalloutsPlus.ini");
            var ini = new InitializationFile(path);
            ini.Create();

            // Read key bindings
            OpenCalloutInteractionMenuKey = ini.ReadEnum("KEYBINDINGS", "OpenCalloutInteractionMenuKey", Keys.I);
            OpenCalloutInteractionMenuModifierKey = ini.ReadEnum("KEYBINDINGS", "OpenCalloutInteractionMenuModifierKey", Keys.None);
            EnableFullSimulation = ini.ReadBoolean("GENERAL", "EnableFullSimulation", false);

            /// === Load Traffic Policer settings === ///
            Game.LogTrivial("[TRACE] AgencyCalloutsPlus: Loading Traffic Policer config...");

            path = Path.Combine(Main.LSPDFRPluginPath, "Traffic Policer.ini");
            ini = new InitializationFile(path);
            ini.Create();

            AudioDivision = ini.ReadInt32("General", "Division", 1);
            AudioUnitType = ini.ReadString("General", "UnitType", "LINCOLN").ToUpperInvariant();
            AudioBeat = ini.ReadInt32("General", "Beat", 18);
        }
    }
}
