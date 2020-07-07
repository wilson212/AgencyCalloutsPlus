using AgencyCalloutsPlus.Callouts;
using AgencyCalloutsPlus.Extensions;
using AgencyCalloutsPlus.Mod;
using LSPD_First_Response.Mod.API;
using LSPD_First_Response.Mod.Callouts;
using Rage;
using System;
using System.Collections.Generic;
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
        internal static Dictionary<CalloutType, TimeOfDaySpawnGenerator<CalloutScenarioInfo>> ScenarioPool { get; set; }

        /// <summary>
        /// Contains a list of scenario's by callout name
        /// </summary>
        internal static Dictionary<string, TimeOfDaySpawnGenerator<CalloutScenarioInfo>> Scenarios { get; set; }

        /// <summary>
        /// Randomizer method used to randomize callouts and locations
        /// </summary>
        private static CryptoRandom Randomizer { get; set; }

        /// <summary>
        /// Gets the player's current selected <see cref="Agency"/>
        /// </summary>
        public static Agency PlayerAgency { get; private set; }

        public static TimeOfDay CurrentTimeOfDay { get; private set; }


        public static event EventHandler OnTimeOfDayChange;

        /// <summary>
        /// 
        /// </summary>
        internal static RegionCrimeGenerator CrimeGenerator { get; set; }

        /// <summary>
        /// Gets the number of zones <see cref="Dispatch"/> is spawning calls in
        /// </summary>
        internal static int ZoneCount => CrimeGenerator.Zones.Length;

        /// <summary>
        /// Gets the overall crime level definition for the current <see cref="Agency"/>
        /// </summary>
        public static CrimeLevel CurrentCrimeLevel => CrimeGenerator.CurrentCrimeLevel;

        /// <summary>
        /// GameFiber containing the AI Police and Dispatching functions
        /// </summary>
        private static GameFiber DispatchFiber { get; set; }

        /// <summary>
        /// The <see cref="GameFiber"/> that runs the logic for all the AI units
        /// </summary>
        private static GameFiber AISimulationFiber { get; set; }

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
        public static PriorityCall PlayerActiveCall { get; private set; }

        /// <summary>
        /// Contains the priority call that needs to be dispatched to the player on next Tick
        /// </summary>
        private static PriorityCall InvokeForPlayer { get; set; }

        /// <summary>
        /// Signals that the next callout should be given to the player
        /// </summary>
        private static bool SendNextCallToPlayer { get; set; }

        /// <summary>
        /// Containts a list of LAPD phonetic radiotelephony alphabet spelling words
        /// </summary>
        public static string[] LAPDphonetic { get; set; } = new string[]
        {
            "ADAM",
            "BOY",
            "CHARLES",
            "DAVID",
            "EDWARD",
            "FRANK",
            "GEORGE",
            "HENRY",
            "IDA",
            "JOHN",
            "KING",
            "LINCOLN",
            "MARY",
            "NORA",
            "OCEAN",
            "PAUL",
            "QUEEN",
            "ROBERT",
            "SAM",
            "TOM",
            "UNION",
            "VICTOR",
            "WILLIAM",
            "XRAY",
            "YOUNG",
            "ZEBRA"
        };

        /// <summary>
        /// Static method called the first time this class is referenced anywhere
        /// </summary>
        static Dispatch()
        {
            // Initialize callout types
            Scenarios = new Dictionary<string, TimeOfDaySpawnGenerator<CalloutScenarioInfo>>();
            ScenarioPool = new Dictionary<CalloutType, TimeOfDaySpawnGenerator<CalloutScenarioInfo>>(8);
            foreach (var type in Enum.GetValues(typeof(CalloutType)))
            {
                ScenarioPool.Add((CalloutType)type, new TimeOfDaySpawnGenerator<CalloutScenarioInfo>());
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
        }

        #region Public API Methods

        /// <summary>
        /// Sets the player's status
        /// </summary>
        /// <param name="status"></param>
        public static void SetPlayerStatus(OfficerStatus status)
        {
            if (!Main.OnDuty) return;

            // Update player status
            PlayerUnit.Status = status;
            PlayerUnit.LastStatusChange = World.DateTime;

            // Tell LSPDFR whats going on
            if (status != OfficerStatus.Available)
            {
                Functions.SetPlayerAvailableForCalls(false);
            }
            else
            {
                Functions.SetPlayerAvailableForCalls(true);
            }
        }

        /// <summary>
        /// Gets the <see cref="PlayerOfficerUnit"/>s status
        /// </summary>
        /// <returns></returns>
        public static OfficerStatus GetPlayerStatus()
        {
            if (!Main.OnDuty) return OfficerStatus.EndingDuty;
            return PlayerUnit.Status;
        }

        /// <summary>
        /// Sets the division ID in the player's call sign. This number must be between 1 and 10.
        /// </summary>
        /// <param name="division"></param>
        public static void SetPlayerDivisionId(int division)
        {
            // Ensure division ID is in range
            if (!division.InRange(1, 10))
                return;

            // Set new unit string
            Settings.AudioDivision = division;
            PlayerUnit.CallSign = $"{Settings.AudioDivision}{Settings.AudioUnitType}-{Settings.AudioBeat}";
        }

        /// <summary>
        /// Sets the phonetic unit type in the player's call sign. This number must be between 1 and 26,
        /// and is essentially the numberic ID of the alphabet (a = 1, b = 2, c = 3 etc etc).
        /// </summary>
        /// <param name="phoneticId"></param>
        public static void SetPlayerUnitType(int phoneticId)
        {
            // Ensure division ID is in range
            if (!phoneticId.InRange(1, 26))
                return;

            // Set new unit string
            Settings.AudioUnitType = LAPDphonetic[phoneticId - 1];
            PlayerUnit.CallSign = $"{Settings.AudioDivision}{Settings.AudioUnitType}-{Settings.AudioBeat}";
        }

        /// <summary>
        /// Sets the beat ID in the player's call sign. This number must be between 1 and 24.
        /// </summary>
        /// <param name="division"></param>
        public static void SetPlayerBeat(int beat)
        {
            // Ensure division ID is in range
            if (!beat.InRange(1, 24))
                return;

            // Set new unit string
            Settings.AudioBeat = beat;
            PlayerUnit.CallSign = $"{Settings.AudioDivision}{Settings.AudioUnitType}-{Settings.AudioBeat}";
        }

        /// <summary>
        /// Gets the number of available units to respond to a call
        /// </summary>
        /// <param name="emergency"></param>
        /// <returns></returns>
        public static int GetAvailableUnits(bool emergency = false)
        {
            if (emergency)
            {
                OfficerStatus[] acceptable = { OfficerStatus.Available, OfficerStatus.Busy, OfficerStatus.MealBreak, OfficerStatus.Dispatched };
                return OfficerUnits.Where(x => acceptable.Contains(x.Status)).Count();
            }
            else
            {
                return OfficerUnits.Where(x => x.Status == OfficerStatus.Available).Count();
            }
        }

        /// <summary>
        /// Gets all the calls by the specified priority
        /// </summary>
        /// <param name="priority"></param>
        /// <returns></returns>
        public static PriorityCall[] GetCallList(int priority)
        {
            if (priority > 4 || priority < 1) return null;

            int index = priority - 1;
            return CallQueue[index].ToArray();
        }

        /// <summary>
        /// Gets the number of calls in an array by priority
        /// </summary>
        /// <returns></returns>
        public static int[] GetCallCount()
        {
            var callCount = new int[4];
            for (int i = 0; i < 4; i++)
            {
                callCount[i] = CallQueue[i].Count;
            }

            return callCount;
        }

        /// <summary>
        /// Invokes a random callout from the CallQueue to the player
        /// </summary>
        /// <returns></returns>
        public static bool InvokeCalloutForPlayer()
        {
            if (CanInvokeCalloutForPlayer())
            {
                // Cache player location
                var location = Game.LocalPlayer.Character.Position;

                // Try and find a call for the player
                for (int i = 0; i < 4; i++)
                {
                    // Get call list
                    var calls = CallQueue[i];
                    if (calls.Count == 0)
                        continue;

                    // Filter calls
                    var list = calls.Where(x => x.NeedsMoreOfficers).ToArray();
                    if (list.Length == 0)
                        continue;

                    // Order calls by distance from player
                    var call = list.OrderBy(x => (x.CallDeclinedByPlayer) ? 1 : 0)
                        .ThenBy(x => x.Location.Position.DistanceTo(location))
                        .FirstOrDefault();

                    // Invoke callout
                    InvokeForPlayer = call;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Signals that the player is to be dispatched to a callout as soon as possible.
        /// </summary>
        /// <param name="dispatched">
        /// If true, then a call is immediatly dispatched to the player. If false, 
        /// the player will be dispatched to the very next incoming call.
        /// </param>
        /// <returns>Returns true if the player is available and can be dispatched to a call, false otherwise</returns>
        public static bool InvokeNextCalloutForPlayer(out bool dispatched)
        {
            if (CanInvokeCalloutForPlayer())
            {
                if (!InvokeCalloutForPlayer())
                {
                    // If we are here, we did not find a call. Signal dispatch to send next call
                    SendNextCallToPlayer = true;
                    dispatched = false;
                }
                else
                {
                    dispatched = true;
                }

                return true;
            }

            dispatched = false;
            return false;
        }

        /// <summary>
        /// On the next call check, the specified call will be dispatched to the player.
        /// If the player is already on a call that is in progress, this method does nothing
        /// and returns false.
        /// </summary>
        /// <param name="call"></param>
        /// <returns></returns>
        public static bool InvokeCalloutForPlayer(PriorityCall call)
        {
            if (CanInvokeCalloutForPlayer())
            {
                InvokeForPlayer = call;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Indicates whether or not a <see cref="Callout"/> can be invoked for the Player
        /// dependant on thier current call status.
        /// </summary>
        /// <returns></returns>
        public static bool CanInvokeCalloutForPlayer()
        {
            CallStatus[] acceptable = { CallStatus.Completed, CallStatus.Dispatched, CallStatus.Waiting };
            return (PlayerActiveCall == null || acceptable.Contains(PlayerActiveCall.CallStatus));
        }

        #endregion Public API Methods

        #region Public API Callout Methods

        /// <summary>
        /// Requests a <see cref="PriorityCall"/> from dispatch using the specified
        /// <see cref="AgencyCallout"/>.
        /// </summary>
        /// <param name="calloutType"></param>
        /// <returns></returns>
        public static PriorityCall RequestPlayerCallInfo(Type calloutType)
        {
            // Extract Callout Name
            string calloutName = calloutType.GetAttributeValue((CalloutInfoAttribute attr) => attr.Name);

            // Have we sent a call to the player?
            if (PlayerActiveCall != null)
            {
                // Do our types match?
                if (calloutName.Equals(PlayerActiveCall.ScenarioInfo.CalloutName))
                {
                    return PlayerActiveCall;
                }
                else
                {
                    // Cancel
                    PlayerActiveCall.CallDeclinedByPlayer = true;
                    PlayerActiveCall.CallStatus = CallStatus.Created;
                    PlayerActiveCall = null;
                    Log.Info($"Dispatch.RequestCallInfo: It appears that the player spawned a callout of type {calloutName}");
                }
            }

            // If we are here, maybe the player used a console command to start the callout.
            // At this point, see if we have anything in the call Queue
            if (Scenarios.ContainsKey(calloutName))
            {
                CallStatus[] status = { CallStatus.Created, CallStatus.Waiting, CallStatus.Dispatched };

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
                        PlayerActiveCall = calls[0];
                        return calls[0];
                    }
                }

                // Still here? Maybe we can create a call?
            }

            // cant do anything at this point
            Log.Warning($"Dispatch.RequestCallInfo(): Unable to spawn a scenario for {calloutName}");
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
                SetPlayerStatus(OfficerStatus.Available);
            }
        }

        /// <summary>
        /// Tells dispatch that we are on scene
        /// </summary>
        /// <param name="call"></param>
        public static void RegisterOnScene(OfficerUnit unit, PriorityCall call)
        {
            if (unit == PlayerUnit)
            {
                SetPlayerStatus(OfficerStatus.OnScene);
            }
            else
            {
                unit.Status = OfficerStatus.OnScene;
                unit.LastStatusChange = World.DateTime;
            }

            // Update call status
            call.CallStatus = CallStatus.OnScene;
        }

        /// <summary>
        /// Tells dispatch that we are on scene
        /// </summary>
        /// <param name="call"></param>
        /// <remarks>Used for Player only, not AI</remarks>
        public static void CalloutAccepted(PriorityCall call, AgencyCallout callout)
        {
            // Player is always primary on a callout
            PlayerUnit.AssignToCall(call, true);
            SetPlayerStatus(OfficerStatus.Dispatched);

            // Assign callout property
            call.Callout = callout;
        }

        /// <summary>
        /// Tells dispatch that did not accept the callout
        /// </summary>
        /// <param name="call"></param>
        /// <remarks>Used for Player only, not AI</remarks>
        public static void CalloutNotAccepted(PriorityCall call)
        {
            // Cancel
            if (PlayerActiveCall != null && call == PlayerActiveCall)
            {
                // Remove player from call
                call.CallStatus = CallStatus.Created;
                call.CallDeclinedByPlayer = true;
                SetPlayerStatus(OfficerStatus.Available);

                // Ensure this is null
                PlayerActiveCall = null;
                Log.Info($"Dispatch: Player declined callout scenario {call.ScenarioInfo.Name}");

                // Set a delay timer for next player call out?
                // @todo
            }
        }

        /// <summary>
        /// Ends the current callout that is running for the player
        /// </summary>
        public static void EndPlayerCallout()
        {
            bool calloutRunning = Functions.IsCalloutRunning();
            if (PlayerActiveCall != null)
            {
                if (PlayerActiveCall.Callout != null)
                {
                    PlayerActiveCall.Callout.End();
                }
                else if (calloutRunning)
                {
                    Log.Error("Dispatch.EndPlayerCallout(): No Player Active Callout. Using LSPDFR's Functions.StopCurrentCallout()");
                    Functions.StopCurrentCallout();
                }

                RegisterCallComplete(PlayerActiveCall);
                PlayerActiveCall = null;
            }
            else if (calloutRunning)
            {
                Log.Warning("Dispatch.EndPlayerCallout(): Player does not have an Active Call. Using LSPDFR's Functions.StopCurrentCallout()");
                Functions.StopCurrentCallout();
            }
        }

        /// <summary>
        /// Adds the specified <see cref="PriorityCall"/> to the Dispatch Queue
        /// </summary>
        /// <param name="call"></param>
        internal static void AddIncomingCall(PriorityCall call)
        {
            // Add call to priority Queue
            lock (_lock)
            {
                CallQueue[call.Priority - 1].Add(call);
                Log.Debug($"Dispatch: Added Call to Queue '{call.ScenarioInfo.Name}' in zone '{call.Zone.FullName}'");

                // Invoke the next callout for player?
                if (SendNextCallToPlayer)
                {
                    SendNextCallToPlayer = false;
                    InvokeForPlayer = call;
                }
            }
        }

        #endregion Public API Callout Methods

        #region Internal Callout Methods

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
                // Is this call already dispatched?
                if (call.PrimaryOfficer != null)
                {
                    // Is an AI on scene already?
                    if (call.CallStatus == CallStatus.OnScene)
                    {
                        // Player is dispatched as seconary officer
                        PlayerUnit.AssignToCall(call);
                        return;
                    }
                }

                PlayerActiveCall = call;
                PlayerActiveCall.CallStatus = CallStatus.Waiting;
                Functions.StartCallout(call.ScenarioInfo.CalloutName);
            }
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
                    $"Unit ~g~{officer.CallSign}~s~ is no longer with us"
                );
            }
        }

        #endregion Internal Callout Methods

        #region Duty Methods

        /// <summary>
        /// Method called at the start of every duty
        /// </summary>
        internal static bool StartDuty()
        {
            try
            {
                // Get players current agency
                Agency oldAgency = PlayerAgency;
                PlayerAgency = Agency.GetCurrentPlayerAgency();
                if (PlayerAgency == null)
                {
                    PlayerAgency = oldAgency;
                    Log.Error("Dispatch.StartDuty(): Player Agency is null");
                    return false;
                }

                // End current crime generation if running
                if (CrimeGenerator?.IsRunning ?? false)
                    CrimeGenerator.End();

                // Did we change agency type?
                if (oldAgency == null || PlayerAgency.AgencyType != oldAgency.AgencyType)
                {
                    // Reload scenarios for updated probabilities
                    LoadScenarios();

                    // Clear AI units
                    DisposeAIUnits();
                    OfficerUnits = new List<OfficerUnit>(PlayerAgency.ActualPatrols);
                }

                // Did we change Agencies?
                if (oldAgency == null || !PlayerAgency.ScriptName.Equals(oldAgency.ScriptName))
                {
                    // Clear calls
                    foreach (var callList in CallQueue)
                        callList.Clear();

                    // Clear old police units
                    DisposeAIUnits();
                    OfficerUnits = new List<OfficerUnit>(PlayerAgency.ActualPatrols);
                }

                // Get current time of day
                CurrentTimeOfDay = GetCurrentWorldTimeOfDay();

                // Here we allow the Agency itself to specify which crime 
                // Generator we are to use
                CrimeGenerator = PlayerAgency.CreateCrimeGenerator();
                var hourGameTimeToSecondsRealTime = (60d / Settings.TimeScale) * 60;

                // Debugging
                Log.Debug("Starting duty with the following Agency data:");
                Log.Debug($"\t\tAgency Name: {PlayerAgency.FriendlyName}");
                Log.Debug($"\t\tAgency Staff Level: {PlayerAgency.StaffLevel}");
                Log.Debug($"\t\tAgency Actual Patrols: {PlayerAgency.ActualPatrols}");
                Log.Debug("Starting duty with the following Region data:");
                Log.Debug($"\t\tRegion Zone Count: {CrimeGenerator.Zones.Length}");

                // Loop through each time period and cache crime numbers
                foreach (TimeOfDay period in Enum.GetValues(typeof(TimeOfDay)))
                {
                    var crimeInfo = CrimeGenerator.RegionCrimeInfoByTimeOfDay[period];
                    string name = Enum.GetName(typeof(TimeOfDay), period);
                    var callsPerSecondRT = (crimeInfo.AverageCallsPerHour / hourGameTimeToSecondsRealTime);
                    var realSecondsPerCall = (1d / callsPerSecondRT);

                    Log.Debug($"\t\tRegion Crime Data during the {name}:");
                    Log.Debug($"\t\t\tAverage Calls: {crimeInfo.AverageCrimeCalls}");
                    Log.Debug($"\t\t\tIdeal Patrols: {crimeInfo.OptimumPatrols}");
                    Log.Debug($"\t\t\tReal Seconds Per Call (Average): {realSecondsPerCall}");
                }        

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
            // End crime generation
            CrimeGenerator?.End();
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
            if (AISimulationFiber != null && (AISimulationFiber.IsAlive || AISimulationFiber.IsSleeping))
            {
                AISimulationFiber.Abort();
            }

            if (DispatchFiber != null && (DispatchFiber.IsAlive || DispatchFiber.IsSleeping))
            {
                DispatchFiber.Abort();
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

            // To be replaced later @todo
            if (Settings.EnableFullSimulation)
            {
                Log.Debug("Full Simulation Enabled");
                for (int i = 0; i < PlayerAgency.ActualPatrols - 1; i++)
                {
                    // Spawn random zone to spread the units out
                    ZoneInfo zone = CrimeGenerator.GetNextRandomCrimeZone();
                    var sp = zone.GetRandomSideOfRoadLocation();
                    while (sp == null)
                    {
                        zone = CrimeGenerator.GetNextRandomCrimeZone();
                        sp = zone.GetRandomSideOfRoadLocation();
                    }

                    // Create AI vehicle
                    var car = PlayerAgency.GetRandomPoliceVehicle(PatrolType.Marked, sp);
                    car.Model.LoadAndWait();
                    car.IsPersistent = true;

                    // Create AI officer
                    var driver = car.CreateRandomDriver();
                    driver.IsPersistent = true;
                    driver.BlockPermanentEvents = true;

                    // Create instance
                    var num = i + 10;
                    var unit = new PersistentAIOfficerUnit(driver, car, $"1A-{num}");
                    OfficerUnits.Add(unit);

                    // Start duty
                    unit.StartDuty();

                    // Yield
                    GameFiber.Yield();
                }
            }
            else
            {
                for (int i = 0; i < PlayerAgency.ActualPatrols - 1; i++)
                {
                    // Spawn random zone to spread the units out
                    ZoneInfo zone = CrimeGenerator.GetNextRandomCrimeZone();
                    var sp = zone.GetRandomSideOfRoadLocation();
                    while (sp == null)
                    {
                        zone = CrimeGenerator.GetNextRandomCrimeZone();
                        sp = zone.GetRandomSideOfRoadLocation();
                    }

                    // Create instance
                    var num = i + 10;
                    var unit = new VirtualAIOfficerUnit(sp.Position, $"1A-{num}");
                    OfficerUnits.Add(unit);

                    // Start Duty
                    unit.StartDuty();

                    // Yield
                    GameFiber.Yield();
                }
            }

            // Add player unit
            string unitNum = $"{Settings.AudioDivision}{Settings.AudioUnitType}-{Settings.AudioBeat}";
            PlayerUnit = new PlayerOfficerUnit(Game.LocalPlayer, unitNum);
            PlayerUnit.StartDuty();
            OfficerUnits.Add(PlayerUnit);

            // Start Dispatching logic fibers
            CrimeGenerator.Begin();
            AISimulationFiber = GameFiber.StartNew(ProcessAISimulationLogic);
            DispatchFiber = GameFiber.StartNew(ProcessDispatchLogic);
        }

        #endregion Timer Methods

        #region GameFiber methods

        private static void ProcessDispatchLogic()
        {
            // Wait
            GameFiber.Wait(3000);

            // While we are on duty accept calls
            while (Main.OnDuty)
            {
                try
                {
                    // Do dispatching checks
                    DoPoliceChecks();

                    // Get current Time of Day and check for changes
                    var currentTimeOfDay = GetCurrentWorldTimeOfDay();
                    if (currentTimeOfDay != CurrentTimeOfDay)
                    {
                        // Set
                        CurrentTimeOfDay = currentTimeOfDay;

                        // Fire event
                        OnTimeOfDayChange?.Invoke(null, null);
                    }
                }
                catch (Exception e)
                {
                    Log.Exception(e);
                }

                // Wait
                GameFiber.Wait(2000);
            }
        }

        /// <summary>
        /// Processes the Logic for AI Simulation
        /// </summary>
        private static void ProcessAISimulationLogic()
        {
            // While on duty main loop
            while (Main.OnDuty)
            {
                // If simulation mode is disabled, call OnTick for
                // the VirtualAIUnit(s)
                if (!Settings.EnableFullSimulation)
                {
                    var date = World.DateTime;

                    // Start duty for each officer
                    foreach (var officer in OfficerUnits)
                    {
                        officer.OnTick(date);
                    }
                }
                else
                {
                    // Lock the call Queue and draw scenes the player
                    // is close enough to see
                    lock (_lock)
                    {
                        // Grab player location just once
                        var location = Game.LocalPlayer.Character.Position;

                        // Loop through each call
                        foreach (var calls in CallQueue)
                        {
                            foreach (var call in calls)
                            {
                                // Is player primary? then skip...
                                if (call.PrimaryOfficer == PlayerUnit)
                                    continue;

                                // Is AI on scene?
                                if (call.CallStatus != CallStatus.OnScene)
                                    continue;

                                // Can the player even see this?
                                bool isInRange = call.AISimulation.IsPlayerInRange(location);
                                if (!isInRange && !call.AISimulation.IsPlaying)
                                    continue;

                                // If the simlation is not playing, but should be...
                                if (!call.AISimulation.IsPlaying)
                                {
                                    // Begin playing!
                                }
                                // If player is out of range
                                else if (!isInRange)
                                {
                                    // Stop playing
                                }
                                else
                                {
                                    // Call Process
                                }
                            }
                        }
                    }
                }

                // Stop this thread for one second
                GameFiber.Wait(1000);
            }
        }

        /// <summary>
        /// Main thread loop for Dispatch handling calls and player activity
        /// </summary>
        private static void DoPoliceChecks()
        {
            /* Keep watch on the players status
            if (PlayerUnit.Status == OfficerStatus.Available)
            {
                if (Functions.GetCurrentPullover() != default(LHandle))
                {
                    SetPlayerStatus(OfficerStatus.OnTrafficStop);
                }
            }
            else if (Functions.IsPlayerAvailableForCalls())
            {
                if (Functions.IsCalloutRunning())
                {
                    SetPlayerStatus(OfficerStatus.Busy);
                }
                else if (PlayerUnit.Status != OfficerStatus.MealBreak)
                {
                    SetPlayerStatus(OfficerStatus.Available);
                }
            }
            */

            // Are we invoking a call to the player?
            if (InvokeForPlayer != null)
            {
                DispatchUnitToCall(PlayerUnit, InvokeForPlayer);
                InvokeForPlayer = null;
            }

            // If we have no  officers, then stop
            if (OfficerUnits.Count == 0)
                return;

            // Stop calls from coming in for right now
            lock (_lock)
            {
                List<OfficerUnit> availableOfficers = null;

                // Itterate through each call priority Queue
                for (int priorityIndex = 0; priorityIndex < 4; priorityIndex++)
                {
                    // If we have no calls
                    if (CallQueue[priorityIndex].Count == 0)
                        continue;

                    // Get an updated list of units that can be pulled for calls
                    availableOfficers = GetAvailableUnitsByCallPriority(priorityIndex + 1);
                    if (availableOfficers.Count == 0)
                        continue;

                    // Grab open calls
                    var calls = (
                            from call in CallQueue[priorityIndex]
                            where call.NeedsMoreOfficers
                            orderby call.Priority ascending, call.CallCreated ascending // Oldest first
                            select call
                        ).Take(availableOfficers.Count).ToList();

                    // Check priority 1 and 2 calls first and dispatch accordingly
                    foreach (var call in calls)
                    {
                        // Check priority 1 and 2 calls first and dispatch accordingly
                        if (priorityIndex < 2)
                        {
                            // Select closest available officer
                            int officersNeeded = (priorityIndex == 0) ? 3 : 2;
                            var officers = GetClosestAvailableOfficers(availableOfficers, call, officersNeeded);

                            // If we have not officers, stop here
                            if (officers == null || officers.Length == 0)
                                break;

                            // Is player in this list?
                            var player = officers.Where(x => !x.IsAIUnit).FirstOrDefault();
                            if (player != null)
                            {
                                // Attempt to Dispatch player as primary to this call
                                DispatchUnitToCall(player, call);
                                availableOfficers.Remove(player);
                            }
                            else
                            {
                                // Dispatch primary AI officer
                                DispatchUnitToCall(officers[0], call);
                                availableOfficers.Remove(officers[0]);

                                // Add other units to the call
                                for (int i = 1; i < officers.Length - 1; i++)
                                {
                                    // Dispatch
                                    var officer = officers[i];
                                    availableOfficers.Remove(officer);
                                    DispatchUnitToCall(officer, call);
                                }
                            }
                        }
                        else
                        {
                            // Select closest available officer
                            var officer = GetClosestAvailableOfficer(availableOfficers, call);
                            if (officer != null)
                            {
                                DispatchUnitToCall(officer, call);
                                availableOfficers.Remove(officer);
                            }
                        }
                    }
                }
            }
        }

        #endregion GameFiber methods

        #region Filter and Order methods

        /// <summary>
        /// Gets a list of available <see cref="OfficerUnit"/>(s) based on the call priority
        /// provided.
        /// </summary>
        /// <param name="priority"></param>
        /// <returns></returns>
        private static List<OfficerUnit> GetAvailableUnitsByCallPriority(int priority)
        {
            switch (priority)
            {
                // Very high priority calls
                // Will pull officers off of breaks, traffic stops and low priority calls
                case 1: // IMMEDIATE BROADCAST
                case 2: // EMERGENCY
                    // Define acceptable status'
                    OfficerStatus[] acceptable = {
                        OfficerStatus.Available,
                        OfficerStatus.Busy,
                        OfficerStatus.MealBreak,
                        OfficerStatus.ReturningToStation,
                        OfficerStatus.OnTrafficStop
                    };

                    return (
                        from officer in OfficerUnits
                        where acceptable.Contains(officer.Status) 
                            || (officer.CurrentCall != null && officer.CurrentCall.Priority > 2)
                        select officer
                    ).ToList();

                default:
                    return (
                        from officer in OfficerUnits
                        where officer.Status == OfficerStatus.Available
                        select officer
                    ).ToList();
            }
        }

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
            var ordered = officers.OrderBy(x => x.GetPosition().DistanceTo(call.Location.Position));
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
            var ordered = officers.OrderBy(x => x.GetPosition().DistanceTo(call.Location.Position));
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
                case 1:
                case 2:
                    return availableOfficers;
                case 3:
                    if (call.CallDeclinedByPlayer)
                        return availableOfficers.Where(x => x.IsAIUnit);
                    else
                        return availableOfficers;
                default:
                    return availableOfficers.Where(x => x.IsAIUnit);
            }
        }

        #endregion Filter and Order methods

        /// <summary>
        /// Gets the <see cref="TimeOfDay"/>
        /// </summary>
        /// <returns></returns>
        internal static TimeOfDay GetCurrentWorldTimeOfDay()
        {
            var currentHour = World.TimeOfDay.Hours;
            var currentTimeOfDay = Parse(currentHour);
            return currentTimeOfDay;

            // Local function
            TimeOfDay Parse(int hour)
            {
                if (hour < 6) return TimeOfDay.Night;
                else if (hour < 12) return TimeOfDay.Morning;
                else if (hour < 18) return TimeOfDay.Day;
                else return TimeOfDay.Evening;
            }
        }

        /// <summary>
        /// Disposes and clears all AI units
        /// </summary>
        private static void DisposeAIUnits()
        {
            // Clear old police units
            if (OfficerUnits != null && OfficerUnits.Count != 0)
            {
                foreach (var officer in OfficerUnits)
                    officer.Dispose();
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
            List<PriorityCallDescription> desc = new List<PriorityCallDescription>(5);

            // Load callout scripts
            foreach (var calloutDirectory in directory.GetDirectories())
            {
                // ensure CalloutMeta.xml exists
                string path = Path.Combine(calloutDirectory.FullName, "CalloutMeta.xml");
                if (File.Exists(path))
                {
                    // Get callout name and type
                    string calloutDirName = calloutDirectory.Name;
                    Type calloutType = assembly.GetType($"AgencyCalloutsPlus.Callouts.{calloutDirName}");
                    if (calloutType == null)
                    {
                        Log.Error($"Dispatch.LoadScenarios(): Unable to find CalloutType in Assembly: '{calloutDirName}'");
                        continue;
                    }

                    // Get callout official name
                    string calloutName = calloutType.GetAttributeValue((CalloutInfoAttribute attr) => attr.Name);

                    // Load XML document
                    document = new XmlDocument();
                    using (var file = new FileStream(path, FileMode.Open))
                    {
                        document.Load(file);
                    }

                    // Process the XML scenarios
                    foreach (XmlNode scenarioNode in document.DocumentElement.SelectSingleNode("Scenarios")?.ChildNodes)
                    {
                        // Grab the CalloutType
                        XmlNode catagoryNode = scenarioNode.SelectSingleNode("Catagory");
                        if (!Enum.TryParse(catagoryNode.InnerText, out CalloutType crimeType))
                        {
                            Log.Warning(
                                $"Dispatch.LoadScenarios(): Unable to extract CalloutType value for '{calloutDirName}/CalloutMeta.xml -> '{scenarioNode.Name}'"
                            );
                            continue;
                        }

                        // Grab probabilities node
                        XmlNode probNode = scenarioNode.SelectSingleNode("Probability");
                        if (probNode == null)
                        {
                            Log.Error($"Dispatch.LoadScenarios(): Unable to load probabilities in CalloutMeta for '{calloutDirectory.Name}'");
                            continue;
                        }

                        // Grab agency list
                        XmlNode agenciesNode = probNode.SelectSingleNode("Agencies");
                        if (agenciesNode == null)
                        {
                            Log.Error($"Dispatch.LoadScenarios(): Unable to load agency data in CalloutMeta for '{calloutDirectory.Name}'");
                            continue;
                        }

                        // No data?
                        if (!agenciesNode.HasChildNodes) continue;

                        // Itterate through items
                        agencyProbabilites.Clear();
                        foreach (XmlNode n in agenciesNode.SelectNodes("Agency"))
                        {
                            // Ensure we have attributes
                            if (n.Attributes == null)
                            {
                                Log.Warning(
                                    $"Dispatch.LoadScenarios(): Agency item has no attributes in '{calloutDirName}/CalloutMeta.xml -> {scenarioNode.Name} -> Agencies'"
                                );
                                continue;
                            }

                            // Try and extract type value
                            if (!Enum.TryParse(n.Attributes["type"].Value, out AgencyType agencyType))
                            {
                                Log.Warning(
                                    $"Dispatch.LoadScenarios(): Unable to extract Agency type value for '{calloutDirName}/CalloutMeta.xml -> '{scenarioNode.Name}'"
                                );
                                continue;
                            }

                            // Try and extract probability value
                            if (!int.TryParse(n.Attributes["probability"].Value, out int probability))
                            {
                                Log.Warning(
                                    $"Dispatch.LoadScenarios(): Unable to extract Agency probability value for '{calloutDirName}/CalloutMeta.xml -> '{scenarioNode.Name}'"
                                );
                            }

                            agencyProbabilites.Add(agencyType, probability);
                        }

                        // If player agency is not included
                        if (!agencyProbabilites.ContainsKey(agency.AgencyType))
                        {
                            // Skip
                            continue;
                        }

                        // Grab TimeOfDay probabilities
                        XmlNode todNode = probNode.SelectSingleNode("TimeOfDay");
                        if (todNode == null || todNode.Attributes == null)
                        {
                            Log.Error($"Dispatch.LoadScenarios(): Unable to load TimeOfDay probability data in CalloutMeta for '{calloutDirectory.Name}'");
                            continue;
                        }

                        var todProbabilities = new int[4];

                        // Extract morning probability
                        if (!Int32.TryParse(todNode.Attributes["morning"]?.Value, out todProbabilities[0]))
                        {
                            Log.Warning(
                                $"Dispatch.LoadScenarios(): Unable to extract TimeOfDay morning attribute value for '{calloutDirName}/CalloutMeta.xml -> '{scenarioNode.Name}'"
                            );
                            continue;
                        }

                        // Extract day probability
                        if (!Int32.TryParse(todNode.Attributes["day"]?.Value, out todProbabilities[1]))
                        {
                            Log.Warning(
                                $"Dispatch.LoadScenarios(): Unable to extract TimeOfDay day attribute value for '{calloutDirName}/CalloutMeta.xml -> '{scenarioNode.Name}'"
                            );
                            continue;
                        }

                        // Extract day probability
                        if (!Int32.TryParse(todNode.Attributes["evening"]?.Value, out todProbabilities[2]))
                        {
                            Log.Warning(
                                $"Dispatch.LoadScenarios(): Unable to extract TimeOfDay evening attribute value for '{calloutDirName}/CalloutMeta.xml -> '{scenarioNode.Name}'"
                            );
                            continue;
                        }

                        // Extract day probability
                        if (!Int32.TryParse(todNode.Attributes["night"]?.Value, out todProbabilities[3]))
                        {
                            Log.Warning(
                                $"Dispatch.LoadScenarios(): Unable to extract TimeOfDay night attribute value for '{calloutDirName}/CalloutMeta.xml -> '{scenarioNode.Name}'"
                            );
                            continue;
                        }

                        // Get the Dispatch Node
                        XmlNode dispatchNode = scenarioNode.SelectSingleNode("Dispatch");
                        if (dispatchNode == null)
                        {
                            Log.Warning(
                                $"Dispatch.LoadScenarios(): Unable to extract scenario Dispatch node for '{calloutDirName}/CalloutMeta.xml -> {scenarioNode.Name}'"
                            );
                            continue;
                        }

                        // Try and extract probability value
                        XmlNode childNode = dispatchNode.SelectSingleNode("Priority");
                        if (!int.TryParse(childNode.InnerText, out int priority))
                        {
                            Log.Warning(
                                $"Dispatch.LoadScenarios(): Unable to extract scenario priority value for '{calloutDirName}/CalloutMeta.xml -> {scenarioNode.Name}'"
                            );
                            continue;
                        }

                        // Try and extract Code value
                        childNode = dispatchNode.SelectSingleNode("Response");
                        if (!Int32.TryParse(childNode.InnerText, out int code) || !code.InRange(1, 3))
                        {
                            Log.Warning(
                                $"Dispatch.LoadScenarios(): Unable to extract scenario respond value for '{calloutDirName}/CalloutMeta.xml -> {scenarioNode.Name}'"
                            );
                            continue;
                        }

                        // Grab the LocationType
                        childNode = dispatchNode.SelectSingleNode("LocationType");
                        if (!Enum.TryParse(childNode.InnerText, out LocationType locationType))
                        {
                            Log.Warning(
                                $"Dispatch.LoadScenarios(): Unable to extract LocationType value for '{calloutDirName}/CalloutMeta.xml -> {scenarioNode.Name}'"
                            );
                            continue;
                        }

                        // Grab the Scanner
                        string scanner = String.Empty;
                        childNode = dispatchNode.SelectSingleNode("Scanner");
                        if (String.IsNullOrWhiteSpace(childNode.InnerText))
                        {
                            Log.Warning(
                                $"Dispatch.LoadScenarios(): Unable to extract Scanner value for '{calloutDirName}/CalloutMeta.xml -> {scenarioNode.Name}'"
                            );
                            continue;
                        }
                        else
                        {
                            scanner = childNode.InnerText;
                        }

                        // Try and extract descriptions
                        childNode = dispatchNode.SelectSingleNode("Description");
                        if (childNode == null || !childNode.HasChildNodes)
                        {
                            Log.Warning(
                                $"Dispatch.LoadScenarios(): Unable to extract scenario description values for '{calloutDirName}/CalloutMeta.xml -> {scenarioNode.Name}'"
                            );
                            continue;
                        }
                        else
                        {
                            // Clear old descriptions
                            desc.Clear();
                            foreach (XmlNode descNode in childNode.ChildNodes)
                            {
                                // Ensure we have attributes
                                if (descNode.Attributes == null)
                                {
                                    desc.Add(new PriorityCallDescription(descNode.InnerText, null));
                                }
                                else
                                {
                                    desc.Add(new PriorityCallDescription(descNode.InnerText, descNode.Attributes["source"]?.Value));
                                }
                            }
                        }

                        // Grab the Incident
                        childNode = dispatchNode.SelectSingleNode("IncidentType");
                        if (String.IsNullOrWhiteSpace(childNode.InnerText))
                        {
                            Log.Warning(
                                $"Dispatch.LoadScenarios(): Unable to extract IncidentType value for '{calloutDirName}->Scenarios->{scenarioNode.Name}'"
                            );
                            continue;
                        }
                        else if (childNode.Attributes == null || childNode.Attributes["abbreviation"]?.Value == null)
                        {
                            Log.Warning(
                                $"Dispatch.LoadScenarios(): Unable to extract Incident abbreviation attribute for '{calloutDirName}->Scenarios->{scenarioNode.Name}'"
                            );
                            continue;
                        }

                        // Create entry if not already
                        if (!Scenarios.ContainsKey(calloutName))
                        {
                            Scenarios.Add(calloutName, new TimeOfDaySpawnGenerator<CalloutScenarioInfo>());
                        }

                        // Get agency probability
                        int baseProbability = agencyProbabilites[agency.AgencyType];
                        var desciptions = desc.ToArray();

                        // Add time of day scenarios
                        for (int i = 0; i < 4; i++)
                        {
                            TimeOfDay time = (TimeOfDay)i;

                            // Create scenario node
                            var scene = new CalloutScenarioInfo()
                            {
                                Name = scenarioNode.Name,
                                CalloutName = calloutName,
                                Probability = baseProbability * todProbabilities[i],
                                Priority = priority,
                                ResponseCode = code,
                                LocationType = locationType,
                                ScannerText = scanner,
                                Descriptions = desciptions,
                                IncidentText = childNode.InnerText,
                                IncidentAbbreviation = childNode.Attributes["abbreviation"].Value
                            };

                            // Add scenario to the pool
                            Scenarios[calloutName].Add(time, scene);
                            ScenarioPool[crimeType].Add(time, scene);
                        }
                        
                        itemsAdded++;
                    }

                    Functions.RegisterCallout(calloutType);
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
