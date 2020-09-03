using AgencyCalloutsPlus.Extensions;
using AgencyCalloutsPlus.Mod;
using Rage;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;

namespace AgencyCalloutsPlus.API
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
        /// Contains a dictionary of how often specific crimes happen in this zone
        /// </summary>
        //public IReadOnlyDictionary<CalloutType, WorldStateMultipliers> CrimeTypeProbabilities { get; protected set; }

        /// <summary>
        /// Contains a dictionary of the average number of calls per time of day in this zone
        /// </summary>
        public IReadOnlyDictionary<TimeOfDay, int> AverageCalls { get; protected set; }

        /// <summary>
        /// Containts a list <see cref="ResidenceLocation"/>(s) in this zone
        /// </summary>
        public ResidenceLocation[] Residences { get; protected set; }

        /// <summary>
        /// Containts an array of SideOfRoad locations
        /// </summary>
        public SpawnPoint[] SideOfRoadLocations { get; protected set; }

        /// <summary>
        /// Gets the crime level probability of this zone
        /// </summary>
        public int Probability => AverageCalls[GameWorld.CurrentTimeOfDay];

        /// <summary>
        /// Spawns a <see cref="CalloutType"/> based on the <see cref="CrimeTypeProbabilities"/> probabilites set
        /// </summary>
        private WorldStateProbabilityGenerator<CalloutType> CrimeGenerator { get; set; }

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
            //var crimeTypeProbabilities = new Dictionary<CalloutType, WorldStateMultipliers>(10);
            CrimeGenerator = new WorldStateProbabilityGenerator<CalloutType>();

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
                    CrimeGenerator.Add(calloutType, multipliers);

                    // Add
                    //crimeTypeProbabilities.Add(calloutType, multipliers);
                }
            }

            // Extract locations
            node = node.SelectSingleNode("Locations");
            if (node == null || !node.HasChildNodes)
            {
                throw new ArgumentNullException("Locations");
            }

            // Load Side Of Road locations
            catagoryNode = node.SelectSingleNode("SideOfRoad");
            SideOfRoadLocations = ExtractSpawnPoints(LocationType.SideOfRoad, catagoryNode);

            // Load Home locations
            catagoryNode = node.SelectSingleNode("Residences");
            Residences = ExtractHomes(catagoryNode);

            // Internal vars
            //CrimeTypeProbabilities = new ReadOnlyDictionary<CalloutType, WorldStateMultipliers>(crimeTypeProbabilities);
        }

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
                item[TimeOfDay.Morning] = 0;
            }
            item[TimeOfDay.Morning] = m;

            // Extract and parse morning value
            if (!Int32.TryParse(subNode.Attributes["day"]?.Value, out m))
            {
                Log.Error($"ZoneInfo.ctor [{subNode.GetFullPath()}]: Unable to extract 'day' attribute on XmlNode");
                item[TimeOfDay.Day] = 0;
            }
            item[TimeOfDay.Day] = m;

            // Extract and parse morning value
            if (!Int32.TryParse(subNode.Attributes["evening"]?.Value, out m))
            {
                Log.Error($"ZoneInfo.ctor [{subNode.GetFullPath()}]: Unable to extract 'evening' attribute on XmlNode");
                item[TimeOfDay.Evening] = 0;
            }
            item[TimeOfDay.Evening] = m;

            // Extract and parse morning value
            if (!Int32.TryParse(subNode.Attributes["night"]?.Value, out m))
            {
                Log.Error($"ZoneInfo.ctor [{subNode.GetFullPath()}]: Unable to extract 'night' attribute on XmlNode");
                item[TimeOfDay.Night] = 0;
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
            if (SideOfRoadLocations != null)
                count += SideOfRoadLocations.Length;

            count += Residences.Length;

            return count;
        }

        /// <summary>
        /// Spawns the next <see cref="CalloutType"/> that will happen in zone,
        /// based on the crime probabilities set
        /// </summary>
        /// <returns>
        /// returns the next callout type on success. On failure, <see cref="CalloutType.Traffic"/>
        /// will always be returned
        /// </returns>
        public CalloutType GetNextRandomCrimeType()
        {
            if (CrimeGenerator.TrySpawn(out CalloutType calloutType))
            {
                return calloutType;
            }

            return CalloutType.Traffic;
        }

        /// <summary>
        /// Gets a random <see cref="WorldLocation"/>
        /// </summary>
        /// <param name="type"></param>
        /// <param name="pool"></param>
        /// <param name="inactiveOnly">If true, will only return a <see cref="WorldLocation"/> that is not currently in use</param>
        /// <returns></returns>
        internal WorldLocation GetRandomLocation(LocationType type, WorldLocation[] pool, bool inactiveOnly)
        {
            // If we have no locations, return null
            if (pool.Length == 0)
            {
                Log.Debug($"ZoneInfo.GetRandomLocation(): Unable to pull a {type} from zone '{ScriptName}' because the list is empty");
                return null;
            }

            // Load randomizer
            var random = new CryptoRandom();

            // Will any location work?
            if (!inactiveOnly) return random.PickOne(pool);

            // Find all locations not in use
            var active = Dispatch.GetActiveCrimeLocationsByType(type);
            var available = pool.Except(active).ToArray();

            // If no locations are available
            if (available.Length == 0)
            {
                Log.Debug($"ZoneInfo.GetRandomLocation(): Unable to pull an available '{type}' location from zone '{ScriptName}' because the list is empty");
                return null;
            }

            // We are good to go!
            return random.PickOne(available);
        }

        /// <summary>
        /// Gets a random Side of the Road location in this zone
        /// </summary>
        /// <param name="inactiveOnly">If true, will only return a <see cref="WorldLocation"/> that is not currently in use</param>
        /// <returns>returns a random <see cref="SpawnPoint"/> on success, or null on failure</returns>
        public SpawnPoint GetRandomSideOfRoadLocation(bool inactiveOnly = false)
        {
            // Get random location
            return GetRandomLocation(LocationType.SideOfRoad, SideOfRoadLocations, inactiveOnly) as SpawnPoint;
        }

        /// <summary>
        /// Gets a random <see cref="ResidenceLocation"/> in this zone
        /// </summary>
        /// <param name="inactiveOnly">If true, will only return a <see cref="WorldLocation"/> that is not currently in use</param>
        /// <returns>returns a random <see cref="ResidenceLocation"/> on success, or null on failure</returns>
        public ResidenceLocation GetRandomResidence(bool inactiveOnly = false)
        {
            // Get random location
            return GetRandomLocation(LocationType.Residence, Residences, inactiveOnly) as ResidenceLocation;
        }

        /// <summary>
        /// Extracts the home locations from the [zoneName].xml
        /// </summary>
        /// <param name="catagoryNode"></param>
        /// <returns></returns>
        private ResidenceLocation[] ExtractHomes(XmlNode catagoryNode)
        {
            // Ensure we have a proper node
            if (catagoryNode == null || !catagoryNode.HasChildNodes)
            {
                Log.Warning($"ZoneInfo.ExtractHomes(): Residences XmlNode is null or has no child nodes for '{ScriptName}'");
                return new ResidenceLocation[0];
            }

            // Create a new list to return
            var nodes = catagoryNode.SelectNodes("Residence");
            var homes = new List<ResidenceLocation>(nodes.Count);
            foreach (XmlNode homeNode in nodes)
            {
                Vector3 vector;

                // Ensure we have attributes
                if (homeNode.Attributes == null)
                {
                    Log.Warning($"ZoneInfo.ExtractHomes(): Home item has no attributes");
                    continue;
                }

                // Try and extract probability value
                if (String.IsNullOrWhiteSpace(homeNode.Attributes["position"]?.Value))
                {
                    Log.Warning($"ZoneInfo.ExtractHomes(): Unable to extract home position attribute value in zone '{ScriptName}'");
                    continue;
                }
                else // Parse Vector3
                {
                    string[] parts = homeNode.Attributes["position"].Value.Split(';');
                    if (parts.Length != 2)
                    {
                        Log.Warning($"ZoneInfo.ExtractHomes(): Home position attribute value is not formatted properly in zone '{ScriptName}'");
                        continue;
                    }

                    if (!Vector3Extensions.TryParse(parts[0], out vector))
                    {
                        Log.Warning($"ZoneInfo.ExtractHomes(): Unable to parse home position attribute value in zone '{ScriptName}'");
                        continue;
                    }
                }

                // Create home
                var home = new ResidenceLocation(this, vector);

                // Extract nodes
                try
                {
                    home.Address = homeNode.SelectSingleNode("Address")?.InnerText ?? throw new ArgumentException("Address");
                    if (int.TryParse(homeNode.SelectSingleNode("Postal")?.InnerText, out int postal))
                    {
                        home.Postal = postal;
                    }

                    // Try and parse social class
                    string val = homeNode.SelectSingleNode("Class")?.InnerText;
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
                    home.Type = sType;

                    // Try and parse type
                    val = homeNode.SelectSingleNode("Flags")?.InnerText;
                    if (!String.IsNullOrEmpty(val))
                    {
                        AddLocationFlagsFromCSV(val, home);
                    }
                }
                catch (ArgumentException e)
                {
                    Log.Warning($"ZoneInfo.ExtractHomes(): Unable to extract/parse home value {e.ParamName} in zone '{ScriptName}'");
                    continue;
                }

                // Parse spawn points!
                XmlNode pointsNode = homeNode.SelectSingleNode("Points");
                if (pointsNode == null || !pointsNode.HasChildNodes)
                {
                    Log.Warning($"ZoneInfo.ExtractHomes(): Unable to extract home SpawnPoints in zone '{ScriptName}'");
                    continue;
                }

                // Parse spawn points
                foreach (XmlNode sp in pointsNode.SelectNodes("SpawnPoint"))
                {
                    var item = ParseSpawnPoint(LocationType.Residence, sp);
                    if (item == null)
                        continue;

                    // Try and extract typ value
                    if (sp.Attributes["id"]?.Value == null || !Enum.TryParse(sp.Attributes["id"].Value, out HomeSpawnId s))
                    {
                        Log.Warning($"ZoneInfo.ParseSpawnPoint(): Unable to extract SpawnPoint id value for '{ScriptName}->Homes->Home->Points'");
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
        /// Extracts and add the <see cref="LocationFlags"/> from a comma seperated string
        /// to a <see cref="WorldLocation"/>
        /// </summary>
        /// <param name="csv">Comma seperated values</param>
        /// <param name="location"></param>
        private void AddLocationFlagsFromCSV(string csv, WorldLocation location)
        {
            if (!String.IsNullOrWhiteSpace(csv))
            {
                string[] vals = csv.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string val in vals)
                {
                    if (Enum.TryParse(val, out LocationFlags flag))
                    {
                        location.Flags |= flag;
                    }
                }
            }
        }

        /// <summary>
        /// Extracts all SpawnPoint xml nodes from a parent node
        /// </summary>
        /// <param name="type"></param>
        /// <param name="catagoryNode"></param>
        /// <returns></returns>
        private SpawnPoint[] ExtractSpawnPoints(LocationType type, XmlNode catagoryNode)
        {
            if (catagoryNode != null && catagoryNode.HasChildNodes)
            {
                var spawnPoints = new List<SpawnPoint>(catagoryNode.ChildNodes.Count);
                foreach (XmlNode spawn in catagoryNode.SelectNodes("SpawnPoint"))
                {
                    var item = ParseSpawnPoint(type, spawn);
                    if (item == null)
                        continue;

                    spawnPoints.Add(item);
                }

                return spawnPoints.ToArray();
            }

            return new SpawnPoint[0];
        }

        /// <summary>
        /// Parses SpawnPoint xml nodes into a <see cref="SpawnPoint"/>
        /// </summary>
        private SpawnPoint ParseSpawnPoint(LocationType type, XmlNode n)
        {
            // Ensure we have attributes
            if (n.Attributes == null)
            {
                Log.Warning($"ZoneInfo.ParseSpawnPoint(): SpawnPoint item has no attributes in '{ScriptName}->{type}'");
                return null;
            }

            // Extract attributes
            float x, y, z, heading = 0f;

            // Try and extract X value
            if (n.Attributes["X"]?.Value == null || !float.TryParse(n.Attributes["X"].Value, out x))
            {
                Log.Warning($"ZoneInfo.ParseSpawnPoint(): Unable to extract SpawnPoint X value for '{ScriptName}->{type}->Location'");
                return null;
            }

            // Try and extract Y value
            if (n.Attributes["Y"]?.Value == null || !float.TryParse(n.Attributes["Y"].Value, out y))
            {
                Log.Warning($"ZoneInfo.ParseSpawnPoint(): Unable to extract SpawnPoint Y value for '{ScriptName}->{type}->Location'");
                return null;
            }

            // Try and extract Z value
            if (n.Attributes["Z"]?.Value == null || !float.TryParse(n.Attributes["Z"].Value, out z))
            {
                Log.Warning($"ZoneInfo.ParseSpawnPoint(): Unable to extract SpawnPoint Z value for '{ScriptName}->{type}->Location'");
                return null;
            }

            // Try and extract heading value
            if (n.Attributes["heading"]?.Value != null && !float.TryParse(n.Attributes["heading"].Value, out heading))
            {
                Log.Warning($"ZoneInfo.ParseSpawnPoint(): Unable to extract SpawnPoint heading value for '{ScriptName}->{type}->Location'");
                return null;
            }

            // Create the Vector3
            Vector3 vector = new Vector3(x, y, z);
            var sp = new SpawnPoint(vector, heading);

            // Try and extract spawn point flags
            if (n.Attributes["flags"]?.Value != null)
                AddLocationFlagsFromCSV(n.Attributes["flags"].Value, sp);

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
                string path = Path.Combine(Main.PluginFolderPath, "Locations", $"{zoneName}.xml");
                XmlDocument document = new XmlDocument();
                using (var file = new FileStream(path, FileMode.Open))
                {
                    document.Load(file);
                }

                // Grab zone node
                XmlNode root = document.DocumentElement;
                if (root == null)
                {
                    Log.Error($"ZoneInfo.ParseSpawnPoint(): Missing location data for zone '{zoneName}'");
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
                    Log.Error($"ZoneInfo.ParseSpawnPoint(): Unable to load location data for zone '{zoneName}'. Missing node '{e.ParamName}'");
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
