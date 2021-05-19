using AgencyDispatchFramework.Dispatching;
using AgencyDispatchFramework.Extensions;
using AgencyDispatchFramework.Game.Locations;
using Rage;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;

namespace AgencyDispatchFramework.Game
{
    /// <summary>
    /// This class contains a series of spawnable locations within a specific zone
    /// </summary>
    public class ZoneInfo : ISpawnable
    {
        /// <summary>
        /// Contains a hash table of zones
        /// </summary>
        /// <remarks>[ ZoneScriptName => ZoneInfo class ]</remarks>
        private static Dictionary<string, ZoneInfo> ZoneCache { get; set; }

        /// <summary>
        /// Containts a hash table of regions, and thier zones
        /// </summary>
        private static Dictionary<string, List<string>> RegionZones { get; set; } = new Dictionary<string, List<string>>(16);

        /// <summary>
        /// Gets the Zone name
        /// </summary>
        public string ScriptName { get; protected set; }

        /// <summary>
        /// Gets the Zone name
        /// </summary>
        public string FullName { get; protected set; }

        /// <summary>
        /// Gets the population density of the zone
        /// </summary>
        public Population Population { get; protected set; }

        /// <summary>
        /// Gets the zone size
        /// </summary>
        public ZoneSize Size { get; protected set; }

        /// <summary>
        /// Gets the primary zone type for this zone
        /// </summary>
        public ZoneType ZoneType { get; protected set; }

        /// <summary>
        /// Gets the social class of this zones citizens
        /// </summary>
        public SocialClass SocialClass { get; protected set; }

        /// <summary>
        /// Contains a dictionary of the average number of calls per time of day in this zone
        /// </summary>
        public IReadOnlyDictionary<TimeOfDay, int> AverageCalls { get; protected set; }

        /// <summary>
        /// Containts a list <see cref="Residence"/>(s) in this zone
        /// </summary>
        public Residence[] Residences { get; protected set; }

        /// <summary>
        /// Containts an array of Road Shoulder locations
        /// </summary>
        public RoadShoulder[] RoadShoulders { get; protected set; }

        /// <summary>
        /// Gets the crime level probability of this zone based on current time of day
        /// </summary>
        public int Probability => AverageCalls[GameWorld.CurrentTimeOfDay];

        /// <summary>
        /// Spawns a <see cref="CalloutType"/> based on the <see cref="WorldStateMultipliers"/> probabilites set
        /// </summary>
        private WorldStateProbabilityGenerator<CalloutType> CrimeTypeGenerator { get; set; }

