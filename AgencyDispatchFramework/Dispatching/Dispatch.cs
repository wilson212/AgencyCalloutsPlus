using AgencyDispatchFramework.Dispatching;
using AgencyDispatchFramework.Dispatching.Assignments;
using AgencyDispatchFramework.Extensions;
using AgencyDispatchFramework.Game;
using AgencyDispatchFramework.Game.Locations;
using AgencyDispatchFramework.NativeUI;
using LSPD_First_Response.Mod.API;
using LSPD_First_Response.Mod.Callouts;
using Rage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace AgencyDispatchFramework
{
    /// <summary>
    /// A class that handles the dispatching of Callouts based on the current 
    /// <see cref="AgencyType"/> in thier Jurisdiction.
    /// </summary>
    public static class Dispatch
    {
        /// <summary>
        /// Our lock object to prevent multi-threading issues
        /// </summary>
        private static object _threadLock = new object();

        /// <summary>
        /// Do write to this property directly!! Not thread safe
        /// </summary>
        private static int _timeCalloutWaiting = 0;

        /// <summary>
        /// Do write to this property directly!! Not thread safe
        /// </summary>
        /// <remarks>
        /// Set high to start, so the player can immediatly be dispatched
        /// </remarks>
        private static int _timeSinceCalloutAttempt = 9999;

        /// <summary>
        /// Gets a value in seconds that have elapsed since a callout was declined by the player
        /// </summary>
        public static int TimeSinceLastCalloutAttempt
        {
            get => _timeSinceCalloutAttempt;
            private set => Interlocked.Exchange(ref _timeSinceCalloutAttempt, value);
        }

        /// <summary>
        /// Gets a value in seconds that have passed since a callout was last dispatched to the player,
        /// waiting for acceptance. If a call is not currently awaiting a players acceptance, this value
        /// will always read zero.
        /// </summary>
        public static int TimeCalloutWaiting
        {
            get => _timeCalloutWaiting;
            private set => Interlocked.Exchange(ref _timeCalloutWaiting, value);
        }

        /// <summary>
        /// Indicates whether an external LSPDFR callout is detected as running, and not handled by <see cref="Dispatch"/>
        /// </summary>
        public static bool IsExternalCalloutRunning { get; private set; }

        /// <summary>
        /// Indicates whether a callout is being displayed to the player currently
        /// </summary>
        public static bool IsCalloutBeingDisplayed { get; private set; }

        /// <summary>
        /// Indicates whether the player is actively on a callout, or is being displayed a callout for acceptance.
        /// </summary>
        public static bool IsPlayerOnACallout => (IsExternalCalloutRunning || PlayerActiveCall != null || IsCalloutBeingDisplayed);

        /// <summary>
        /// Gets the value of <see cref="Functions.IsPlayerAvailableForCalls()"/> value before dispatching
        /// a player to a <see cref="PriorityCall"/>, so we can set it back afterwards
        /// </summary>
        private static bool? PreviousLSPDFRAvailability { get; set; }

        /// <summary>
        /// Contains a hash table of <see cref="WorldLocation"/>s that are currently in use by the Call Queue
        /// </summary>
        /// <remarks>HashSet{T}.Contains is an O(1) operation</remarks>
        internal static HashSet<WorldLocation> ActiveCrimeLocations { get; set; }

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
        /// Event called when a call is removed from the call list due to expiration
        /// </summary>
        public static event CallListUpdateHandler OnCallExpired;

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
        /// Contains a list of zones currently loaded and handled by the active agencies
        /// </summary>
        internal static List<WorldZone> Zones { get; set; }

        /// <summary>
        /// Gets the overall crime level definition for the current <see cref="Agency"/>
        /// </summary>
        public static CrimeLevel CurrentCrimeLevel => CrimeGenerator.CurrentCrimeLevel;

        /// <summary>
        /// The <see cref="GameFiber"/> that runs the logic for all the AI units
        /// </summary>
        private static GameFiber AISimulationFiber { get; set; }

        /// <summary>
        /// Contains the active agencies by type
        /// </summary>
        private static Dictionary<string, Agency> AgenciesByName { get; set; }

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
        /// Contains the priority call being dispatched to the player currently
        /// </summary>
        public static PriorityCall PlayerActiveCall { get; private set; }

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
            ActiveCrimeLocations = new HashSet<WorldLocation>();

            // Create call Queue
            // See also: https://grantpark.org/info/16029
            CallQueue = new List<PriorityCall>[4] 
            {
                new List<PriorityCall>(4),  // IMMEDIATE EMERGENCY BROADCAST
                new List<PriorityCall>(8),  // EMERGENCY RESPONSE
                new List<PriorityCall>(12), // EXPEDITED RESPONSE
                new List<PriorityCall>(20), // ROUTINE RESPONSE
            };

            // Create agency lookup
            AgenciesByName = new Dictionary<string, Agency>();

            // Register for event
            Dispatcher.OnCallRaised += Dispatcher_OnCallRaised;
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

            // @todo: Tell LSPDFR whats going on 
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
        public static Dictionary<CallPriority, int> GetCallCount()
        {
            var callCount = new Dictionary<CallPriority, int>();
            foreach (CallPriority priority in Enum.GetValues(typeof(CallPriority)))
            {
                var index = ((int)priority) - 1;
                callCount.Add(priority, CallQueue[index].Count);
            }

            return callCount;
        }

        /// <summary>
        /// Gets an array of <see cref="Agency"/> instances that are currently
        /// being simulated
        /// </summary>
        /// <returns>If the player is not on duty, this method return null</returns>
        public static Agency[] GetEnabledAgencies()
        {
            return (Main.OnDuty) ? AgenciesByName.Values.ToArray() : null;
        }

        /// <summary>
        /// Determines if the specified <see cref="Agency"/> can be dispatched to a call.
        /// </summary>
        /// <param name="agency"></param>
        /// <param name="call"></param>
        /// <returns></returns>
        public static bool CanAssignAgencyToCall(Agency agency, PriorityCall call)
        {
            // Check
            switch (agency.AgencyType)
            {
                case AgencyType.CountySheriff:
                    // Sheriffs can take this call IF not assigned already, or the
                    // primary agency was dispatched already but needs support
                    return (call.Priority == CallPriority.Immediate || call.PrimaryOfficer == null || call.NeedsMoreOfficers);
                case AgencyType.CityPolice:
                    // City police can only take calls in their primary jurisdiction
                    return agency.Zones.Contains(call.Location.Zone);
                case AgencyType.HighwayPatrol:
                    return call.ScenarioInfo.Category == CallCategory.Traffic;
                default:
                    return call.ScenarioInfo.AgencyTypes.Contains(agency.AgencyType);
            }
        }

        /// <summary>
        /// Gets an array of <see cref="WorldLocation"/>s currently in use based
        /// on the specified type <typeparamref name="T"/>
        /// </summary>
        /// <param name="type"></param>
        /// <exception cref="InvalidCastException">thrown if the <paramref name="type"/> does not match the <typeparamref name="T"/></exception>
        /// <typeparam name="T">A type that inherits from <see cref="WorldLocation"/></typeparam>
        /// <returns></returns>
        public static T[] GetActiveLocationsOfType<T>() where T : WorldLocation
        {
            return (from x in ActiveCrimeLocations where x is T select (T)x).ToArray();
        }

        /// <summary>
        /// Gets an array of locations that are not currently in use from the provided
        /// <see cref="WorldLocation"/> pool
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="pool"></param>
        /// <returns></returns>
        public static T[] GetInactiveLocationsFromPool<T>(T[] pool) where T : WorldLocation
        {
            return (from x in pool where !ActiveCrimeLocations.Contains(x) select x).ToArray();
        }

        /// <summary>
        /// Invokes a random callout from the CallQueue to the player
        /// </summary>
        /// <returns></returns>
        public static bool InvokeAnyCalloutForPlayer()
        {
            if (CanInvokeAnyCalloutForPlayer(true))
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
                    var list = calls.Where(x => CanPlayerHandleCall(x) && x.NeedsMoreOfficers).ToArray();
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
            if (CanInvokeAnyCalloutForPlayer(true))
            {
                if (!InvokeAnyCalloutForPlayer())
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
        public static bool InvokeCallForPlayer(PriorityCall call)
        {
            if (CanPlayerHandleCall(call))
            {
                InvokeForPlayer = call;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Indicates whether or not a <see cref="Callout"/> can be invoked for the Player
        /// dependant on thier current call status, cooldown timer between declined callouts,
        /// and whether or not a callout is currenlty being displayed to the player.
        /// </summary>
        /// <param name="ignoreCalloutTimeout">If true, the result will not take into account the timeout between callout attempts.</param>
        /// <returns></returns>
        public static bool CanInvokeAnyCalloutForPlayer(bool ignoreCalloutTimeout = false)
        {
            // Do we ignore the cooldown period?
            if (!ignoreCalloutTimeout && TimeSinceLastCalloutAttempt < Settings.TimeoutBetweenCalloutAttempts)
            {
                return false;
            }

            // Acceptable status'
            CallStatus[] acceptable = { CallStatus.Completed, CallStatus.Dispatched, CallStatus.Waiting };

            // Is an external callout running?
            if (IsCalloutBeingDisplayed)
            {
                // Maybe an external call is being displayed?
                if (PlayerActiveCall == null)
                {
                    return false;
                }
                else
                {
                    // We will allow the by-pass
                    return acceptable.Contains(PlayerActiveCall.CallStatus);
                }
            }
            else if (IsExternalCalloutRunning)
            {
                return false;
            }

            // default
            return PlayerActiveCall == null || acceptable.Contains(PlayerActiveCall.CallStatus);
        }

        /// <summary>
        /// Indicates whether or not a <see cref="Callout"/> can be invoked for the Player
        /// dependant on thier current assignment, call status and jurisdiction limits.
        /// </summary>
        /// <returns></returns>
        public static bool CanPlayerHandleCall(PriorityCall call)
        {
            // If a call is less priority than a players current assignment
            if (PlayerUnit.Assignment != null && (int)call.Priority >= (int)PlayerUnit.Assignment.Priority)
            {
                return false;
            }

            // Check to make sure the agency can preform this type of call
            if (!call.ScenarioInfo.AgencyTypes.Contains(PlayerAgency.AgencyType))
            {
                return false;
            }

            // Check jurisdiction
            return call.Location.Zone.DoesAgencyHaveJurisdiction(PlayerAgency);
        }

        #endregion Public API Methods

        #region Public API Callout Methods

        /// <summary>
        /// Requests a <see cref="PriorityCall"/> from dispatch using the specified
        /// <see cref="Callout"/> type.
        /// </summary>
        /// <param name="calloutType"></param>
        /// <returns></returns>
        public static PriorityCall RequestPlayerCallInfo(Callout callout)
        {
            // Extract Callout Name
            string calloutName = callout.ScriptInfo.Name;

            // Have we sent a call to the player?
            if (PlayerActiveCall != null)
            {
                // Do our types match?
                if (calloutName.Equals(PlayerActiveCall.ScenarioInfo.CalloutName))
                {
                    return PlayerActiveCall;
                }
                else if (PlayerActiveCall.CallStatus != CallStatus.Waiting)
                {
                    // This call is currently running!
                    EndPlayerCallout();
                    PlayerActiveCall = null;
                    Log.Error($"Dispatch.RequestPlayerCallInfo: Player was already on a call out when a request from {calloutName} was recieved");
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

                    // Add call to priority Queue
                    lock (_threadLock)
                    {
                        var index = ((int)call.OriginalPriority) - 1;
                        CallQueue[index].Add(call);
                        Log.Debug($"Dispatch: Added Call to Queue '{call.ScenarioInfo.Name}' in zone '{call.Location.Zone.FullName}'");

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

            // End call
            call.End(CallCloseFlag.Completed);
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
            // Clear radio block
            Scanner.IsWaitingPlayerResponse = false;
            TimeCalloutWaiting = 0;

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
            // Cancel callout
            if (PlayerActiveCall != null && call == PlayerActiveCall)
            {
                // Remove player from call
                call.CallStatus = CallStatus.Created;
                call.CallDeclinedByPlayer = true;
                SetPlayerStatus(OfficerStatus.Available);

                // Remove player
                call.RemoveOfficer(PlayerUnit);

                // Ensure this is null
                PlayerActiveCall = null;
                TimeCalloutWaiting = 0;
                Log.Info($"Dispatch: Player declined callout scenario {call.ScenarioInfo.Name}");

                // Clear radio block
                Scanner.IsWaitingPlayerResponse = false;
            }
        }

        /// <summary>
        /// Tells dispatch that the current assignment given to the player is finished.
        /// </summary>
        public static void ClearPlayerAssignment()
        {
            // Set previous value
            if (PreviousLSPDFRAvailability != null)
                Functions.SetPlayerAvailableForCalls(PreviousLSPDFRAvailability.Value);

            // Reset
            PreviousLSPDFRAvailability = null;
        }

        /// <summary>
        /// Ends the current callout that is running for the player
        /// </summary>
        public static void EndPlayerCallout()
        {
            bool calloutRunning = Functions.IsCalloutRunning();
            if (PlayerActiveCall != null)
            {
                // Do we have a callout instance?
                if (PlayerActiveCall.Callout != null)
                {
                    PlayerActiveCall.Callout.End();
                }
                else if (calloutRunning)
                {
                    Log.Info("Dispatch.EndPlayerCallout(): No Player Active Callout. Using LSPDFR's Functions.StopCurrentCallout()");
                    Functions.StopCurrentCallout();
                }

                // If the call still isnt ended... do it manually
                // Need to do a null check!!! Callout.End() could set PlayerActiveCall to null
                if (PlayerActiveCall != null && !PlayerActiveCall.HasEnded)
                {
                    RegisterCallComplete(PlayerActiveCall);
                }
                
                PlayerActiveCall = null;
            }
            else if (calloutRunning)
            {
                Log.Warning("Dispatch.EndPlayerCallout(): Player does not have an Active Call. Using LSPDFR's Functions.StopCurrentCallout()");
                Functions.StopCurrentCallout();
            }
        }

        #endregion Public API Callout Methods

        #region Internal Callout Methods

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

            // Register first for events
            call.OnCallEnded += EndCall;

            // Add call to priority Queue
            lock (_threadLock)
            {
                if (ActiveCrimeLocations.Add(call.Location))
                {
                    var index = ((int)call.OriginalPriority) - 1;
                    CallQueue[index].Add(call);
                    Log.Debug($"Dispatch.AddIncomingCall(): Added Call to Queue '{call.ScenarioInfo.Name}' in zone '{call.Location.Zone.FullName}'");
                }
                else
                {
                    // Failed to add?
                    Log.Error($"Dispatch.AddIncomingCall(): Tried to add a call to Queue in zone '{call.Location.Zone.FullName}', but the location selected was already in use.");
                    return;
                }
            }

            // Decide which agency gets this call
            switch (call.ScenarioInfo.Targets)
            {
                case CallTarget.Police:
                    call.Location.Zone.PoliceAgencies[0].Dispatcher.AddCall(call);
                    break;
                case CallTarget.Fire:
                case CallTarget.Medical:
                    throw new NotSupportedException();
            }

            // Call event
            OnCallAdded?.Invoke(call);

            // Invoke the next callout for player?
            if (SendNextCallToPlayer)
            {
                // Check jurisdiction
                if (!call.Location.Zone.DoesAgencyHaveJurisdiction(PlayerAgency))
                {
                    return;
                }

                // Check call type
                if (!call.ScenarioInfo.AgencyTypes.Contains(PlayerAgency.AgencyType))
                {
                    return;
                }

                // Unflag
                SendNextCallToPlayer = false;

                // Ensure player isnt in a callout already!
                if (PlayerActiveCall == null)
                {
                    // Set call to invoke
                    InvokeForPlayer = call;
                }
            }
        }

        /// <summary>
        /// Method called when a call is ended <see cref="PriorityCall.OnCallEnded"/>
        /// </summary>
        /// <param name="call"></param>
        /// <param name="flag"></param>
        internal static void EndCall(PriorityCall call, CallCloseFlag flag)
        {
            // Unregister
            call.OnCallEnded -= EndCall;

            // Call handle
            switch (flag)
            {
                case CallCloseFlag.Expired:
                    OnCallExpired?.Invoke(call);
                    break;
                // @Todo: handle more intances
            }

            // Remove call
            var priority = ((int)call.OriginalPriority) - 1;
            lock (_threadLock)
            {
                CallQueue[priority].Remove(call);
                ActiveCrimeLocations.Remove(call.Location);
            }

            // Set player status
            if (call.PrimaryOfficer == PlayerUnit)
            {
                PlayerUnit.CompleteCall(CallCloseFlag.Completed);
                SetPlayerStatus(OfficerStatus.Available);

                // Reset
                if (PreviousLSPDFRAvailability != null)
                    Functions.SetPlayerAvailableForCalls(PreviousLSPDFRAvailability.Value);

                // Set active call to null
                PlayerActiveCall = null;

                // Fire event
                OnPlayerCallCompleted?.Invoke(call);
            }

            // Call event
            OnCallCompleted?.Invoke(call);
        }

        /// <summary>
        /// Internally used as part of a handshake with a <see cref="Dispatcher"/> when
        /// sending the player to a callout
        /// </summary>
        /// <param name="call"></param>
        internal static void AssignedPlayerToCall(PriorityCall call)
        {
            // wait for the radio to be clear before actually dispatching the player
            PlayerActiveCall = call;

            // Store this value for after our callout is ended, so we can
            // allows LSPFDFR the ability to still gives calls to the player
            if (PreviousLSPDFRAvailability == null)
                PreviousLSPDFRAvailability = Functions.IsPlayerAvailableForCalls();

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

            // Set call status
            PlayerActiveCall.CallStatus = CallStatus.Waiting;

            // Bind event
            message.BeforePlayed += CalloutMessage_BeforePlayed;

            // Queue radio
            Scanner.QueueCalloutAudioToPlayer(message);
        }

        /// <summary>
        /// Method called when the <see cref="RadioMessage"/> for the callout is played
        /// for the player
        /// </summary>
        /// <param name="message"></param>
        /// <param name="args"></param>
        private static void CalloutMessage_BeforePlayed(RadioMessage message, RadioCancelEventArgs args)
        {
            // Double check, but this should never be true
            if (Functions.IsCalloutRunning())
            {
                // Cancel the audio message
                args.Cancel = true;
                Log.Warning("Dispatch.CalloutMessage_BeforePlayed: Cannot dispatch player to call, already busy");
                return;
            }

            // Clear radio for response
            Scanner.IsWaitingPlayerResponse = true;
            TimeSinceLastCalloutAttempt = 0;

            // Start callout
            Functions.StartCallout(PlayerActiveCall.ScenarioInfo.CalloutName);
        }

        /// <summary>
        /// Tells dispatch the AI officer has died
        /// </summary>
        /// <param name="officer"></param>
        internal static void RegisterDeadOfficer(OfficerUnit officer)
        {
            if (officer.IsAIUnit)
            {
                // Dispose
                officer.Dispose();

                // Show player notification
                Rage.Game.DisplayNotification(
                    "3dtextures",
                    "mpgroundlogo_cops",
                    "Agency Dispatch Framework",
                    "~g~An Officer has Died.",
                    $"Unit ~g~{officer.CallSign}~s~ is no longer with us"
                );

                //@todo Spawn new officer unit?
            }
        }

        /// <summary>
        /// Method called when a <see cref="Dispatcher"/> raises a call. This method analyzes
        /// what additional resources are needed for the call, and assigns the appropriate agencies 
        /// to assist.
        /// </summary>
        /// <remarks>
        /// Each <see cref="Dispatcher"/> instance is responsible for keeping track of which calls have
        /// been raised in the <see cref="Dispatcher.RaisedCalls"/> HashSet
        /// </remarks>
        /// <param name="agency">The agency that raised the call</param>
        /// <param name="call">The call that requires additional resources</param>
        /// <param name="args">Container that directs the dispatch of what resoures are being requested</param>
        private static void Dispatcher_OnCallRaised(Agency agency, PriorityCall call, CallRaisedEventArgs args)
        {
            // Of we are not on duty, then wth
            if (!Main.OnDuty) return;

            // @todo finish
            Agency newAgency = null;

            // So far this is all we support
            if (args.NeedsPolice)
            {
                switch (agency.AgencyType)
                {
                    case AgencyType.CityPolice:
                        // Grab county agency
                        newAgency = call.Location.Zone.PoliceAgencies.Where(x => x.AgencyType == AgencyType.CountySheriff).FirstOrDefault();
                        break;
                    case AgencyType.StateParks:
                    case AgencyType.CountySheriff:
                        // Grab state agency
                        newAgency = call.Location.Zone.PoliceAgencies.Where(x => x.IsStateAgency && x.AgencyType != AgencyType.StateParks).FirstOrDefault();
                        break;
                    case AgencyType.HighwayPatrol:
                    case AgencyType.StatePolice:
                        // We need to grab the county that has jurisdiction here
                        newAgency = call.Location.Zone.PoliceAgencies.Where(x => x.AgencyType == AgencyType.CountySheriff).FirstOrDefault();
                        break;
                    default:
                        throw new NotSupportedException();
                }
            }
            else
            {
                throw new NotSupportedException();
            }

            // Add the call to the dispatcher of the next tier.
            // Both agencies should now be tracking the call.
            if (newAgency != null)
            {
                // Only log if the call was added successfully
                if (newAgency.Dispatcher.AddCall(call))
                {
                    Log.Debug($"{agency.ScriptName.ToUpper()} Dispatcher: Requested assistance with call '{call.ScenarioInfo.Name}' from {newAgency.ScriptName.ToUpper()}");
                }
            }
            else
            {
                Log.Debug($"{agency.ScriptName.ToUpper()} Dispatcher: Attemtped to request assitance with call '{call.ScenarioInfo.Name}', but there is other agency.");
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
                CrimeGenerator?.End();

                // Did we change Agencies?
                if (oldAgency != null && !PlayerAgency.ScriptName.Equals(oldAgency.ScriptName))
                {
                    // Clear agency data
                    foreach (var ag in AgenciesByName)
                        ag.Value.Disable();

                    AgenciesByName.Clear();

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

                // ********************************************
                // Load all agencies that need to be loaded
                // ********************************************
                AgenciesByName.Add(PlayerAgency.ScriptName, PlayerAgency);

                // Next we need to determine which agencies to load alongside the players agency
                switch (PlayerAgency.AgencyType)
                {
                    case AgencyType.CityPolice:
                        // Add county
                        var name = (PlayerAgency.BackingCounty == County.Blaine) ? "bcso" : "lssd";
                        AgenciesByName.Add(name, Agency.GetAgencyByName(name));
                        zonesToLoad.UnionWith(Agency.GetZoneNamesByAgencyName(name));

                        // Add state
                        AgenciesByName.Add("sahp", Agency.GetAgencyByName("sahp"));
                        zonesToLoad.UnionWith(Agency.GetZoneNamesByAgencyName("sahp"));
                        break;
                    case AgencyType.CountySheriff:
                    case AgencyType.StateParks:
                        // Add state
                        AgenciesByName.Add("sahp", Agency.GetAgencyByName("sahp"));
                        zonesToLoad.UnionWith(Agency.GetZoneNamesByAgencyName("sahp"));
                        break;
                    case AgencyType.StatePolice:
                    case AgencyType.HighwayPatrol:
                        // Add both counties
                        AgenciesByName.Add("bcso", Agency.GetAgencyByName("bcso"));
                        AgenciesByName.Add("lssd", Agency.GetAgencyByName("lssd"));

                        // Load both counties zones
                        zonesToLoad.UnionWith(Agency.GetZoneNamesByAgencyName("bcso"));
                        zonesToLoad.UnionWith(Agency.GetZoneNamesByAgencyName("lssd"));
                        break;
                }

                // ********************************************
                // Load locations based on current agency jurisdiction.
                // This method needs called everytime the player Agency is changed
                // ********************************************
                Zones = WorldZone.GetZonesByName(zonesToLoad.ToArray(), out int numZonesLoaded).ToList();
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

                // Yield to prevent freezing
                GameFiber.Yield();

                // Create player, and initialize all agencies
                PlayerUnit = new PlayerOfficerUnit(Rage.Game.LocalPlayer, PlayerAgency);
                foreach (var a in AgenciesByName.Values)
                {
                    a.Enable();
                }

                // Yield to prevent freezing
                GameFiber.Yield();

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
                foreach (TimePeriod period in Enum.GetValues(typeof(TimePeriod)))
                {
                    // Add player before logging
                    PlayerAgency.OfficersByShift[period].Add(PlayerUnit);

                    // Log crime logic
                    var crimeInfo = CrimeGenerator.RegionCrimeInfoByTimePeriod[period];
                    string name = Enum.GetName(typeof(TimePeriod), period);

                    Log.Debug($"\t\tRegion Data during the {name}:");
                    Log.Debug($"\t\t\tAverage Calls: {crimeInfo.AverageCrimeCalls}");
                    Log.Debug($"\t\t\tAverage millseconds Per Call: {crimeInfo.AverageMillisecondsPerCall}");
                    Log.Debug($"\t\t\tIdeal Patrols: {crimeInfo.OptimumPatrols}");

                    // Get total officer counts by agency
                    StringBuilder b = new StringBuilder();
                    foreach (var a in AgenciesByName)
                    {
                        b.Append($"{a.Key} => {a.Value.OfficersByShift[period].Count}, ");
                    }
                    var value = b.ToString().TrimEnd(',', ' ');
                    Log.Debug($"\t\t\tActual Patrols by Agency: [{value}]");
                }

                // Initialize CAD. This needs called everytime we go on duty
                ComputerAidedDispatchMenu.Initialize();

                // Register for LSPDFR events
                LSPD_First_Response.Mod.API.Events.OnCalloutDisplayed += LSPDFR_OnCalloutDisplayed;
                LSPD_First_Response.Mod.API.Events.OnCalloutAccepted += LSPDFR_OnCalloutAccepted;
                LSPD_First_Response.Mod.API.Events.OnCalloutNotAccepted += LSPDFR_OnCalloutNotAccepted;
                LSPD_First_Response.Mod.API.Events.OnCalloutFinished += LSPDFR_OnCalloutFinished;

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

        /// <summary>
        /// Stops the internal thread and preforms clean up
        /// </summary>
        internal static void StopDuty()
        {
            // End crime generation
            CrimeGenerator?.End();
            StopAISimulation();

            // Register for LSPDFR events
            LSPD_First_Response.Mod.API.Events.OnCalloutDisplayed -= LSPDFR_OnCalloutDisplayed;
            LSPD_First_Response.Mod.API.Events.OnCalloutAccepted -= LSPDFR_OnCalloutAccepted;
            LSPD_First_Response.Mod.API.Events.OnCalloutNotAccepted -= LSPDFR_OnCalloutNotAccepted;
            LSPD_First_Response.Mod.API.Events.OnCalloutFinished -= LSPDFR_OnCalloutFinished;

            // Deactivate all agencies
            foreach (var a in AgenciesByName.Values)
            {
                a.Disable();
            }
        }

        #endregion Duty Methods

        #region GameFiber methods

        /// <summary>
        /// Main thread loop for Dispatch. Processes the Logic for AI officers every
        /// second, and every 2 seconds performs dispatching logic
        /// </summary>
        private static void Process()
        {
            // While on duty main loop
            bool skip = false;
            while (Main.OnDuty)
            {
                // Every other loop, do dispatching logic
                skip = !skip;
                if (!skip)
                {
                    // Are we invoking a call to the player?
                    if (InvokeForPlayer != null)
                    {
                        // Tell the players dispatcher they are about to get a call
                        PlayerAgency.Dispatcher.AssignUnitToCall(PlayerUnit, InvokeForPlayer);
                        InvokeForPlayer = null;
                    }

                    // Process each agency
                    var orderedAgencies = AgenciesByName.Values.OrderBy(x => (int)x.AgencyType);
                    foreach (var agency in orderedAgencies)
                    {
                        agency.Dispatcher.Process();

                        // Be nice
                        GameFiber.Yield();
                    }
                }

                // Get current datetime and timeperiod
                var date = World.DateTime;
                var tod = GameWorld.CurrentTimePeriod;

                // Call OnTick for the VirtualAIUnit(s)
                foreach (var agency in AgenciesByName.Values)
                {
                    foreach (var officer in agency.OnDutyOfficers)
                    {
                        officer.OnTick(date);
                    }

                    // Be nice
                    GameFiber.Yield();
                }

                // Increment thread safe, since callouts can be initiated from other threads!
                Interlocked.Increment(ref _timeSinceCalloutAttempt);

                // Are we waiting for a player to accept a callout?
                if (PlayerActiveCall?.CallStatus == CallStatus.Waiting)
                {
                    // A callout should call this method when expired, but just in
                    // case, we will check after a safe amount of time
                    if (TimeCalloutWaiting > 20)
                    {
                        // Ensure this is called, since the callout failed!
                        CalloutNotAccepted(PlayerActiveCall);
                    }
                    else
                    {
                        // Set safely, since Callout.OnCalloutAccepted is in a different thread
                        Interlocked.Increment(ref _timeCalloutWaiting);
                    }
                }

                // Stop this thread for one second
                GameFiber.Wait(1000);
            }
        }

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
            AISimulationFiber = GameFiber.StartNew(Process);
        }

        #endregion GameFiber methods

        #region LSPDFR event methods

        /// <summary>
        /// Method called whenever a player does not accept a callout in LSPDFR
        /// </summary>
        /// <param name="handle">The callout handle</param>
        private static void LSPDFR_OnCalloutNotAccepted(LHandle handle)
        {
            IsCalloutBeingDisplayed = false;
            IsExternalCalloutRunning = false;
        }

        /// <summary>
        /// Method called whenever a callout is displayed to the player for acceptance
        /// </summary>
        /// <param name="handle">The callout handle</param>
        private static void LSPDFR_OnCalloutDisplayed(LHandle handle)
        {
            IsCalloutBeingDisplayed = true;
        }

        /// <summary>
        /// Method called whenever a player finishes a callout
        /// </summary>
        /// <param name="handle">The callout handle</param>
        private static void LSPDFR_OnCalloutFinished(LHandle handle)
        {
            if (IsExternalCalloutRunning)
            {
                // @todo add more, such as player assignment changing
                IsExternalCalloutRunning = false;
            }
        }

        /// <summary>
        /// Event called when a callout is accepted in LSPDFR
        /// </summary>
        /// <param name="handle"></param>
        private static void LSPDFR_OnCalloutAccepted(LHandle handle)
        {
            // If we detect no active call, must be external
            if (PlayerActiveCall == null)
            {
                IsExternalCalloutRunning = true;
            }
            else if (PlayerActiveCall.Callout == null)
            {
                Log.Warning("Dispatch: Player call is not null, but the Callout property is not set!");
                IsExternalCalloutRunning = true;
            }
            else
            {
                // Check to ensure our callout is running!
                var runningName = Functions.GetCalloutName(handle);
                var calloutName = PlayerActiveCall.Callout.ScriptInfo.Name;
                if (!calloutName.Equals(runningName))
                {
                    // Log
                    Log.Warning($"Dispatch: Player active callout ({calloutName}) does not match the running callout name ({runningName})!");

                    // Close the player callout
                    PlayerActiveCall = null;

                    // Set flag
                    IsExternalCalloutRunning = true;
                }
                else
                {
                    // Log
                    Log.Debug($"Completed call handshake for scenario '{calloutName}'");

                    // Callout is verified
                    IsExternalCalloutRunning = false;
                }
            }

            // No longer displayed
            IsCalloutBeingDisplayed = false;

            // Set player assignment
            if (IsExternalCalloutRunning)
            {
                // @todo
            }
        }

        #endregion
    }
}
