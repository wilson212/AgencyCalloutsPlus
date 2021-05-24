using Rage;
using System;
using System.Linq;
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
        /// Gets the zone of this location, if known
        /// </summary>
        public ZoneInfo Zone { get; internal set; }

        /// <summary>
        /// Gets the <see cref="LocationType"/> for this <see cref="WorldLocation"/>
        /// </summary>
        public abstract LocationTypeCode LocationType { get; }

        public int[] Flags { get; internal set; }

        /// <summary>
        /// Creates a new instance of <see cref="WorldLocation"/>
        /// </summary>
        /// <param name="coordinates"></param>
        internal WorldLocation(Vector3 coordinates)
        {
            Position = coordinates;
        }

        public virtual string GetAddress()
        {
            StringBuilder builder = new StringBuilder();

            if (!String.IsNullOrWhiteSpace(StreetName))
                builder.Append(StreetName);

            return builder.ToString();
        }

        /// <summary>
        /// Determines whether this <see cref="WorldLocation"/> instance contains all the specified flags.
        /// This method is used for filtering locations based on Callout location requirements.
        /// </summary>
        /// <param name="requiredFlags"></param>
        /// <returns></returns>
        public bool HasAllFlags(int[] requiredFlags)
        {
            return requiredFlags.All(i => Flags.Contains(i));
        }

        /// <summary>
        /// Determines whether this <see cref="WorldLocation"/> instance contains any of the specified flags.
        /// This method is used for filtering locations based on Callout location requirements.
        /// </summary>
        /// <param name="flags"></param>
        /// <returns></returns>
        public bool HasAnyFlag(int[] flags)
        {
            return flags.Any(i => Flags.Contains(i));
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
