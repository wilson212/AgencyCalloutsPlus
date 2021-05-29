using AgencyDispatchFramework.Extensions;
using AgencyDispatchFramework.Game;
using AgencyDispatchFramework.Game.Locations;
using AgencyDispatchFramework.Simulation;
using LSPD_First_Response.Mod.API;
using Rage;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;

namespace AgencyDispatchFramework.Dispatching
{
    /// <summary>
    /// Represents the Current Player Police/Sheriff Department
    /// </summary>
    public abstract class Agency
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
        /// Gets the <see cref="Dispatching.AgencyType"/> for this <see cref="Agency"/>
        /// </summary>
        public AgencyType AgencyType => AgencyTypes[ScriptName];

        /// <summary>
        /// Gets the funding level of this department. This will be used to determine
        /// how frequent callouts will be handled by the other AI officers.
        /// </summary>
        public StaffLevel StaffLevel { get; protected set; }

        /// <summary>
        /// Gets the optimum patrol count based on <see cref="TimePeriod" />
        /// </summary>
        internal Dictionary<TimePeriod, int> OptimumPatrols { get; set; }

        /// <summary>
        /// Contains a list of zones in this jurisdiction
        /// </summary>
        internal WorldZone[] Zones { get; set; }

        /// <summary>
        /// Containts a <see cref="ProbabilityGenerator{T}"/> list of vehicles for this agency
        /// </summary>
        private Dictionary<PatrolVehicleType, ProbabilityGenerator<PoliceVehicleInfo>> Vehicles { get; set; }

        /// <summary>
        /// Contains a list of all active on duty officers
        /// </summary>
        public OfficerUnit[] OnDutyOfficers => OfficersByShift[GameWorld.CurrentTimePeriod].ToArray();

        /// <summary>
        /// 
        /// </summary>
        internal Dictionary<TimePeriod, List<OfficerUnit>> OfficersByShift { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="Dispatcher"/> for this <see cref="Agency"/>
        /// </summary>
        internal Dispatcher Dispatcher { get; set; }

        /// <summary>
        /// Indicates whether this agency is active in game at the moment
        /// </summary>
        public bool IsActive { get; internal set; }

        /// <summary>
        /// Indicates whether this agency is a State wide agency (jurisdiction wise).
        /// </summary>
        /// <remarks>
        /// This being true changes the way the program handles calling for backup units when on duty
        /// </remarks>
        public bool IsStateAgency
        {
            get
            {
                var type = AgencyTypes[ScriptName];
                return (type == AgencyType.HighwayPatrol || type == AgencyType.StateParks || type == AgencyType.StatePolice);
            }
        }

        /// <summary>
        /// Indicates whether this agency is a law enforcement agency
        /// </summary>
        /// <remarks>
        /// This being true changes the way the program handles calling for backup units when on duty
        /// </remarks>
        public bool IsLawEnforcementAgency
        {
            get
            {
                var type = AgencyTypes[ScriptName];
                return (
                    type == AgencyType.HighwayPatrol
                    || type == AgencyType.StateParks
                    || type == AgencyType.StatePolice
                    || type == AgencyType.CountySheriff
                    || type == AgencyType.CityPolice
                );
            }
        }

        /// <summary>
        /// Gets or sets the backing county of this <see cref="Agency"/>
        /// </summary>
        public County BackingCounty { get; internal set; }

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

            // *******************************************
            // Load backup.xml for agency backup mapping
            // *******************************************
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

            // cycle through each child node 
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

            // *******************************************
            // Load regions.xml for agency jurisdiction zones
            // *******************************************
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

                // Add or Update
                if (AgencyZones.ContainsKey(agency))
                {
                    AgencyZones[agency].AddRange(zones);
                }
                else
                {
                    AgencyZones.Add(agency, zones);
                }
                
                WorldZone.AddRegion(name, zones);
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

            // *******************************************
            // Load Agencies.xml for agency types
            // *******************************************
            path = Path.Combine(Main.FrameworkFolderPath, "Agencies.xml");
            document = new XmlDocument();
            using (var file = new FileStream(path, FileMode.Open))
            {
                document.Load(file);
            }

            // Get names
            string[] enumNames = Enum.GetNames(typeof(PatrolVehicleType));

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
                string sLevel = n.SelectSingleNode("StaffLevel")?.InnerText;
                string county = n.SelectSingleNode("County")?.InnerText;
                string customBackAgency = n.SelectSingleNode("CustomBackingAgency")?.InnerText;
                XmlNode vehicleNode = n.SelectSingleNode("Vehicles");

                // Check name
                if (String.IsNullOrWhiteSpace(sname))
                {
                    Log.Warning($"Agency.Initialize(): Unable to extract ScriptName value for agency in Agencies.xml");
                    continue;
                }

                // Try and parse agency type
                if (String.IsNullOrWhiteSpace(atype) || !Enum.TryParse(atype, out AgencyType type))
                {
                    Log.Warning($"Agency.Initialize(): Unable to extract AgencyType value for '{sname}' in Agencies.xml");
                    continue;
                }

                // Try and parse funding level
                if (String.IsNullOrWhiteSpace(sLevel) || !Enum.TryParse(sLevel, out StaffLevel staffing))
                {
                    Log.Warning($"Agency.Initialize(): Unable to extract StaffLevel value for '{sname}' in Agencies.xml");
                    continue;
                }

                // Load vehicles
                Agency agency = CreateAgency(type, sname, name, staffing);
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
                            Log.Warning($"Agency.Initialize(): Vehicle item for '{sname}' has no attributes in Agencies.xml");
                            continue;
                        }

