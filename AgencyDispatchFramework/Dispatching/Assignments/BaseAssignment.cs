using AgencyDispatchFramework.Dispatching;
using AgencyDispatchFramework.Game.Locations;
using Rage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgencyDispatchFramework.Dispatching.Assignments
{
    /// <summary>
    /// Base class for a player invoked assignment (such as a traffic stop). A <see cref="BaseAssignment"/> is
    /// much like a callout, but does not require the player to "Accept" the event first, because they are invoked
    /// by the player. While the player is in a <see cref="BaseAssignment"/>, they will not
    /// be dispatched to calls with a lower than priority than the <see cref="BaseAssignment"/>.
    /// </summary>
    public abstract class BaseAssignment
    {
        /// <summary>
        /// Gets or sets the <see cref="CallPriority"/> for this event. <see cref="Callouts.AgencyCallout"/>'s
        /// with a lower priority than this value will not be paged to the player during this event.
        /// </summary>
        public CallPriority Priority { get; protected set; }

        /// <summary>
        /// Gets or sets a short description or name of this assignment
        /// </summary>
        public string Description { get; protected set; }

        /// <summary>
        /// Gets or sets the 10-CODE for this assignment
        /// </summary>
        public string TenCode { get; protected set; }

        /// <summary>
        /// The players location where the event was started or currently at.
        /// </summary>
        public Vector3 Location { get; protected set; }
    }
}
