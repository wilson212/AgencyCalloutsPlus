using AgencyDispatchFramework.Extensions;
using AgencyDispatchFramework.Game;
using Rage;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AgencyDispatchFramework.Dispatching
{
    /// <summary>
    /// Creates a dispatcher that is specifically designed to dispatch
    /// <see cref="OfficerUnit"/>s optimally
    /// </summary>
    internal class PoliceDispatcher : Dispatcher
    {
        /// <summary>
        /// Creates a new instance
        /// </summary>
        /// <param name="agency"></param>
        public PoliceDispatcher(Agency agency) : base(agency)
        {

        }

        /// <summary>
        /// Method called every tick to manage calls
        /// </summary>
        public override void Process()
        {
            // Don't dispatch low priority calls if shift changes in less than 20 minutes!
            bool shiftChangesSoon = GameWorld.GetTimeUntilNextTimePeriod() < TimeSpan.FromMinutes(20);
            var currentTime = World.DateTime;
            var expiredCalls = new List<PriorityCall>();

            // Stop calls from coming in for right now
            lock (_threadLock)
            {
                // If we have no calls
                if (CallQueue.Count == 0)
                    return;

                // Grab open calls
                var calls = (
                        from call in CallQueue
                        where call.NeedsMoreOfficers
                        orderby (int)call.Priority ascending, call.CallCreated ascending // Oldest first
                        select call
                    );

                // Check priority 1 and 2 calls first and dispatch accordingly
                foreach (var call in calls)
                {
                    // Create officer pool, grouped by dispatching priority
                    var officerPool = CreateOfficerPriorityPool(Agency.OnDutyOfficers, call);

                    /******************************************************
                     * Immediate Emergency Calls
                     * 
                     * Pull available officers from all agencies until we have enough officers,
                     * prioritizing the primary agency
                     */
                    if (call.Priority == CallPriority.Immediate)
                    {
                        // Select closest available officer
                        var availableOfficers = GetClosestOfficersByPriority(officerPool, call);
                        if (availableOfficers.Count == 0)
                        {
                            RaiseCall(call, new CallRaisedEventArgs() { NeedsPolice = true });
                            break;
                        }

                        // Add other units to the call
                        foreach (var officer in availableOfficers)
                        {
                            // We already dispatched the player
                            if (!officer.IsAIUnit) continue;

                            // Dispatch
                            AssignUnitToCall(officer, call);
                        }

                        // If we have not officers, stop here
                        if (availableOfficers.Count < call.AdditionalUnitsRequired)
                        {
                            RaiseCall(call, new CallRaisedEventArgs() { NeedsPolice = true });
                        }
                    }

                    /******************************************************
                     * Emergency Calls / Dangerous Calls
                     * 
                     * Pull available officers from all agencies until we have enough officers,
                     * prioritizing the primary agency
                     */
                    else if (call.Priority == CallPriority.Emergency)
                    {
                        // Select closest available officer
                        var availableOfficers = GetClosestOfficersByPriority(officerPool, call);

                        // If we have no officers at all, then stop here and pass it up
                        if (availableOfficers.Count == 0)
                        {
                            // Raise this up if we are not on scene yet!
                            if (call.CallStatus != CallStatus.OnScene)
                            {
                                RaiseCall(call, new CallRaisedEventArgs() { NeedsPolice = true });
                            }
                            break;
                        }

                        // Add other units to the call
                        foreach (var officer in availableOfficers)
                        {
                            // We already dispatched the player
                            if (!officer.IsAIUnit) continue;

                            // Dispatch
                            AssignUnitToCall(officer, call);
                        }
                    }

                    /******************************************************
                     * Expedited Calls
                     * 
                     * These calls must be taken care of in a timely manner,
                     * Pull officers in higher agencies after 15 minutes
                     */
                    else if (call.Priority == CallPriority.Expedited)
                    {
                        // Select closest available officer
                        var availableOfficers = GetClosestOfficersByPriority(officerPool, call);
                        if (availableOfficers.Count == 0) break;

                        // Add other units to the call
                        foreach (var officer in availableOfficers)
                        {
                            // We already dispatched the player
                            if (!officer.IsAIUnit) continue;

                            // Dispatch
                            AssignUnitToCall(officer, call);
                        }

                        // If after 20 minutes, we still have no officers to send from the
                        // primary agency, pull officers from higher agencies
                        if (call.AttachedOfficers.Count == 0 && (currentTime - call.CallCreated > TimeSpan.FromMinutes(30)))
                        {
                            // Raise this up!
                            RaiseCall(call, new CallRaisedEventArgs() { NeedsPolice = true });
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
                        // Remove if expired
                        if (currentTime - call.CallCreated > TimeSpan.FromHours(8))
                        {
                            expiredCalls.Add(call);
                            continue;

                        }
                        // If shifts end soon, do not dispatch
                        if (!shiftChangesSoon)
                        {
                            // Select closest available officer
                            var availableOfficers = GetClosestOfficersByPriority(officerPool, call);
                            if (availableOfficers.Count == 0) break;

                            // Add other units to the call
                            foreach (var officer in availableOfficers)
                            {
                                // We already dispatched the player
                                if (!officer.IsAIUnit) continue;

                                // Dispatch
                                AssignUnitToCall(officer, call);
                            }
                        }
                    }
                }
            }

            // Remove expire calls
            if (expiredCalls.Count > 0)
            {
                foreach (var call in expiredCalls)
                {
                    // Remove call
                    RemoveCall(call);

                    // Call events
                    Dispatch.EndCall(call, CallCloseFlag.Expired);
                }
            }
        }

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
                    if (call.CallDeclinedByPlayer || !Dispatch.CanInvokeAnyCalloutForPlayer())
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
                    currentPriority = (int)officer.CurrentCall.Priority;
                    isOnScene = officer.Status != OfficerStatus.Dispatched;
                }
                else if (officer.Assignment != null)
                {
                    currentPriority = (int)officer.Assignment.Priority;
                }

                // Easy, if the call is less priority than what we are doing, forget it
                if ((int)call.Priority >= currentPriority)
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
                        if (call.Priority == CallPriority.Immediate)
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
                        if (call.Priority == CallPriority.Expedited)
                        {
                            // Do not pull someone off a routine call to preform a expedited call
                            if (isOnScene) break;

                            officer.Priority = DispatchPriority.Low;
                            availableOfficers.Add(officer);
                        }
                        else
                        {
                            // Pull this officer off
                            officer.Priority = (call.Priority == CallPriority.Immediate) ? DispatchPriority.High : DispatchPriority.Moderate;
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
        internal static List<OfficerUnit> GetClosestOfficersByPriority(Dictionary<DispatchPriority, List<OfficerUnit>> officers, PriorityCall call)
        {
            var count = call.AdditionalUnitsRequired;
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
                    list.AddRange(GetClosestOfficers(units, call.Location.Position, count));
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
        internal static List<OfficerUnit> GetClosestOfficers(List<OfficerUnit> availableOfficers, Vector3 location, int count)
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
