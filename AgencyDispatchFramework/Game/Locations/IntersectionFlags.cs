using System;

namespace AgencyDispatchFramework.Game.Locations
{
    public enum IntersectionFlags
    {
        /// <summary>
        /// Describes a T-intersecion with 3 roads
        /// </summary>
        ThreeWayIntersection,

        /// <summary>
        /// Describes a 4 way intersectiond
        /// </summary>
        CrossIntersection,

        /// <summary>
        /// Describes a 3 way intersection that splits into a Y shape
        /// </summary>
        SplitIntersection,

        /// <summary>
        /// Describes an intersection with more than 4 roads
        /// </summary>
        MultiDirectionIntersection,

        /// <summary>
        /// Describes an intersection with stop lights on each road
        /// </summary>
        Lighted,

        /// <summary>
        /// Describes a T or split intersection where only the ajoining road has a stop sign.
        /// </summary>
        OneWayStop,

        /// <summary>
        /// Describes a T or split intersection where only the ajoining road has a yield sign.
        /// </summary>
        OneWayYield,

        /// <summary>
        /// Describes an intersection where 2 intersecting roads have a stop sign
        /// </summary>
        /// <remarks>
        /// In the case of a T intersection with this flag, the ajoining road has right of way and no stop sign
        /// </remarks>
        TwoWayStop,

        /// <summary>
        /// Describes an intersection where 2 intersecting roads have a yield sign
        /// </summary>
        /// <remarks>
        /// In the case of a T intersection with this flag, the ajoining road has right of way
        /// </remarks>
        TwoWayYield,

        /// <summary>
        /// Describes an intersection with Right-of-way rules, such as a 3 or 4 way stop sign intersection.
        /// </summary>
        RightOfWayStop,

        /// <summary>
        /// Describes an intersection with Right-of-way rules, such as a 3 or 4 way yield intersection.
        /// </summary>
        RightOfWayYield,
    }
}
