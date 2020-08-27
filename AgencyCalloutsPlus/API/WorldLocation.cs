using Rage;
using System;

namespace AgencyCalloutsPlus.API
{
    /// <summary>
    /// Represents a <see cref="Vector3"/> position within the GTA V world
    /// </summary>
    public abstract class WorldLocation : IEquatable<WorldLocation>
    {
        /// <summary>
        /// A location ID counter that increments with each instance
        /// </summary>
        private static int LocationIdCounter = 0;

        /// <summary>
        /// Gets the unique location ID
        /// </summary>
        public int Id { get; protected set; }

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
        /// Gets the <see cref="LocationFlags"/> for this <see cref="WorldLocation"/>
        /// </summary>
        public LocationFlags Flags { get; internal set; }

        /// <summary>
        /// Gets the <see cref="AgencyCalloutsPlus.API.LocationType"/> for this <see cref="WorldLocation"/>
        /// </summary>
        public abstract LocationType LocationType { get; }

        /// <summary>
        /// Creates a new instance of <see cref="WorldLocation"/>
        /// </summary>
        /// <param name="position"></param>
        internal WorldLocation(Vector3 position)
        {
            Id = LocationIdCounter++;
            Position = position;
            Flags = new LocationFlags();
        }

        /// <summary>
        /// Enables casting to a <see cref="Vector3"/>
        /// </summary>
        /// <param name="s"></param>
        public static implicit operator Vector3(WorldLocation w)
        {
            return w.Position;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Equals(WorldLocation other)
        {
            if (other == null) return false;
            return (other.Position == Position);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            return Equals(obj as WorldLocation);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return Position.ToString();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
