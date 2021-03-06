﻿using AgencyDispatchFramework.Game;
using AgencyDispatchFramework.Simulation;
using System;
using System.Collections.Generic;
using System.Text;

namespace AgencyDispatchFramework.Dispatching
{
    /// <summary>
    /// Represents a policing agency that is statewide
    /// </summary>
    public class HighwayPatrolAgency : Agency
    {
        internal HighwayPatrolAgency(string scriptName, string friendlyName, StaffLevel staffLevel) 
            : base(scriptName, friendlyName, staffLevel)
        {
        }

        internal override void Enable()
        {
            // Saftey
            if (IsActive) return;
            base.Enable();

            // Get our zones of jurisdiction, and ensure each zone has the primary agency set
            foreach (var zone in Zones)
            {
                // Grab county
                var name = zone.County == County.Blaine ? "bcso" : "lssd";

                // Set agencies. Order is important here!
                zone.PoliceAgencies = new List<Agency>()
                {
                    this,
                    GetAgencyByName(name)
                };
            }

            // Loop through each time period and cache crime numbers
            foreach (TimePeriod period in Enum.GetValues(typeof(TimePeriod)))
            {
                // Spawn
                int aiPatrolCount = OptimumPatrols[period];
                OfficersByShift.Add(period, new List<OfficerUnit>());
                var periodName = Enum.GetName(typeof(TimePeriod), period);

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
                    if (period == GameWorld.CurrentTimePeriod)
                    {
                        var sp = locations[i];
                        unit.StartDuty(sp);
                    }
                }

                // Log for debugging
                Log.Debug($"Loaded {aiPatrolCount} Virtual AI officer units for agency '{FriendlyName}' on {periodName} shift");
            }
        }

        /// <summary>
        /// Calculates the optimum patrols for this agencies jurisdiction
        /// </summary>
        /// <param name="zones"></param>
        /// <returns></returns>-
        protected override Dictionary<TimePeriod, int> GetOptimumUnitCounts(WorldZone[] zones)
        {
            if (Dispatch.PlayerAgency.IsStateAgency)
            {
                return new Dictionary<TimePeriod, int>()
                {
                    { TimePeriod.Day, 20 },
                    { TimePeriod.Evening, 24 },
                    { TimePeriod.Night, 16 },
                    { TimePeriod.Morning, 24 }
                };
            }
            else
            {
                return new Dictionary<TimePeriod, int>()
                {
                    { TimePeriod.Day, 10 },
                    { TimePeriod.Evening, 12 },
                    { TimePeriod.Night, 8 },
                    { TimePeriod.Morning, 12 }
                };
            }
        }

        /// <summary>
        /// Creates the dispatcher for this agency type
        /// </summary>
        /// <returns></returns>
        internal override Dispatcher CreateDispatcher()
        {
            return new PoliceDispatcher(this);
        }
    }
}
