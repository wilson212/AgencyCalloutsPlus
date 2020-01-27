using AgencyCalloutsPlus.API;
using AgencyCalloutsPlus.Integration;
using LSPD_First_Response.Mod.API;
using Rage;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;

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
        /// Gets the root path to the LSPDFR plugin folder
        /// </summary>
        public static string LSPDFRPluginPath { get; internal set; }

        /// <summary>
        /// Gets the root path to the AgencyCalloutsPlus plugin folder
        /// </summary>
        public static string PluginFolderPath { get; internal set; }

        /// <summary>
        /// Contains a list of dynamic link libraries this Plugin depends on
        /// </summary>
        private static readonly List<Dependancy> DependenciesToCheck = new List<Dependancy>
        {
            new Dependancy("Plugins/LSPD First Response.dll", new Version("0.4.6")),
            new Dependancy("Plugins/LSPDFR/Traffic Policer.dll",  new Version("6.16.0.0")),
            new Dependancy("RAGENativeUI.dll", new Version("1.6.3.0")),
        };

        /// <summary>
        /// Main entry point for the plugin. Initializer
        /// </summary>
        public override void Initialize()
        {
            // Define our plugin version and root paths
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            PluginVersion = assembly.GetName().Version;
            UriBuilder uri = new UriBuilder(assembly.CodeBase);

            // Get GTA 5 root directory path
            GTARootPath = Path.GetDirectoryName(Uri.UnescapeDataString(uri.Path));
            LSPDFRPluginPath = Path.Combine(GTARootPath, "Plugins", "lspdfr");
            PluginFolderPath = Path.Combine(GTARootPath, "Plugins", "lspdfr", "AgencyCalloutsPlus");

            // Dependency checks
            foreach (var dependency in DependenciesToCheck)
            {
                // Ensure file exists
                string path = Path.Combine(GTARootPath, dependency.FilePath);
                if (!File.Exists(path))
                {
                    Game.DisplayNotification(
                        $"~r~Failed to locate ~b~{dependency.FilePath}~r~, please make sure you have the dependency installed correctly."
                    );

                    throw new Exception($"Failed to locate missing dependency: {dependency.FilePath}");
                }

                // Check minimum version
                var version = new Version(FileVersionInfo.GetVersionInfo(path).FileVersion);
                if (version < dependency.MinimumVersion)
                {
                    Game.DisplayNotification(
                        $"~o~Detected that ~b~{dependency.FilePath}~o~ isn't up to date, please download the required version (~y~{dependency.MinimumVersion}~o~)."
                    );

                    throw new Exception($"Located a dependency that isn't up to date: {dependency.FilePath}");
                }
            }

            // Load settings
            Settings.Initialize();

            // Initialize log file
            Log.Initialize(Path.Combine(PluginFolderPath, "Game.log"));

            // Register for On Duty state changes
            AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(LSPDFRResolveEventHandler);
            Functions.OnOnDutyStateChanged += OnOnDutyStateChangedHandler;
            Functions.PlayerWentOnDutyFinishedSelection += PlayerWentOnDutyFinishedSelection;

            // Log stuff
            Game.LogTrivial("[TRACE] Agency Dispatch and Callouts Plus v" + PluginVersion + " has been initialised.");
        }

        private Assembly LSPDFRResolveEventHandler(object sender, ResolveEventArgs args)
        {
            foreach (Assembly assembly in Functions.GetAllUserPlugins())
            {
                if (args.Name.ToLower().Contains(assembly.GetName().Name.ToLower()))
                {
                    return assembly;
                }
            }
            return null;
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
                    PlayerWentOnDutyFinishedSelection(true);
                }
            }
            else
            {
                Dispatch.StopDuty();
            }
        }

        private void PlayerWentOnDutyFinishedSelection()
        {
            PlayerWentOnDutyFinishedSelection(false);
        }

        private void PlayerWentOnDutyFinishedSelection(bool spawnedOnDuty)
        {
            // Run this in a new thread, since this will block the main thread for awhile
            GameFiber.StartNew(delegate
            {
                // Wait!
                GameFiber.Wait(2000);

                // Load our agencies and such (this will only initialize once per game session)
                Agency.Initialize();

                // Load locations based on current agency jurisdiction.
                // This method needs called everytime the player Agency is changed
                ZoneInfo.LoadZones(Agency.GetCurrentAgencyJurisdictionZones());

                // Load vehicles (this will only initialize once per game session)
                VehicleInfo.Initialize();

                // See what else is running
                ComputerPlusAPI.Initialize();

                // Finally, start dispatch call center
                if (Dispatch.StartDuty())
                {
                    var agency = Dispatch.PlayerAgency;
                    var lvl = Dispatch.OverallCrimeLevel;
                    Game.DisplayNotification(
                        "3dtextures",
                        "mpgroundlogo_cops",
                        "Agency Dispatch and Callouts+",
                        "~g~Plugin is Now Active.",
                        $"Now on duty serving ~g~{agency.ZoneCount}~s~ zone(s) with an overall crime level of ~b~{lvl}"
                    );
                }
            });
        }

        public override void Finally()
        {
            Game.LogTrivial("[TRACE] AgencyCalloutsPlus has been cleaned up.");
        }
    }
}
