using Rage;

namespace AgencyCalloutsPlus.API
{
    /// <summary>
    /// Represents a <see cref="Vector3"/> position within the GTA V world
    /// </summary>
    public class GameLocation
    {
        /// <summary>
        /// Gets the <see cref="Vector3"/> position of this location
        /// </summary>
        public Vector3 Position { get; protected set; }

        /// <summary>
        /// Gets the address to be used in Computer+
        /// </summary>
        public string Address { get; set; }

        /// <summary>
        /// Creates a new instance of <see cref="GameLocation"/>
        /// </summary>
        /// <param name="position"></param>
        public GameLocation(Vector3 position)
        {
            this.Position = position;
        }
    }
}
