using AgencyCalloutsPlus.Extensions;
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
        private static Dictionary<string, ZoneInfo> Zones { get; set; }

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
        public string FriendlyName { get; protected set; }

        /// <summary>
        /// Gets the population density of the zone
        /// </summary>
        public Population Population { get; protected set; }

        /// <summary>
        /// Gets the overall crime level of the zone
        /// </summary>
        public CrimeLevel CrimeLevel { get; protected set; }

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
        /// Gets the number of desired patrols for this zone
        /// </summary>
        public double IdealPatrolCount { get; protected set; }

        /// <summary>
        /// Contains a dictionary of how often specific cimes happen in this zone
        /// </summary>
        public Dictionary<CalloutType, int> Crimes { get; protected set; }

        /// <summary>
        /// Containts a list <see cref="HomeLocation"/>(s) in this zone
        /// </summary>
        public HomeLocation[] HomeLocations { get; protected set; }

        /// <summary>
        /// Containts an array of SideOfRoad locations
        /// </summary>
        public SpawnPoint[] SideOfRoadLocations { get; protected set; }

        /// <summary>
        /// Gets the crime level probability of this zone
        /// </summary>
        public int Probability => (int)CrimeLevel;

        /// <summary>
        /// Spawns a <see cref="CalloutType"/> based on the <see cref="Crimes"/> probabilites set
        /// </summary>
        private SpawnGenerator<SpawnableCalloutType> CrimeGenerator { get; set; }

        /// <summary>
        /// Creates a new instance of <see cref="ZoneInfo"/>
        /// </summary>
        /// <param name="node">The XML node for this zone from the Locations.xml</param>
        public ZoneInfo(XmlNode node)
        {
            // Load zone info
            XmlNode catagoryNode = node.SelectSingleNode("Name");
            FriendlyName = catagoryNode?.InnerText ?? throw new ArgumentNullException("Name");
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
            catagoryNode = node.SelectSingleNode("CrimeLevel");
            if (String.IsNullOrWhiteSpace(catagoryNode?.InnerText) || !Enum.TryParse(catagoryNode.InnerText, out CrimeLevel crime))
            {
                throw new ArgumentNullException("CrimeLevel");
            }
            CrimeLevel = crime;

            // Load crime probabilites
            Crimes = new Dictionary<CalloutType, int>(6);
            CrimeGenerator = new SpawnGenerator<SpawnableCalloutType>();
            if (crime != CrimeLevel.None)
            {
                catagoryNode = node.SelectSingleNode("Crimes");
                if (catagoryNode == null || !catagoryNode.HasChildNodes)
                {
                    throw new ArgumentNullException("Crimes");
                }

                // Extract crime probabilites
                foreach (XmlNode n in catagoryNode.ChildNodes)
                {
                    // Try and parse the crime type from the node name
                    if (!Enum.TryParse(n.Name, out CalloutType calloutType))
                    {
                        Log.Warning($"ZoneInfo.ctor: Unable to parse CrimeType {n.Name} in zone '{ScriptName}'");
                        continue;
                    }

                    // Try and parse the probability level
                    if (!int.TryParse(n.InnerText, out int level))
                    {
                        Log.Warning($"ZoneInfo.ctor: Unable to parse CrimeType probability for {n.Name} in zone '{ScriptName}'");
                        continue;
                    }

                    // Add
                    Crimes.Add(calloutType, level);
                    CrimeGenerator.Add(new SpawnableCalloutType(level, calloutType));
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
            catagoryNode = node.SelectSingleNode("Homes");
            HomeLocations = ExtractHomes(catagoryNode);

            // Internal vars
            IdealPatrolCount = GetOptimumPatrolCount(Size, Population, CrimeLevel);
            CrimeGenerator = new SpawnGenerator<SpawnableCalloutType>();
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
            if (CrimeGenerator.TrySpawn(out SpawnableCalloutType type))
            {
                return type.CalloutType;
            }

            return CalloutType.Traffic;
        }

        /// <summary>
        /// Gets a randome Side of the Road location in this zone
        /// </summary>
        /// <returns>returns a random <see cref="SpawnPoint"/> on success, or null on failure</returns>
        public SpawnPoint GetRandomSideOfRoadLocation()
        {
            // Load randomizer
            var rando = new CryptoRandom();

            // Get zones in this jurisdiction
            if (SideOfRoadLocations == null || SideOfRoadLocations.Length == 0)
            {
                return null;
            }

            // Get random location
            return SideOfRoadLocations[rando.Next(0, SideOfRoadLocations.Length - 1)];
        }

        /// <summary>
        /// Extracts the home locations from the [zoneName].xml
        /// </summary>
        /// <param name="catagoryNode"></param>
        /// <returns></returns>
        private HomeLocation[] ExtractHomes(XmlNode catagoryNode)
        {
            if (catagoryNode != null && catagoryNode.HasChildNodes)
            {
                var homes = new List<HomeLocation>(catagoryNode.ChildNodes.Count);
                foreach (XmlNode homeNode in catagoryNode.SelectNodes("Home"))
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
                    var home = new HomeLocation(this, vector);
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
                        if (String.IsNullOrEmpty(val) || !Enum.TryParse(val, out HomeType sType))
                        {
                            throw new ArgumentException("Type");
                        }
                        home.Type = sType;
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
                        var item = ParseSpawnPoint(LocationType.Homes, sp);
                        if (item == null)
                            continue;

                        // Try and extract typ value
                        if (sp.Attributes["id"]?.Value == null || !Enum.TryParse(sp.Attributes["id"].Value, out HomeSpawn s))
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

                return homes.ToArray();
            }

            return new HomeLocation[0];
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
            return new SpawnPoint(vector, heading);
        }

        /// <summary>
        /// Determines the optimal number of patrols this zone should have based
        /// on <see cref="ZoneSize"/>, <see cref="API.Population"/> and
        /// <see cref="API.CrimeLevel"/> of crimes
        /// </summary>
        /// <param name="Size"></param>
        /// <param name="Population"></param>
        /// <param name="CrimeLevel"></param>
        /// <returns></returns>
        private static double GetOptimumPatrolCount(ZoneSize Size, Population Population, CrimeLevel CrimeLevel)
        {
            double baseCount = 0;
            double modifier = 0;

            switch (Size)
            {
                case ZoneSize.VerySmall:
                    baseCount = 0.25;
                    break;
                case ZoneSize.Small:
                    baseCount = 0.50;
                    break;
                case ZoneSize.Medium:
                    baseCount = 1;
                    break;
                case ZoneSize.Large:
                    baseCount = 1.50;
                    break;
                case ZoneSize.VeryLarge:
                    baseCount = 2.25;
                    break;
                case ZoneSize.Massive:
                    baseCount = 3.50;
                    break;
            }

            switch (Population)
            {
                default: // None
                    return 0;
                case Population.Scarce:
                    // No adjustment
                    break;
                case Population.Moderate:
                    baseCount *= 1.5;
                    break;
                case Population.Dense:
                    baseCount *= 2;
                    break;
            }

            switch (CrimeLevel)
            {
                default: // None
                    modifier = 0.25;
                    break;
                case CrimeLevel.VeryLow:
                    modifier = 0.33;
                    break;
                case CrimeLevel.Low:
                    modifier = 0.66;
                    break;
                case CrimeLevel.Moderate:
                    modifier = 1;
                    break;
                case CrimeLevel.High:
                    modifier = 1.33;
                    break;
                case CrimeLevel.VeryHigh:
                    modifier = 1.75;
                    break;
            }

            return baseCount * modifier;
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
            if (Zones == null)
            {
                Zones = new Dictionary<string, ZoneInfo>();
            }

            int itemsAdded = 0;
            int zonesAdded = 0;

            // Cycle through each child node (Zone)
            foreach (string zoneName in names)
            {
                // If we have loaded this zone already, skip it
                if (Zones.ContainsKey(zoneName)) continue;

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
                    Zones.Add(zoneName, zone);
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
            if (Zones.TryGetValue(name, out ZoneInfo locations))
            {
                return locations;
            }

            return null;
        }

        internal static void AddRegion(string name, List<string> zones)
        {
            RegionZones.Add(name, zones);
        }

        public static string[] GetRegions()
        {
            return RegionZones.Keys.ToArray();
        }
    }
}
