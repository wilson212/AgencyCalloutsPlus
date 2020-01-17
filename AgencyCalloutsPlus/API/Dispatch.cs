using AgencyCalloutsPlus.Extensions;
using LSPD_First_Response.Mod.API;
using LSPD_First_Response.Mod.Callouts;
using Rage;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Xml;

namespace AgencyCalloutsPlus.API
{
    /// <summary>
    /// A class that handles the dispatching of Callouts based on the current 
    /// <see cref="AgencyType"/> in thier Jurisdiction.
    /// </summary>
    public static class Dispatch
    {
        /// <summary>
        /// Our lock object to prevent threading issues
        /// </summary>
        private static System.Object _lock = new System.Object();

        /// <summary>
        /// Timescale is 30:1 (30 seconds in game equals 1 second in real life)
        /// </summary>
        public static readonly int TimeScale = 30;

        /// <summary>
        /// Contains a list Scenarios seperated by CalloutType that will be used
        /// to populate the calls board
        /// </summary>
        private static Dictionary<CalloutType, SpawnGenerator<CalloutScenarioInfo>> ScenarioPool { get; set; }

        /// <summary>
        /// Randomizer method used to randomize callouts and locations
        /// </summary>
        private static CryptoRandom Randomizer { get; set; }

        /// <summary>
        /// Contains the last Call ID used
        /// </summary>
        private static int NextCallId { get; set; }

        /// <summary>
        /// Gets the player's current selected <see cref="Agency"/>
        /// </summary>
        public static Agency PlayerAgency { get; private set; }

        /// <summary>
        /// Gets the overall crime level definition for the current <see cref="Agency"/>
        /// </summary>
        public static CrimeLevel OverallCrimeLevel { get; private set; }

        /// <summary>
        /// Containts a range of time between calls.
        /// </summary>
        private static Range<int> CallTimerRange { get; set; }

        /// <summary>
        /// GameFiber containing the CallCenter functions
        /// </summary>
        private static GameFiber CallFiber { get; set; }

        /// <summary>
        /// GameFiber containing the AI Police and Dispatching functions
        /// </summary>
        private static GameFiber PoliceFiber { get; set; }

        /// <summary>
        /// Temporary: Containts a list of police vehicles
        /// </summary>
        private static List<OfficerUnit> OfficerUnits { get; set; }

        /// <summary>
        /// Our call Queue, seperated into 4 priority queues
        /// </summary>
        /// <remarks>
        /// 0 => IMMEDIATE EMERGENCY RESPONSE: The 3 closest units will be removed from priority 3 and 4 calls to respond
        /// 1 => EMERGENCY RESPONSE: The 2 closest units will be removed from priority 4 calls to respond
        /// 2 => EXPEDITED RESPONSE: Dispatched to a close unit that is available or will be done with thier call soon
        /// 3 => ROUTINE RESPONSE: Dispatched to an available unit that is close
        /// </remarks>
        private static List<PriorityCall>[] CallQueue { get; set; }

        /// <summary>
        /// Contains the priority call being dispatched to the player currently
        /// </summary>
        private static PriorityCall DispatchedToPlayer { get; set; }

        /// <summary>
        /// Static method called the first time this class is referenced anywhere
        /// </summary>
        static Dispatch()
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
                new List<PriorityCall>(4),  // IMMEDIATE EMERGENCY BROADCAST
                new List<PriorityCall>(8),  // EMERGENCY RESPONSE
                new List<PriorityCall>(12), // EXPEDITED RESPONSE
                new List<PriorityCall>(20), // ROUTINE RESPONSE
            };

