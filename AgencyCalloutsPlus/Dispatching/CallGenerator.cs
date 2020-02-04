using AgencyCalloutsPlus.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgencyCalloutsPlus.Dispatching
{
    internal abstract class CallGenerator
    {
        /// <summary>
        /// Contains the last Call ID used
        /// </summary>
        private static int NextCallId { get; set; }

        /// <summary>
        /// Randomizer method used to randomize callouts and locations
        /// </summary>
        private static CryptoRandom Randomizer { get; set; }

        /// <summary>
        /// Containts a range of time between calls.
        /// </summary>
        public Range<int> CallTimerRange { get; set; }

        static CallGenerator()
        {
            // Create next random call ID
            Randomizer = new CryptoRandom();
            NextCallId = Randomizer.Next(21234, 34567);
        }

        public CallGenerator()
        {

        }

        public virtual PriorityCall GenerateCall()
        {
            // Try to generate a call
            for (int i = 0; i < Settings.MaxLocationAttempts; i++)
            {
                try
                {
                    // Spawn a zone in our jurisdiction
                    ZoneInfo zone = Dispatch.PlayerAgency?.GetNextRandomCrimeZone();
                    if (zone == null)
                    {
                        Log.Debug($"Dispatch: Attempted to pull a zone but zone is null");
                        continue;
                    }

                    // Spawn crime type from our spawned zone
                    CalloutType type = zone.GetNextRandomCrimeType();
                    if (!Dispatch.ScenarioPool[type].TrySpawn(out CalloutScenarioInfo scenario))
                    {
                        Log.Debug($"Dispatch: Unable to pull CalloutType {type} from zone '{zone.FriendlyName}'");
                        continue;
                    }

                    // Get a random location!
                    WorldLocation location = null;
                    switch (scenario.LocationType)
                    {
                        case LocationType.SideOfRoad:
                            location = zone.GetRandomSideOfRoadLocation();
                            break;
                    }

                    // no location?
                    if (location == null)
                    {
                        Log.Debug($"Dispatch: Unable to pull Location type {scenario.LocationType} from zone '{zone.FriendlyName}'");
                        continue;
                    }

                    // Create PriorityCall wrapper
                    var call = new PriorityCall(NextCallId++, scenario)
                    {
                        Location = location,
                        CallStatus = CallStatus.Created,
                        Zone = zone
                    };

                    // Add call to priority Queue
                    return call;
                }
                catch (Exception ex)
                {
                    Log.Exception(ex);
                }
            }

            return null;
        }
    }
}
