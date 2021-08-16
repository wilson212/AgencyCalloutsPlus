using AgencyDispatchFramework.Dispatching;
using AgencyDispatchFramework.Extensions;
using AgencyDispatchFramework.Game;
using AgencyDispatchFramework.Simulation;
using Rage;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Xml;

namespace AgencyDispatchFramework.Xml
{
    internal class AgenciesFile : XmlFileBase
    {
        /// <summary>
        /// A dictionary of agencies
        /// </summary>
        public Dictionary<string, Agency> Agencies { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="filePath"></param>
        public AgenciesFile(string filePath) : base(filePath)
        {
            Agencies = new Dictionary<string, Agency>();
        }

        /// <summary>
        /// Parses the XML in the agency.xml file
        /// </summary>
        public void Parse()
        {
            var mapping = new Dictionary<string, string>();
            var agencyZones = new Dictionary<string, HashSet<string>>();

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
                var zones = new HashSet<string>();

                // Make sure we have zones!
                XmlNode node = region.SelectSingleNode("Zones");
                if (!node.HasChildNodes)
                {
                    continue;
                }

                // Load all zones of jurisdiction
                foreach (XmlNode zNode in node.ChildNodes)
                {
                    zones.Add(zNode.InnerText.ToUpperInvariant());
                }

                // Add or Update
                if (agencyZones.ContainsKey(agency))
                {
                    agencyZones[agency].UnionWith(zones);
                }
                else
                {
                    agencyZones.Add(agency, zones);
                }

                WorldZone.AddRegion(name, zones.ToList());
            }

            // Add Highway to highway patrol
            if (agencyZones.ContainsKey("sahp"))
            {
                agencyZones["sahp"].Add("HIGHWAY");
            }
            else
            {
                agencyZones.Add("sahp", new HashSet<string>() { "HIGHWAY" });
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

            // cycle though agencies
            foreach (XmlNode agencyNode in document.DocumentElement.SelectNodes("Agency"))
            {
                // extract data
                string name = agencyNode.SelectSingleNode("Name")?.InnerText;
                string sname = agencyNode.SelectSingleNode("ScriptName")?.InnerText;
                string atype = agencyNode.SelectSingleNode("AgencyType")?.InnerText;
                string sLevel = agencyNode.SelectSingleNode("StaffLevel")?.InnerText;
                string county = agencyNode.SelectSingleNode("County")?.InnerText;
                string customBackAgency = agencyNode.SelectSingleNode("CustomBackingAgency")?.InnerText;
                XmlNode unitsNode = agencyNode.SelectSingleNode("Units");

                // Skip if we have no units
                if (unitsNode == null) continue;

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

                // Load vehicle sets
                var unitMapping = new Dictionary<UnitType, SpecializedUnit>();
                Agency agency = Agency.CreateAgency(type, sname, name, staffing);
                foreach (XmlNode unitNode in unitsNode.SelectNodes("Unit"))
                {
                    // Get type attribute
                    if (!Enum.TryParse(unitNode.GetAttribute("type"), out UnitType unitType))
                    {
                        Log.Warning($"Agency.Initialize(): Unable to extract Unit type value for '{sname}' in Agencies.xml");
                        continue;
                    }

                    // Get derives attribute
                    if (Enum.TryParse(unitNode.GetAttribute("derives"), out UnitType unitDerives))
                    {
                        // @todo
                    }

                    // Create unit
                    var unit = new SpecializedUnit(unitType);

                    // Load each catagory of vehicle sets
                    string[] catagories = { "Officer", "Supervisor" };
                    foreach (string catagory in catagories)
                    {
                        var n = unitNode.SelectSingleNode(catagory);
                        ParseVehicleSets(n, unit);
                    }

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

                        
                    }
                }

                // Try and parse funding level
                if (!String.IsNullOrWhiteSpace(county) && Enum.TryParse(county, out County c))
                {
                    agency.BackingCounty = c;
                }

                // Set internals
                agency.ZoneNames = agencyZones[agency.ScriptName].ToArray();
                Agencies.Add(sname, agency);
            }

            // Clean up!
            GameFiber.Yield();
            document = null;
        }

        /// <summary>
        /// Parses a VehicleSets node
        /// </summary>
        /// <param name="n"></param>
        /// <param name="unit"></param>
        private void ParseVehicleSets(XmlNode n, SpecializedUnit unit)
        {
            var nodes = n.SelectNodes("VehicleSet");
            foreach (XmlNode node in nodes)
            {
                // Check for a chance attribute
            }
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
    }
}
