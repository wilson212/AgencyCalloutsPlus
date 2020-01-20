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
        /// Contains a list Scenarios seperated by CalloutType that will be used
        /// to populate the calls board
        /// </summary>
        private static Dictionary<CalloutType, SpawnGenerator<CalloutScenarioInfo>> ScenarioPool { get; set; }

        /// <summary>
        /// Contains a list of scenario's by callout name
        /// </summary>
        private static Dictionary<string, SpawnGenerator<CalloutScenarioInfo>> Scenarios { get; set; }

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
        /// Gets the player's <see cref="OfficerUnit"/> instance
        /// </summary>
        private static OfficerUnit PlayerUnit { get; set; }

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
            Scenarios = new Dictionary<string, SpawnGenerator<CalloutScenarioInfo>>();
            ScenarioPool = new Dictionary<CalloutType, SpawnGenerator<CalloutScenarioInfo>>(8);
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

        #region Callout Methods

        /// <summary>
        /// Requests a <see cref="PriorityCall"/> from dispatch using the specified
        /// <see cref="AgencyCallout"/>.
        /// </summary>
        /// <param name="calloutType"></param>
        /// <returns></returns>
        public static PriorityCall RequestCallInfo(Type calloutType)
        {
            // Extract Callout Name
            string calloutName = calloutType.GetAttributeValue((CalloutInfoAttribute attr) => attr.Name);

            // Have we sent a call to the player?
            if (DispatchedToPlayer != null)
            {
                // Do our types match?
                if (calloutName.Equals(DispatchedToPlayer.ScenarioInfo.CalloutName))
                {
                    return DispatchedToPlayer;
                }
                else
                {
                    // Cancel
                    DispatchedToPlayer.CallStatus = CallStatus.DeclinedByPlayer;
                    DispatchedToPlayer = null;
                    Log.Info($"Dispatch.RequestCallInfo: It appears that the player spawned a callout of type {calloutName}");
                }
            }

            // If we are here, maybe the player used a console command to start the callout.
            // At this point, see if we have anything in the call Queue
            if (Scenarios.ContainsKey(calloutName))
            {
                CallStatus[] status = { CallStatus.Created, CallStatus.WaitingForPlayerAccept, CallStatus.DeclinedByPlayer };

                // Lets see if we have a call already created
                for (int i = 0; i < 4; i++)
                {
                    var calls = (
                        from x in CallQueue[i]
                        where x.ScenarioInfo.CalloutName.Equals(calloutName) && status.Contains(x.CallStatus)
                        select x
                    ).ToArray();

                    // Do we have any active calls?
                    if (calls.Length > 0)
                    {
                        DispatchedToPlayer = calls[0];
                        return calls[0];
                    }
                }

                // Still here? Maybe we can create a call?
            }

            // cant do anything at this point
            Log.Warning($"Dispatch.RequestCallInfo: Unable to spawn a scenario for {calloutName}");
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

            // Set player status
            if (call.PrimaryOfficer == PlayerUnit)
            {
                PlayerUnit.CompleteCall(CallCloseFlag.Completed);
                PlayerUnit.Status = OfficerStatus.Available;
            }
        }

        /// <summary>
        /// Tells dispatch that we are on scene
        /// </summary>
        /// <param name="call"></param>
        public static void RegisterOnScene(PriorityCall call)
        {
            call.CallStatus = CallStatus.OnScene;
            call.PrimaryOfficer.Status = OfficerStatus.OnScene;
            call.PrimaryOfficer.LastStatusChange = World.DateTime;
        }

        /// <summary>
        /// Tells dispatch the AI officer has died
        /// </summary>
        /// <param name="officer"></param>
        internal static void RegisterDeadOfficer(OfficerUnit officer)
        {
            if (officer.IsAIUnit)
            {
                // Remove officer
                OfficerUnits.Remove(officer);

                // Dispose
                officer.Dispose();

                // Show player notification
                Game.DisplayNotification(
                    "3dtextures",
                    "mpgroundlogo_cops",
                    "Agency Dispatch and Callouts+",
                    "~g~An Officer has Died.",
                    $"Unit ~g~{officer.UnitString}~s~ is no longer with us"
                );
            }
        }

        /// <summary>
        /// Tells dispatch that we are on scene
        /// </summary>
        /// <param name="call"></param>
        /// <remarks>Used for Player only, not AI</remarks>
        public static void CalloutAccepted(PriorityCall call)
        {
            PlayerUnit.AssignToCall(call);
        }

        /// <summary>
        /// Tells dispatch that did not accept the callout
        /// </summary>
        /// <param name="call"></param>
        /// <remarks>Used for Player only, not AI</remarks>
        public static void CalloutNotAccepted(PriorityCall call)
        {
            // Cancel
            if (DispatchedToPlayer != null && call == DispatchedToPlayer)
            {
                // Remove dispatched call from player
                DispatchedToPlayer.CallStatus = CallStatus.DeclinedByPlayer;
                DispatchedToPlayer = null;
                Log.Info($"Dispatch: Player declined callout scenario {call.ScenarioInfo.Name}");

                // Is this an important call?
                if (call.Priority < 3)
                {
                    // Dispatch one more AI unit to this call
                    // @todo
                }

                // Set a delay timer for next player call out?
                // @todo
            }
        }

        /// <summary>
        /// Dispatches the provided <see cref="OfficerUnit"/> to the provided
        /// <see cref="PriorityCall"/>. If the <paramref name="officer"/> is
        /// the Player, then the callout is started
        /// </summary>
        /// <param name="officer"></param>
        /// <param name="call"></param>
        private static void DispatchUnitToCall(OfficerUnit officer, PriorityCall call)
        {
            if (officer.IsAIUnit)
            {
                officer.AssignToCall(call);
            }
            else
            {
                DispatchedToPlayer = call;
                DispatchedToPlayer.CallStatus = CallStatus.WaitingForPlayerAccept;
                Functions.StartCallout(call.ScenarioInfo.CalloutName);
            }
        }

        #endregion Callout Methods

        #region Duty Methods

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

                    // Dispose of old officer AI units
                    if (OfficerUnits.Count != 0)
                    {
                        foreach (var officer in OfficerUnits)
                            officer.Dispose();

                        OfficerUnits = new List<OfficerUnit>(PlayerAgency.ActualPatrols);
                    }
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
                Log.Debug($"\t\tCalls Per Hour (Average): {callsPerHour}");

                // 5s real life time equals 2.5m in game
                // Timescale is 30:1 (30 seconds in game equals 1 second in real life)
                // Every hour in game is 2 minutes in real life
                var hourGameTimeToSecondsRealTime = (60d / TimeScale.GameTimeScale) * 60;
                var callsPerSecondRT = (callsPerHour / hourGameTimeToSecondsRealTime);
                var realSecondsPerCall = (1d / callsPerSecondRT);
                var milliseconds = (int)(realSecondsPerCall * 1000);
                Log.Debug($"\t\tReal Seconds Per Call (Average): {realSecondsPerCall}");

                // Create call timer range
                CallTimerRange = new Range<int>(
                    (int)(milliseconds / 2d),
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

        internal static void StopDuty()
        {
            StopCallTimer();   
        }

        #endregion Duty Methods

        #region Timer Methods

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

            // To be replaced later @todo
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

                    var unit = new OfficerUnit(driver, $"1-A-{i}", car) { VehicleBlip = blip };
                    unit.StartDuty();
                    OfficerUnits.Add(unit);

                    // Yield
                    GameFiber.Yield();
                }

                PlayerUnit = new OfficerUnit(Game.LocalPlayer) { Status = OfficerStatus.Busy };
                OfficerUnits.Add(PlayerUnit);
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
        }

        #endregion Timer Methods

        #region GameFiber methods

        /// <summary>
        /// Main thread loop for Dispatch handling calls and player activity
        /// </summary>
        private static void DoPoliceChecks()
        {
            // Keep watch on the players status
            if (Functions.GetCurrentPullover() != default(LHandle))
            {
                PlayerUnit.Status = OfficerStatus.OnTrafficStop;
            }
            else if (Functions.IsPlayerAvailableForCalls())
            {
                if (Functions.IsCalloutRunning())
                {
                    PlayerUnit.Status = OfficerStatus.Busy;
                }
                else if (PlayerUnit.Status != OfficerStatus.Break)
                {
                    PlayerUnit.Status = OfficerStatus.Available;
                }
            }
            else if (PlayerUnit.Status == OfficerStatus.Available)
            {
                // Available but not available
                PlayerUnit.Status = OfficerStatus.Busy;
            }

            // If we have no  officers, then stop
            if (OfficerUnits.Count == 0)
                return;

            // Check AI police officers for finished calls. Removed finished call
            // from the call Queue
            var availableOfficers = (
                from x in OfficerUnits
                where x.Status == OfficerStatus.Available
                orderby x.LastStatusChange ascending // Oldest first
                select x
            ).ToList();

            // If we have officers available for calls
            if (availableOfficers.Count == 0)
                return;

            // Stop calls from coming in for right now
            lock (_lock)
            {
                // Itterate through each call priority Queue
                int available = availableOfficers.Count;
                for (int priority = 0; priority < 4; priority++)
                {
                    // Stop if we have no available officers or calls
                    if (available == 0 || CallQueue[priority].Count == 0)
                        continue;

                    // Grab open calls
                    var calls = (
                            from x in CallQueue[priority]
                            where x.CallStatus == CallStatus.Created || x.CallStatus == CallStatus.DeclinedByPlayer
                            orderby x.Priority ascending, x.CallCreated ascending // Oldest first
                            select x
                        ).Take(available).ToList();

                    // Check priority 1 and 2 calls, and dispatch accordingly
                    if (priority < 2)
                    {
                        foreach (var call in calls)
                        {
                            // Select closest available officer
                            int amount = (priority == 0) ? 3 : 2;
                            var officers = GetClosestAvailableOfficers(availableOfficers, call, amount);
                            if (officers != null || officers.Length > 0)
                            {
                                // Is player in this list?
                                var player = officers.Where(x => !x.IsAIUnit).FirstOrDefault();
                                if (player != null)
                                {
                                    DispatchUnitToCall(player, call);
                                    available--;
                                }
                                else
                                {
                                    DispatchUnitToCall(officers[0], call);
                                    officers = officers.Skip(1).ToArray();
                                    available--;
                                }
                                
                                // Add other units to the call
                                if (officers.Length > 0)
                                {
                                    foreach (var officer in officers)
                                    {
                                        // Skip player
                                        if (player != null && !officer.IsAIUnit)
                                            continue;

                                        // Dispatch
                                        DispatchUnitToCall(officer, call);
                                        available--;
                                    }
                                }
                            }
                        }
                    }
                    else // Priority 3 and 4 calls
                    {
                        foreach (var call in calls)
                        {
                            // Select closest available officer
                            var officer = GetClosestAvailableOfficer(availableOfficers, call);
                            if (officer != null)
                            {
                                DispatchUnitToCall(officer, call);
                                available--;
                            }
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
                        Zone = zone
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

        #endregion GameFiber methods

        #region Filter and Order methods

        /// <summary>
        /// Gets the closest available officer to a call based on Call priority dispatching
        /// requirements.
        /// </summary>
        /// <param name="availableOfficers"></param>
        /// <param name="call"></param>
        /// <returns></returns>
        private static OfficerUnit GetClosestAvailableOfficer(List<OfficerUnit> availableOfficers, PriorityCall call)
        {
            // Stop if we have no available officers
            if (availableOfficers.Count == 0)
                return null;

            // As call priority changes, so does the dispatching requirements of the call
            var officers = FilterOfficers(availableOfficers, call);
            if (officers == null)
                return null;

            // Order officers by distance to the call
            var ordered = officers.OrderBy(x => x.Officer.Position.DistanceTo(call.Location.Position));
            return ordered.FirstOrDefault();
        }

        /// <summary>
        /// Gets the closest available officer to a call based on Call priority dispatching
        /// requirements.
        /// </summary>
        /// <param name="availableOfficers"></param>
        /// <param name="call"></param>
        /// <returns></returns>
        private static OfficerUnit[] GetClosestAvailableOfficers(List<OfficerUnit> availableOfficers, PriorityCall call, int count)
        {
            // Stop if we have no available officers
            if (availableOfficers.Count == 0)
                return null;

            // As call priority changes, so does the dispatching requirements of the call
            var officers = FilterOfficers(availableOfficers, call);
            if (officers == null)
                return null;

            // Order officers by distance to the call
            var ordered = officers.OrderBy(x => x.Officer.Position.DistanceTo(call.Location.Position));
            return ordered.Take(count).ToArray();
        }

        /// <summary>
        /// Filters available officers depending on the call priority
        /// </summary>
        /// <param name="availableOfficers"></param>
        /// <param name="call"></param>
        /// <returns></returns>
        private static IEnumerable<OfficerUnit> FilterOfficers(List<OfficerUnit> availableOfficers, PriorityCall call)
        {
            switch (call.Priority)
            {
                case 0: // Priority 1
                    return availableOfficers.Where(x => x.CurrentCall == null || x.CurrentCall.Priority > 2);
                case 1: // Priority 2
                    return availableOfficers.Where(x => x.CurrentCall == null || x.CurrentCall.Priority > 2);
                case 2: // Priority 3
                    if (call.CallStatus == CallStatus.DeclinedByPlayer)
                        return availableOfficers.Where(x => x.Status == OfficerStatus.Available && x.IsAIUnit);
                    else
                        return availableOfficers.Where(x => x.Status == OfficerStatus.Available);
                default: // Priority 4
                    return availableOfficers.Where(x => x.Status == OfficerStatus.Available && x.IsAIUnit);
            }
        }

        #endregion Filter and Order methods

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
                Log.Error($"Dispatch.LoadScenarios(): Callouts directory is missing");
                throw new Exception($"[ERROR] AgencyCalloutsPlus: Callouts directory is missing");
            }

            // Clear old scenarios
            Scenarios.Clear();
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
                        Log.Error($"Dispatch.LoadScenarios(): Unable to find CalloutType in Assembly: '{calloutName}'");
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
                        Log.Error($"Dispatch.LoadScenarios(): Unable to load agency data in CalloutMeta for '{calloutDirectory.Name}'");
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
                            Log.Warning(
                                $"Dispatch.LoadScenarios(): Agency item has no attributes in '{calloutName}/CalloutMeta.xml->Agencies'"
                            );
                            continue;
                        }

                        // Try and extract type value
                        if (!Enum.TryParse(n.Attributes["type"].Value, out AgencyType agencyType))
                        {
                            Log.Warning(
                                $"Dispatch.LoadScenarios(): Unable to extract Agency type value for '{calloutName}/CalloutMeta.xml'"
                            );
                            continue;
                        }

                        // Try and extract probability value
                        if (!int.TryParse(n.Attributes["probability"].Value, out int probability))
                        {
                            Log.Warning(
                                $"Dispatch.LoadScenarios(): Unable to extract Agency probability value for '{calloutName}/CalloutMeta.xml'"
                            );
                        }

                        agencyProbabilites.Add(agencyType, probability);
                    }

                    // Grab the CalloutType
                    XmlNode calloutNode = document.DocumentElement.SelectSingleNode("CalloutType");
                    if (!Enum.TryParse(calloutNode.InnerText, out CalloutType crimeType))
                    {
                        Log.Warning(
                            $"Dispatch.LoadScenarios(): Unable to extract CalloutType value for '{calloutName}/CalloutMeta.xml'"
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

                        // Create entry
                        Scenarios.Add(calloutName, new SpawnGenerator<CalloutScenarioInfo>());

                        // Process the XML scenarios
                        foreach (XmlNode n in document.DocumentElement.SelectSingleNode("Scenarios")?.ChildNodes)
                        {
                            // Ensure we have attributes
                            if (n.Attributes == null)
                            {
                                Log.Warning($"Dispatch.LoadScenarios(): Scenario item has no attributes '{calloutName}->Scenarios->{n.Name}'");
                                continue;
                            }

                            // Try and extract probability value
                            if (n.Attributes["probability"]?.Value == null || !int.TryParse(n.Attributes["probability"].Value, out int prob))
                            {
                                Log.Warning($"Dispatch.LoadScenarios(): Unable to extract scenario probability value for '{calloutName}->Scenarios->{n.Name}'");
                                continue;
                            }

                            // Get the Dispatch Node
                            XmlNode dispatchNode = n.SelectSingleNode("Dispatch");
                            if (dispatchNode == null)
                            {
                                Log.Warning(
                                    $"Dispatch.LoadScenarios(): Unable to extract scenario Dispatch node for '{calloutName}->Scenarios->{n.Name}'"
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
                                Log.Warning(
                                    $"Dispatch.LoadScenarios(): Unable to extract scenario priority value for '{calloutName}->Scenarios->{n.Name}'"
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
                                Log.Warning(
                                    $"Dispatch.LoadScenarios(): Unable to extract scenario respond value for '{calloutName}->Scenarios->{n.Name}'"
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
                                Log.Warning(
                                    $"Dispatch.LoadScenarios(): Unable to extract LocationType value for '{calloutName}->Scenarios->{n.Name}'"
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
                                Log.Warning(
                                    $"Dispatch.LoadScenarios(): Unable to extract Scanner value for '{calloutName}->Scenarios->{n.Name}'"
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
                                Log.Warning(
                                    $"Dispatch.LoadScenarios(): Unable to extract scenario description values for '{calloutName}->Scenarios->{n.Name}'"
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
                            Scenarios[calloutName].Add(scene);
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
            Log.Info($"Dispatch initialized with {itemsAdded} callout scenarios registered into Scenario Pool");
            return itemsAdded;
        }
    }
}
