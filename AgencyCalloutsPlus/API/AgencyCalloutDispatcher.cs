using AgencyCalloutsPlus.Extensions;
using LSPD_First_Response.Mod.API;
using LSPD_First_Response.Mod.Callouts;
using Rage;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Xml;

namespace AgencyCalloutsPlus.API
{
    /// <summary>
    /// A class that handles the dispatching of Callouts based on the current 
    /// <see cref="AgencyType"/> in thier Jurisdiction.
    /// </summary>
    [CalloutInfo("AgencyCalloutDispatcher", CalloutProbability.VeryHigh)]
    public sealed class AgencyCalloutDispatcher : Callout
    {
        /// <summary>
        /// Indicates whether the AgecnyCalloutsPlus callouts have been registered yet.
        /// </summary>
        public static bool IsInitialized { get; private set; } = false;

        private static Dictionary<CalloutType, SpawnGenerator<CalloutScenarioInfo>> ScenarioPool { get; set; }

        private static Timer CallTimer { get; set; }

        private static int NextCallId { get; set; }

        public static Agency LoadedAgency { get; private set; }

        public static ProbabilityLevel OverallCrimeLevel { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        private static List<PriorityCall>[] CallQueue { get; set; }

        public AgencyCalloutDispatcher()
        {
            if (!IsInitialized) LoadScenarios();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override bool OnBeforeCalloutDisplayed()
        {
            // Tell LSDPFR that NOPE, we call the shots here!
            return false;
        }

        /// <summary>
        /// Static method called the first time this class is referenced anywhere
        /// </summary>
        static AgencyCalloutDispatcher()
        {
            // Initialize callout types
            ScenarioPool = new Dictionary<CalloutType, SpawnGenerator<CalloutScenarioInfo>>();
            foreach (var type in Enum.GetValues(typeof(CalloutType)))
            {
                ScenarioPool.Add((CalloutType)type, new SpawnGenerator<CalloutScenarioInfo>());
            }

            // Create call Queue
            // See also: https://grantpark.org/info/16029
            CallQueue = new List<PriorityCall>[4] 
            {
                new List<PriorityCall>(5),  // IMMEDIATE EMERGENCY BROADCAST
                new List<PriorityCall>(5),  // EMERGENCY RESPONSE
                new List<PriorityCall>(8),  // EXPEDITED RESPONSE
                new List<PriorityCall>(10), // ROUTINE RESPONSE
            };

            NextCallId = new CryptoRandom().Next(21234, 34567);

            // Register this callout manager as a callout
            Functions.RegisterCallout(typeof(AgencyCalloutDispatcher));
        }

        /// <summary>
        /// Loads all of the AgencyCalloutsPlus callouts
        /// </summary>
        /// <returns>
        /// returns the number of callouts loaded
        /// </returns>
        private static int LoadScenarios()
        {
            // Load directory
            var directory = new DirectoryInfo(Path.Combine(Main.PluginFolderPath, "Callouts"));
            if (!directory.Exists)
            {
                Game.LogTrivial($"[ERROR] AgencyCalloutsPlus: Callouts directory is missing");
                throw new Exception($"[ERROR] AgencyCalloutsPlus: Callouts directory is missing");
            }

            // Clear old scenarios
            foreach (var item in ScenarioPool.Values)
            {
                item.Clear();
            }

            // Initialize vars
            int itemsAdded = 0;
            var assembly = typeof(AgencyCalloutDispatcher).Assembly;
            XmlDocument document = new XmlDocument();
            var agencyProbabilites = new Dictionary<AgencyType, int>(10);
            Agency agency = Agency.GetCurrentPlayerAgency();
            List<string> desc = new List<string>(5);

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
                    Type calloutType = assembly.GetType($"AgencyCalloutsPlus.Callouts.{calloutName}");
                    if (calloutType == null)
                    {
                        Game.LogTrivial($"[ERROR] AgencyCalloutsPlus: Unable to find CalloutType in Assembly: '{calloutName}'");
                        continue;
                    }

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
                    agencyProbabilites.Clear();

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

                        agencyProbabilites.Add(agencyType, probability);
                    }

                    // Grab the CalloutType
                    XmlNode calloutNode = document.DocumentElement.SelectSingleNode("CalloutType");
                    if (!Enum.TryParse(calloutNode.InnerText, out CalloutType crimeType))
                    {
                        Game.LogTrivial(
                            $"[WARN] AgencyCalloutsPlus: Unable to extract CalloutType value for '{calloutName}/CalloutMeta.xml'"
                        );
                        continue;
                    }

                    // If callout was added
                    if (agencyProbabilites.ContainsKey(agency.AgencyType))
                    {
                        // Get agency probability
                        int aprob = agencyProbabilites[agency.AgencyType];

                        // Cache scenarios
                        calloutName = calloutType.GetAttributeValue((CalloutInfoAttribute attr) => attr.Name);

                        // Process the XML scenarios
                        foreach (XmlNode n in document.DocumentElement.SelectSingleNode("Scenarios").ChildNodes)
                        {
                            // Get the Dispatch Node
                            XmlNode dispatchNode = n.SelectSingleNode("Dispatch");
                            if (dispatchNode == null)
                            {
                                Game.LogTrivial(
                                    $"[WARN] AgencyCalloutsPlus: Unable to extract scenario Dispatch node for '{calloutName}->Scenarios->{n.Name}'"
                                );
                                continue;
                            }

                            // Create scenario node
                            var scene = new CalloutScenarioInfo()
                            {
                                Name = n.Name,
                                CalloutName = calloutName,
                            };

                            // Try and extract probability value
                            XmlNode childNode = dispatchNode.SelectSingleNode("Priority");
                            if (!int.TryParse(childNode.InnerText, out int probability))
                            {
                                Game.LogTrivial(
                                    $"[WARN] AgencyCalloutsPlus: Unable to extract scenario probability value for '{calloutName}->Scenarios->{n.Name}'"
                                );
                                continue;
                            }
                            else
                            {
                                scene.Probability = probability * aprob;
                            }

                            // Try and extract Code value
                            childNode = dispatchNode.SelectSingleNode("Respond");
                            if (String.IsNullOrWhiteSpace(childNode.InnerText))
                            {
                                Game.LogTrivial(
                                    $"[WARN] AgencyCalloutsPlus: Unable to extract scenario respond value for '{calloutName}->Scenarios->{n.Name}'"
                                );
                                continue;
                            }
                            else
                            {
                                scene.RespondCode3 = (childNode.InnerText.Contains("3"));
                            }

                            // Grab the LocationType
                            childNode = dispatchNode.SelectSingleNode("LocationType");
                            if (!Enum.TryParse(childNode.InnerText, out LocationType locationType))
                            {
                                Game.LogTrivial(
                                    $"[WARN] AgencyCalloutsPlus: Unable to extract LocationType value for '{calloutName}->Scenarios->{n.Name}'"
                                );
                                continue;
                            }
                            else
                            {
                                scene.LocationType = locationType;
                            }

                            // Grab the Scanner
                            childNode = dispatchNode.SelectSingleNode("Scanner");
                            if (String.IsNullOrWhiteSpace(childNode.InnerText))
                            {
                                Game.LogTrivial(
                                    $"[WARN] AgencyCalloutsPlus: Unable to extract Scanner value for '{calloutName}->Scenarios->{n.Name}'"
                                );
                                continue;
                            }
                            else
                            {
                                scene.ScannerText = childNode.InnerText;
                            }

                            // Try and extract descriptions
                            childNode = dispatchNode.SelectSingleNode("Description");
                            if (childNode == null || !childNode.HasChildNodes)
                            {
                                Game.LogTrivial(
                                    $"[WARN] AgencyCalloutsPlus: Unable to extract scenario description values for '{calloutName}->Scenarios->{n.Name}'"
                                );
                                continue;
                            }
                            else
                            {
                                // Clear old descriptions
                                desc.Clear();
                                foreach (XmlNode descNode in childNode.ChildNodes)
                                {
                                    desc.Add(descNode.InnerText);
                                }
                                scene.Descriptions = desc.ToArray();
                            }

                            // Add scenario to the pool
                            ScenarioPool[crimeType].Add(scene);
                            itemsAdded++;
                        }

                        Functions.RegisterCallout(calloutType);
                    }
                }   
            }

