using AgencyDispatchFramework.Game;
using AgencyDispatchFramework.Simulation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgencyDispatchFramework.Dispatching
{
    public class HighwayPatrolAgency : Agency
    {
        internal HighwayPatrolAgency(string scriptName, string friendlyName, StaffLevel staffLevel) 
            : base(scriptName, friendlyName, staffLevel)
        {
        }

        /// <summary>
        /// 
        /// </summary>
        internal override void Enable()
        {
            // Saftey
            if (IsActive) return;

            // Get our zones of jurisdiction, and ensure each zone has the primary agency set
            Zones = Zones ?? GetZoneNamesByAgencyName(ScriptName).Select(x => ZoneInfo.GetZoneByName(x)).ToArray();
            foreach (var zone in Zones)
            {
                zone.PrimaryAgency = this;
            }

            // Load officers
            if (OfficersByShift == null)
            {
                OfficersByShift = new Dictionary<TimeOfDay, List<OfficerUnit>>();
            }

            // Get our patrol counts
            OptimumPatrols = new Dictionary<TimeOfDay, int>()
            {
                { TimeOfDay.Day, 10 },
                { TimeOfDay.Evening, 12 },
                { TimeOfDay.Night, 8 },
                { TimeOfDay.Morning, 12 }
            };

            // Loop through each time period and cache crime numbers
            foreach (TimeOfDay period in Enum.GetValues(typeof(TimeOfDay)))
            {
                // Spawn
                int aiPatrolCount = OptimumPatrols[period];
                OfficersByShift.Add(period, new List<OfficerUnit>());
                var periodName = Enum.GetName(typeof(TimeOfDay), period);

                // Ensure we have enough locations to spawn patrols at
                var locations = GetRandomShoulderLocations(aiPatrolCount);
                if (locations.Length < aiPatrolCount)
                {
                    StringBuilder b = new StringBuilder("The number of RoadShoulders available (");
                    b.Append(locations.Length);
                    b.Append(") to spawn AI officer units is less than the number of total AI officers (");
                    b.Append(aiPatrolCount);
                    b.Append(") for '");
                    b.Append(FriendlyName);
                    b.Append("' on ");
                    b.Append(periodName);
                    b.Append(" shift.");
                    Log.Warning(b.ToString());

                    // Adjust count
                    aiPatrolCount = locations.Length;
                }

                // Create officer units
                for (int i = 0; i < aiPatrolCount; i++)
                {
                    // Create instance
                    var num = i + 10;
                    var unit = new VirtualAIOfficerUnit(this, 1, 'A', num);
                    OfficersByShift[period].Add(unit);

                    // Start Duty
                    if (period == GameWorld.CurrentTimeOfDay)
                    {
                        var sp = locations[i];
                        unit.StartDuty(sp);
                    }
                }

                // Log for debugging
                Log.Debug($"Loaded {aiPatrolCount} Virtual AI officer units for agency '{FriendlyName}' on {periodName} shift");
            }

            // Register for TimeOfDay changes!
            GameWorld.OnTimeOfDayChanged += GameWorld_OnTimeOfDayChanged;

            // Finally, flag
            IsActive = true;
        }

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override string ToString()
        {
            return base.ToString();
        }
    }
}
