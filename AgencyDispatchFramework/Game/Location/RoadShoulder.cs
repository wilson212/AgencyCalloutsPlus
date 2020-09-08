using Rage;

namespace AgencyDispatchFramework.Game.Location
{
    /// <summary>
    /// A <see cref="WorldLocation"/> that is a shoulder of a road, used for pullover locations
    /// to spawn a <see cref="Vehicle"/> or a <see cref="Ped"/>
    /// </summary>
    public class RoadShoulder : WorldLocation
    {
        /// <summary>
        /// Gets the <see cref="AgencyDispatchFramework.API.LocationType"/>
        /// </summary>
        public override LocationType LocationType => LocationType.SideOfRoad;

        /// <summary>
        /// Gets the <see cref="ZoneInfo"/> this home belongs in
        /// </summary>
        public ZoneInfo Zone { get; protected set; }

        /// <summary>
        /// Gets the heading of an object <see cref="Entity"/> at this location, if any
        /// </summary>
        public float Heading { get; protected set; }

        /// <summary>
        /// Creates a new instance of <see cref="RoadShoulder"/>
        /// </summary>
        /// <param name="zone"></param>
        /// <param name="vector"></param>
        /// <param name="heading"></param>
        public RoadShoulder(ZoneInfo zone, Vector3 vector, float heading) : base(vector)
        {
            Zone = zone;
            Heading = heading;
        }

        /// <summary>
        /// Enables casting to a <see cref="Vector3"/>
        /// </summary>
        /// <param name="s"></param>
        public static implicit operator Vector3(RoadShoulder s)
        {
            return s.Position;
        }

        /// <summary>
        /// Enables casting to  a <see cref="float"/>
        /// </summary>
        /// <param name="s"></param>
        public static implicit operator float(RoadShoulder s)
        {
            return s.Heading;
        }
    }
}
