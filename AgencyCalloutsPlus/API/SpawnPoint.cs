using Rage;

namespace AgencyCalloutsPlus.API
{
    public class SpawnPoint : GameLocation
    {
        /// <summary>
        /// Gets the <see cref="Vector3"/> position of this location
        /// </summary>
        public Vector3 Position { get; protected set; }

        /// <summary>
        /// Gets the heading of an object entity at this location, if any
        /// </summary>
        public float Heading { get; protected set; }

        public SpawnPoint(Vector3 location, float heading = 0f)
        {
            this.Position = location;
            this.Heading = heading;
        }
    }
}
