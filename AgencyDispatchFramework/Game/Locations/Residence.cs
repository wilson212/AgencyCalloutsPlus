using Rage;
using System;
using System.Collections.Generic;

namespace AgencyDispatchFramework.Game.Locations
{
    /// <summary>
    /// Represents a <see cref="WorldLocation"/> that is a home
    /// </summary>
    public class Residence : WorldLocation
    {
        /// <summary>
        /// Containts a list of spawn points for <see cref="Entity"/> types
        /// </summary>
        internal Dictionary<ResidencePosition, SpawnPoint> SpawnPoints { get; set; }

        /// <summary>
        /// Gets the numerical buiding number of the address to be used in the CAD, if any
        /// </summary>
        public string BuildingNumber { get; internal set; } = String.Empty;

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
        public ResidenceType BuildingType { get; internal set; }

        /// <summary>
        /// Gets the Appartment/Suite/Room number of the address to be used in the CAD, if any
        /// </summary>
        public string UnitId { get; internal set; } = String.Empty;

        /// <summary>
        /// Gets the <see cref="Locations.LocationType"/> for this <see cref="WorldLocation"/>
        /// </summary>
        public override LocationType LocationType => LocationType.Residence;

        /// <summary>
        /// Creates a new instance of <see cref="Residence"/>
        /// </summary>
        /// <param name="position"></param>
        internal Residence(ZoneInfo zone, Vector3 position) : base(position)
        {
            // @todo
            Zone = zone;
            SpawnPoints = new Dictionary<ResidencePosition, SpawnPoint>(20);
        }

        internal bool IsValid()
        {
            // Ensure spawn points is full
            foreach (ResidencePosition type in Enum.GetValues(typeof(ResidencePosition)))
            {
                if (!SpawnPoints.ContainsKey(type))
                    return false;
            }

            return true;
        }

        public SpawnPoint GetPositionById(ResidencePosition id)
        {
            if (!SpawnPoints.ContainsKey(id))
                return null;

            return SpawnPoints[id];
        }
    }
}
