using Rage;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace AgencyCalloutsPlus.API
{
    public abstract class LocationInfo
    {
        /// <summary>
        /// Gets the <see cref="Vector3"/> position of this location
        /// </summary>
        public Vector3 Position { get; protected set; }

        /// <summary>
        /// Containts a list of Ped spawn points for this location, if any
        /// </summary>
        public List<SpawnPoint> PedSpawnPoints { get; internal set; }

        /// <summary>
        /// Containts a list of Vehicle spawn points for this location, if any
        /// </summary>
        public List<SpawnPoint> VehicleSpawnPoints { get; internal set; }

        /// <summary>
        /// Containts a hash table to locations, location types and Location data
        /// </summary>
        private static Dictionary<string, Dictionary<LocationType, LocationInfo[]>> Locations { get; set; }

        protected LocationInfo(Vector3 position)
        {
            this.Position = position;
        }

        /// <summary>
        /// Loads the specified zones and of thier position data from the Locations.xml into
        /// memory, and returns the number of locations added.
        /// </summary>
        /// <param name="zones">An array of zones to load (should be all uppercase)</param>
        /// <returns>returns the number of locations loaded</returns>
        public static int LoadZones(string[] zones)
        {
            // Create instance of not already!
            if (Locations == null)
            {
                Locations = new Dictionary<string, Dictionary<LocationType, LocationInfo[]>>(zones.Length + 1);
            }

            // Load backup.xml for agency backup mapping
            string[] names = Enum.GetNames(typeof(LocationType));
            string path = Path.Combine(Main.PluginFolderPath, "Locations.xml");
            var items = new List<LocationInfo>(50);
            int itemsAdded = 0;

            // Load XML document
            XmlDocument document = new XmlDocument();
            using (var file = new FileStream(path, FileMode.Open))
            {
                document.Load(file);
            }

            // cycle through each child noed 
            foreach (string zone in zones)
            {
                // Ensure we havent loaded this zone already
                if (Locations.ContainsKey(zone)) continue;

                // Grab node
                XmlNode zoneNameNode = document.DocumentElement.SelectSingleNode(zone);

                // Skip and log errors
                if (zoneNameNode == null)
                {
                    Game.LogTrivial($"[ERROR] AgencyCalloutsPlus: Unable to load location data for zone '{zone}'");
                    continue;
                }

                // No data?
                if (!zoneNameNode.HasChildNodes) continue;

                // Store data
                var typeDict = new Dictionary<LocationType, LocationInfo[]>(names.Length);
                items.Clear(); // clear out old stuff

                // Select each location type as needed
                foreach (string name in names)
                {
                    LocationType type = (LocationType)Enum.Parse(typeof(LocationType), name);

                    // Check for null or child nodes (no locations)
                    XmlNode locationTypeNode = zoneNameNode.SelectSingleNode(name);
                    if (locationTypeNode == null || !locationTypeNode.HasChildNodes)
                    {
                        //if (locationTypeNode == null)
                            //Game.LogTrivial($"[AgencyCalloutsPlus] TRACE: Zone is missing location type '{zone}->{name}'");

                        typeDict.Add(type, new LocationInfo[0]);
                        continue;
                    }

                    // Itterate through items
                    foreach (XmlNode n in locationTypeNode.SelectNodes("SpawnPoint"))
                    {
                        // Ensure we have attributes
                        if (n.Attributes == null)
                        {
                            Game.LogTrivial($"[WARN] AgencyCalloutsPlus: Location item has no attributes in '{zone}->{name}'");
                            continue;
                        }

                        // Extract attributes
                        float x, y, z, heading = 0f;
                            
                        // Try and extract X value
                        if (n.Attributes["X"]?.Value == null || !float.TryParse(n.Attributes["X"].Value, out x))
                        {
                            Game.LogTrivial($"[WARN] AgencyCalloutsPlus: Unable to extract location X value for '{zone}->{name}->Location'");
                            continue;
                        }

                        // Try and extract Y value
                        if (n.Attributes["Y"]?.Value == null || !float.TryParse(n.Attributes["Y"].Value, out y))
                        {
                            Game.LogTrivial($"[WARN] AgencyCalloutsPlus: Unable to extract location Y value for '{zone}->{name}->Location'");
                            continue;
                        }

                        // Try and extract Z value
                        if (n.Attributes["Z"]?.Value == null || !float.TryParse(n.Attributes["Z"].Value, out z))
                        {
                            Game.LogTrivial($"[WARN] AgencyCalloutsPlus: Unable to extract location Z value for '{zone}->{name}->Location'");
                            continue;
                        }

                        // Try and extract heading value
                        if (n.Attributes["heading"]?.Value != null && !float.TryParse(n.Attributes["heading"].Value, out heading))
                        {
                            Game.LogTrivial($"[WARN] AgencyCalloutsPlus: Unable to extract location heading value for '{zone}->{name}->Location'");
                            continue;
                        }

                        // Create the Vector3
                        Vector3 vector = new Vector3(x, y, z);
                        items.Add(new SideOfRoadLocation(vector, heading));
                        itemsAdded++;

                        // Does this node have children too???
                        if (n.HasChildNodes)
                        {
                            // todo: later
                        }
                    }

                    // add
                    typeDict.Add(type, items.ToArray());
                }

                // Add zone to dictionary
                Locations.Add(zone, typeDict);
            }

            // Clean up
            document = null;

            // Log and return
            Game.LogTrivial($"[TRACE] AgencyCalloutsPlus: Added {Locations.Count} zones with {itemsAdded} locations into memory'");
            return itemsAdded;
        }

        /// <summary>
        /// Gets all <see cref="LocationInfo"/> entities in a zone by <see cref="LocationType"/>
        /// </summary>
        /// <param name="name"></param>
        /// <param name="type"></param>
        /// <returns>an array of <see cref="LocationInfo"/> entities, or null if the zone has not been loaded yet</returns>
        public static LocationInfo[] GetZoneLocationsByName(string name, LocationType type)
        {
            // Ensure zone exists
            if (Locations.TryGetValue(name, out Dictionary<LocationType, LocationInfo[]> locations))
            {
                return locations[type];
            }

            return null;
        }
    }
}
