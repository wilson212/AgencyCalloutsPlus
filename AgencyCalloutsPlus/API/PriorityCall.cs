using AgencyCalloutsPlus.API.Simulation;
using AgencyCalloutsPlus.Callouts;
using Rage;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AgencyCalloutsPlus.API
{
    /// <summary>
    /// Represents a <see cref="LSPD_First_Response.Mod.Callouts.Callout"/> that can
    /// be queued up and dispatched to <see cref="API.OfficerUnit"/>(s)
    /// </summary>
    public class PriorityCall : IEquatable<PriorityCall>
    {
        /// <summary>
        /// The call ID
        /// </summary>
        public int CallId { get; internal set; }

        /// <summary>
        /// The callout scenario for this call
        /// </summary>
        internal CalloutScenarioInfo ScenarioInfo { get; private set; }

        /// <summary>
        /// Gets the Callout handle
        /// </summary>
        internal AgencyCallout Callout { get; set; }

        /// <summary>
        /// The <see cref="CallPriority"/>
        /// </summary>
        public int Priority => ScenarioInfo.Priority;

        /// <summary>
        /// Gets the <see cref="World.DateTime"/> when this call was created
        /// </summary>
        public DateTime CallCreated { get; internal set; }

        /// <summary>
        /// The current <see cref="CallStatus"/>
        /// </summary>
        public CallStatus CallStatus { get; internal set; }

        /// <summary>
        /// The general <see cref="WorldLocation"/> that this callout takes place at
        /// </summary>
        public WorldLocation Location { get; internal set; }

        /// <summary>
        /// Gets the primary <see cref="OfficerUnit"/> assigned to this call
        /// </summary>
        public OfficerUnit PrimaryOfficer { get; private set; }

        /// <summary>
        /// Gets a list of officers attached to this <see cref="PriorityCall"/>
        /// </summary>
        public List<OfficerUnit> AttachedOfficers { get; private set; }

        public AISceneSimulation AISimulation { get; set; }

        /// <summary>
        /// Indicates whether this Call needs more <see cref="OfficerUnit"/>(s)
        /// assigned to it.
        /// </summary>
        public bool NeedsMoreOfficers
        {
            get
            {
                switch (Priority)
                {
                    case 1: return AttachedOfficers.Count < 3;
                    case 2: return AttachedOfficers.Count < 2;
                    default: return AttachedOfficers.Count == 0;
                }
            }
        }

        /// <summary>
        /// Indicates whether this call was declined by the player
        /// </summary>
        public bool CallDeclinedByPlayer { get; internal set; }

        /// <summary>
        /// Gets the <see cref="ZoneInfo"/> this <see cref="PriorityCall"/>
        /// takes place in
        /// </summary>
        public ZoneInfo Zone { get; internal set; }

        /// <summary>
        /// Indicates wether the OfficerUnit should repsond code 3
        /// </summary>
        public int ResponseCode => ScenarioInfo.ResponseCode;

        /// <summary>
        /// Gets the incident text
        /// </summary>
        public string IncidentText => ScenarioInfo.IncidentText;

        /// <summary>
        /// Gets the incident abbreviation text
        /// </summary>
        public string IncidentAbbreviation => ScenarioInfo.IncidentAbbreviation;

        /// <summary>
        /// Gets the description of the call
        /// </summary>
        public PriorityCallDescription Description { get; internal set; }

        /// <summary>
        /// Creates a new instance of <see cref="PriorityCall"/>
        /// </summary>
        /// <param name="id"></param>
        /// <param name="scenarioInfo"></param>
        internal PriorityCall(int id, CalloutScenarioInfo scenarioInfo)
        {
            CallId = id;
            CallCreated = World.DateTime;
            ScenarioInfo = scenarioInfo ?? throw new ArgumentNullException(nameof(scenarioInfo));
            Description = scenarioInfo.Descriptions.Spawn();
            AttachedOfficers = new List<OfficerUnit>(4);

            // Temp
            AISimulation = new AISceneSimulation(this);
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

        internal void OnTick()
        {

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

        public override string ToString()
        {
            return ScenarioInfo?.Name;
        }

        public override int GetHashCode()
        {
            return CallId.GetHashCode();
        }
    }
}
