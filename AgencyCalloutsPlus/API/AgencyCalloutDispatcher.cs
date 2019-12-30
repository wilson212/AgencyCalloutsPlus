using AgencyCalloutsPlus.Extensions;
using LSPD_First_Response.Mod.API;
using LSPD_First_Response.Mod.Callouts;
using Rage;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace AgencyCalloutsPlus.API
{
    /// <summary>
    /// A class that handles the dispatching of Callouts based on the current 
    /// <see cref="AgencyType"/> in thier Jurisdiction.
    /// </summary>
    [CalloutInfo("AgencyCalloutDispatcher", CalloutProbability.VeryHigh)]
    public class AgencyCalloutDispatcher : Callout
    {
        /// <summary>
        /// Indicates whether the AgecnyCalloutsPlus callouts have been registered yet.
        /// </summary>
        private static bool IsInitialized { get; set; } = false;

        /// <summary>
        /// Contains a list of callouts registered
        /// </summary>
        private static Dictionary<AgencyType, SpawnGenerator<SpawnableCallout>> Callouts { get; set; }

        public AgencyCalloutDispatcher()
        {
            if (!IsInitialized) Initialize();
        }

        /// <summary>
        /// This is the magic of this Callout class. Loads a callout based on <see cref="AgencyType"/>
        /// and probability.
        /// </summary>
        /// <returns></returns>
        public override bool OnBeforeCalloutDisplayed()
        {
            try
            {
                // Loads a random callout from the callout selection based on CURRENT AGENCY
                string name = Functions.GetCurrentAgencyScriptName();
                AgencyType type = Agency.GetAgencyTypeByName(name);
                if (!Callouts[type].TrySpawn(out SpawnableCallout spawned))
                {
                    Game.LogTrivial("[ERROR] AgencyCalloutsPlus: Unable to spawn callout (No spawnable entities?)");
                    return false;
                }

                // Extract callouts name
                Type calloutType = spawned.CalloutType;
                string calloutName = calloutType.GetAttributeValue((CalloutInfoAttribute attr) => attr.Name);

                // Start callout manually
                // Make sure to do this in a new thread, other wise the callout will
                // get cleaned up with this instance and fail to work properly.
                Game.LogTrivial($"[TRACE] AgencyCalloutsPlus: Loading callout {calloutName}...");
                GameFiber.StartNew(delegate
                {
                    GameFiber.Sleep(2000);
                    Functions.StartCallout(calloutName);
                });
            }
            catch (Exception e)
            {
                ErrorHandler.HandleException(e);
            }

            return false;
        }

        /// <summary>
        /// Static method called the first time this class is referenced anywhere
        /// </summary>
        static AgencyCalloutDispatcher()
        {
            // Initialize callout types
            Callouts = new Dictionary<AgencyType, SpawnGenerator<SpawnableCallout>>(10);
            foreach (var type in Enum.GetValues(typeof(AgencyType)))
            {
                Callouts.Add((AgencyType)type, new SpawnGenerator<SpawnableCallout>());
            }

            // Register this callout manager as a callout
            Functions.RegisterCallout(typeof(AgencyCalloutDispatcher));
        }

        /// <summary>
        /// Registers a callout with the indicated probability of spawning for the indicated
        /// <see cref="AgencyType"/>(s). The Callout should have a <see cref="CalloutProbability"/> attribute
        /// of <see cref="CalloutProbability.Never"/>!!
        /// </summary>
        /// <param name="calloutType"></param>
        /// <param name="calloutProbability"></param>
        /// <param name="types"></param>
        public static bool RegisterCallout(Type calloutType, int calloutProbability, AgencyType[] types)
        {
            // Ensure this is actually a callout
            if (!calloutType.IsSubclassOf(typeof(Callout)))
            {
                return false;
            }

            // Ensure we have at least one agency type
            if (types.Length > 0)
            {
                // Add the callout to each agency
                foreach (AgencyType agencyType in types)
                {
                    Callouts[agencyType].Add(new SpawnableCallout(calloutType, calloutProbability));
                }

                Functions.RegisterCallout(calloutType);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Loads all of the AgencyCalloutsPlus callouts
        /// </summary>
        /// <returns>
        /// returns the number of callouts loaded
        /// </returns>
        internal static int Initialize()
        {
            if (IsInitialized) return 0;

            // Set internal flag to initialize just once
            IsInitialized = true;

            // Initialize vars
            int itemsAdded = 0;
            var assembly = typeof(AgencyCalloutDispatcher).Assembly;
            XmlDocument document = new XmlDocument();
            string[] calloutTypes = { "Priority", "Routine", "Traffic" };

            // Itterate through each callout type
            foreach (string type in calloutTypes)
            {
                // Load directory
                var directory = new DirectoryInfo(Path.Combine(Main.PluginFolderPath, "Callouts", type));
                if (!directory.Exists) continue;

                // Load callout scripts
                foreach (var calloutDirectory in directory.GetDirectories())
                {
                    // ensure CalloutMeta.xml exists
                    bool added = false;
                    string path = Path.Combine(calloutDirectory.FullName, "CalloutMeta.xml");
                    if (File.Exists(path))
                    {
                        // define vars
                        string calloutName = calloutDirectory.Name;
                        Type calloutType = assembly.GetType($"AgencyCalloutsPlus.Callouts.{type}.{calloutName}");

                        // Load XML document
                        document = new XmlDocument();
                        using (var file = new FileStream(path, FileMode.Open))
                        {
                            document.Load(file);
                        }

                        // Grab agency list
                        XmlNode agenciesNode = document.DocumentElement.SelectSingleNode("Agencies");

                        // Skip and log errors
                        if (agenciesNode == null)
                        {
                            Game.LogTrivial($"[ERROR] AgencyCalloutsPlus: Unable to load agency data in CalloutMeta for '{calloutDirectory.Name}'");
                            continue;
                        }

                        // No data?
                        if (!agenciesNode.HasChildNodes) continue;

                        // Itterate through items
                        foreach (XmlNode n in agenciesNode.SelectNodes("Agency"))
                        {
                            // Ensure we have attributes
                            if (n.Attributes == null)
                            {
                                Game.LogTrivial(
                                    $"[WARN] AgencyCalloutsPlus: Agency item has no attributes in '{calloutName}/CalloutMeta.xml->Agencies'"
                                );
                                continue;
                            }

                            // Try and extract type value
                            if (!Enum.TryParse(n.Attributes["type"].Value, out AgencyType agencyType))
                            {
                                Game.LogTrivial(
                                    $"[WARN] AgencyCalloutsPlus: Unable to extract Agency type value for '{calloutName}/CalloutMeta.xml'"
                                );
                                continue;
                            }

                            // Try and extract probability value
                            if (!int.TryParse(n.Attributes["probability"].Value, out int probability))
                            {
                                Game.LogTrivial(
                                    $"[WARN] AgencyCalloutsPlus: Unable to extract Agency probability value for '{calloutName}/CalloutMeta.xml'"
                                );
                            }

                            // Add callout to the registry
                            Callouts[agencyType].Add(new SpawnableCallout(calloutType, probability));
                            added = true;
                        }

                        if (added)
                        {
                            itemsAdded++;
                            Functions.RegisterCallout(calloutType);
                        }
                    }   
                }
            }

            // Log and return
            Game.LogTrivial($"[TRACE] AgencyCalloutsPlus: Registered {itemsAdded} callouts into CalloutWrapper");
            return itemsAdded;
        }
    }
}
