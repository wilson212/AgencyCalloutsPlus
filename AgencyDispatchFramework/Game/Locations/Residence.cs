﻿using Rage;
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
        /// Gets the <see cref="SocialClass"/> of this home
        /// </summary>
        public SocialClass Class { get; internal set; }

        /// <summary>
        /// Gets the Appartment/Suite/Room number of the address to be used in the CAD, if any
        /// </summary>
        public string UnitId { get; internal set; } = String.Empty;

        /// <summary>
        /// Gets the <see cref="Locations.LocationTypeCode"/> for this <see cref="WorldLocation"/>
        /// </summary>
        public override LocationTypeCode LocationType => LocationTypeCode.Residence;

        /// <summary>
        /// Gets an array of Flags that describe this <see cref="Residence"/>
        /// </summary>
        public ResidenceFlags[] ResidenceFlags { get; internal set; }

        /// <summary>
        /// Creates a new instance of <see cref="Residence"/>
        /// </summary>
        /// <param name="position"></param>
        internal Residence(WorldZone zone, Vector3 position) : base(position)
        {
            // @todo
            Zone = zone;
            SpawnPoints = new Dictionary<ResidencePosition, SpawnPoint>(21);
        }

        /// <summary>
        /// Returns whether the <see cref="SpawnPoint"/> collection is complete
        /// for this <see cref="WorldLocation"/> instance.
        /// </summary>
        /// <returns>true if all spawn points are set, false otherwise</returns>
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

        /// <summary>
        /// Gets an identifiable <see cref="SpawnPoint"/> by name for this <see cref="Residence"/>
        /// </summary>
        /// <param name="id">The <see cref="SpawnPoint"/> id</param>
        /// <returns>a <see cref="SpawnPoint"/> on success, false otherwise</returns>
        public SpawnPoint GetSpawnPositionById(ResidencePosition id)
        {
            if (!SpawnPoints.ContainsKey(id))
                return null;

            return SpawnPoints[id];
        }
    }
}
