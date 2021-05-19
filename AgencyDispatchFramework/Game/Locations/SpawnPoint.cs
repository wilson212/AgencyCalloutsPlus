using Rage;

namespace AgencyDispatchFramework.Game.Locations
{
    /// <summary>
    /// Represents a <see cref="WorldLocation"/> with a heading, that is used for 
    /// spawning an <see cref="Entity"/> in its place.
    /// </summary>
    public class SpawnPoint : WorldLocation
    {
        /// <summary>
        /// Gets the heading of an object <see cref="Entity"/> at this location, if any
        /// </summary>
        public float Heading { get; protected set; }

        /// <summary>
        /// Gets the <see cref="AgencyDispatchFramework.API.LocationType"/>
        /// </summary>
        public override LocationTypeCode LocationType => LocationTypeCode.SpawnPoint;

        /// <summary>
        /// Creates a new instance of <see cref="SpawnPoint"/>
        /// </summary>
        /// <param name="position">The <see cref="Vector3"/> location</param>
        /// <param name="heading">The directional heading for an <see cref="Entity"/> to face</param>
        public SpawnPoint(Vector3 position, float heading = 0f) : base(position)
        {
            this.Heading = heading;
        }

        /// <summary>
        /// Enables casting to a <see cref="Vector3"/>
        /// </summary>
        /// <param name="s"></param>
        public static implicit operator Vector3(SpawnPoint s)
        {
            return s.Position;
        }

        /// <summary>
        /// Enables casting to  a <see cref="float"/>
        /// </summary>
        /// <param name="s"></param>
        public static implicit operator float(SpawnPoint s)
        {
            return s.Heading;
        }
    }
}