            // Cleanup
            document = null;

            // Log and return
            Game.LogTrivial($"[TRACE] AgencyCalloutsPlus: Registered {itemsAdded} callout scenarios into CalloutWrapper");
            return itemsAdded;
        }

        /// <summary>
        /// Method called at the start of every duty
        /// </summary>
        internal static bool StartDuty()
        {
            try
            {
                // Get players current agency
                Agency agency = Agency.GetCurrentPlayerAgency();
                if (agency == null)
                {
                    Game.LogTrivial("[ERROR] AgencyCalloutsPlus: StartDuty() Player Agency is null");
                    return false;
                }

                // Initialize data
                agency.InitializeData();

                // Did we change agency type?
                if (LoadedAgency == null || agency.AgencyType != LoadedAgency.AgencyType)
                {
                    LoadScenarios();
                    LoadedAgency = agency;
                }

                // Clear all calls in the queue if we changed agency
                if (!agency.ScriptName.Equals(LoadedAgency.ScriptName))
                {
                    foreach (var callList in CallQueue)
                    {
                        callList.Clear();
                    }

                    LoadedAgency = agency;
                }

                // Debugging
                Game.LogTrivial("[DEBUG] AgencyCalloutsPlus: Loaded with the following Agency data:");
                Game.LogTrivial($"\t\t\tAgency Name: {agency.FriendlyName}");
                Game.LogTrivial($"\t\t\tAgency Staff Level: {agency.StaffLevel}");
                Game.LogTrivial($"\t\t\tAgency Zone Count: {agency.ZoneCount}");
                Game.LogTrivial($"\t\t\tAgency Ideal Patrols: {agency.OptimumPatrols}");
                Game.LogTrivial($"\t\t\tAgency Actual Patrols: {agency.ActualPatrols}");
                Game.LogTrivial($"\t\t\tAgency Overall Crime Level: {agency.OverallCrimeLevel}");
                Game.LogTrivial($"\t\t\tAgency Max Crime Level: {agency.MaxCrimeLevel}");

                // Determine our overall crime level in this agencies jurisdiction
                double percent = (agency.OverallCrimeLevel / (double)agency.MaxCrimeLevel);
                int val = (int)Math.Ceiling(percent * (int)ProbabilityLevel.VeryHigh);
                OverallCrimeLevel = (ProbabilityLevel)val;

                Game.LogTrivial($"\t\t\tAgency Crime Definition value: {val}");
                Game.LogTrivial($"\t\t\tAgency Crime Definition: {OverallCrimeLevel}");

                // Fill Call Queue
                // Overall crime level is number of calls per shift (8 hours) ?

                return true;
            }
            catch (Exception e)
            {
                ErrorHandler.HandleException(e);
            }

            return false;
        }

        internal static void StopDuty()
        {

        }

        internal static void BeginCallTimer()
        {
            // Get agency call frequency
        }

        internal static void OnCallTimerElapsed()
        {
            // Get agency 
            Agency agency = Agency.GetCurrentPlayerAgency();
            if (agency == null) return;

            // Spawn a zone in our jurisdiction
            ZoneInfo zone = agency.GetNextRandomCrimeZone();
            if (zone == null) return;

            // Spawn crime type from zone
            CalloutType type = zone.GetNextRandomCrimeType();

            // Spawn callout
            if (!ScenarioPool[type].TrySpawn(out CalloutScenarioInfo scenario))
            {
                return;
            }

            // Get a random location!
            GameLocation location = null;
            switch (scenario.LocationType)
            {
                case LocationType.SideOfRoad:
                    location = zone.GetRandomSideOfRoadLocation();
                    break;
            }

            // no location?
            if (location == null)
            {
                return;
            }

            // create call
            var call = new PriorityCall(NextCallId++, scenario)
            {
                Location = location,
                CallStatus = CallStatus.Created,
                ZoneShortName = zone.ScriptName
            };

            // Add call to priority Queue
            CallQueue[call.Priority - 1].Add(call);
        }
    }
}
