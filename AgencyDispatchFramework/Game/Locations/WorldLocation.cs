using Rage;
using System;
using System.Text;

namespace AgencyDispatchFramework.Game.Locations
{
    /// <summary>
    /// Represents a <see cref="Vector3"/> position within the GTA V world
    /// </summary>
    public abstract class WorldLocation : IEquatable<WorldLocation>
    {
        /// <summary>
        /// Gets the <see cref="Vector3"/> coordinates of this location
        /// </summary>
        public Vector3 Position { get; protected set; }

        /// <summary>
        /// Gets the address to be used in the CAD
        /// </summary>
        public string StreetName { get; internal set; } = String.Empty;

        /// <summary>
        /// Gets the hint or description text of this location if any
        /// </summary>
        public string Hint { get; internal set; } = String.Empty;

        /// <summary>
        /// Gets the postal address if any
        /// </summary>
        public int Postal { get; internal set; }

        /// <summary>
        /// Gets the <see cref="LocationFlags"/> for this <see cref="WorldLocation"/>
        /// </summary>
        public LocationFlags Flags { get; internal set; }

        /// <summary>
        /// Gets the <see cref="AgencyDispatchFramework.API.LocationType"/> for this <see cref="WorldLocation"/>
        /// </summary>
        public abstract LocationType LocationType { get; }

        /// <summary>
        /// Creates a new instance of <see cref="WorldLocation"/>
        /// </summary>
        /// <param name="coordinates"></param>
        internal WorldLocation(Vector3 coordinates)
        {
            Position = coordinates;
            Flags = new LocationFlags();
        }

        public virtual string GetAddress()
        {
            StringBuilder builder = new StringBuilder();

            if (!String.IsNullOrWhiteSpace(StreetName))
                builder.Append(StreetName);

            return builder.ToString();
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
        public override string ToString() => Position.ToString();

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode() => Position.GetHashCode();
    }
}
