using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgencyCalloutsPlus.API
{
    public class HomeLocation
    {
        public string Address { get; protected set; }

        private Dictionary<HomeSpawnType, SpawnPoint> SpawnPoints { get; set; }
    }
}
