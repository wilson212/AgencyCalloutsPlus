using AgencyDispatchFramework.Dispatching;
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
        private static object _lock = new object();

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
        public static Agency PlayerAgency { get; private set; }

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
        /// 
        /// </summary>
        internal static List<ZoneInfo> Zones { get; set; }

        /// <summary>
        /// Gets the number of zones under the player's <see cref="Agency"/> jurisdiction
        /// </summary>
        public static int ZoneCount => Zones.Count;

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
        /// Contains the active agencies by type
        /// </summary>
        private static Dictionary<string, Agency> Agencies { get; set; }

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

            // Create agency lookup
            Agencies = new Dictionary<string, Agency>();
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
            return PlayerActiveCall == null || acceptable.Contains(PlayerActiveCall.CallStatus);
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

            // Remove call
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
                    Log.Info("Dispatch.EndPlayerCallout(): No Player Active Callout. Using LSPDFR's Functions.StopCurrentCallout()");
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

                // Did we change Agencies?
                if (oldAgency != null && !PlayerAgency.ScriptName.Equals(oldAgency.ScriptName))
                {
                    // Clear agency data
                    foreach (var ag in Agencies)
                        ag.Value.Disable();

                    // Clear calls
                    foreach (var callList in CallQueue)
                        callList.Clear();
                }

                // ********************************************
                // Build a hashset of zones to load
                // ********************************************
                var zonesToLoad = new HashSet<string>(Agency.GetCurrentAgencyZoneNames());
                if (zonesToLoad.Count == 0)
                {
                    // Display notification to the player
                    Rage.Game.DisplayNotification(
                        "3dtextures",
                        "mpgroundlogo_cops",
                        "Agency Dispatch Framework",
                        "~o~Initialization Failed.",
                        $"~y~Selected Agency does not have any zones in it's jurisdiction."
                    );

                    return false;
                }

                // Add player agency
                var agency = PlayerAgency;
                Agencies.Add(agency.ScriptName, agency);

                // Load all backing agency zones also
                while (!String.IsNullOrEmpty(agency.BackingAgencyScriptName))
                {
                    string name = agency.BackingAgencyScriptName;
                    agency = Agency.GetAgencyByName(name);
                    if (agency == null)
                    {
                        Agencies.Clear();
                        throw new Exception($"Unknown agency with scriptname '{agency.BackingAgencyScriptName}'");
                    }

                    // Add agency
                    Agencies.Add(agency.ScriptName, agency);
                    zonesToLoad.UnionWith(Agency.GetZoneNamesByAgencyName(agency.ScriptName));
                }

                // Load locations based on current agency jurisdiction.
                // This method needs called everytime the player Agency is changed
                Zones = ZoneInfo.GetZonesByName(zonesToLoad.ToArray(), true, out int numZonesLoaded).ToList();
                if (Zones.Count == 0)
                {
                    // Display notification to the player
                    Rage.Game.DisplayNotification(
                        "3dtextures",
                        "mpgroundlogo_cops",
                        "Agency Dispatch Framework",
                        "~o~Initialization Failed.",
                        $"~r~Failed to load all zone XML files."
                    );

                    return false;
                }

                // Create player, and initialize all agencies
                PlayerUnit = new PlayerOfficerUnit(Rage.Game.LocalPlayer, PlayerAgency);
                foreach (var a in Agencies.Values)
                {
                    a.Enable();
                }

                // ********************************************
                // Build the crime generator
                // ********************************************
                CrimeGenerator = new RegionCrimeGenerator(Zones.ToArray());

                // Debugging
                Log.Debug("Starting duty with the following Player Agency data:");
                Log.Debug($"\t\tAgency Name: {PlayerAgency.FriendlyName}");
                Log.Debug($"\t\tAgency Type: {PlayerAgency.AgencyType}");
                Log.Debug($"\t\tAgency Staff Level: {PlayerAgency.StaffLevel}");
                Log.Debug("Starting duty with the following Region data:");
                Log.Debug($"\t\tRegion Zone Count: {Zones.Count}");

                // Loop through each time period and cache crime numbers
                foreach (TimeOfDay period in Enum.GetValues(typeof(TimeOfDay)))
                {
                    // Add player before logging
                    PlayerAgency.OfficersByShift[period].Add(PlayerUnit);

                    // Log crime logic
                    var crimeInfo = CrimeGenerator.RegionCrimeInfoByTimeOfDay[period];
                    string name = Enum.GetName(typeof(TimeOfDay), period);

                    Log.Debug($"\t\tRegion Data during the {name}:");
                    Log.Debug($"\t\t\tAverage Calls: {crimeInfo.AverageCrimeCalls}");
                    Log.Debug($"\t\t\tAverage millseconds Per Call: {crimeInfo.AverageMillisecondsPerCall}");
                    Log.Debug($"\t\t\tIdeal Patrols: {crimeInfo.OptimumPatrols}");

                    // Get total officer counts by agency
                    StringBuilder b = new StringBuilder();
                    foreach (var a in Agencies)
                    {
                        b.Append($"{a.Key} => {a.Value.OfficersByShift[period].Count}, ");
                    }
                    var value = b.ToString().TrimEnd(',', ' ');
                    Log.Debug($"\t\t\tActual Patrols by Agency: [{value}]");
                }

                // Start Dispatching logic fibers
                CrimeGenerator.Begin();
                Log.Debug($"Setting current crime level: {CrimeGenerator.CurrentCrimeLevel}");

                // Start timer
                BeginAISimulation();

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

            // Deactivate all agencies
            foreach (var a in Agencies.Values)
            {
                a.Disable();
            }
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

            /* @todo

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
            */

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
                    var tod = GameWorld.CurrentTimeOfDay;

                    // Start duty for each officer
                    foreach (var agency in Agencies.Values)
                    foreach (var officer in agency.OnDutyOfficers)
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

            // Don't dispatch low priority calls if shift changes in less than 20 minutes!
            bool shiftChangesSoon = GameWorld.GetTimeUntilNextTimeOfDay() < TimeSpan.FromMinutes(20);
            var currentTime = World.DateTime;
            var expiredCalls = new List<PriorityCall>();

            // Stop calls from coming in for right now
            lock (_lock)
            {
                // Itterate through each call priority Queue
                for (int priorityIndex = 0; priorityIndex < 4; priorityIndex++)
                {
                    // If we have no calls
                    if (CallQueue[priorityIndex].Count == 0)
                        continue;

                    // Grab open calls
                    var calls = (
                            from call in CallQueue[priorityIndex]
                            where call.NeedsMoreOfficers
                            orderby call.Priority ascending, call.CallCreated ascending // Oldest first
                            select call
                        );

                    // Define vars
                    OfficerUnit[] agencyOfficers = null;

                    /******************************************************
                     * Immediate Emergency Calls
                     * 
                     * Pull the closest available officers from ALL agencies until we have 
                     * enough officers, regarldess of agency. Need to get onsite ASAP
                     */
                    if (priorityIndex == 0)
                    {
                        var temp = new List<OfficerUnit>();

                        // Add officers from all agencies into the pool for this one
                        foreach (var age in Agencies.Values)
                        {
                            temp.AddRange(Agencies[age.ScriptName].OnDutyOfficers);
                        }

                        agencyOfficers = temp.ToArray();
                    }

                    // Check priority 1 and 2 calls first and dispatch accordingly
                    foreach (var call in calls)
                    {
                        // Get the primary agency and its officers
                        var primaryAgency = call.Location.Zone.PrimaryAgency;
                        if (agencyOfficers == null)
                        {
                            agencyOfficers = Agencies[primaryAgency.ScriptName].OnDutyOfficers;
                        }

                        // Create officer pool, grouped by dispatching priority
                        var officerPool = CreateOfficerPriorityPool(agencyOfficers, call);

                        /******************************************************
                         * All Emergency Calls
                         * 
                         * Pull available officers from all agencies until we have enough officers,
                         * prioritizing the primary agency
                         */
                        if (priorityIndex < 2)
                        {
                            // Select closest available officer
                            int officersNeeded = (priorityIndex == 0) ? 4 : 3;
                            var availableOfficers = GetClosestOfficersByPriority(officerPool, officersNeeded, call.Location.Position);

                            // If we have not officers, stop here
                            if (availableOfficers.Count == 0)
                            {
                                break;
                            }

                            // Is player in this list?
                            var player = availableOfficers.Where(x => !x.IsAIUnit).FirstOrDefault();
                            if (player != null)
                            {
                                // Attempt to Dispatch player as primary to this call
                                DispatchUnitToCall(player, call);
                            }
                            else
                            {
                                // Dispatch primary AI officer
                                DispatchUnitToCall(availableOfficers[0], call);

                                // Add other units to the call
                                for (int i = 1; i < availableOfficers.Count - 1; i++)
                                {
                                    // Dispatch
                                    var officer = availableOfficers[i];
                                    DispatchUnitToCall(officer, call);
                                }
                            }
                        }

                        /******************************************************
                         * Expedited Calls
                         * 
                         * These calls must be taken care of in a timely manner,
                         * Pull officers in higher agencies after 15 minutes
                         */
                        else if (priorityIndex == 2)
                        {
                            // Select closest available officer
                            var officer = GetClosestOfficersByPriority(officerPool, 1, call.Location.Position).FirstOrDefault();

                            // If we have not officers, stop here
                            if (officer != null)
                            {
                                DispatchUnitToCall(officer, call);
                                continue;
                            }

                            // If after 20 minutes, we still have no officers to send from the
                            // primary agency, pull officers from higher agencies
                            if (currentTime - call.CallCreated > TimeSpan.FromMinutes(20))
                            {
                                var agency = primaryAgency;
                                while (!String.IsNullOrEmpty(agency.BackingAgencyScriptName))
                                {
                                    // Grab officers
                                    agencyOfficers = Agencies[agency.BackingAgencyScriptName].OnDutyOfficers;
                                    officerPool = CreateOfficerPriorityPool(agencyOfficers, call);

                                    officer = GetClosestOfficersByPriority(officerPool, 1, call.Location.Position).FirstOrDefault();
                                    if (officer != null)
                                    {
                                        DispatchUnitToCall(officer, call);
                                        break;
                                    }
                                    else
                                    {
                                        // Look to the next agency
                                        agency = Agency.GetAgencyByName(agency.BackingAgencyScriptName);
                                    }
                                }
                            }
                        }

                        /******************************************************
                         * Routine Calls
                         * 
                         * We don't really care about these. They will expire and
                         * removed from the call list after 12 hours
                         */
                        else
                        {
                            // If shifts end soon, do not dispatch
                            if (shiftChangesSoon) break;

                            // Select closest available officer
                            var officer = GetClosestOfficersByPriority(officerPool, 1, call.Location.Position).FirstOrDefault();
                            if (officer != null)
                            {
                                DispatchUnitToCall(officer, call);
                            }

                            // Remove
                            if (currentTime - call.CallCreated > TimeSpan.FromHours(12))
                            {
                                expiredCalls.Add(call);
                            }
                        }
                    }
                }

                // Remove expire calls
                if (expiredCalls.Count > 0)
                {
                    foreach (var call in expiredCalls)
                    {
                        var priority = call.Priority - 1;
                        CallQueue[priority].Remove(call);
                        ActiveCrimeLocations[call.Location.LocationType].Remove(call.Location);
                    }
                }
            }
        }

        #endregion GameFiber methods

        #region Filter and Order methods

        /// <summary>
        /// Groups <see cref="OfficerUnit"/>s for a call based on availability and <see cref="DispatchPriority"/>. Officers
        /// that are busy or not a priority for the call are no included in the list.
        /// </summary>
        /// <param name="priority"></param>
        /// <returns></returns>
        private static Dictionary<DispatchPriority, List<OfficerUnit>> CreateOfficerPriorityPool(OfficerUnit[] officers, PriorityCall call)
        {
            // Create a new list
            var availableOfficers = new List<OfficerUnit>();
            foreach (var officer in officers)
            {
                // Special checks for the player
                if (!officer.IsAIUnit)
                {
                    // Do not add player if they are busy, declined the call already
                    if (call.CallDeclinedByPlayer || !CanInvokeCalloutForPlayer())
                    {
                        continue;
                    }
                }

                // Set default
                var currentPriority = 5;
                bool isOnScene = false;

                // If the officer is on a call, lets determine if our currrent call is more important
                if (officer.CurrentCall != null)
                {
                    currentPriority = officer.CurrentCall.Priority;
                    isOnScene = officer.Status != OfficerStatus.Dispatched;
                }
                else if (officer.Assignment != null)
                {
                    currentPriority = (int)officer.Assignment.Priority;
                }

                // Easy, if the call is less priority than what we are doing, forget it
                if (call.Priority >= currentPriority)
                {
                    continue;
                }

                // Lets do some dispatch logic and assign priorities
                switch (currentPriority)
                {
                    case 1: // Already on an Immediate Emergency assignment
                    case 2: // Already on an Emergency assignment
                        break;
                    case 3: // On Expdited assignment
                        if (call.Priority == 1)
                        {
                            officer.Priority = (isOnScene) ? DispatchPriority.Moderate : DispatchPriority.High;
                        }
                        else
                        {
                            officer.Priority = (isOnScene) ? DispatchPriority.Low : DispatchPriority.Moderate;
                        }
                        availableOfficers.Add(officer);
                        break;
                    case 4: // On routine assignment
                        if (call.Priority == 3)
                        {
                            // Do not pull someone off a routine call to preform a expedited call
                            if (isOnScene) break;

                            officer.Priority = DispatchPriority.Low;
                            availableOfficers.Add(officer);
                        }
                        else
                        {
                            // Pull this officer off
                            officer.Priority = (call.Priority == 1) ? DispatchPriority.High : DispatchPriority.Moderate;
                            availableOfficers.Add(officer);
                        }
                        break;
                    default: // Not busy at all
                        officer.Priority = DispatchPriority.VeryHigh;
                        availableOfficers.Add(officer);
                        break;
                }
            }

            return availableOfficers.GroupBy(x => x.Priority).ToDictionary();
        }

        /// <summary>
        /// Gets the closest available officers to a call based on priority dispatching
        /// </summary>
        /// <param name="officers">All available officers that can be dispatched to the call</param>
        /// <param name="count">The desired number of officers to fetch</param>
        /// <param name="location">The location of the call</param>
        /// <returns></returns>
        private static List<OfficerUnit> GetClosestOfficersByPriority(Dictionary<DispatchPriority, List<OfficerUnit>> officers, int count, Vector3 location)
        {
            var list = new List<OfficerUnit>();
            for (int i = 1; i < 5; i++)
            {
                // Since we are grouping by priority, we may be missing some
                if (officers.TryGetValue((DispatchPriority)i, out List<OfficerUnit> units))
                {
                    // Double check that we have units
                    if (units.Count == 0)
                        continue;

                    // Add officers, sorting first by distance to the call
                    list.AddRange(GetClosestOfficers(units, location, count));
                    count -= units.Count;

                    // If we are at length, quit here
                    if (count <= 0)
                        break;
                }
            }

            return list;
        }

        /// <summary>
        /// Gets the closest available officers to a location
        /// </summary>
        /// <param name="availableOfficers">All available officers that can be dispatched to the call</param>
        /// <param name="location">The location of the call</param>
        /// <param name="count">The desired number of officers to fetch</param>
        /// <returns></returns>
        private static List<OfficerUnit> GetClosestOfficers(List<OfficerUnit> availableOfficers, Vector3 location, int count)
        {
            // Stop if we have no available officers
            if (availableOfficers.Count == 0)
                return availableOfficers;

            // Order officers by distance to the call
            var ordered = availableOfficers.OrderBy(x => x.GetPosition().DistanceTo(location));
            return ordered.Take(count).ToList();
        }

        #endregion Filter and Order methods
    }
}
