using Rage;
using System;
using System.Collections.Generic;
using System.IO;
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
        public ProbabilityLevel CrimeLevel { get; protected set; }

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
            if (String.IsNullOrWhiteSpace(catagoryNode?.InnerText) || !Enum.TryParse(catagoryNode.InnerText, out ProbabilityLevel crime))
            {
                throw new ArgumentNullException("CrimeLevel");
            }
            CrimeLevel = crime;

            // Load crime probabilites
            Crimes = new Dictionary<CalloutType, int>(6);
            CrimeGenerator = new SpawnGenerator<SpawnableCalloutType>();
            if (crime != ProbabilityLevel.None)
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
                        Game.LogTrivial($"[WARN] AgencyCalloutsPlus: Unable to parse CrimeType {n.Name} in zone '{ScriptName}'");
                        continue;
                    }

                    // Try and parse the probability level
                    if (!int.TryParse(n.InnerText, out int level))
                    {
                        Game.LogTrivial($"[WARN] AgencyCalloutsPlus: Unable to parse CrimeType probability for {n.Name} in zone '{ScriptName}'");
                        continue;
                    }

                    // Add
                    Crimes.Add(calloutType, level);
                    CrimeGenerator.Add(new SpawnableCalloutType(level, calloutType));
                }
            }

            // Load Side Of Road locations
            catagoryNode = node.SelectSingleNode("Locations")?.SelectSingleNode("SideOfRoad");
            SideOfRoadLocations = ExtractSpawnPoints(LocationType.SideOfRoad, catagoryNode);

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

        private SpawnPoint ParseSpawnPoint(LocationType type, XmlNode n)
        {
            // Ensure we have attributes
            if (n.Attributes == null)
            {
                Game.LogTrivial($"[WARN] AgencyCalloutsPlus: Location item has no attributes in '{ScriptName}->{type}'");
                return null;
            }

            // Extract attributes
            float x, y, z, heading = 0f;

            // Try and extract X value
            if (n.Attributes["X"]?.Value == null || !float.TryParse(n.Attributes["X"].Value, out x))
            {
                Game.LogTrivial($"[WARN] AgencyCalloutsPlus: Unable to extract location X value for '{ScriptName}->{type}->Location'");
                return null;
            }

            // Try and extract Y value
            if (n.Attributes["Y"]?.Value == null || !float.TryParse(n.Attributes["Y"].Value, out y))
            {
                Game.LogTrivial($"[WARN] AgencyCalloutsPlus: Unable to extract location Y value for '{ScriptName}->{type}->Location'");
                return null;
            }

            // Try and extract Z value
            if (n.Attributes["Z"]?.Value == null || !float.TryParse(n.Attributes["Z"].Value, out z))
            {
                Game.LogTrivial($"[WARN] AgencyCalloutsPlus: Unable to extract location Z value for '{ScriptName}->{type}->Location'");
                return null;
            }

            // Try and extract heading value
            if (n.Attributes["heading"]?.Value != null && !float.TryParse(n.Attributes["heading"].Value, out heading))
            {
                Game.LogTrivial($"[WARN] AgencyCalloutsPlus: Unable to extract location heading value for '{ScriptName}->{type}->Location'");
                return null;
            }

            // Create the Vector3
            Vector3 vector = new Vector3(x, y, z);
            return new SpawnPoint(vector, heading);
        }

        /// <summary>
        /// Determines the optimal number of patrols this zone should have based
        /// on <see cref="ZoneSize"/>, <see cref="API.Population"/> and
        /// <see cref="ProbabilityLevel"/> of crimes
        /// </summary>
        /// <param name="Size"></param>
        /// <param name="Population"></param>
        /// <param name="CrimeLevel"></param>
        /// <returns></returns>
        private static double GetOptimumPatrolCount(ZoneSize Size, Population Population, ProbabilityLevel CrimeLevel)
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
                    baseCount *= 1.8;
                    break;
                case Population.Dense:
                    baseCount *= 2.6;
                    break;
            }

            switch (CrimeLevel)
            {
                default: // None
                    modifier = 0.25;
                    break;
                case ProbabilityLevel.VeryLow:
                    modifier = 0.33;
                    break;
                case ProbabilityLevel.Low:
                    modifier = 0.66;
                    break;
                case ProbabilityLevel.Moderate:
                    modifier = 1;
                    break;
                case ProbabilityLevel.High:
                    modifier = 1.33;
                    break;
                case ProbabilityLevel.VeryHigh:
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

            // Load backup.xml for agency backup mapping
            string path = Path.Combine(Main.PluginFolderPath, "Locations.xml");
            int itemsAdded = 0;
            int zonesAdded = 0;

            // Load XML document
            XmlDocument document = new XmlDocument();
            using (var file = new FileStream(path, FileMode.Open))
            {
                document.Load(file);
            }

            // Cycle through each child node (Zone)
            foreach (string zoneName in names)
            {
                // If we have loaded this zone already, skip it
                if (Zones.ContainsKey(zoneName)) continue;

                // Grab zone node
                XmlNode zoneNameNode = document.DocumentElement.SelectSingleNode(zoneName);

                // Skip and log errors
                if (zoneNameNode == null)
                {
                    Game.LogTrivial($"[ERROR] AgencyCalloutsPlus: Missing location data for zone '{zoneName}'");
                    continue;
                }

                // No data?
                if (!zoneNameNode.HasChildNodes) continue;

                // Create our spawn point collection and store it
                try
                {
                    var zone = new ZoneInfo(zoneNameNode);

                    // Save
                    Zones.Add(zoneName, zone);
                    itemsAdded += zone.GetTotalNumberOfLocations();
                    zonesAdded++;
                }
                catch (ArgumentNullException e)
                {
                    Game.LogTrivial($"[ERROR] AgencyCalloutsPlus: Unable to load location data for zone '{zoneName}'. Missing node '{e.ParamName}'");
                    continue;
                }
            }

            // Clean up
            document = null;

            // Log and return
            Game.LogTrivial($"[TRACE] AgencyCalloutsPlus: Added {zonesAdded} zones with {itemsAdded} locations into memory'");
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
    }
}
