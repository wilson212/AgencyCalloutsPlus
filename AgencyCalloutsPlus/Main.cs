using AgencyCalloutsPlus.API;
using AgencyCalloutsPlus.Integration;
using LSPD_First_Response.Mod.API;
using Rage;
using System;
using System.IO;


namespace AgencyCalloutsPlus
{
    /// <summary>
    /// Main entry point for this Plugin
    /// </summary>
    public class Main : Plugin
    {
        /// <summary>
        /// Gets the current plugin version
        /// </summary>
        public static Version PluginVersion { get; protected set; }

        /// <summary>
        /// Gets whether the player is currently on duty
        /// </summary>
        public static bool OnDuty { get; private set; } = false;

        /// <summary>
        /// Gets the root path to GTA V
        /// </summary>
        public static string GTARootPath { get; internal set; }

        /// <summary>
        /// Gets the root path to this DLL
        /// </summary>
        public static string LSPDFRPluginPath { get; internal set; }

        /// <summary>
        /// Gets the root path to this DLL
        /// </summary>
        public static string PluginFolderPath { get; internal set; }

        /// <summary>
        /// Main entry point for the plugin. Initializer
        /// </summary>
        public override void Initialize()
        {
            // Version check!
            var lspdfrVersion = Functions.GetVersion();
            var requiredVersion = new Version("0.4.6");

            // Check LSPDFR compatibility
            if (lspdfrVersion < requiredVersion)
            {
                string message = $"Detected LSPDFR version {lspdfrVersion}; v{requiredVersion} or greater is required";
                Game.LogTrivial($"[ERROR] AgencyCalloutsPlus: {message}");
                throw new Exception(message);
            }
            else
            {
                Game.LogTrivial($"[TRACE] AgencyCalloutsPlus: Detected LSPDFR v{lspdfrVersion}");
            }

            // Define our plugin version and root paths
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            PluginVersion = assembly.GetName().Version;

            // Get GTA 5 root directory path
            string codeBase = assembly.CodeBase;
            UriBuilder uri = new UriBuilder(codeBase);
            string path = Uri.UnescapeDataString(uri.Path);
            GTARootPath = Path.GetDirectoryName(path);
            LSPDFRPluginPath = Path.Combine(GTARootPath, "Plugins", "lspdfr");
            PluginFolderPath = Path.Combine(GTARootPath, "Plugins", "lspdfr", "AgencyCalloutsPlus");

            // Ensure we are properly located
            if (!File.Exists(Path.Combine(GTARootPath, "GTA5.exe")))
            {
                Game.LogTrivial("[ERROR] AgencyCalloutsPlus v" + PluginVersion + " failed to initialise! Cannot find GTA5.exe");
                throw new Exception("[ERROR] AgencyCalloutsPlus v" + PluginVersion + " failed to initialise! Cannot find GTA5.exe");
            }

            // Register for On Duty state changes
            Functions.OnOnDutyStateChanged += OnOnDutyStateChangedHandler;
            Functions.PlayerWentOnDutyFinishedSelection += PlayerWentOnDutyFinishedSelection;

            // Log stuff
            Game.LogTrivial("[TRACE] AgencyCalloutsPlus v" + PluginVersion + " has been initialised.");
        }

        private void OnOnDutyStateChangedHandler(bool onDuty)
        {
            OnDuty = onDuty;
            if (onDuty)
            {
                // Did we spawn on duty?
                if (!Functions.IsPlayerInStationMenu())
                {
                    Game.LogTrivial("[TRACE] AgencyCalloutsPlus: Detected that player spawned On Duty");
                    PlayerWentOnDutyFinishedSelection();
                }
            }
        }

        private void PlayerWentOnDutyFinishedSelection()
        {
            // Run this in a new thread, since this will block the main thread for awhile
            GameFiber.StartNew(delegate
            {
                // Wait!
                GameFiber.Wait(1000);

                // We require traffic policer by Albo1125!
                if (!GlobalFunctions.IsLSPDFRPluginRunning("Traffic Policer", new Version("6.16.0.0"), out Version version))
                {
                    string message = "";
                    if (version.Major > 0)
                        message = $"Detected Traffic Policer version {version}; v6.16.0.0 or greater is required";
                    else
                        message = $"Traffic Policer not detected and is required";

                    // Log and exit
                    Game.LogTrivial($"[ERROR] AgencyCalloutsPlus: {message}");
                    return;
                }
                else
                {
                    Game.LogTrivial($"[TRACE] AgencyCalloutsPlus: Detected Traffic Policer version {version}");
                }

                // Load our agencies and such (this will only initialize once per game session)
                Agency.Initialize();

                // Load locations based on current agency jurisdiction.
                // This method needs called everytime the player Agency is changed
                LocationInfo.LoadZones(Agency.GetCurrentAgencyJurisdictionZones());

                // Load vehicles (this will only initialize once per game session)
                VehicleInfo.Initialize();

                // Register callouts (this will only initialize once per game session)
                AgencyCalloutDispatcher.Initialize();

                // See what else is running
                ComputerPlusAPI.Initialize();
            });
        }

        public override void Finally()
        {
            Game.LogTrivial("[TRACE] AgencyCalloutsPlus has been cleaned up.");
        }
    }
}
