using Rage;

namespace AgencyCalloutsPlus.API
{
    public class SpawnPoint : WorldLocation
    {
        /// <summary>
        /// Gets the heading of an object entity at this location, if any
        /// </summary>
        public float Heading { get; protected set; }

        public SpawnPoint(Vector3 location, float heading = 0f) : base(location)
        {
            this.Heading = heading;
        }
    }
}
