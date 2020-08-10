using System;

namespace AgencyCalloutsPlus.API
{
    [Flags]
    public enum LocationFlags
    {
        /// <summary>
        /// Describes the location as being near a lighted intersection
        /// </summary>
        /// <remarks>Intended location types: SideOfRoad</remarks>
        NearLightedIntersection = 0,

        /// <summary>
        /// Describes the location as being after a lighted intersection
        /// </summary>
        /// <remarks>Intended location types: SideOfRoad</remarks>
        AfterLightedIntersection = 1,

        /// <summary>
        /// Describes the location as being near an intersection with stop signs
        /// </summary>
        /// <remarks>Intended location types: SideOfRoad</remarks>
        Near4wayIntersection = 2,

        /// <summary>
        /// Describes the location as being after an intersection with stop signs
        /// </summary>
        /// <remarks>Intended location types: SideOfRoad</remarks>
        After4wayIntersection = 4,
    }
}
