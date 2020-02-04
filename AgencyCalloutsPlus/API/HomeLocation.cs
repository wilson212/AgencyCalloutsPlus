using Rage;
using System;
using System.Collections.Generic;

namespace AgencyCalloutsPlus.API
{
    /// <summary>
    /// Represents a <see cref="WorldLocation"/> that is a home
    /// </summary>
    public class HomeLocation : WorldLocation
    {
        /// <summary>
        /// Containts a list of spawn points for <see cref="Rage.Entity"/> types
        /// </summary>
        internal Dictionary<HomeSpawn, SpawnPoint> SpawnPoints { get; set; }

        /// <summary>
        /// Gets the <see cref="ZoneInfo"/> this home belongs in
        /// </summary>
        public ZoneInfo Zone { get; protected set; }

        /// <summary>
        /// Gets the <see cref="SocialClass"/> of this home
        /// </summary>
        public SocialClass Class { get; internal set; }

        /// <summary>
        /// Gets the <see cref="API.HomeType"/> of this home
        /// </summary>
        public HomeType Type { get; internal set; }

        /// <summary>
        /// Creates a new instance of <see cref="HomeLocation"/>
        /// </summary>
        /// <param name="location"></param>
        internal HomeLocation(ZoneInfo zone, Vector3 location) : base(location)
        {
            // @todo
            Zone = zone;
            SpawnPoints = new Dictionary<HomeSpawn, SpawnPoint>(20);
        }

        internal bool IsValid()
        {
            // Ensure spawn points is full
            foreach (HomeSpawn type in Enum.GetValues(typeof(HomeSpawn)))
            {
                if (!SpawnPoints.ContainsKey(type))
                    return false;
            }

            return true;
        }
    }
}
