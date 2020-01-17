using Rage;
using System;

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
        public OfficerUnit OfficerUnit { get; internal set; }

        /// <summary>
        /// Gets the <see cref="ZoneInfo.ScriptName"/> this <see cref="PriorityCall"/>
        /// takes place in
        /// </summary>
        public string ZoneScriptName { get; internal set; }

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
