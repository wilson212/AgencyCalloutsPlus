using AgencyDispatchFramework.Dispatching;
using AgencyDispatchFramework.Game;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AgencyDispatchFramework.Simulation
{
    /// <summary>
    /// Represents a police type agency
    /// </summary>
    public class PoliceAgency : Agency
    {
        internal PoliceAgency(string scriptName, string friendlyName, StaffLevel staffLevel, CallSignStyle signStyle) 
            : base(scriptName, friendlyName, staffLevel, signStyle)
        {
            
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        internal override Dispatcher CreateDispatcher()
        {
            return new PoliceDispatcher(this);
        }

        /// <summary>
        /// 
        /// </summary>
        protected override void AssignZones()
        {
            // Get our zones of jurisdiction, and ensure each zone has the primary agency set
            foreach (var zone in Zones)
            {
                // Grab county
                var name = zone.County == County.Blaine ? "bcso" : "lssd";

                // Set agencies. Order is important here!
                zone.PoliceAgencies = new List<Agency>()
                {
                    this,
                    GetAgencyByName(name),
                    GetAgencyByName("sahp")
                };
            }
        }

        /// <summary>
        /// Calculates the optimum patrols for this agencies jurisdiction
        /// </summary>
        /// <param name="zoneNames"></param>
        /// <returns></returns>
        protected override void CalculateAgencySize()
        {
            // --------------------------------------------------
            // Get number of optimal patrols, and total calls per period
            // --------------------------------------------------
            var officerCounts = new Dictionary<TimePeriod, int>();
            var callsByPeriod = new Dictionary<TimePeriod, int>();
            int totalDailyCalls = 0;
            int totalRosterSize = 0;

            // Define which responsibilities that Patrol is the primary responder for
            var patrolHandlingCalls = new HashSet<CallCategory>((CallCategory[])Enum.GetValues(typeof(CallCategory)));

            // If we have a traffic unit, remove traffic responsibilities from patrol
            if (Units.ContainsKey(UnitType.Traffic))
                patrolHandlingCalls.Remove(CallCategory.Traffic);

            // If we have a gang unit, remove gang responsibilities from patrol
            if (Units.ContainsKey(UnitType.Gang))
                patrolHandlingCalls.Remove(CallCategory.Gang);

            // Calculate each TimePeriod of the day
            foreach (TimePeriod period in Enum.GetValues(typeof(TimePeriod)))
            {
                // Add period to counts
                officerCounts.Add(period, 0);
                callsByPeriod.Add(period, 0);

                // Cache variables
                double optimumPatrols = 0;
                int officers = 0; 

                // Add data from each zone in the Jurisdiction
                foreach (var zone in Zones)
                {
                    // Get average calls per period
                    var calls = zone.AverageCalls[period];
                    callsByPeriod[period] += calls;
                    totalDailyCalls += calls;

                    // Grab crime report of this zone
                    var report = zone.GetCrimeReport(period, Weather.Clear);

                    // --------------------------------------------------
                    // Add Patrol units
                    // --------------------------------------------------
                    if (Units.ContainsKey(UnitType.Patrol))
                    {
                        // Get average number of calls
                        var callCount = report.GetExpectedCallCountsOf(patrolHandlingCalls.ToArray());
                        optimumPatrols = GetOptimumPatrolCountForZone(callCount, period, zone);

                        officers = (int)Math.Ceiling(optimumPatrols);
                        officerCounts[period] += officers;
                        totalRosterSize += officers;
                        Units[UnitType.Patrol].OptimumPatrols[period] = officers;
                    }

                    // --------------------------------------------------
                    // Add Traffic units
                    // --------------------------------------------------
                    if (Units.ContainsKey(UnitType.Traffic))
                    {
                        // Get average number of calls
                        var callCount = report.GetExpectedCallCountsOf(CallCategory.Traffic);
                        optimumPatrols = GetOptimumTrafficCountForZone(callCount, period, zone);

                        officers = (int)Math.Ceiling(optimumPatrols);
                        officerCounts[period] += officers;
                        totalRosterSize += officers;
                        Units[UnitType.Traffic].OptimumPatrols[period] = officers;
                    }

                    // @todo Add gang and drug units
                }
            }

            // Do stuff

        }

        /// <summary>
        /// Determines the optimal number of patrols this zone should have based
        /// on <see cref="ZoneSize"/> and <see cref="CrimeLevel"/>
        /// </summary>
        /// <param name="averageCalls"></param>
        /// <param name="zone"></param>
        /// <returns></returns>
        protected virtual double GetOptimumPatrolCountForZone(double averageCalls, TimePeriod period, WorldZone zone)
        {
            double callsPerOfficerPerShift = 4d;
            double baseCount = Math.Max(0.5d, averageCalls / callsPerOfficerPerShift);

            switch (zone.Size)
            {
                case ZoneSize.VerySmall:
                case ZoneSize.Small:
                    baseCount--;
                    break;
                case ZoneSize.Medium:
                case ZoneSize.Large:
                    break;
                case ZoneSize.VeryLarge:
                case ZoneSize.Massive:
                    baseCount += 1;
                    break;
            }

            return Math.Max(0.25d, baseCount);
        }

        /// <summary>
        /// Determines the optimal number of patrols this zone should have based
        /// on <see cref="ZoneSize"/>, <see cref="Population"/> and <see cref="CrimeLevel"/> rate
        /// </summary>
        /// <param name="averageCalls"></param>
        /// <param name="zone"></param>
        /// <returns></returns>
        protected virtual double GetOptimumTrafficCountForZone(double averageCalls, TimePeriod period, WorldZone zone)
        {
            double callsPerOfficerPerShift = 4d;
            double baseCount = Math.Max(0.5d, averageCalls / callsPerOfficerPerShift);

            if (Units.ContainsKey(UnitType.Traffic))
            {
                switch (zone.Size)
                {
                    case ZoneSize.VerySmall:
                    case ZoneSize.Small:
                        baseCount--;
                        break;
                    case ZoneSize.Medium:
                    case ZoneSize.Large:
                        break;
                    case ZoneSize.VeryLarge:
                    case ZoneSize.Massive:
                        baseCount += 1;
                        break;
                }

                switch (zone.Population)
                {
                    default: // None
                        return 0;
                    case Population.Scarce:
                        baseCount *= 0.75;
                        break;
                    case Population.Moderate:
                        // No adjustment
                        break;
                    case Population.Dense:
                        baseCount *= 1.25;
                        break;
                }
            }

            return Math.Max(0.25d, baseCount);
        }
    }
}
