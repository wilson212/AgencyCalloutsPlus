using Rage;
using System.Collections.Generic;
using System.Xml;

namespace AgencyCalloutsPlus.API
{
    /// <summary>
    /// Represents a <see cref="Vector3"/> position within the GTA V world
    /// </summary>
    public abstract class WorldLocation
    {
        /// <summary>
        /// Gets the <see cref="Vector3"/> position of this location
        /// </summary>
        public Vector3 Position { get; protected set; }

        /// <summary>
        /// Gets the address to be used in Computer+
        /// </summary>
        public string Address { get; internal set; }

        /// <summary>
        /// Gets the postal address if any
        /// </summary>
        public int Postal { get; internal set; }

        /// <summary>
        /// Creates a new instance of <see cref="WorldLocation"/>
        /// </summary>
        /// <param name="position"></param>
        internal WorldLocation(Vector3 position)
        {
            this.Position = position;
        }

        /// <summary>
        /// Enables casting to a <see cref="Vector3"/>
        /// </summary>
        /// <param name="s"></param>
        public static implicit operator Vector3(WorldLocation w)
        {
            return w.Position;
        }
    }
}