            // Create next random call ID
            Randomizer = new CryptoRandom();
            NextCallId = Randomizer.Next(21234, 34567);
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
                    Log.Error("AgencyCalloutDispatcher.StartDuty(): Player Agency is null");
                    return false;
                }

                // Initialize data
                agency.InitializeData();

                // Did we change agency type?
                if (PlayerAgency == null || agency.AgencyType != PlayerAgency.AgencyType)
                {
                    LoadScenarios();
                    PlayerAgency = agency;

                    OfficerUnits = new List<OfficerUnit>(PlayerAgency.ActualPatrols);
                }

                // Clear all calls in the queue if we changed agency
                if (!agency.ScriptName.Equals(PlayerAgency.ScriptName))
                {
                    foreach (var callList in CallQueue)
                    {
                        callList.Clear();
                    }

                    PlayerAgency = agency;

                    if (OfficerUnits.Count != 0)
                        OfficerUnits = new List<OfficerUnit>(PlayerAgency.ActualPatrols);
                }

                // Debugging
                Log.Debug("Loaded with the following Agency data:");
                Log.Debug($"\t\tAgency Name: {agency.FriendlyName}");
                Log.Debug($"\t\tAgency Staff Level: {agency.StaffLevel}");
                Log.Debug($"\t\tAgency Zone Count: {agency.ZoneCount}");
                Log.Debug($"\t\tAgency Ideal Patrols: {agency.OptimumPatrols}");
                Log.Debug($"\t\tAgency Actual Patrols: {agency.ActualPatrols}");
                Log.Debug($"\t\tAgency Overall Crime Level: {agency.OverallCrimeLevel}");
                Log.Debug($"\t\tAgency Max Crime Level: {agency.MaxCrimeLevel}");

                // Determine our overall crime level in this agencies jurisdiction
                double percent = (agency.OverallCrimeLevel / (double)agency.MaxCrimeLevel);
                int val = (int)Math.Ceiling(percent * (int)CrimeLevel.VeryHigh);
                OverallCrimeLevel = (CrimeLevel)val;
                Log.Debug($"\t\tAgency Crime Definition: {OverallCrimeLevel}");

                // Fill Call Queue
                // Overall crime level is number of calls per 4 hours ?
                var callsPerHour = (int)(agency.OverallCrimeLevel / 4d);
                Log.Debug($"\t\tCalls Per Hour: {callsPerHour}");

                // 5s real life time equals 2.5m in game
                // Timescale is 30:1 (30 seconds in game equals 1 second in real life)
                // Every hour in game is 2 minutes in real life
                var hourGameTimeToSecondsRealTime = (60d / TimeScale) * 60;
                var callsPerSecondRT = (callsPerHour / hourGameTimeToSecondsRealTime);
                var realSecondsPerCall = (1d / callsPerSecondRT);
                var milliseconds = (int)(realSecondsPerCall * 1000);
                Log.Debug($"\t\tReal Seconds Per Call: {realSecondsPerCall}");

                // Create call timer range
                CallTimerRange = new Range<int>(
                    (int)(milliseconds / 1.5d),
                    (int)(milliseconds * 1.5d)
                );

                // Start timer
                BeginCallTimer();
                return true;
            }
            catch (Exception e)
            {
                Log.Exception(e);
            }

            return false;
        }

        public static PriorityCall RequestCallInfo(Type calloutType)
        {
            // Have we sent a call to the player?
            if (DispatchedToPlayer != null)
            {
                // Do our types match?
                string calloutName = calloutType.GetAttributeValue((CalloutInfoAttribute attr) => attr.Name);
                if (calloutName.Equals(DispatchedToPlayer.ScenarioInfo.CalloutName))
                {
                    return DispatchedToPlayer;
                }
            }

            return null;
        }

        /// <summary>
        /// Tells dispatch that the call is complete
        /// </summary>
        /// <param name="call"></param>
        public static void RegisterCallComplete(PriorityCall call)
        {
            var priority = call.Priority - 1;
            lock (_lock)
            {
                CallQueue[priority].Remove(call);
            }
        }

        /// <summary>
        /// Tells dispatch that we are on scene
        /// </summary>
        /// <param name="call"></param>
        public static void RegisterOnScene(PriorityCall call)
        {
            call.CallStatus = CallStatus.OnScene;
        }

        internal static void StopDuty()
        {
            StopCallTimer();   
        }

        /// <summary>
        /// Stops the 2 <see cref="GameFiber"/>(s) that run the call center
        /// and AI dispatching
        /// </summary>
        private static void StopCallTimer()
        {
            if (CallFiber != null && (CallFiber.IsAlive || CallFiber.IsSleeping))
            {
                CallFiber.Abort();
            }

            if (PoliceFiber != null && (PoliceFiber.IsAlive || PoliceFiber.IsSleeping))
            {
                PoliceFiber.Abort();
            }
        }

        /// <summary>
        /// Begins the 2 <see cref="GameFiber"/>(s) that run the call center
        /// and AI dispatching
        /// </summary>
        private static void BeginCallTimer()
        {
            // Always call Stop first!
            StopCallTimer();

            // Start fresh
            CallFiber = GameFiber.StartNew(delegate 
            {
                // While we are on duty accept calls
                while (Main.OnDuty)
                {
                    GenerateCall();

                    // Determine random time till next call
                    var time = Randomizer.Next(CallTimerRange.Minimum, CallTimerRange.Maximum);
                    Log.Debug($"Starting next call in {time}ms");

                    // Wait
                    GameFiber.Wait(time);
                }
            });

            PoliceFiber = GameFiber.StartNew(delegate
            {
                // Wait
                GameFiber.Wait(3000);

                // While we are on duty accept calls
                while (Main.OnDuty)
                {
                    try
                    {
                        DoPoliceChecks();
                    }
                    catch (Exception e)
                    {
                        Log.Exception(e);
                    }

                    // Wait
                    GameFiber.Wait(2000);
                }
            });

            GameFiber.StartNew(delegate
            {
                for (int i = 0; i < PlayerAgency.ActualPatrols - 1; i++)
                {
                    ZoneInfo zone = PlayerAgency.GetNextRandomCrimeZone();

                    var sp = zone.GetRandomSideOfRoadLocation();
                    while (sp == null)
                    {
                        zone = PlayerAgency.GetNextRandomCrimeZone();
                        sp = zone.GetRandomSideOfRoadLocation();
                    }

                    var car = PlayerAgency.SpawnPoliceVehicleOfType(PatrolType.LocalPatrol, sp);
                    car.IsPersistent = true;

                    var blip = car.AttachBlip();
                    blip.Color = Color.White;
                    blip.Sprite = BlipSprite.PolicePatrol;

                    var driver = car.CreateRandomDriver();
                    driver.IsPersistent = true;
                    driver.BlockPermanentEvents = true;

                    car.Driver.Tasks.CruiseWithVehicle(18, VehicleDrivingFlags.Normal);

                    var unit = new OfficerUnit(true, $"1-A-{i}", car)
                    {
                        VehicleBlip = blip
                    };
                    OfficerUnits.Add(unit);

                    unit.StartDuty();

                    // Yield
                    GameFiber.Yield();
                }

                OfficerUnits.Add(new OfficerUnit(false, "player", null) { Status = OfficerStatus.Busy });
            });
        }

        private static void DoPoliceChecks()
        {
            // If we have no  officers, then stop
            if (OfficerUnits.Count == 0)
                return;

            // Check AI police officers for finished calls. Removed finished call
            // from the call Queue
            var officers = (from x in OfficerUnits
                            where x.Status == OfficerStatus.Available
                            orderby x.LastStatusChange descending
                            select x).ToList();

            // If we have officers available for calls
            if (officers.Count > 0)
            {
                // Check priority 1 and 2 calls, and dispatch accordingly
                var calls = (from x in CallQueue[3]
                             where x.CallStatus == CallStatus.Created
                             orderby x.CallCreated descending
                             select x
                            ).Take(officers.Count).ToList();

                foreach (var call in calls)
                {
                    var officer = (from x in officers
                                   where x.Status == OfficerStatus.Available // This may have changed
                                   orderby x.PoliceCar.Position.DistanceTo(call.Location.Position) descending
                                   select x).FirstOrDefault();

                    // Do we have someone
                    if (officer != null)
                    {
                        if (officer.IsAIUnit)
                        {
                            officer.AssignToCall(call);
                        }
                        else
                        {
                            DispatchedToPlayer = call;
                            Functions.StartCallout(call.ScenarioInfo.CalloutName);
                        }
                    }
                }
            }

            // Any left over AI should be dispatched to priority 3 and 4 calls
            // after the call has sat for awhile
        }

        /// <summary>
        /// Generates a new call and adds it to the dispatch Queue
        /// </summary>
        private static void GenerateCall()
        {
            // Try to generate a call
            for (int i = 0; i < Settings.MaxLocationAttempts; i++)
            {
                try
                {
                    // Spawn a zone in our jurisdiction
                    ZoneInfo zone = PlayerAgency?.GetNextRandomCrimeZone();
                    if (zone == null)
                    {
                        Log.Debug($"Dispatch: Attempted to pull a zone but zone is null");
                        continue;
                    }

                    // Spawn crime type from our spawned zone
                    CalloutType type = zone.GetNextRandomCrimeType();
                    if (!ScenarioPool[type].TrySpawn(out CalloutScenarioInfo scenario))
                    {
                        Log.Debug($"Dispatch: Unable to pull CalloutType {type} from zone '{zone.FriendlyName}'");
                        continue;
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
                        Log.Debug($"Dispatch: Unable to pull Location type {scenario.LocationType} from zone '{zone.FriendlyName}'");
                        continue;
                    }

                    // Create PriorityCall wrapper
                    var call = new PriorityCall(NextCallId++, scenario)
                    {
                        Location = location,
                        CallStatus = CallStatus.Created,
                        ZoneScriptName = zone.ScriptName
                    };

                    // Add call to priority Queue
                    lock (_lock)
                    {
                        CallQueue[call.Priority - 1].Add(call);
                        Log.Debug($"Dispatch: Added Call to Queue '{scenario.Name}' in zone '{zone.FriendlyName}'");
                    }

                    // Stop
                    break;
                }
                catch (Exception ex)
                {
                    Log.Exception(ex);
                }
            }
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
            var assembly = typeof(Dispatch).Assembly;
            XmlDocument document = new XmlDocument();
            var agencyProbabilites = new Dictionary<AgencyType, int>(10);
            Agency agency = Agency.GetCurrentPlayerAgency();
            List<string> desc = new List<string>(5);

            // Load callout scripts
            foreach (var calloutDirectory in directory.GetDirectories())
            {
                // ensure CalloutMeta.xml exists
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
                            // Ensure we have attributes
                            if (n.Attributes == null)
                            {
                                Game.LogTrivial($"[WARN] AgencyCalloutsPlus: Scenario item has no attributes '{calloutName}->Scenarios->{n.Name}'");
                                continue;
                            }

                            // Try and extract probability value
                            if (n.Attributes["probability"]?.Value == null || !int.TryParse(n.Attributes["probability"].Value, out int prob))
                            {
                                Game.LogTrivial($"[WARN] AgencyCalloutsPlus: Unable to extract scenario probability value for '{calloutName}->Scenarios->{n.Name}'");
                                continue;
                            }

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
                                Probability = prob * aprob
                            };

                            // Try and extract probability value
                            XmlNode childNode = dispatchNode.SelectSingleNode("Priority");
                            if (!int.TryParse(childNode.InnerText, out int priority))
                            {
                                Game.LogTrivial(
                                    $"[WARN] AgencyCalloutsPlus: Unable to extract scenario priority value for '{calloutName}->Scenarios->{n.Name}'"
                                );
                                continue;
                            }
                            else
                            {
                                scene.Priority = priority;
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
    }
}
