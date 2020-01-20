using Rage;
using System.Collections.Generic;

namespace AgencyCalloutsPlus.API
{
    /// <summary>
    /// Represents a <see cref="GameLocation"/> that is a home
    /// </summary>
    public class HomeLocation : GameLocation
    {
        /// <summary>
        /// Containts a list of spawn points for <see cref="Rage.Entity"/> types
        /// </summary>
        private Dictionary<HomeSpawnType, SpawnPoint> SpawnPoints { get; set; }

        /// <summary>
        /// Creates a new instance of <see cref="HomeLocation"/>
        /// </summary>
        /// <param name="location"></param>
        public HomeLocation(Vector3 location) : base(location)
        {
            // @todo
        }
    }
}
