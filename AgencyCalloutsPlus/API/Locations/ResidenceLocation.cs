using Rage;
using System;
using System.Collections.Generic;

namespace AgencyCalloutsPlus.API
{
    /// <summary>
    /// Represents a <see cref="WorldLocation"/> that is a home
    /// </summary>
    public class ResidenceLocation : WorldLocation
    {
        /// <summary>
        /// Containts a list of spawn points for <see cref="Rage.Entity"/> types
        /// </summary>
        internal Dictionary<HomeSpawnId, SpawnPoint> SpawnPoints { get; set; }

        /// <summary>
        /// Gets the <see cref="ZoneInfo"/> this home belongs in
        /// </summary>
        public ZoneInfo Zone { get; protected set; }

        /// <summary>
        /// Gets the <see cref="SocialClass"/> of this home
        /// </summary>
        public SocialClass Class { get; internal set; }

        /// <summary>
        /// Gets the <see cref="API.ResidenceType"/> of this home
        /// </summary>
        public ResidenceType Type { get; internal set; }

        /// <summary>
        /// Gets the <see cref="AgencyCalloutsPlus.API.LocationType"/> for this <see cref="WorldLocation"/>
        /// </summary>
        public override LocationType LocationType => LocationType.Residence;

        /// <summary>
        /// Gets the <see cref="Vector3"/> location of this residence
        /// </summary>
        public Vector3 Location { get; set; }

        /// <summary>
        /// Creates a new instance of <see cref="ResidenceLocation"/>
        /// </summary>
        /// <param name="location"></param>
        internal ResidenceLocation(ZoneInfo zone, Vector3 location) : base(location)
        {
            // @todo
            Zone = zone;
            SpawnPoints = new Dictionary<HomeSpawnId, SpawnPoint>(20);
        }

        internal bool IsValid()
        {
            // Ensure spawn points is full
            foreach (HomeSpawnId type in Enum.GetValues(typeof(HomeSpawnId)))
            {
                if (!SpawnPoints.ContainsKey(type))
                    return false;
            }

            return true;
        }

        public SpawnPoint GetSpawnPoint(HomeSpawnId type)
        {
            if (!SpawnPoints.ContainsKey(type))
                return null;

            return SpawnPoints[type];
        }
    }
}