                        // Try and extract probability value
                        if (vn.Attributes["probability"]?.Value == null || !int.TryParse(vn.Attributes["probability"].Value, out int probability))
                        {
                            Log.Warning($"Agency.Initialize(): Unable to extract vehicle probability value for '{sname}' in Agencies.xml");
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
                            info.Extras = ParseExtras(extras);
                        }

                        // Extract spawn color
                        if (!String.IsNullOrWhiteSpace(vn.Attributes["color"]?.Value))
                        {
                            string color = vn.Attributes["color"].Value;
                            info.SpawnColor = (Color)Enum.Parse(typeof(Color), color);
                        }

                        // Add vehicle to agency
                        agency.AddVehicle((PatrolVehicleType)Enum.Parse(typeof(PatrolVehicleType), ename), info);
                    }
                }

                // Try and parse funding level
                if (!String.IsNullOrWhiteSpace(county) && Enum.TryParse(county, out County c))
                {
                    agency.BackingCounty = c;
                }

                Agencies.Add(sname, agency);
                AgencyTypes.Add(sname, type);
            }

            // Clean up!
            GameFiber.Yield();
            document = null;
        }

        /// <summary>
        /// Creates and returns an <see cref="Agency"/> instance based on <see cref="AgencyType"/>
        /// </summary>
        /// <param name="type"></param>
        /// <param name="sname"></param>
        /// <param name="name"></param>
        /// <param name="staffing"></param>
        /// <returns></returns>
        private static Agency CreateAgency(AgencyType type, string sname, string name, StaffLevel staffing)
        {
            switch (type)
            {
                case AgencyType.CityPolice:
                    return new CityPoliceAgency(sname, name, staffing);
                case AgencyType.CountySheriff:
                case AgencyType.StateParks:
                    return new SheriffAgency(sname, name, staffing);
                case AgencyType.StatePolice:
                case AgencyType.HighwayPatrol:
                    return new HighwayPatrolAgency(sname, name, staffing);
                default:
                    throw new NotImplementedException($"The AgencyType '{type}' is yet supported");
            }
        }

        /// <summary>
        /// Gets an array of all zones under the players current Jurisdiction
        /// </summary>
        public static string[] GetCurrentAgencyZoneNames()
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
            Vehicles = new Dictionary<PatrolVehicleType, ProbabilityGenerator<PoliceVehicleInfo>>();
        }

        /// <summary>
        /// Creates the dispatcher for this agency type
        /// </summary>
        /// <returns></returns>
        internal abstract Dispatcher CreateDispatcher();

        /// <summary>
        /// Calculates the optimum patrols for this agencies jurisdiction
        /// </summary>
        /// <param name="zoneNames"></param>
        /// <returns></returns>
        protected abstract Dictionary<TimePeriod, int> GetOptimumUnitCounts(WorldZone[] zones);

        /// <summary>
        /// Enables simulation on this <see cref="Agency"/> in the game
        /// </summary>
        internal virtual void Enable()
        {
            // Saftey
            if (IsActive) return;

            // Get our zones of jurisdiction, and ensure each zone has the primary agency set
            Zones = GetZoneNamesByAgencyName(ScriptName).Select(x => WorldZone.GetZoneByName(x)).ToArray();

            // Load officers
            OfficersByShift = new Dictionary<TimePeriod, List<OfficerUnit>>();

            // Create dispatcher
            Dispatcher = CreateDispatcher();

            // Get our patrol counts
            OptimumPatrols = GetOptimumUnitCounts(Zones);

            // Register for TimeOfDay changes!
            GameWorld.OnTimePeriodChanged += GameWorld_OnTimeOfDayChanged;

            // Finally, flag
            IsActive = true;
        }

        /// <summary>
        /// Disables simulation on this <see cref="Agency"/> in the game
        /// </summary>
        internal virtual void Disable()
        {
            // Un-Register for TimeOfDay changes!
            GameWorld.OnTimePeriodChanged -= GameWorld_OnTimeOfDayChanged;

            // Dispose the dispatcher
            Dispatcher?.Dispose();

            // Dispose officer units
            DisposeAIUnits();

            // flag
            IsActive = false;
        }

        /// <summary>
        /// Disposes and clears all AI units
        /// </summary>
        private void DisposeAIUnits()
        {
            // Clear old police units
            if (OnDutyOfficers != null)
            {
                foreach (var officerUnits in OfficersByShift.Values)
                foreach (var officer in officerUnits)
                    officer.Dispose();
            }
        }

        /// <summary>
        /// Method called when <see cref="GameWorld.OnTimePeriodChanged"/> is called. Manages the
        /// shifts of all the <see cref="OfficerUnit"/>s
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void GameWorld_OnTimeOfDayChanged(object sender, EventArgs e)
        {
            // Ensure we have enough locations to spawn patrols at
            var period = GameWorld.CurrentTimePeriod;
            int aiPatrolCount = OfficersByShift[period].Count;
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
                b.Append(Enum.GetName(typeof(TimePeriod), period));
                b.Append(" shift.");
                Log.Warning(b.ToString());

                // Adjust count
                aiPatrolCount = locations.Length;
            }

            // Tell new units they are on duty
            int i = 0;
            foreach (var unit in OfficersByShift[period])
            {
                unit.StartDuty(locations[i]);
                i++;
            }

            // Tell old units they are off duty last!
            period = GameWorld.GetPreviousTimePeriod();
            foreach (var unit in OfficersByShift[period])
            {
                unit.EndDuty();
            }
        }

        /// <summary>
        /// Gets a number of random <see cref="RoadShoulder"/> locations within
        /// this <see cref="Agency" /> jurisdiction
        /// </summary>
        /// <param name="desiredCount">The desired amount</param>
        /// <returns></returns>
        protected RoadShoulder[] GetRandomShoulderLocations(int desiredCount)
        {
            // Get a list of all locations
            var locs = new List<RoadShoulder>(desiredCount);
            foreach (var zone in Zones)
            {
                locs.AddRange(zone.RoadShoulders);
            }

            // Shuffle
            locs.Shuffle();

            return locs.Take(desiredCount).ToArray();
        }

        /// <summary>
        /// Adds a vehicle to the list of vehicles that can be spawned from this agency
        /// </summary>
        /// <param name="type"></param>
        /// <param name="info"></param>
        internal void AddVehicle(PatrolVehicleType type, PoliceVehicleInfo info)
        {
            if (!Vehicles.ContainsKey(type))
            {
                Vehicles.Add(type, new ProbabilityGenerator<PoliceVehicleInfo>());
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
        public Vehicle GetRandomPoliceVehicle(PatrolVehicleType type, SpawnPoint spawnPoint)
        {
            // Does this agency contain a patrol car of this type?
            if (!Vehicles.ContainsKey(type))
            {
                return null;
            }

            // Try and spawn a police vehicle
            if (!Vehicles[type].TrySpawn(out PoliceVehicleInfo info))
            {
                Log.Warning($"Agency.GetRandomPoliceVehicle(): unable to find vehicle for class {type}");
                return null;
            }

            // Spawn vehicle
            var vehicle = new Vehicle(info.ModelName, spawnPoint.Position, spawnPoint.Heading);

            // Add any extras
            if (info.Extras != null && info.Extras.Count > 0)
            {
                foreach (var extra in info.Extras)
                {
                    // Ensure this vehicle has this livery index
                    if (!vehicle.DoesExtraExist(extra.Key))
                        continue;

                    // Enable
                    vehicle.SetExtraEnabled(extra.Key, extra.Value);
                }
            }

            // IS a spawn Color set?
            if (info.SpawnColor != default(Color))
            {
                vehicle.PrimaryColor = info.SpawnColor;
            }

            // Livery?
            if (info.Livery > 0)
            {
                vehicle.SetLivery(info.Livery);
            }

            return vehicle;
        }

        /// <summary>
        /// Parses the extras attribute into a hash table
        /// </summary>
        /// <param name="extras"></param>
        /// <returns></returns>
        private static Dictionary<int, bool> ParseExtras(string extras)
        {
            // No extras?
            if (String.IsNullOrWhiteSpace(extras))
            {
                return new Dictionary<int, bool>();
            }

            string toParse = extras.Replace("extra", "").Replace(" ", String.Empty);
            string[] parts = toParse.Split(',', '=');

            // Ensure we have an even number of things
            if (parts.Length % 2 != 0)
            {
                return new Dictionary<int, bool>();
            }

            // Parse items
            var dic = new Dictionary<int, bool>(parts.Length / 2);
            for (int i = 0; i < parts.Length - 1; i += 2)
            {
                if (!int.TryParse(parts[i], out int id))
                    continue;

                if (!bool.TryParse(parts[i + 1], out bool value))
                    continue;

                dic.Add(id, value);
            }

            return dic;
        }

        public override string ToString()
        {
            return FriendlyName;
        }

        #endregion

        internal struct PoliceVehicleInfo : ISpawnable
        {
            public int Probability { get; set; }

            public string ModelName { get; set; }

            public int Livery { get; set; }

            public Color SpawnColor { get; set; }

            public Dictionary<int, bool> Extras { get; set; }
        }
    }
}
