using AgencyCalloutsPlus.Extensions;
using Rage;
using System;
using System.Collections.Generic;

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
        /// The general <see cref="GameLocation"/> that this callout takes place at
        /// </summary>
        public GameLocation Location { get; internal set; }

        /// <summary>
        /// Gets the primary <see cref="OfficerUnit"/> assigned to this call
        /// </summary>
        public OfficerUnit PrimaryOfficer { get; private set; }

        /// <summary>
        /// Gets a list of backup officers or null
        /// </summary>
        public List<OfficerUnit> BackupOfficers { get; private set; }

        /// <summary>
        /// Indicates whether this Call needs more <see cref="OfficerUnit"/>(s)
        /// assigned to it.
        /// </summary>
        public bool NeedsMoreOfficers
        {
            get
            {
                if (PrimaryOfficer == null || (Priority == 1 && BackupOfficers.Count < 2))
                {
                    return true;
                }
                else if (Priority == 2 && BackupOfficers.Count < 1)
                {
                    return true;
                }

                return false;
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
        public bool RespondCode3 => ScenarioInfo.RespondCode3;

        /// <summary>
        /// Gets the incident text
        /// </summary>
        public string IncidentText => ScenarioInfo.IncidentText;

        /// <summary>
        /// Gets the description of the call
        /// </summary>
        public string Description { get; internal set; } = String.Empty;

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
            Description = scenarioInfo.Descriptions.GetRandom();
            BackupOfficers = new List<OfficerUnit>(3);
        }

        /// <summary>
        /// Assigns the provided <see cref="OfficerUnit"/> as the primary officer of the 
        /// call if there isnt one, or adds the officer to the <see cref="BackupOfficers"/>
        /// list otherwise
        /// </summary>
        /// <param name="officer"></param>
        internal void AssignOfficer(OfficerUnit officer, bool asPrimary)
        {
            if (PrimaryOfficer == null)
            {
                PrimaryOfficer = officer;
            }
            else if (asPrimary)
            {
                BackupOfficers.Add(PrimaryOfficer);
                PrimaryOfficer = officer;
            }
            else
            {
                BackupOfficers.Add(officer);
            }
        }

        /// <summary>
        /// Removes the specified <see cref="OfficerUnit"/> from the call. If the 
        /// <see cref="OfficerUnit"/> was the primary officer, and <see cref="BackupOfficers"/>
        /// is populated, the topmost <see cref="OfficerUnit"/> will be the new
        /// <see cref="PrimaryOfficer"/>
        /// </summary>
        /// <param name="officer"></param>
        internal void RemoveOfficer(OfficerUnit officer)
        {
            if (officer == PrimaryOfficer)
            {
                if (BackupOfficers.Count > 0)
                {
                    // Dispatch one more AI unit to this call
                    var primary = BackupOfficers[0];

                    // remove as backup
                    BackupOfficers.Remove(primary);

                    // Assign new
                    PrimaryOfficer = primary;
                }
                else
                {
                    PrimaryOfficer = null;
                }
            }
            else
            {
                BackupOfficers.Remove(officer);
            }
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
