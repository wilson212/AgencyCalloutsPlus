using AgencyCalloutsPlus.Extensions;
using LSPD_First_Response.Mod.API;
using Rage;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Xml;

namespace AgencyCalloutsPlus.API
{
    /// <summary>
    /// Represents the Current Player Police/Sheriff Department
    /// </summary>
    public class Agency
    {
        #region Static Properties

        /// <summary>
        /// Indicates whether the the Agency data has been loaded into memory
        /// </summary>
        private static bool IsInitialized { get; set; } = false;

        /// <summary>
        /// A dictionary of agencies
        /// </summary>
        private static Dictionary<string, Agency> Agencies { get; set; }

        /// <summary>
        /// Containts a list of zones of jurisdiction by each agency
        /// </summary>
        /// <remarks>
        /// [ AgencyScriptName => List of ZoneNames ]
        /// </remarks>
        private static Dictionary<string, List<string>> AgencyZones { get; set; }

        /// <summary>
        /// A dictionary of agencies and thier <see cref="AgencyType"/>. This will be used
        /// to determine which callouts are registered for the player in game
        /// </summary>
        private static Dictionary<string, AgencyType> AgencyTypes { get; set; }

        #endregion

        #region Instance Properties

        /// <summary>
        /// Gets the full string name of this agency
        /// </summary>
        public string FriendlyName { get; private set; }

        /// <summary>
        /// Gets the full script name of this agency
        /// </summary>
        public string ScriptName { get; private set; }

        /// <summary>
        /// Gets the <see cref="API.AgencyType"/> for this <see cref="Agency"/>
        /// </summary>
        public AgencyType AgencyType => AgencyTypes[ScriptName];

        /// <summary>
        /// Gets the funding level of this department. This will be used to determine
        /// how frequent callouts will be handled by the other AI officers.
        /// </summary>
        public StaffLevel StaffLevel { get; protected set; }

        /// <summary>
        /// Gets the optimum number of patrols
        /// </summary>
        public double OptimumPatrols { get; private set; }

        /// <summary>
        /// Gets the number of patrol units for this agency
        /// </summary>
        public int ActualPatrols { get; protected set; }

        /// <summary>
        /// Gets the max crime level index of this agency
        /// </summary>
        public int MaxCrimeLevel { get; protected set; }

        /// <summary>
        /// Gets the overall crime level index of this agency
        /// </summary>
        public int OverallCrimeLevel { get; protected set; }

        /// <summary>
        /// Gets the number of zones in this jurisdiction
        /// </summary>
        public int ZoneCount { get; protected set; }

        /// <summary>
        /// Gets a list of zones in this jurisdiction
        /// </summary>
        private SpawnGenerator<ZoneInfo> ZoneGenerator { get; set; }

        /// <summary>
        /// Containts a <see cref="SpawnGenerator{T}"/> list of vehicles for this agency
        /// </summary>
        private Dictionary<PatrolType, SpawnGenerator<PoliceVehicleInfo>> Vehicles { get; set; }

        #endregion

        #region Static Methods 

