using Rage;

namespace AgencyCalloutsPlus.API
{
    /// <summary>
    /// Represents a <see cref="WorldLocation"/> with a heading, used for 
    /// spawning an <see cref="Entity"/>
    /// </summary>
    public class SpawnPoint : WorldLocation
    {
        /// <summary>
        /// Gets the heading of an object <see cref="Entity"/> at this location, if any
        /// </summary>
        public float Heading { get; protected set; }

        /// <summary>
        /// Creates a new instance of <see cref="SpawnPoint"/>
        /// </summary>
        /// <param name="location">The <see cref="Vector3"/> location</param>
        /// <param name="heading">The directional heading for an <see cref="Entity"/> to face</param>
        public SpawnPoint(Vector3 location, float heading = 0f) : base(location)
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
