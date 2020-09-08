using AgencyDispatchFramework.Dispatching;
using AgencyDispatchFramework.Game;
using AgencyDispatchFramework.Integration;
using AgencyDispatchFramework.NativeUI;
using LSPD_First_Response.Mod.API;
using Rage;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace AgencyDispatchFramework
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
        /// Gets the root folder path to the GTA V installation folder
        /// </summary>
        public static string GTARootPath { get; private set; }

        /// <summary>
        /// Gets the root folder path to the LSPDFR plugin folder
        /// </summary>
        public static string LSPDFRPluginPath { get; private set; }

        /// <summary>
        /// Gets the root folder path to the AgencyDispatchFramework plugin folder
        /// </summary>
        public static string ThisPluginFolderPath { get; internal set; }

        /// <summary>
        /// Contains the <see cref="RAGENativeUI.UIMenu"/> for this plgin
        /// </summary>
        private static ADACMainMenu PluginMenu { get; set; }

        /// <summary>
        /// Contains a list of dynamic link libraries this Plugin depends on
        /// </summary>
        private static readonly List<Dependancy> Dependencies = new List<Dependancy>
        {
            new Dependancy("Plugins/LSPD First Response.dll", new Version("0.4.6")),
            //new Dependancy("Plugins/LSPDFR/Traffic Policer.dll",  new Version("6.16.0.0")),
            new Dependancy("RAGENativeUI.dll", new Version("1.6.3.0")),
        };

        /// <summary>
        /// Main entry point for the plugin. Initializer
        /// </summary>
        public override void Initialize()
        {
            // Define our plugin version and root paths
            var assembly = Assembly.GetExecutingAssembly();
            PluginVersion = assembly.GetName().Version;
            UriBuilder uri = new UriBuilder(assembly.CodeBase);

            // Get GTA 5 root directory path
            GTARootPath = Path.GetDirectoryName(Uri.UnescapeDataString(uri.Path));
            LSPDFRPluginPath = Path.Combine(GTARootPath, "Plugins", "lspdfr");
            ThisPluginFolderPath = Path.Combine(GTARootPath, "Plugins", "lspdfr", "AgencyDispatchFramework");

            // Dependency checks
            foreach (var dep in Dependencies)
            {
                // Ensure file exists
                string path = Path.Combine(GTARootPath, dep.FilePath);
                if (!File.Exists(path))
                {
                    Rage.Game.DisplayNotification(
                        $"~r~Failed to locate ~b~{dep.FilePath}~r~, please make sure you have the dependency installed correctly."
                    );

                    throw new Exception($"Failed to locate missing dependency: {dep.FilePath}");
                }

                // Check minimum version
                var version = new Version(FileVersionInfo.GetVersionInfo(path).FileVersion);
                if (version < dep.MinimumVersion)
                {
                    Rage.Game.DisplayNotification(
                        $"~o~Detected that ~b~{dep.FilePath}~o~ isn't up to date, please download the required version (~y~{dep.MinimumVersion}~o~)."
                    );

                    throw new Exception($"Located a dependency that isn't up to date: {dep.FilePath}");
                }
            }

            // Initialize log file
            Log.Initialize(Path.Combine(ThisPluginFolderPath, "Game.log"));

            // Load settings
            Settings.Initialize();

            // Register for events
            AppDomain.CurrentDomain.AssemblyResolve += LSPDFRResolveEventHandler;
            Functions.OnOnDutyStateChanged += OnOnDutyStateChangedHandler;
            Functions.PlayerWentOnDutyFinishedSelection += PlayerWentOnDutyFinishedSelection;

            // Log stuff
            Rage.Game.LogTrivial("[TRACE] Agency Dispatch Framework v" + PluginVersion + " has been initialized.");
            Log.Info("Agency Dispatch Framework v" + PluginVersion + " has been initialized.");
        }

        /// <summary>
        /// Event fired when resolving assembly names in LSPDFR
        /// </summary>
        /// <returns></returns>
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

        /// <summary>
        /// Event fired when the player's Duty status changes
        /// </summary>
        /// <param name="onDuty">Indicates whether the player is going on or off duty</param>
        private void OnOnDutyStateChangedHandler(bool onDuty)
        {
            OnDuty = onDuty;
            if (onDuty)
            {
                // Did we spawn on duty?
                if (!Functions.IsPlayerInStationMenu())
                {
                    Log.Info("Detected that player spawned On Duty");
                    PlayerWentOnDutyFinishedSelection(true);
                }
            }
            else
            {
                // Stop generating calls
                Dispatch.StopDuty();
            }
        }

        /// <summary>
        /// Event fired when the Locker room menu is closed when going on duty
        /// </summary>
        private void PlayerWentOnDutyFinishedSelection()
        {
            PlayerWentOnDutyFinishedSelection(false);
        }

        /// <summary>
        /// Event fired when the Locker room menu is closed when going on duty
        /// </summary>
        /// <param name="spawnedOnDuty">Indicates whether the player spawned on duty when the game loaded</param>
        private void PlayerWentOnDutyFinishedSelection(bool spawnedOnDuty)
        {
            // Run this in a new thread, since this will block the main thread for awhile
            GameFiber.StartNew(delegate
            {
                // Wait!
                GameFiber.Wait(2000);

                // Initialize GameWorld FIRST!!! Important!
                GameWorld.Initialize();

                // Load our agencies and such (this will only initialize once per game session)
                Agency.Initialize();

                // Load vehicles (this will only initialize once per game session)
                VehicleInfo.Initialize();

                // Load peds (this will only initialize once per game session)
                PedInfo.Initialize();

                // Load locations based on current agency jurisdiction.
                // This method needs called everytime the player Agency is changed
                var numZonesLoaded = ZoneInfo.LoadZones(Agency.GetCurrentAgencyZoneNames());

                // If no zones are loaded, we cannot continue
                if (numZonesLoaded == 0)
                {
                    // Display notification to the player
                    Rage.Game.DisplayNotification(
                        "3dtextures",
                        "mpgroundlogo_cops",
                        "Agency Dispatch Framework",
                        "~o~Initialization Failed.",
                        $"~y~Selected Agency does not have any zones in it's jurisdiction."
                    );
                }
                // Finally, start dispatch call center
                else if (Dispatch.StartDuty())
                {
                    // Tell GameWorld to begin listening
                    GameWorld.BeginFibers();

                    // Begin listening for the Plugin Menu
                    PluginMenu = new ADACMainMenu();
                    PluginMenu.BeginListening();

                    // Check for and initialize API classes
                    ComputerPlusAPI.Initialize();
                    StopThePedAPI.Initialize();

                    // Initialize CAD
                    ComputerAidedDispatchMenu.Initialize();

                    // Display notification to the player
                    Rage.Game.DisplayNotification(
                        "3dtextures",
                        "mpgroundlogo_cops",
                        "Agency Dispatch Framework",
                        "~g~Plugin is Now Active.",
                        $"Now on duty serving ~g~{Dispatch.ZoneCount}~s~ zone(s)"
                    );
                }
                else
                {
                    // Display notification to the player
                    Rage.Game.DisplayNotification(
                        "3dtextures",
                        "mpgroundlogo_cops",
                        "Agency Dispatch Framework",
                        "~o~Initialization Failed.",
                        $"~y~Please check your Game.log for errors."
                    );
                }
            });
        }

        /// <summary>
        /// Gets a value indicating whether the specified plugin is running, and at the minimum version specified
        /// </summary>
        /// <param name="pluginName"></param>
        /// <param name="minversion"></param>
        /// <returns></returns>
        internal static bool IsLSPDFRPluginRunning(string pluginName, Version minversion = null)
        {
            foreach (Assembly assembly in Functions.GetAllUserPlugins())
            {
                AssemblyName an = assembly.GetName();
                if (an.Name.ToLower() == pluginName.ToLower())
                {
                    if (minversion == null || an.Version.CompareTo(minversion) >= 0)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Gets a value indicating whether the specified plugin is running, and returns the running version
        /// </summary>
        /// <param name="Plugin"></param>
        /// <param name="version"></param>
        /// <returns></returns>
        internal static bool IsLSPDFRPluginRunning(string Plugin, out Version version)
        {
            foreach (Assembly assembly in Functions.GetAllUserPlugins())
            {
                AssemblyName an = assembly.GetName();
                if (an.Name.ToLower() == Plugin.ToLower())
                {
                    version = an.Version;
                    return true;
                }
            }

            version = default(Version);
            return false;
        }

        /// <summary>
        /// Gets a value indicating whether the specified plugin is running, and at the minimum version specified.
        /// Returns the version that is running
        /// </summary>
        /// <param name="Plugin"></param>
        /// <param name="minversion"></param>
        /// <param name="version"></param>
        /// <returns></returns>
        internal static bool IsLSPDFRPluginRunning(string Plugin, Version minversion, out Version version)
        {
            foreach (Assembly assembly in Functions.GetAllUserPlugins())
            {
                AssemblyName an = assembly.GetName();
                if (an.Name.ToLower() == Plugin.ToLower())
                {
                    if (minversion == null || an.Version.CompareTo(minversion) >= 0)
                    {
                        version = an.Version;
                        return true;
                    }
                }
            }

            version = default(Version);
            return false;
        }

        public override void Finally()
        {
            Rage.Game.LogTrivial("[TRACE] AgencyDispatchFramework has been cleaned up.");
        }
    }
}