        /// <summary>
        /// Loads the XML data that defines all police agencies, and thier jurisdiction
        /// </summary>
        internal static void Initialize()
        {
            if (IsInitialized) return;

            // Set internal flag to initialize just once
            IsInitialized = true;

            // Create collections
            Agencies = new Dictionary<string, Agency>(5);
            AgencyTypes = new Dictionary<string, AgencyType>(25);
            AgencyZones = new Dictionary<string, List<string>>();
            var mapping = new Dictionary<string, string>();

            // Load backup.xml for agency backup mapping
            string rootPath = Path.Combine(Main.GTARootPath, "lspdfr", "data");
            string path = Path.Combine(rootPath, "backup.xml");

            // Load XML document
            XmlDocument document = new XmlDocument();
            using (var file = new FileStream(path, FileMode.Open))
            {
                document.Load(file);
            }

            // Allow other plugins time to do whatever
            GameFiber.Yield();

            // cycle through each child noed 
            foreach (XmlNode node in document.DocumentElement.SelectSingleNode("LocalPatrol").ChildNodes)
            {
                // Skip errors
                if (!node.HasChildNodes) continue;

                // extract needed data
                string nodeName = node.LocalName;
                string agency = node.FirstChild.InnerText.ToLowerInvariant();

                // add
                mapping.Add(nodeName, agency);
            }

            // Load regions.xml for agency jurisdiction zones
            path = Path.Combine(rootPath, "regions.xml");
            document = new XmlDocument();
            using (var file = new FileStream(path, FileMode.Open))
            {
                document.Load(file);
            }

            // Allow other plugins time to do whatever
            GameFiber.Yield();

            // cycle though regions
            foreach (XmlNode region in document.DocumentElement.ChildNodes)
            {
                string name = region.SelectSingleNode("Name").InnerText;
                string agency = mapping[name];
                var zones = new List<string>();

                // Make sure we have zones!
                XmlNode node = region.SelectSingleNode("Zones");
                if (!node.HasChildNodes)
                {
                    continue;
                }

                // Load all zones of jurisdiction
                foreach (XmlNode zNode in node.ChildNodes)
                {
                    zones.Add(zNode.InnerText);
                }

                // add
                AgencyZones.Add(agency, zones);
            }

            // Add Highway to highway patrol
            if (AgencyZones.ContainsKey("sahp"))
            {
                AgencyZones["sahp"].Add("HIGHWAY");
            }
            else
            {
                AgencyZones.Add("sahp", new List<string>() { "HIGHWAY" });
            }


            // Load each custom agency XML to get police car names!
            GameFiber.Yield();

            // Load Agencies.xml for agency types
            path = Path.Combine(Main.PluginFolderPath, "Agencies.xml");
            document = new XmlDocument();
            using (var file = new FileStream(path, FileMode.Open))
            {
                document.Load(file);
            }

            // Get names
            string[] enumNames = Enum.GetNames(typeof(PatrolType));

            // cycle though agencies
            foreach (XmlNode n in document.DocumentElement.ChildNodes)
            {
                // Skip comments
                if (n.NodeType == XmlNodeType.Comment)
                    continue;

                // extract data
                string name = n.SelectSingleNode("Name")?.InnerText;
                string sname = n.SelectSingleNode("ScriptName")?.InnerText;
                string atype = n.SelectSingleNode("AgencyType")?.InnerText;
                string ftype = n.SelectSingleNode("StaffLevel")?.InnerText;
                XmlNode vehicleNode = n.SelectSingleNode("Vehicles");

                // Check name
                if (String.IsNullOrWhiteSpace(sname))
                {
                    Game.LogTrivial($"[WARN] AgencyCalloutsPlus: Unable to extract ScriptName value for agency in Agencies.xml");
                    continue;
                }

                // Try and parse agency type
                if (String.IsNullOrWhiteSpace(atype) || !Enum.TryParse(atype, out AgencyType type))
                {
                    Game.LogTrivial($"[WARN] AgencyCalloutsPlus: Unable to extract AgencyType value for '{sname}' in Agencies.xml");
                    continue;
                }

                // Try and parse funding level
                if (String.IsNullOrWhiteSpace(ftype) || !Enum.TryParse(ftype, out StaffLevel staffing))
                {
                    Game.LogTrivial($"[WARN] AgencyCalloutsPlus: Unable to extract StaffLevel value for '{sname}' in Agencies.xml");
                    continue;
                }

                // Load vehicles
                Agency agency = new Agency(sname, name, staffing);
                foreach (string ename in enumNames)
                {
                    // Try and extract patrol vehicle type
                    XmlNode cn = vehicleNode.SelectSingleNode(ename);
                    if (cn == null)
                        continue;

                    // Load each vehicle
                    foreach (XmlNode vn in cn.ChildNodes)
                    {
                        // Ensure we have attributes
                        if (vn.Attributes == null)
                        {
                            Game.LogTrivial($"[WARN] AgencyCalloutsPlus: Vehicle item for '{sname}' has no attributes in Agencies.xml");
                            continue;
                        }

                        // Try and extract probability value
                        if (vn.Attributes["probability"]?.Value == null || !int.TryParse(vn.Attributes["probability"].Value, out int probability))
                        {
                            Game.LogTrivial($"[WARN] AgencyCalloutsPlus: Unable to extract vehicle probability value for '{sname}' in Agencies.xml");
                            continue;
                        }

                        // Create vehicle info
                        var info = new PoliceVehicleInfo()
                        {
                            ModelName = vn.InnerText,
                            Probability = probability
                        };

                        // Try and extract livery value
                        if (vn.Attributes["livery"]?.Value != null && int.TryParse(vn.Attributes["livery"].Value, out int livery))
                        {
                            info.Livery = livery;
                        }

                        // Extract extras
                        if (!String.IsNullOrWhiteSpace(vn.Attributes["extras"]?.Value))
                        {
                            string extras = vn.Attributes["extras"].Value;
                            info.Extras = extras.ToIntList().ToArray();
                        }

                        // Extract spawn color
                        if (!String.IsNullOrWhiteSpace(vn.Attributes["color"]?.Value))
                        {
                            string color = vn.Attributes["color"].Value;
                            info.SpawnColor = (Color)Enum.Parse(typeof(Color), color);
                        }

                        // Add vehicle to agency
                        agency.AddVehicle((PatrolType)Enum.Parse(typeof(PatrolType), ename), info);
                    }
                }

                Agencies.Add(sname, agency);
                AgencyTypes.Add(sname, type);
            }

            // Clean up!
            GameFiber.Yield();
            document = null;
        }

