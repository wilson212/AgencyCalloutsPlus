using Rage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgencyCalloutsPlus.API
{
    /// <summary>
    /// Represents a traffic stop location on the side of a road with heading
    /// </summary>
    public class SideOfRoadLocation : LocationInfo
    {
        /// <summary>
        /// Gets the heading of an object entity at this location, if any
        /// </summary>
        public float Heading { get; protected set; }

        public SideOfRoadLocation(Vector3 position, float heading) : base(position)
        {
            this.Heading = heading;
        }
    }
}