        /// <summary>
        /// Creates a new instance of <see cref="ZoneInfo"/>
        /// </summary>
        /// <param name="node">The XML node for this zone from the Locations.xml</param>
        public ZoneInfo(XmlNode node)
        {
            // Load zone info
            XmlNode catagoryNode = node.SelectSingleNode("Name");
            FullName = catagoryNode?.InnerText ?? throw new ArgumentNullException("Name");
            ScriptName = node.Name;

            // Extract size
            catagoryNode = node.SelectSingleNode("Size");
            if (String.IsNullOrWhiteSpace(catagoryNode?.InnerText) || !Enum.TryParse(catagoryNode.InnerText, out ZoneSize size))
            {
                throw new ArgumentNullException("Size");
            }
            Size = size;

            // Extract type
            catagoryNode = node.SelectSingleNode("Type");
            if (String.IsNullOrWhiteSpace(catagoryNode?.InnerText) || !Enum.TryParse(catagoryNode.InnerText, out ZoneType type))
            {
                throw new ArgumentNullException("Type");
            }
            ZoneType = type;

            // Extract social class
            catagoryNode = node.SelectSingleNode("Class");
            if (String.IsNullOrWhiteSpace(catagoryNode?.InnerText) || !Enum.TryParse(catagoryNode.InnerText, out SocialClass sclass))
            {
                throw new ArgumentNullException("Class");
            }
            SocialClass = sclass;

            // Extract population
            catagoryNode = node.SelectSingleNode("Population");
            if (String.IsNullOrWhiteSpace(catagoryNode?.InnerText) || !Enum.TryParse(catagoryNode.InnerText, out Population pop))
            {
                throw new ArgumentNullException("Population");
            }
            Population = pop;

            // Extract crime level
            catagoryNode = node.SelectSingleNode("Crime");
            if (catagoryNode == null || !catagoryNode.HasChildNodes)
            {
                throw new ArgumentNullException("Crime");
            }

            // Load crime probabilites
            CrimeTypeGenerator = new WorldStateProbabilityGenerator<CalloutType>();

            // Get average calls by time of day
            var subNode = catagoryNode.SelectSingleNode("AverageCalls");
            AverageCalls = GetTimeOfDayProbabilities(subNode);
            int maxCalls = AverageCalls.Values.Sum();

            // Does this zone get any calls?
            if (maxCalls > 0)
            {
                catagoryNode = catagoryNode.SelectSingleNode("Probabilities");
                if (catagoryNode == null || !catagoryNode.HasChildNodes)
                {
                    throw new ArgumentNullException("Probabilities");
                }

                // Extract crime probabilites
                foreach (CalloutType calloutType in Enum.GetValues(typeof(CalloutType)))
                {
                    var nodeName = Enum.GetName(typeof(CalloutType), calloutType);
                    XmlNode n = catagoryNode.SelectSingleNode(nodeName);

                    // Try and parse the crime type from the node name
                    if (n == null)
                    {
                        Log.Warning($"ZoneInfo.ctor: Missing CrimeType '{nodeName}' in zone '{ScriptName}'");
                        continue;
                    }

                    // See if this calloutType is possible in this zone
                    if (bool.TryParse(n.Attributes["possible"]?.Value, out bool possible))
                    {
                        if (!possible) continue;
                    }

                    // Try and parse the probability levels by Time of Day
                    var multipliers = XmlExtractor.GetWorldStateMultipliers(n);
                    CrimeTypeGenerator.Add(calloutType, multipliers);
                }
            }

            // Extract locations
            node = node.SelectSingleNode("Locations");
            if (node == null || !node.HasChildNodes)
            {
                throw new ArgumentNullException("Locations");
            }

            // Load Side Of Road locations
            catagoryNode = node.SelectSingleNode("RoadShoulders");
            RoadShoulders = ExtractRoadLocations(catagoryNode);

            // Load Home locations
            catagoryNode = node.SelectSingleNode("Residences");
            Residences = ExtractHomes(catagoryNode);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="subNode"></param>
        /// <returns></returns>
        private Dictionary<TimeOfDay, int> GetTimeOfDayProbabilities(XmlNode subNode)
        {
            // If attributes is null, we know... we know...
            if (subNode?.Attributes == null)
            {
                throw new ArgumentNullException(subNode.Name);
            }

            // Create our dictionary
            var item = new Dictionary<TimeOfDay, int>(4)
            {
                { TimeOfDay.Morning, 0 },
                { TimeOfDay.Day, 0 },
                { TimeOfDay.Evening, 0 },
                { TimeOfDay.Night, 0 }
            };

            // Extract and parse morning value
            if (!Int32.TryParse(subNode.Attributes["morning"]?.Value, out int m))
            {
                Log.Error($"ZoneInfo.ctor [{subNode.GetFullPath()}]: Unable to extract 'morning' attribute on XmlNode");
            }
            item[TimeOfDay.Morning] = m;

            // Extract and parse morning value
            if (!Int32.TryParse(subNode.Attributes["day"]?.Value, out m))
            {
                Log.Error($"ZoneInfo.ctor [{subNode.GetFullPath()}]: Unable to extract 'day' attribute on XmlNode");
            }
            item[TimeOfDay.Day] = m;

            // Extract and parse morning value
            if (!Int32.TryParse(subNode.Attributes["evening"]?.Value, out m))
            {
                Log.Error($"ZoneInfo.ctor [{subNode.GetFullPath()}]: Unable to extract 'evening' attribute on XmlNode");
            }
            item[TimeOfDay.Evening] = m;

            // Extract and parse morning value
            if (!Int32.TryParse(subNode.Attributes["night"]?.Value, out m))
            {
                Log.Error($"ZoneInfo.ctor [{subNode.GetFullPath()}]: Unable to extract 'night' attribute on XmlNode");
            }
            item[TimeOfDay.Night] = m;

            return item;
        }

        /// <summary>
        /// Gets the total number of Locations in this collection, regardless of type
        /// </summary>
        /// <returns></returns>
        public int GetTotalNumberOfLocations()
        {
            var count = 0;

            // Do we have SideOfRoad locations? If so, add em
            if (RoadShoulders != null)
                count += RoadShoulders.Length;

            // Final count
            count += Residences.Length;

            return count;
        }

        /// <summary>
        /// Spawns the next <see cref="CalloutType"/> that will happen in this zone
        /// based on the crime probabilities set
        /// </summary>
        /// <returns>
        /// returns the next callout type on success. On failure, <see cref="CalloutType.Traffic"/>
        /// will always be returned
        /// </returns>
        public CalloutType GetNextRandomCrimeType()
        {
            if (CrimeTypeGenerator.TrySpawn(out CalloutType calloutType))
            {
                return calloutType;
            }

            return CalloutType.Traffic;
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
                Log.Debug($"ZoneInfo.GetRandomLocationFromPool<T>(): Unable to pull a {typeof(T).Name} from zone '{ScriptName}' because no locations were provided in the list");
                return null;
            }

            var type = locationPool[0].LocationType;

            // Filtering by flags? Do this first so we can log debugging info if there are no available locations with these required flags in this zone
            if (filters != null && filters.Requirements.Count > 0)
            {
                locationPool = locationPool.Filter(filters).ToArray();
                if (locationPool.Length == 0)
                {
                    Log.Warning($"ZoneInfo.GetRandomLocationFromPool<T>(): There are no locations of type '{type}' in zone '{ScriptName}' using the following flags:");
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
                    var active = Dispatch.GetActiveCrimeLocationsByType<T>(type);
                    locationPool = locationPool.Except(active).ToArray();
                }
                catch (InvalidCastException ex)
                {
                    Log.Error($"ZoneInfo.GetRandomLocationFromPool<T>(): Cast exception to {typeof(T).Name} from location pool. Logging exception data");
                    Log.Exception(ex);
                    return null;
                }
            }

            // If no locations are available
            if (locationPool.Length == 0)
            {
                Log.Debug($"ZoneInfo.GetRandomLocationFromPool<T>(): Unable to pull an available '{type}' location from zone '{ScriptName}' because they are all in use");
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
        public RoadShoulder GetRandomSideOfRoadLocation(FlagFilterGroup filters, bool inactiveOnly = false)
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
        /// Extracts the home locations from the [zoneName].xml
        /// </summary>
        /// <param name="catagoryNode"></param>
        /// <returns></returns>
        private Residence[] ExtractHomes(XmlNode catagoryNode)
        {
            // Ensure we have a proper node
            if (catagoryNode == null || !catagoryNode.HasChildNodes)
            {
                Log.Warning($"ZoneInfo.ExtractHomes(): Residences XmlNode is null or has no child nodes for '{ScriptName}'");
                return new Residence[0];
            }

            // Create a new list to return
            var nodes = catagoryNode.SelectNodes("Residence");
            var homes = new List<Residence>(nodes.Count);
            foreach (XmlNode homeNode in nodes)
            {
                // Ensure we have attributes
                if (homeNode.Attributes == null)
                {
                    Log.Warning($"ZoneInfo.ExtractHomes(): Residence item has no attributes");
                    continue;
                }

                // Try and extract probability value
                if (!Vector3Extensions.TryParse(homeNode.Attributes["coordinates"]?.Value, out Vector3 vector))
                {
                    Log.Warning($"ZoneInfo.ExtractHomes(): Unable to parse Residence[coordinates] attribute value in zone '{ScriptName}'");
                    continue;
                }

                // Create home
                var home = new Residence(this, vector);

                // Extract nodes
                try
                {
                    home.StreetName = homeNode.SelectSingleNode("Street")?.InnerText ?? throw new ArgumentException("Street");
                    if (int.TryParse(homeNode.SelectSingleNode("Postal")?.InnerText, out int postal))
                    {
                        home.Postal = postal;
                    }

                    // See if there is a building number
                    string val = homeNode.SelectSingleNode("Number")?.InnerText;
                    if (!String.IsNullOrEmpty(val))
                    {
                        home.BuildingNumber = val;
                    }

                    // See if there is a building number
                    val = homeNode.SelectSingleNode("Unit")?.InnerText;
                    if (!String.IsNullOrEmpty(val))
                    {
                        home.UnitId = val;
                    }

                    // Try and parse social class
                    val = homeNode.SelectSingleNode("Class")?.InnerText;
                    if (String.IsNullOrEmpty(val) || !Enum.TryParse(val, out SocialClass sClass))
                    {
                        throw new ArgumentException("Class");
                    }
                    home.Class = sClass;

                    // Try and parse type
                    val = homeNode.SelectSingleNode("Type")?.InnerText;
                    if (String.IsNullOrEmpty(val) || !Enum.TryParse(val, out ResidenceType sType))
                    {
                        throw new ArgumentException("Type");
                    }
                    home.BuildingType = sType;

                    // Try and parse type
                    val = homeNode.SelectSingleNode("Flags")?.InnerText;
                    if (!String.IsNullOrEmpty(val))
                    {
                        home.ResidenceFlags = val.CSVToEnumArray<ResidenceFlags>();
                        home.Flags = home.ResidenceFlags.Cast<int>().ToArray();
                    }
                }
                catch (ArgumentException e)
                {
                    Log.Warning($"ZoneInfo.ExtractHomes(): Unable to extract/parse Residence value {e.ParamName} in zone '{ScriptName}'");
                    continue;
                }

                // Parse spawn points!
                XmlNode pointsNode = homeNode.SelectSingleNode("Positions");
                if (pointsNode == null || !pointsNode.HasChildNodes)
                {
                    Log.Warning($"ZoneInfo.ExtractHomes(): Unable to extract Residence->Positions in zone '{ScriptName}'");
                    continue;
                }

                // Parse spawn points
                foreach (XmlNode sp in pointsNode.SelectNodes("SpawnPoint"))
                {
                    var item = ParseSpawnPoint(LocationTypeCode.Residence, sp);
                    if (item == null)
                        continue;

                    // Try and extract typ value
                    if (sp.Attributes["id"]?.Value == null || !Enum.TryParse(sp.Attributes["id"].Value, out ResidencePosition s))
                    {
                        Log.Warning($"ZoneInfo.ParseSpawnPoint(): Unable to extract SpawnPoint id value for '{ScriptName}->Residences->Residence->Positions'");
                        break;
                    }

                    home.SpawnPoints[s] = item;
                }

                // Not ok?
                if (!home.IsValid())
                {
                    Log.Warning($"ZoneInfo.ExtractHomes(): Home node is missing some SpawnPoints in zone '{ScriptName}'");
                    continue;
                }

                // Add home to collection
                homes.Add(home);
            }

            // Did we extract anything?
            if (homes.Count == 0)
            {
                Log.Warning($"ZoneInfo.ExtractHomes(): No residences to extract in '{ScriptName}'");
            }

            return homes.ToArray();
        }

        /// <summary>
        /// Extracts all SpawnPoint xml nodes from a parent node
        /// </summary>
        /// <param name="type"></param>
        /// <param name="catagoryNode"></param>
        /// <returns></returns>
        private RoadShoulder[] ExtractRoadLocations(XmlNode catagoryNode)
        {
            if (catagoryNode != null && catagoryNode.HasChildNodes)
            {
                // Extract attributes
                var locations = catagoryNode.SelectNodes("Location");
                var shoulders = new List<RoadShoulder>(locations.Count);
                foreach (XmlNode n in locations)
                {
                    // Ensure we have attributes
                    if (n.Attributes == null)
                    {
                        Log.Warning($"ZoneInfo.ExtractRoadLocations(): Location item has no attributes in '{n.GetFullPath()}'");
                        continue;
                    }

                    // Try and extract coordinates
                    if (!Vector3Extensions.TryParse(n.Attributes["coordinates"]?.Value, out Vector3 vector))
                    {
                        Log.Warning($"ZoneInfo.ExtractRoadLocations(): Unable to parse Location[coordinates] attribute value in zone '{ScriptName}'");
                        continue;
                    }

                    // Try and extract heading value
                    if (!n.TryGetAttribute("heading", out float heading))
                    {
                        Log.Warning($"ZoneInfo.ExtractRoadLocations(): Unable to extract Location[heading] value for '{n.GetFullPath()}'");
                        continue;
                    }

                    // Create instance
                    var sp = new RoadShoulder(this, vector, heading);

                    // Extract street name
                    var val = n.SelectSingleNode("Street")?.InnerText;
                    if (String.IsNullOrWhiteSpace(val))
                    {
                        Log.Warning($"ZoneInfo.ExtractRoadLocations(): Unable to extract Street value for '{n.GetFullPath()}'");
                        continue;
                    }
                    sp.StreetName = val;

                    // Try and extract heading value
                    if (!Int32.TryParse(n.SelectSingleNode("Postal")?.InnerText, out int postal))
                    {
                        Log.Warning($"ZoneInfo.ExtractRoadLocations(): Unable to extract Postal value for '{n.GetFullPath()}'");
                        continue;
                    }
                    sp.Postal = postal;

                    // Extract street name
                    val = n.SelectSingleNode("Hint")?.InnerText;
                    if (!String.IsNullOrWhiteSpace(val))
                    {
                        sp.Hint = val;
                    }

                    // Try and extract spawn point flags
                    var subNode = n.SelectSingleNode("Flags");
                    if (subNode == null || !subNode.HasChildNodes)
                    {
                        Log.Warning($"ZoneInfo.ExtractRoadLocations(): Unable to extract Flags for '{n.GetFullPath()}'");
                        continue;
                    }

                    // Extract RoadFlags
                    val = subNode.SelectSingleNode("Road")?.InnerText;
                    if (String.IsNullOrWhiteSpace(val))
                    {
                        Log.Warning($"ZoneInfo.ExtractRoadLocations(): Unable to extract Road value for '{subNode.GetFullPath()}'");
                        continue;
                    }
                    sp.RoadFlags = val.CSVToEnumArray<RoadFlags>();
                    sp.Flags = sp.RoadFlags.Cast<int>().ToArray();

                    // Extract IntersectionFlags
                    sp.BeforeIntersectionFlags = ParseIntersectionFlags(subNode.SelectSingleNode("BeforeIntersection"), out RelativeDirection bdir);
                    sp.AfterIntersectionFlags = ParseIntersectionFlags(subNode.SelectSingleNode("AfterIntersection"), out RelativeDirection adir);
                    sp.BeforeIntersectionDirection = bdir;
                    sp.AfterIntersectionDirection = adir;

                    // Add spawnpoint to list
                    shoulders.Add(sp);
                }

                return shoulders.ToArray();
            }

            return new RoadShoulder[0];
        }

        /// <summary>
        /// Reads and parses an <see cref="XmlNode"/> containing <see cref="IntersectionFlags"/>
        /// </summary>
        /// <param name="node"></param>
        /// <param name="dir"></param>
        /// <returns></returns>
        private static IntersectionFlags[] ParseIntersectionFlags(XmlNode node, out RelativeDirection dir)
        {
            // Default return value
            dir = RelativeDirection.None;
            string val = node?.InnerText;

            // Check for empty strings
            if (String.IsNullOrWhiteSpace(val))
            {
                return new IntersectionFlags[0];
            }

            // Do we have a direction?
            if (node.HasAttribute("direction"))
            {
                Enum.TryParse(node.GetAttribute("direction"), out dir);
            }

            // Parse comma seperated values
            return val.CSVToEnumArray<IntersectionFlags>();
        }

        /// <summary>
        /// Parses SpawnPoint xml nodes into a <see cref="SpawnPoint"/>
        /// </summary>
        private SpawnPoint ParseSpawnPoint(LocationTypeCode type, XmlNode n)
        {
            // Ensure we have attributes
            if (n.Attributes == null)
            {
                Log.Warning($"ZoneInfo.ParseSpawnPoint(): SpawnPoint item has no attributes in '{n.GetFullPath()}'");
                return null;
            }

            // Try and extract coordinates
            if (!Vector3Extensions.TryParse(n.Attributes["coordinates"]?.Value, out Vector3 vector))
            {
                Log.Warning($"ZoneInfo.ParseSpawnPoint(): Unable to parse SpawnPoint[coordinates] attribute value in zone '{ScriptName}'");
                return null;
            }

            // Try and extract heading value
            if (!float.TryParse(n.Attributes["heading"]?.Value, out float heading))
            {
                Log.Warning($"ZoneInfo.ParseSpawnPoint(): Unable to extract SpawnPoint heading value for '{n.GetFullPath()}'");
                return null;
            }

            // Create the Vector3
            var sp = new SpawnPoint(vector, heading);
            return sp;
        }

        /// <summary>
        /// Loads the specified zones and of thier position data from the Locations.xml into
        /// memory, and returns the number of locations added.
        /// </summary>
        /// <param name="names">An array of zones to load (should be all uppercase)</param>
        /// <returns>returns the number of locations loaded</returns>
        public static int LoadZones(string[] names)
        {
            // Create instance of not already!
            if (ZoneCache == null)
            {
                ZoneCache = new Dictionary<string, ZoneInfo>();
            }

            int itemsAdded = 0;
            int zonesAdded = 0;

            // Cycle through each child node (Zone)
            foreach (string zoneName in names)
            {
                // If we have loaded this zone already, skip it
                if (ZoneCache.ContainsKey(zoneName)) continue;

                // Load XML document
                string path = Path.Combine(Main.ThisPluginFolderPath, "Locations", $"{zoneName}.xml");
                XmlDocument document = new XmlDocument();
                using (var file = new FileStream(path, FileMode.Open))
                {
                    document.Load(file);
                }

                // Grab zone node
                XmlNode root = document.DocumentElement;
                if (root == null)
                {
                    Log.Error($"ZoneInfo.LoadZones(): Missing location data for zone '{zoneName}'");
                    continue;
                }

                // No data?
                if (!root.HasChildNodes) continue;

                // Create our spawn point collection and store it
                try
                {
                    var zone = new ZoneInfo(root);

                    // Save
                    ZoneCache.Add(zoneName, zone);
                    itemsAdded += zone.GetTotalNumberOfLocations();
                    zonesAdded++;
                }
                catch (ArgumentNullException e)
                {
                    Log.Error($"ZoneInfo.LoadZones(): Unable to load location data for zone '{zoneName}'. Missing node '{e.ParamName}'");
                    continue;
                }

                // Clean up
                document = null;
            }

            // Log and return
            Log.Info($"Loaded {zonesAdded} zones with {itemsAdded} locations into memory'");
            return itemsAdded;
        }

        /// <summary>
        /// Gets a <see cref="ZoneInfo"/> for a zone by name
        /// </summary>
        /// <param name="name">The short name (or ingame name) of the zone as written in the Locations.xml</param>
        /// <returns>return a <see cref="ZoneInfo"/>, or null if the zone has not been loaded yet</returns>
        public static ZoneInfo GetZoneByName(string name)
        {
            // Ensure zone exists
            if (ZoneCache.TryGetValue(name, out ZoneInfo locations))
            {
                return locations;
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