        /// <summary>
        /// Gets an array of all zones under the players current Jurisdiction
        /// </summary>
        public static string[] GetCurrentAgencyJurisdictionZones()
        {
            string name = Functions.GetCurrentAgencyScriptName().ToLowerInvariant();
            if (AgencyZones.ContainsKey(name))
            {
                return AgencyZones[name].ToArray();
            }

            return null;
        }

        /// <summary>
        /// Gets an array of all zones under the Jurisdiction of the specified Agency sriptname
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static string[] GetZoneNamesByAgencyName(string name)
        {
            name = name.ToLowerInvariant();
            if (AgencyZones.ContainsKey(name))
            {
                return AgencyZones[name].ToArray();
            }

            return null;
        }

        /// <summary>
        /// Gets a random <see cref="SpawnPoint"/> on the side of a road within the Players 
        /// current agency area of jurisdiction
        /// </summary>
        /// <remarks>
        /// This method is best NOT used by state troopers with full map jurisdiction
        /// </remarks>
        /// <param name="range">
        /// if not null, sets the distance requirement for the position using 
        /// <see cref="Vector3.TravelDistanceTo(Vector3)"/>. If the player is outside this range from all
        /// positions in his jurisdiction, this method with return null.
        /// </param>
        /// <returns>returns a <see cref="LocationInfo"/> on success, or null on failure</returns>
        public static SpawnPoint GetRandomSideOfRoadLocation(Range<float> range = null)
        {
            // Load randomizer
            var rando = new CryptoRandom();

            // Get zones in this jurisdiction
            string[] zones = GetCurrentAgencyJurisdictionZones();
            if (zones == null)
            {
                throw new Exception($"Curernt player agency does not have any locations of jurisdiction.");
            }

            // Shuffle zones for randomness
            // Try and find a street location in our jurisdiction
            foreach (string zoneName in zones.OrderBy(x => rando.Next()))
            {
                // Grab zone positions
                ZoneInfo zone = ZoneInfo.GetZoneByName(zoneName);
                var locations = zone?.SideOfRoadLocations;

                // No spawn points?
                if (locations == null || locations.Length == 0)
                    continue;

                // Do we have distance requirements?
                if (range != null)
                {
                    // Get player location and 
                    var playerLocation = Game.LocalPlayer.Character.Position;

                    // Calculate distance until we find a street within parameters
                    foreach (var location in locations.OrderBy(x => rando.Next()))
                    {
                        // calculate distance
                        var distance = playerLocation.TravelDistanceTo(location.Position);
                        if (range.ContainsValue(distance))
                        {
                            return location;
                        }
                    }
                }
                else
                {
                    var pos = locations[rando.Next(0, locations.Length - 1)];
                    return pos;
                }
            }

            // If we are here, we failed to find a position in our juristiction
            return null;
        }

        /// <summary>
        /// Gets the <see cref="AgencyType"/> based on the script name of the Agency
        /// </summary>
        /// <param name="scriptName"></param>
        /// <returns></returns>
        public static AgencyType GetAgencyTypeByName(string scriptName)
        {
            if (AgencyTypes.TryGetValue(scriptName, out AgencyType type))
            {
                return type;
            }

            return AgencyType.NotSupported;
        }

        /// <summary>
        /// Gets the players current police agency
        /// </summary>
        /// <returns></returns>
        public static Agency GetCurrentPlayerAgency()
        {
            string name = Functions.GetCurrentAgencyScriptName().ToLowerInvariant();
            return (Agencies.ContainsKey(name)) ? Agencies[name] : null;
        }

