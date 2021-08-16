using AgencyDispatchFramework.Dispatching;
using AgencyDispatchFramework.Extensions;
using AgencyDispatchFramework.Game.Locations;
using AgencyDispatchFramework.Xml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AgencyDispatchFramework.Game
{
    /// <summary>
    /// This class contains a series of spawnable locations within a specific zone
    /// </summary>
    public sealed class WorldZone : ISpawnable
    {
        /// <summary>
        /// Contains a hash table of zones
        /// </summary>
        /// <remarks>[ ZoneScriptName => ZoneInfo class ]</remarks>
        private static Dictionary<string, WorldZone> ZoneCache { get; set; }

        /// <summary>
        /// Containts a hash table of regions, and thier zones
        /// </summary>
        private static Dictionary<string, List<string>> RegionZones { get; set; } = new Dictionary<string, List<string>>(16);

        /// <summary>
        /// Gets the Zone name
        /// </summary>
        public string ScriptName { get; internal set; }

        /// <summary>
        /// Gets the Zone name
        /// </summary>
        public string FullName { get; internal set; }

        /// <summary>
        /// Gets the population density of the zone
        /// </summary>
        public Population Population { get; internal set; }

        /// <summary>
        /// Gets the zone size
        /// </summary>
        public ZoneSize Size { get; internal set; }

        /// <summary>
        /// Gets the primary zone type for this zone
        /// </summary>
        public ZoneType ZoneType { get; internal set; }

        /// <summary>
        /// Gets the social class of this zones citizens
        /// </summary>
        public SocialClass SocialClass { get; internal set; }

        /// <summary>
        /// Contains a dictionary of the average number of calls per time of day in this zone
        /// </summary>
        public IReadOnlyDictionary<TimePeriod, int> AverageCalls { get; internal set; }

        /// <summary>
        /// Containts a list <see cref="Residence"/>(s) in this zone
        /// </summary>
        public Residence[] Residences { get; internal set; }

        /// <summary>
        /// Containts an array of Road Shoulder locations
        /// </summary>
        public RoadShoulder[] RoadShoulders { get; internal set; }

        /// <summary>
        /// Gets the crime level probability of this zone based on current time of day
        /// </summary>
        public int Probability => AverageCalls[GameWorld.CurrentTimePeriod];

        /// <summary>
        /// Spawns a <see cref="CallCategory"/> based on the <see cref="WorldStateMultipliers"/> probabilites set
        /// </summary>
        internal WorldStateProbabilityGenerator<CallCategory> CallCategoryGenerator { get; set; }

        /// <summary>
        /// Contains a list of police <see cref="Agency"/> instances that have jurisdiction in this <see cref="WorldZone"/>
        /// </summary>
        public List<Agency> PoliceAgencies { get; internal set; }

        /// <summary>
        /// Gets the medical <see cref="Agency"/> that services this <see cref="WorldZone"/>
        /// </summary>
        public Agency EmsAgency { get; internal set; }

        /// <summary>
        /// Gets the fire <see cref="Agency"/> that services this <see cref="WorldZone"/>
        /// </summary>
        public Agency FireAgeny { get; internal set; }

        /// <summary>
        /// Gets the <see cref="Game.County"/> in game this zone belongs in
        /// </summary>
        public County County { get; internal set; }

        /// <summary>
        /// Creates a new instance of <see cref="WorldZone"/>
        /// </summary>
        public WorldZone()
        {
            PoliceAgencies = new List<Agency>();
        }

        /// <summary>
        /// Indicates whether the given <see cref="Agency"/> has jurisdiction in this <see cref="WorldZone"/>
        /// </summary>
        /// <param name="agency"></param>
        /// <returns></returns>
        public bool DoesAgencyHaveJurisdiction(Agency agency)
        {
            if (agency.IsLawEnforcementAgency)
            {
                return PoliceAgencies.Contains(agency);
            }

            return false;
        }

        /// <summary>
        /// Gets the total number of Locations in this collection, regardless of type
        /// </summary>
        /// <returns></returns>
        public int GetTotalNumberOfLocations()
        {
            // Add up location counts
            var count = RoadShoulders?.Length ?? 0;
                count += Residences?.Length ?? 0;

            // Final count
            return count;
        }

        /// <summary>
        /// Spawns the next <see cref="CallCategory"/> that will happen in this zone
        /// based on the crime probabilities set
        /// </summary>
        /// <returns>
        /// returns the next callout type on success. On failure, <see cref="CallCategory.Traffic"/>
        /// will always be returned
        /// </returns>
        public CallCategory GetNextRandomCrimeType()
        {
            if (CallCategoryGenerator.TrySpawn(out CallCategory calloutType))
            {
                return calloutType;
            }

            return CallCategory.Traffic;
        }

        /// <summary>
        /// This is where the magic happens. This method Gets a random <see cref="WorldLocation"/> from a pool
        /// of locations, applying filters and checking to see if the location is already in use
        /// </summary>
        /// <param name="type"></param>
        /// <param name="locationPool"></param>
        /// <param name="inactiveOnly">If true, will only return a <see cref="WorldLocation"/> that is not currently in use</param>
        /// <returns></returns>
        private T GetRandomLocationFromPool<T>(T[] locationPool, FlagFilterGroup filters, bool inactiveOnly) where T : WorldLocation
        {
            // If we have no locations, return null
            if (locationPool == null || locationPool.Length == 0)
            {
                Log.Debug($"WorldZone.GetRandomLocationFromPool<T>(): Unable to pull a {typeof(T).Name} from zone '{ScriptName}' because no locations were provided in the list");
                return null;
            }

            var type = locationPool[0].LocationType;

            // Filtering by flags? Do this first so we can log debugging info if there are no available locations with these required flags in this zone
            if (filters != null && filters.Requirements.Count > 0)
            {
                locationPool = locationPool.Filter(filters).ToArray();
                if (locationPool.Length == 0)
                {
                    Log.Warning($"WorldZone.GetRandomLocationFromPool<T>(): There are no locations of type '{type}' in zone '{ScriptName}' using the following flags:");
                    Log.Warning($"\t{filters}");
                    return null;
                }
            }

            // Will any location work?
            if (inactiveOnly)
            {
                try
                {
                    // Find all locations not in use
                    locationPool = Dispatch.GetInactiveLocationsFromPool(locationPool);
                }
                catch (InvalidCastException ex)
                {
                    Log.Exception(ex, $"WorldZone.GetRandomLocationFromPool<T>(): Cast exception to {typeof(T).Name} from location pool. Logging exception data");
                    return null;
                }
            }

            // If no locations are available
            if (locationPool.Length == 0)
            {
                Log.Debug($"WorldZone.GetRandomLocationFromPool<T>(): Unable to pull an available '{type}' location from zone '{ScriptName}' because they are all in use");
                return null;
            }

            // Load randomizer
            var random = new CryptoRandom();

            // We are good to go!
            return random.PickOne(locationPool);
        }

        /// <summary>
        /// Gets a random Side of the Road location in this zone
        /// </summary>
        /// <param name="inactiveOnly">If true, will only return a <see cref="WorldLocation"/> that is not currently in use</param>
        /// <returns>returns a random <see cref="SpawnPoint"/> on success, or null on failure</returns>
        public RoadShoulder GetRandomRoadShoulder(FlagFilterGroup filters, bool inactiveOnly = false)
        {
            // Get random location
            return GetRandomLocationFromPool(RoadShoulders, filters, inactiveOnly);
        }

        /// <summary>
        /// Gets a random <see cref="Residence"/> in this zone
        /// </summary>
        /// <param name="inactiveOnly">If true, will only return a <see cref="WorldLocation"/> that is not currently in use</param>
        /// <returns>returns a random <see cref="Residence"/> on success, or null on failure</returns>
        public Residence GetRandomResidence(FlagFilterGroup filters, bool inactiveOnly = false)
        {
            // Get random location
            return GetRandomLocationFromPool(Residences, filters, inactiveOnly);
        }

        /// <summary>
        /// Loads the specified zones and of thier position data from the Locations.xml into
        /// memory, and returns the number of locations added.
        /// </summary>
        /// <param name="names">An array of zones to load (should be all uppercase)</param>
        /// <returns>returns the number of locations loaded</returns>
        /// <param name="loaded">Returns the number of zones loaded directly from the XML files.</param>
        public static WorldZone[] GetZonesByName(string[] names, out int loaded)
        {
            // Create instance of not already!
            if (ZoneCache == null)
            {
                ZoneCache = new Dictionary<string, WorldZone>();
            }

            int totalLocations = 0;
            int zonesAdded = 0;
            List<WorldZone> zones = new List<WorldZone>();

            // Cycle through each child node (Zone)
            foreach (string zoneName in names)
            {
                // If we have loaded this zone already, skip it
                if (ZoneCache.ContainsKey(zoneName))
                {
                    zones.Add(ZoneCache[zoneName]);
                    continue;
                }

                // Check file exists
                string path = Path.Combine(Main.FrameworkFolderPath, "Locations", $"{zoneName}.xml");
                if (!File.Exists(path))
                {
                    Log.Warning($"WorldZone.LoadZones(): Missing xml file for zone '{zoneName}'");
                    continue;
                }

                // Create our spawn point collection and store it
                try
                {
                    // Load XML document
                    using (var file = new WorldZoneFile(path))
                    {
                        // Parse the XML contents
                        file.Parse();

                        zones.Add(file.Zone);

                        // Save
                        ZoneCache.Add(zoneName, file.Zone);
                        totalLocations += file.Zone.GetTotalNumberOfLocations();
                        zonesAdded++;
                    }
                }
                catch (ArgumentException e)
                {
                    Log.Error($"WorldZone.LoadZones(): Unable to load location data for zone '{zoneName}'. Missing node '{e.ParamName}'");
                    continue;
                }
                catch (Exception fe)
                {
                    // Error should already
                    Log.Error($"WorldZone.LoadZones(): {fe.Message}");
                    continue;
                }
            }

            // Log and return
            Log.Info($"Loaded {zonesAdded} zones with {totalLocations} locations into memory'");

            loaded = zonesAdded;
            return zones.ToArray();
        }

        /// <summary>
        /// Gets a <see cref="WorldZone"/> for a zone by name
        /// </summary>
        /// <param name="name">The short name (or ingame name) of the zone as written in the Locations.xml</param>
        /// <returns>return a <see cref="WorldZone"/>, or null if the zone has not been loaded yet</returns>
        public static WorldZone GetZoneByName(string name)
        {
            // Ensure zone exists
            if (ZoneCache.TryGetValue(name, out WorldZone zone))
            {
                return zone;
            }

            return null;
        }

        /// <summary>
        /// Adds a region
        /// </summary>
        /// <param name="name">The name of the region</param>
        /// <param name="zones">A list of zone names contained within this region</param>
        internal static void AddRegion(string name, List<string> zones)
        {
            RegionZones.Add(name, zones);
        }

        /// <summary>
        /// Gets a list of regions
        /// </summary>
        /// <returns>an array of region names found in the LSPDFR regions.xml file</returns>
        public static string[] GetRegions()
        {
            return RegionZones.Keys.ToArray();
        }

        /// <summary>
        /// Gets a list of zone names by the region name
        /// </summary>
        /// <param name="region">Name of the region, located in the LSPDFR regions.xml file</param>
        /// <returns>an array of zone names on success, otherwise null</returns>
        public static string[] GetZoneNamesByRegion(string region)
        {
            if (!RegionZones.ContainsKey(region))
            {
                return null;
            }

            return RegionZones[region].ToArray();
        }
    }
}
