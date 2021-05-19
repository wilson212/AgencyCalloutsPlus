﻿using AgencyDispatchFramework.Dispatching;
using AgencyDispatchFramework.Extensions;
using AgencyDispatchFramework.Game;
using AgencyDispatchFramework.Game.Locations;
using AgencyDispatchFramework.Simulation;
using LSPD_First_Response.Mod.API;
using LSPD_First_Response.Mod.Callouts;
using Rage;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AgencyDispatchFramework
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
        /// Contains a hash table of <see cref="WorldLocation"/>s that are currently in use by the Call Queue
        /// </summary>
        internal static Dictionary<LocationTypeCode, List<WorldLocation>> ActiveCrimeLocations { get; set; }

        /// <summary>
        /// Randomizer method used to randomize callouts and locations
        /// </summary>
        private static CryptoRandom Randomizer { get; set; }

        /// <summary>
        /// Gets the player's current selected <see cref="Agency"/>
        /// </summary>
        public static Agency ActiveAgency { get; private set; }

        /// <summary>
        /// Event called when a call is added to the call list
        /// </summary>
        public static event CallListUpdateHandler OnCallAdded;

        /// <summary>
        /// Event called when a call is removed from the call list
        /// </summary>
        public static event CallListUpdateHandler OnCallCompleted;

        /// <summary>
        /// Event called when a call is accepted by the player
        /// </summary>
        public static event CallListUpdateHandler OnPlayerCallAccepted;

        /// <summary>
        /// Event called when a call is completed by the player
        /// </summary>
        public static event CallListUpdateHandler OnPlayerCallCompleted;

        /// <summary>
        /// Gets or sets the <see cref="RegionCrimeGenerator"/>, responsible for spawning
        /// <see cref="PriorityCall"/>s
        /// </summary>
        internal static RegionCrimeGenerator CrimeGenerator { get; set; }

        /// <summary>
        /// Gets the number of zones under the player's <see cref="Agency"/> jurisdiction
        /// </summary>
        internal static int ZoneCount => CrimeGenerator.Zones.Length;

        /// <summary>
        /// Gets the overall crime level definition for the current <see cref="Agency"/>
        /// </summary>
        public static CrimeLevel CurrentCrimeLevel => CrimeGenerator.CurrentCrimeLevel;

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
        public static OfficerUnit PlayerUnit { get; private set; }

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
        /// Gets a queue of radio messages to transmit in game
        /// </summary>
        private static ConcurrentQueue<RadioMessage> RadioQueue { get; set; }

        /// <summary>
        /// Contains the priority call being dispatched to the player currently
        /// </summary>
        public static PriorityCall PlayerActiveCall { get; private set; }

        /// <summary>
        /// Indicates whether the PlayerActiveCall is waiting to be dispatched for a free radio spot
        /// </summary>
        private static bool CalloutWaitingForRadio { get; set; } = false;

        /// <summary>
        /// Contains the priority call that needs to be dispatched to the player on next Tick
        /// </summary>
        private static PriorityCall InvokeForPlayer { get; set; }

        /// <summary>
        /// Signals that the next callout should be given to the player, regardless of distance
        /// or player availability.
        /// </summary>
        private static bool SendNextCallToPlayer { get; set; } = false;

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
            // Intialize a hash table of active crime locations
            ActiveCrimeLocations = new Dictionary<LocationTypeCode, List<WorldLocation>>();
            foreach (LocationTypeCode type in Enum.GetValues(typeof(LocationTypeCode)))
            {
                ActiveCrimeLocations.Add(type, new List<WorldLocation>(10));
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

            // Create Radio Queue
            RadioQueue = new ConcurrentQueue<RadioMessage>();

            // Create next random call ID
            Randomizer = new CryptoRandom();
        }

        #region Public API Methods

        /// <summary>
        /// Converts a latin character to the corresponding letter's unit type string
        /// </summary>
        /// <param name="value">An upper- or lower-case Latin character</param>
        /// <returns></returns>
        public static string GetUnitStringFromChar(char value)
        {
            // Uses the uppercase character unicode code point. 'A' = U+0042 = 65, 'Z' = U+005A = 90
            char upper = char.ToUpper(value);
            if (upper < 'A' || upper > 'Z')
            {
                throw new ArgumentOutOfRangeException("value", "This method only accepts standard Latin characters.");
            }

            int index = (int)upper - (int)'A';
            return LAPDphonetic[index];
        }

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

            // Set new callsign
            PlayerUnit.SetCallSign(division, PlayerUnit.Unit[0], PlayerUnit.Beat);
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

            // Set new callsign
            PlayerUnit.SetCallSign(PlayerUnit.Division, LAPDphonetic[phoneticId - 1][0], PlayerUnit.Beat);
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

            // Set new callsign
            PlayerUnit.SetCallSign(PlayerUnit.Division, PlayerUnit.Unit[0], beat);
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
        /// Gets an array of <see cref="WorldLocation"/>s currently in use based
        /// on the specified <see cref="LocationTypeCode"/>
        /// </summary>
        /// <param name="type"></param>
        /// <exception cref="InvalidCastException">thrown if the <paramref name="type"/> does not match the <typeparamref name="T"/></exception>
        /// <typeparam name="T">A type that inherits from <see cref="WorldLocation"/></typeparam>
        /// <returns></returns>
        public static T[] GetActiveCrimeLocationsByType<T>(LocationTypeCode type) where T : WorldLocation
        {
            return ActiveCrimeLocations[type].Cast<T>().ToArray();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <param name="rightNow"></param>
        public static void PlayRadioMessage(RadioMessage message, bool rightNow = false)
        {
            if (rightNow)
            {
                // Append target of message
                StringBuilder builder = new StringBuilder();
                if (String.IsNullOrEmpty(message.TargetCallsign))
                {
                    builder.Append("ATTENTION_ALL_UNITS_02 ");
                }
                else
                {
                    builder.Append($"DISP_ATTENTION_UNIT {message.TargetCallsign}");
                }

                // Append radio message
                builder.Append(message.Message);
                if (message.LocationInfo != Vector3.Zero)
                {
                    Functions.PlayScannerAudioUsingPosition(builder.ToString(), message.LocationInfo);
                }
                else
                {
                    Functions.PlayScannerAudio(builder.ToString());
                }
            }
            else
            {
                RadioQueue.Enqueue(message);
            }
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
                var location = Rage.Game.LocalPlayer.Character.Position;

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
                    Log.Warning($"Dispatch.RequestPlayerCallInfo: Player active call type does not match callout of type {calloutName}");
                }
            }

            // If we are here, maybe the player used a console command to start the callout.
            // At this point, see if we have anything in the call Queue
            if (ScenarioPool.ScenariosByCalloutName.ContainsKey(calloutName))
            {
                CallStatus[] status = { CallStatus.Created, CallStatus.Waiting, CallStatus.Dispatched };
                var playerPosition = Rage.Game.LocalPlayer.Character.Position;

                // Lets see if we have a call already created
                for (int i = 0; i < 4; i++)
                {
                    var calls = (
                        from x in CallQueue[i]
                        where x.ScenarioInfo.CalloutName.Equals(calloutName) && status.Contains(x.CallStatus)
                        orderby x.Location.Position.DistanceTo(playerPosition) ascending
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
                if (ScenarioPool.ScenariosByCalloutName[calloutName].TrySpawn(out CalloutScenarioInfo scenarioInfo))
                {
                    // Log
                    Log.Info($"Dispatch.RequestPlayerCallInfo: It appears that the player requested a callout of type '{calloutName}' using an outside source. Creating a call out of thin air");

                    // Create the call
                    var call = CrimeGenerator.CreateCallFromScenario(scenarioInfo);
                    if (call == null)
                    {
                        return null;
                    }

                    // Add call
                    // Add call to priority Queue
                    lock (_lock)
                    {
                        CallQueue[call.Priority - 1].Add(call);
                        Log.Debug($"Dispatch: Added Call to Queue '{call.ScenarioInfo.Name}' in zone '{call.Zone.FullName}'");

                        // Invoke the next callout for player
                        InvokeForPlayer = call;
                    }

                    return call;
                }
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
            // Ensure the call is not null. This can happen when trying
            // to initiate a scenario from a menu
            if (call == null)
            {
                Log.Error("Dispatch.RegisterCallComplete(): Tried to reference a call that is null");
                return;
            }

            var priority = call.Priority - 1;
            lock (_lock)
            {
                CallQueue[priority].Remove(call);
                ActiveCrimeLocations[call.Location.LocationType].Remove(call.Location);
            }

            // Set player status
            if (call.PrimaryOfficer == PlayerUnit)
            {
                PlayerUnit.CompleteCall(CallCloseFlag.Completed);
                SetPlayerStatus(OfficerStatus.Available);

                // Set active call to null
                PlayerActiveCall = null;

                // Fire event
                OnPlayerCallCompleted?.Invoke(call);
            }

            // Call event
            OnCallCompleted?.Invoke(call);
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
                unit.LastStatusChange = Rage.World.DateTime;
            }

            // Update call status
            call.CallStatus = CallStatus.OnScene;
        }

        /// <summary>
        /// Tells dispatch that we are on scene
        /// </summary>
        /// <param name="call"></param>
        /// <remarks>Used for Player only, not AI</remarks>
        public static void CalloutAccepted(PriorityCall call, Callout callout)
        {
            // Player is always primary on a callout
            PlayerUnit.AssignToCall(call, true);
            SetPlayerStatus(OfficerStatus.Dispatched);

            // Assign callout property
            call.Callout = callout;

            // Fire event
            OnPlayerCallAccepted?.Invoke(call);
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
            // Ensure the call is not null. This can happen when trying
            // to initiate a scenario from a menu
            if (call == null || call.Location == null)
            {
                Log.Error("Dispatch.AddIncomingCall(): Tried to add a call that is a null reference, or has a null location reference");
                return;
            }

            // Add call to priority Queue
            lock (_lock)
            {
                CallQueue[call.Priority - 1].Add(call);
                ActiveCrimeLocations[call.Location.LocationType].Add(call.Location);
                Log.Debug($"Dispatch.AddIncomingCall(): Added Call to Queue '{call.ScenarioInfo.Name}' in zone '{call.Zone.FullName}'");

                // Invoke the next callout for player?
                if (SendNextCallToPlayer)
                {
                    // Unflag
                    SendNextCallToPlayer = false;

                    // Ensure player isnt in a callout already!
                    if (PlayerActiveCall == null)
                    {
                        // Set call to invoke
                        InvokeForPlayer = call;
                    }
                }

                // Call event
                OnCallAdded?.Invoke(call);
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
            // TODO: Secondary dispatching
            // Assign the officer to the call
            officer.AssignToCall(call, officer == PlayerUnit);

            // If player, wait for the radio to be clear before actually dispatching the player
            if (officer == PlayerUnit)
            {
                PlayerActiveCall = call;
                CalloutWaitingForRadio = true;
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
                Rage.Game.DisplayNotification(
                    "3dtextures",
                    "mpgroundlogo_cops",
                    "Agency Dispatch and Callouts+",
                    "~g~An Officer has Died.",
                    $"Unit ~g~{officer.CallSign}~s~ is no longer with us"
                );

                //@todo Spawn new officer unit?
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
                Agency oldAgency = ActiveAgency;
                ActiveAgency = Agency.GetCurrentPlayerAgency();
                if (ActiveAgency == null)
                {
                    ActiveAgency = oldAgency;
                    Log.Error("Dispatch.StartDuty(): Player Agency is null");
                    return false;
                }

                // End current crime generation if running
                if (CrimeGenerator?.IsRunning ?? false)
                    CrimeGenerator.End();

                // Did we change agency type?
                if (oldAgency == null || ActiveAgency.AgencyType != oldAgency.AgencyType)
                {
                    // Clear AI units
                    DisposeAIUnits();
                    OfficerUnits = new List<OfficerUnit>(ActiveAgency.ActualPatrols);

                    // Fire event
                    //OnAgencyChanged?.Invoke(oldAgency, ActiveAgency);
                }

                // Did we change Agencies?
                if (oldAgency == null || !ActiveAgency.ScriptName.Equals(oldAgency.ScriptName))
                {
                    // Clear calls
                    foreach (var callList in CallQueue)
                        callList.Clear();

                    // Clear old police units
                    DisposeAIUnits();
                    OfficerUnits = new List<OfficerUnit>(ActiveAgency.ActualPatrols);
                }

                // Here we allow the Agency itself to specify which crime 
                // Generator we are to use
                CrimeGenerator = ActiveAgency.CreateCrimeGenerator();
                var hourGameTimeToSecondsRealTime = (60d / Settings.TimeScale) * 60;

                // Debugging
                Log.Debug("Starting duty with the following Agency data:");
                Log.Debug($"\t\tAgency Name: {ActiveAgency.FriendlyName}");
                Log.Debug($"\t\tAgency Type: {ActiveAgency.AgencyType}");
                Log.Debug($"\t\tAgency Staff Level: {ActiveAgency.StaffLevel}");
                Log.Debug($"\t\tAgency Actual Patrols: {ActiveAgency.ActualPatrols}");
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

                // Start Dispatching logic fibers
                CrimeGenerator.Begin();
                Log.Debug($"Current crime level: {CrimeGenerator.CurrentCrimeLevel}");

                // Start timer
                BeginAISimulation();

                // Add player unit LAST!
                PlayerUnit = new PlayerOfficerUnit(Rage.Game.LocalPlayer);
                PlayerUnit.StartDuty();
                OfficerUnits.Add(PlayerUnit);

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
            StopAISimulation();   
        }

        #endregion Duty Methods

        #region AI Officer Simulation

        /// <summary>
        /// Stops the 2 <see cref="GameFiber"/>(s) that run the call center
        /// and AI dispatching
        /// </summary>
        private static void StopAISimulation()
        {
            if (AISimulationFiber != null && (AISimulationFiber.IsAlive || AISimulationFiber.IsSleeping))
            {
                AISimulationFiber.Abort();
            }
        }

        /// <summary>
        /// Loads the AI officer units and begins the <see cref="RegionCrimeGenerator"/>
        /// </summary>
        private static void BeginAISimulation()
        {
            // Always call Stop first!
            StopAISimulation();

            // Get total ai patrols to spawn
            int aiPatrolCount = Math.Max(0, ActiveAgency.ActualPatrols);
            var locations = CrimeGenerator.GetRandomShoulderLocations(aiPatrolCount);

            // Ensure we have enough locations to spawn patrols at
            if (locations.Length < aiPatrolCount)
            {
                Log.Warning($"The amount of locations available ({locations.Length}) to spawn AI officer units is less than the number of total AI officers ({aiPatrolCount})");
                aiPatrolCount = locations.Length;
            }

            // To be replaced later @todo
            if (Settings.EnableFullSimulation)
            {
                Log.Debug("Full Simulation Mode Enabled");
                for (int i = 0; i < aiPatrolCount; i++)
                {
                    // Create AI vehicle
                    var sp = locations[i];
                    var car = ActiveAgency.GetRandomPoliceVehicle(PatrolType.Marked, sp);
                    car.Model.LoadAndWait();
                    car.IsPersistent = true;

                    // Create AI officer
                    var driver = car.CreateRandomDriver();
                    driver.MakeMissionPed(true);
                    Functions.SetPedAsCop(driver);

                    // Create instance
                    var num = i + 10;
                    var unit = new PersistentAIOfficerUnit(driver, car, 1, 'A', num);
                    OfficerUnits.Add(unit);

                    // Start duty
                    unit.StartDuty();
                    GameFiber.Yield();
                }

                // Log for debugging
                Log.Debug($"Loaded {aiPatrolCount} Persistent AI officer units");
            }
            else
            {
                for (int i = 0; i < aiPatrolCount; i++)
                {
                    // Create instance
                    var sp = locations[i];
                    var num = i + 10;
                    var unit = new VirtualAIOfficerUnit(sp.Position, 1, 'A', num);
                    OfficerUnits.Add(unit);

                    // Start Duty
                    unit.StartDuty();
                    GameFiber.Yield();
                }

                // Log for debugging
                Log.Debug($"Loaded {aiPatrolCount} Virtual AI officer units");
            }

            // Determine initial amount of calls to spawn in
            var crime = CrimeGenerator.RegionCrimeInfoByTimeOfDay[GameWorld.CurrentTimeOfDay];
            var numCalls = 0;
            switch (CrimeGenerator.CurrentCrimeLevel)
            {
                default:
                case CrimeLevel.VeryLow:
                    break;
                case CrimeLevel.Low:
                    numCalls = Convert.ToInt32(crime.MaxCrimeCalls * 0.10);
                    break;
                case CrimeLevel.Moderate:
                    numCalls = Convert.ToInt32(crime.MaxCrimeCalls * 0.20);
                    break;
                case CrimeLevel.High:
                    numCalls = Convert.ToInt32(crime.MaxCrimeCalls * 0.33);
                    break;
                case CrimeLevel.VeryHigh:
                    numCalls = Convert.ToInt32(crime.MaxCrimeCalls * 0.50);
                    break;
            }

            // Assign officers to initial amount of calls
            var officers = new List<OfficerUnit>(OfficerUnits);
            for (int i = 0; i < numCalls; i++)
            {
                // Keep generating calls
                var call = CrimeGenerator.GenerateCall();
                if (call == null)
                    break;

                // Add call
                AddIncomingCall(call);

                // If we have more calls than available officers
                if (officers.Count <= i)
                    continue;

                // Dispatch an AI unit to this call at a random completion time
                officers[i].AssignToCallWithRandomCompletion(call);
            }

            // Start simulation fiber
            AISimulationFiber = GameFiber.StartNew(ProcessAISimulationLogic);
        }

        #endregion AI Officer Simlation

        #region GameFiber methods

        /// <summary>
        /// Processes the Logic for AI Simulation as well as the dispatch radio
        /// </summary>
        private static void ProcessAISimulationLogic()
        {
            // While on duty main loop
            while (Main.OnDuty)
            {
                // Check to see if the radio is busy
                if (!Functions.GetIsAudioEngineBusy())
                {
                    // If the player is assigned to a call, but has yet to be
                    // dispatched due to the radio being busy, dispatch it now
                    if (CalloutWaitingForRadio && PlayerActiveCall != null)
                    {
                        // Play callout radio
                        var call = PlayerActiveCall;
                        CalloutWaitingForRadio = false;

                        // Create radio message
                        var message = new RadioMessage(call.ScenarioInfo.ScannerAudioString);

                        // Are we using location?
                        if (call.ScenarioInfo.ScannerUsePosition)
                        {
                            message.LocationInfo = call.Location.Position;
                        }

                        // Add callsign?
                        if (call.ScenarioInfo.ScannerPrefixCallSign)
                        {
                            message.SetTarget(PlayerUnit);
                        }

                        // TODO: Make this work with secondary dispatching
                        // Start the callout
                        PlayerActiveCall.CallStatus = CallStatus.Waiting;
                        Functions.StartCallout(PlayerActiveCall.ScenarioInfo.CalloutName);

                        // Play callout radio
                        PlayRadioMessage(message, true);
                    }
                    else if (RadioQueue.TryDequeue(out RadioMessage message))
                    {
                        PlayRadioMessage(message, true);
                    }
                }

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
                        var location = Rage.Game.LocalPlayer.Character.Position;

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
        /// Main thread loop for Dispatch handling calls and player activity. This method must
        /// be called only from the <see cref="GameWorld.WorldWatchingFiber"/>
        /// </summary>
        internal static void ProcessDispatchLogic()
        {
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
            var officers = ApplyFilterToPlayer(availableOfficers, call);
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
            var officers = ApplyFilterToPlayer(availableOfficers, call);
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
        private static IEnumerable<OfficerUnit> ApplyFilterToPlayer(List<OfficerUnit> availableOfficers, PriorityCall call)
        {
            // Remove player if they are busy with ANY call regardless of priority!
            if (call.Priority == 4 || call.CallDeclinedByPlayer || !CanInvokeCalloutForPlayer())
            {
                // Remove the player unit
                var count = availableOfficers.RemoveAll(x => !x.IsAIUnit);
                if (count > 0)
                {
                    // Play scanner audio?
                }
            }

            switch (call.Priority)
            {
                case 1:
                case 2:
                case 3:
                    return availableOfficers;
                default:
                    return availableOfficers;
            }
        }

        #endregion Filter and Order methods

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
    }
}