        /// <summary>
        /// Gets the Agency data for the specied Agency
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static Agency GetAgencyByName(string name)
        {
            name = name.ToLowerInvariant();
            return (Agencies.ContainsKey(name)) ? Agencies[name] : null;
        }

        #endregion Static Methods

        #region Instance Methods

        internal Agency(string scriptName, string friendlyName, StaffLevel staffLevel)
        {
            ScriptName = scriptName ?? throw new ArgumentNullException(nameof(scriptName));
            FriendlyName = friendlyName ?? throw new ArgumentNullException(nameof(friendlyName));
            StaffLevel = staffLevel;

            // Initiate vars
            Vehicles = new Dictionary<PatrolType, SpawnGenerator<PoliceVehicleInfo>>();
            ZoneGenerator = new SpawnGenerator<ZoneInfo>();
        }

        internal void InitializeData()
        {
            if (ZoneGenerator.ItemCount == 0)
            {
                var zones = GetZoneNamesByAgencyName(ScriptName).Select(x => ZoneInfo.GetZoneByName(x)).ToArray();
                ZoneGenerator.AddRange(zones);
                ZoneCount = zones.Length;

                // Determine our overall crime level
                foreach (var zone in zones)
                {
                    // Skip dead zones
                    if (zone.CrimeLevel == ProbabilityLevel.None)
                        continue;

                    MaxCrimeLevel += (int)ProbabilityLevel.VeryHigh;
                    OverallCrimeLevel += (int)zone.CrimeLevel;
                    OptimumPatrols += zone.IdealPatrolCount;
                }

                // Deterime our patrol count
                int staffLevel = (int)StaffLevel;
                ActualPatrols = (int)(OptimumPatrols * (staffLevel / 100d));
            }
        }

        /// <summary>
        /// Adds a vehicle to the list of vehicles that can be spawned from this agency
        /// </summary>
        /// <param name="type"></param>
        /// <param name="info"></param>
        internal void AddVehicle(PatrolType type, PoliceVehicleInfo info)
        {
            if (!Vehicles.ContainsKey(type))
            {
                Vehicles.Add(type, new SpawnGenerator<PoliceVehicleInfo>());
            }

            Vehicles[type].Add(info);
        }

        /// <summary>
        /// Spawns a random police vehicle for this agency at the specified location. The vehicle
        /// will not contain a police ped inside.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="spawnPoint"></param>
        /// <returns></returns>
        public Vehicle SpawnPoliceVehicleOfType(PatrolType type, SpawnPoint spawnPoint)
        {
            // Does this agency contain a patrol car of this type?
            if (!Vehicles.ContainsKey(type))
            {
                return null;
            }

            // Try and spawn a police vehicle
            if (!Vehicles[type].TrySpawn(out PoliceVehicleInfo info))
            {
                Game.LogTrivial($"[TRACE] AgencyCalloutsPlus: Agency.SpawnPoliceVehicleOfType() unable to find vehicle for class {type}");
                return null;
            }

            // Spawn vehicle
            var vehicle = new Vehicle(info.ModelName, spawnPoint.Position, spawnPoint.Heading);

            // Add any extras
            if (info.Extras != null && info.Extras.Length > 0)
            {
                foreach (int extraId in info.Extras)
                {
                    // Ensure this vehicle has this livery index
                    if (!vehicle.DoesExtraExist(extraId))
                        continue;

                    // Enable
                    vehicle.SetExtraEnabled(extraId, true);
                }
            }

            // IS a spawn Color set?
            if (info.SpawnColor != default(Color))
            {
                vehicle.PrimaryColor = info.SpawnColor;
            }

            // Livery?
            if (info.Livery >= 0)
            {
                vehicle.SetLivery(info.Livery);
            }

            return vehicle;
        }

        public ZoneInfo GetNextRandomCrimeZone()
        {
            if (ZoneGenerator.TrySpawn(out ZoneInfo zone))
            {
                return zone;
            }

            return null;
        }

        #endregion

        internal class PoliceVehicleInfo : ISpawnable
        {
            public int Probability { get; set; }

            public string ModelName { get; set; }

            public int Livery { get; set; } = -1;

            public Color SpawnColor { get; set; }

            public int[] Extras { get; set; }
        }
    }
}
