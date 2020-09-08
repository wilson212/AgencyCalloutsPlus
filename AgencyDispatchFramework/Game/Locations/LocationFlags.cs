using System;

namespace AgencyDispatchFramework.Game.Locations
{
    /// <summary>
    /// A series of flags that help describe a <see cref="WorldLocation"/>. These flags can be used used
    /// to filter locations for a <see cref="Callouts.CalloutScenario"/> when a call is created
    /// </summary>
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
        /// Describes the location as being near an intersection with stop signs only
        /// on the intersecting road
        /// </summary>
        /// <remarks>Intended location types: SideOfRoad</remarks>
        Near4wayIntersectionNoStop = 4,

        /// <summary>
        /// Describes the location as being after an intersection with stop signs
        /// </summary>
        /// <remarks>Intended location types: SideOfRoad</remarks>
        After4wayIntersection = 8,

        /// <summary>
        /// Describes the location as being after an intersection with stop signs only
        /// on the intersecting road
        /// </summary>
        /// <remarks>Intended location types: SideOfRoad</remarks>
        After4wayIntersectionNoStop = 16,

        /// <summary>
        /// Describes the location as being on the side of a interstate freeway
        /// </summary>
        /// <remarks>Intended location types: SideOfRoad</remarks>
        AlongInterstate = 32,

        /// <summary>
        /// Describes the location as being on the side of a interstate freeway after an onramp
        /// </summary>
        /// <remarks>Intended location types: SideOfRoad</remarks>
        AlongInterstateAfterRamp = 64,

        /// <summary>
        /// Describes the location as being along a 2 lane road
        /// </summary>
        /// <remarks>Intended location types: SideOfRoadResidence, Store</remarks>
        Along2LaneRoad = 128,

        /// <summary>
        /// Describes the location as being along a 3 lane road, having a center turn lane
        /// </summary>
        /// <remarks>Intended location types: SideOfRoadResidence, Store</remarks>
        Along3LaneCenterTurnRoad = 256,

        /// <summary>
        /// Describes the location as being along a 4 lane road
        /// </summary>
        /// <remarks>Intended location types: SideOfRoadResidence, Store</remarks>
        Along4LaneRoad = 512,

        /// <summary>
        /// Describes the location as being along a 5 lane road, having a center turn lane
        /// </summary>
        /// <remarks>Intended location types: SideOfRoadResidence, Store</remarks>
        Along5LaneCenterTurnRoad = 1024,

        /// <summary>
        /// Describes the location as being along an unpaved road
        /// </summary>
        /// <remarks>Intended location types: SideOfRoad, Residence, Store</remarks>
        AlongDirtRoad = 2048,

        /// <summary>
        /// Describes the location as being along a one way road
        /// </summary>
        /// <remarks>Intended location types: SideOfRoad, Residence, Store</remarks>
        AlongOneWayRoad = 4096
    }
}
