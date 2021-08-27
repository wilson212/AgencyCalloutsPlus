using AgencyDispatchFramework.Game.Locations;
using AgencyDispatchFramework.Scripting;
using LSPD_First_Response.Mod.Callouts;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AgencyDispatchFramework.Dispatching
{
    /// <summary>
    /// Represents a <see cref="LSPD_First_Response.Mod.Callouts.Callout"/> that can
    /// be queued up and dispatched to <see cref="Dispatching.OfficerUnit"/>(s)
    /// </summary>
    public sealed class PriorityCall : IEquatable<PriorityCall>, IDisposable
    {
        /// <summary>
        /// The unique call ID
        /// </summary>
        public int CallId { get; internal set; }

        /// <summary>
        /// The callout scenario for this call
        /// </summary>
        public CalloutScenarioInfo ScenarioInfo { get; private set; }

        /// <summary>
        /// Gets the Callout handle
        /// </summary>
        internal Callout Callout { get; set; }

        /// <summary>
        /// Gets whether this call has been escelated to a dangerous level, 
        /// requiring more immediate attention
        /// </summary>
        public bool IsEscalated { get; internal set; }

        /// <summary>
        /// The current <see cref="CallPriority"/>
        /// </summary>
        public CallPriority Priority { get; internal set; }

        /// <summary>
        /// The <see cref="CallPriority"/> when it was created
        /// </summary>
        public CallPriority OriginalPriority => ScenarioInfo.Priority;

        /// <summary>
        /// Gets the  <see cref="DateTime"/> when this call was created using Game Time
        /// </summary>
        public DateTime CallCreated { get; internal set; }

        /// <summary>
        /// The current <see cref="CallStatus"/>
        /// </summary>
        public CallStatus CallStatus { get; internal set; }

        /// <summary>
        /// The <see cref="WorldLocation"/> that this callout takes place or begins at
        /// </summary>
        public WorldLocation Location { get; internal set; }

        /// <summary>
        /// Gets the primary <see cref="OfficerUnit"/> assigned to this call
        /// </summary>
        public OfficerUnit PrimaryOfficer { get; private set; }

        /// <summary>
        /// Gets a list of officers attached to this <see cref="PriorityCall"/>
        /// </summary>
        private List<OfficerUnit> AttachedOfficers { get; set; }

        /// <summary>
        /// Indicates whether this Call needs more <see cref="OfficerUnit"/>(s)
        /// assigned to it.
        /// </summary>
        public bool NeedsMoreOfficers => AttachedOfficers.Count < TotalRequiredUnits;

        /// <summary>
        /// Gets the number of <see cref="OfficerUnit"/>s required for this call
        /// </summary>
        public int TotalRequiredUnits { get; internal set; }

        /// <summary>
        /// Gets the number of additional <see cref="OfficerUnit"/>s still required for this call
        /// </summary>
        public int NumberOfAdditionalUnitsRequired => Math.Max(TotalRequiredUnits - AttachedOfficers.Count, 0);

        /// <summary>
        /// Indicates whether this call was declined by the player
        /// </summary>
        public bool CallDeclinedByPlayer { get; internal set; }

        /// <summary>
        /// Indicates wether the OfficerUnit should repsond code 3
        /// </summary>
        public ResponseCode ResponseCode => (IsEscalated) ? ResponseCode.Code3 : ScenarioInfo.ResponseCode;

        /// <summary>
        /// Gets the description of the call
        /// </summary>
        public PriorityCallDescription Description { get; internal set; }

        /// <summary>
        /// Gets a value indicated whether the call has ended
        /// </summary>
        public bool HasEnded { get; internal set; }

        /// <summary>
        /// Gets a value indicating whether this instance is disposed
        /// </summary>
        public bool Disposed { get; internal set; }

        /// <summary>
        /// An event fired when this call is closed. This event is used to remove the call from
        /// ever <see cref="Dispatcher"/> that has added this call to its <see cref="Dispatcher.CallQueue" />
        /// </summary>
        internal event CallEndedHandler OnCallEnded;

        /// <summary>
        /// Creates a new instance of <see cref="PriorityCall"/>
        /// </summary>
        /// <param name="id"></param>
        /// <param name="scenarioInfo"></param>
        internal PriorityCall(int id, CalloutScenarioInfo scenarioInfo, WorldLocation location)
        {
            CallId = id;
            CallCreated = Rage.World.DateTime;
            ScenarioInfo = scenarioInfo ?? throw new ArgumentNullException(nameof(scenarioInfo));
            Description = scenarioInfo.Descriptions.Spawn();
            AttachedOfficers = new List<OfficerUnit>(4);
            CallStatus = CallStatus.Created;
            Location = location;
            Priority = scenarioInfo.Priority;
            TotalRequiredUnits = scenarioInfo.UnitCount;
        }

        /// <summary>
        /// Assigns the provided <see cref="OfficerUnit"/> as the primary officer of the 
        /// call if there isnt one, or adds the officer to the <see cref="AttachedOfficers"/>
        /// list otherwise
        /// </summary>
        /// <param name="officer"></param>
        internal void AssignOfficer(OfficerUnit officer, bool forcePrimary)
        {
            // Do we have a primary? Or are we forcing one?
            if (PrimaryOfficer == null || forcePrimary)
            {
                PrimaryOfficer = officer;
            }

            // Attach officer
            AttachedOfficers.Add(officer);
        }

        /// <summary>
        /// Removes the specified <see cref="OfficerUnit"/> from the call. If the 
        /// <see cref="OfficerUnit"/> was the primary officer, and <see cref="AttachedOfficers"/>
        /// is populated, the topmost <see cref="OfficerUnit"/> will be the new
        /// <see cref="PrimaryOfficer"/>
        /// </summary>
        /// <param name="officer"></param>
        internal void RemoveOfficer(OfficerUnit officer)
        {
            // Do we need to assign a new primary officer?
            if (officer == PrimaryOfficer)
            {
                if (AttachedOfficers.Count > 1)
                {
                    // Dispatch one more AI unit to this call
                    var primary = AttachedOfficers.Where(x => x != PrimaryOfficer).FirstOrDefault();
                    if (primary != null)
                        PrimaryOfficer = primary;
                }
                else
                {
                    PrimaryOfficer = null;
                }
            }

            // Finally, remove
            AttachedOfficers.Remove(officer);
        }

        /// <summary>
        /// Ends the call and calls the event <see cref="OnCallEnded"/>
        /// </summary>
        /// <param name="flag"></param>
        internal void End(CallCloseFlag flag)
        {
            if (!HasEnded)
            {
                HasEnded = true;

                // Fire event
                OnCallEnded?.Invoke(this, flag);

                // Dispose of this instance
                Dispose();
            }
        }

        public void Dispose()
        {
            // Only dispose once, and on internal calls only
            if (Disposed || !HasEnded) return;

            // Flag
            Disposed = true;

            // Clear
            Callout = null;
            ScenarioInfo = null;
            AttachedOfficers.Clear();
            AttachedOfficers = null;
            PrimaryOfficer = null;
            Location = null;
            Description = null;
        }

        /// <summary>
        /// Gets a list of officers attached to this <see cref="PriorityCall"/>
        /// </summary>
        public OfficerUnit[] GetAttachedOfficers() => AttachedOfficers.ToArray();

        public override string ToString()
        {
            return ScenarioInfo?.Name;
        }

        public bool Equals(PriorityCall other)
        {
            if (other == null) return false;
            return other.CallId == CallId;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as PriorityCall);
        }

        public override int GetHashCode()
        {
            return CallId.GetHashCode();
        }
    }
}
