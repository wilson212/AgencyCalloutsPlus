using Rage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgencyCalloutsPlus.API
{
    internal class PriorityCall
    {
        public int CallId { get; set; }

        public CalloutScenarioInfo ScenarioInfo { get; private set; }

        public int Priority => ScenarioInfo.Priority;

        public DateTime CallCreated { get; set; }

        public CallStatus CallStatus { get; set; }

        public GameLocation Location { get; set; }

        public string ZoneShortName { get; set; }

        public PriorityCall(int id, CalloutScenarioInfo scenarioInfo)
        {
            CallId = id;
            CallCreated = World.DateTime;
            ScenarioInfo = scenarioInfo ?? throw new ArgumentNullException(nameof(scenarioInfo));
        }
    }
}
