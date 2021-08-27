using AgencyDispatchFramework.Dispatching;
using AgencyDispatchFramework.Game;
using AgencyDispatchFramework.Simulation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgencyDispatchFramework.Simulation
{
    public class SheriffAgency : PoliceAgency
    {
        internal SheriffAgency(string scriptName, string friendlyName, StaffLevel staffLevel, CallSignStyle signStyle)
            : base(scriptName, friendlyName, staffLevel, signStyle)
        {

        }

        protected override void AssignZones()
        {
            // Get our zones of jurisdiction, and ensure each zone has the primary agency set
            foreach (var zone in Zones)
            {
                // Set agencies. Order is important here!
                zone.PoliceAgencies = new List<Agency>()
                {
                    this,
                    GetAgencyByName("sahp")
                };
            }
        }
    }
}
