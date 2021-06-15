using AgencyDispatchFramework.Dispatching;
using AgencyDispatchFramework.Extensions;
using AgencyDispatchFramework.Game;
using AgencyDispatchFramework.Game.Locations;
using Rage;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;

namespace AgencyDispatchFramework.Xml
{
    /// <summary>
    /// Loads and parses the XML data from a location.xml
    /// </summary>
    public class WorldZoneFile : XmlFileBase
    {
        /// <summary>
        /// Gets the Zone name
        /// </summary>
        public WorldZone Zone { get; protected set; }

        /// <summary>
        /// Creates a new instance of <see cref="WorldZoneFile"/>
        /// </summary>
        /// <param name="filePath"></param>
        public WorldZoneFile(string filePath) : base(filePath)
        {
            // === Document loaded successfully by base class if we are here === //
            // 
            Zone = new WorldZone();
        }

        /// <summary>
        /// Parses the zone XML data in the file, and throws an exception on failure
        /// </summary>
        public void Parse()
        {
            // Grab zone node
            XmlNode node = Document.DocumentElement;
            if (node == null || !node.HasChildNodes)
            {
                var zoneName = Path.GetFileNameWithoutExtension(FilePath);
                throw new FormatException($"ZoneInfo.LoadZones(): Missing location data for zone '{zoneName}'");
            }

            // Load zone info
            XmlNode catagoryNode = node.SelectSingleNode("Name");
            Zone.FullName = catagoryNode?.InnerText ?? throw new ArgumentNullException("Name");
            Zone.ScriptName = node.Name;

            // Extract size
            catagoryNode = node.SelectSingleNode("Size");
            if (String.IsNullOrWhiteSpace(catagoryNode?.InnerText) || !Enum.TryParse(catagoryNode.InnerText, out ZoneSize size))
            {
                throw new ArgumentNullException("Size");
            }
            Zone.Size = size;

            // Extract type
            catagoryNode = node.SelectSingleNode("Type");
            if (String.IsNullOrWhiteSpace(catagoryNode?.InnerText) || !Enum.TryParse(catagoryNode.InnerText, out ZoneType type))
            {
                throw new ArgumentNullException("Type");
            }
            Zone.ZoneType = type;

            // Extract social class
            catagoryNode = node.SelectSingleNode("Class");
            if (String.IsNullOrWhiteSpace(catagoryNode?.InnerText) || !Enum.TryParse(catagoryNode.InnerText, out SocialClass sclass))
            {
                throw new ArgumentNullException("Class");
            }
            Zone.SocialClass = sclass;

            // Extract population
            catagoryNode = node.SelectSingleNode("Population");
            if (String.IsNullOrWhiteSpace(catagoryNode?.InnerText) || !Enum.TryParse(catagoryNode.InnerText, out Population pop))
            {
                throw new ArgumentNullException("Population");
            }
            Zone.Population = pop;

            // Extract crime level
            catagoryNode = node.SelectSingleNode("Crime");
            if (catagoryNode == null || !catagoryNode.HasChildNodes)
            {
                throw new ArgumentNullException("Crime");
            }

            // Load crime probabilites
            Zone.CallCategoryGenerator = new WorldStateProbabilityGenerator<CallCategory>();

            // Get average calls by time of day
            var subNode = catagoryNode.SelectSingleNode("AverageCalls");
            Zone.AverageCalls = GetTimeOfDayProbabilities(subNode);
            int maxCalls = Zone.AverageCalls.Values.Sum();

            // Does this zone get any calls?
            if (maxCalls > 0)
            {
                catagoryNode = catagoryNode.SelectSingleNode("Probabilities");
                if (catagoryNode == null || !catagoryNode.HasChildNodes)
                {
                    throw new ArgumentNullException("Probabilities");
                }

                // Extract crime probabilites
                foreach (CallCategory calloutType in Enum.GetValues(typeof(CallCategory)))
                {
                    var nodeName = Enum.GetName(typeof(CallCategory), calloutType);
                    XmlNode n = catagoryNode.SelectSingleNode(nodeName);

                    // Try and parse the crime type from the node name
                    if (n == null)
                    {
                        Log.Warning($"ZoneInfo.ctor: Missing CrimeType '{nodeName}' in zone '{Zone.ScriptName}'");
                        continue;
                    }

                    // See if this calloutType is possible in this zone
                    if (bool.TryParse(n.Attributes["possible"]?.Value, out bool possible))
                    {
                        if (!possible) continue;
                    }

                    // Try and parse the probability levels by Time of Day
                    var multipliers = XmlExtractor.GetWorldStateMultipliers(n);
                    Zone.CallCategoryGenerator.Add(calloutType, multipliers);
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
            Zone.RoadShoulders = ExtractRoadLocations(catagoryNode);

            // Load Home locations
            catagoryNode = node.SelectSingleNode("Residences");
            Zone.Residences = ExtractHomes(catagoryNode);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="subNode"></param>
        /// <returns></returns>
        private Dictionary<TimePeriod, int> GetTimeOfDayProbabilities(XmlNode subNode)
        {
            // If attributes is null, we know... we know...
            if (subNode?.Attributes == null)
            {
                throw new ArgumentNullException(subNode.Name);
            }

            // Create our dictionary
            var item = new Dictionary<TimePeriod, int>(4)
            {
                { TimePeriod.Morning, 0 },
                { TimePeriod.Day, 0 },
                { TimePeriod.Evening, 0 },
                { TimePeriod.Night, 0 }
            };

            // Extract and parse morning value
            if (!Int32.TryParse(subNode.Attributes["morning"]?.Value, out int m))
            {
                Log.Error($"ZoneInfo.ctor [{subNode.GetFullPath()}]: Unable to extract 'morning' attribute on XmlNode");
            }
            item[TimePeriod.Morning] = m;

            // Extract and parse morning value
            if (!Int32.TryParse(subNode.Attributes["day"]?.Value, out m))
            {
                Log.Error($"ZoneInfo.ctor [{subNode.GetFullPath()}]: Unable to extract 'day' attribute on XmlNode");
            }
            item[TimePeriod.Day] = m;

            // Extract and parse morning value
            if (!Int32.TryParse(subNode.Attributes["evening"]?.Value, out m))
            {
                Log.Error($"ZoneInfo.ctor [{subNode.GetFullPath()}]: Unable to extract 'evening' attribute on XmlNode");
            }
            item[TimePeriod.Evening] = m;

            // Extract and parse morning value
            if (!Int32.TryParse(subNode.Attributes["night"]?.Value, out m))
            {
                Log.Error($"ZoneInfo.ctor [{subNode.GetFullPath()}]: Unable to extract 'night' attribute on XmlNode");
            }
            item[TimePeriod.Night] = m;

            return item;
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
                Log.Warning($"ZoneInfo.ExtractHomes(): Residences XmlNode is null or has no child nodes for '{Zone.ScriptName}'");
                return new Residence[0];
            }

            // Create a new list to return
            var nodes = catagoryNode.SelectNodes("Location");
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
                    Log.Warning($"ZoneInfo.ExtractHomes(): Unable to parse Residence[coordinates] attribute value in zone '{Zone.ScriptName}'");
                    continue;
                }

                // Create home
                var home = new Residence(Zone, vector);

                // Extract nodes
                try
                {
                    home.StreetName = homeNode.SelectSingleNode("Street")?.InnerText ?? throw new ArgumentException("Street");

                    // See if there is a building number
                    string val = homeNode.SelectSingleNode("BuildingNumber")?.InnerText;
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
                    val = homeNode.SelectSingleNode("Flags")?.InnerText;
                    if (!String.IsNullOrEmpty(val))
                    {
                        home.ResidenceFlags = val.CSVToEnumArray<ResidenceFlags>();
                        home.Flags = home.ResidenceFlags.Cast<int>().ToArray();
                    }
                }
                catch (ArgumentException e)
                {
                    Log.Warning($"ZoneInfo.ExtractHomes(): Unable to extract/parse Residence value {e.ParamName} in zone '{Zone.ScriptName}'");
                    continue;
                }

                // Parse spawn points!
                XmlNode pointsNode = homeNode.SelectSingleNode("Positions");
                if (pointsNode == null || !pointsNode.HasChildNodes)
                {
                    Log.Warning($"ZoneInfo.ExtractHomes(): Unable to extract Residence->Positions in zone '{Zone.ScriptName}'");
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
                        Log.Warning($"ZoneInfo.ParseSpawnPoint(): Unable to extract SpawnPoint id value for '{Zone.ScriptName}->Residences->Location->Positions'");
                        break;
                    }

                    home.SpawnPoints[s] = item;
                }

                // Not ok?
                if (!home.IsValid())
                {
                    Log.Warning($"ZoneInfo.ExtractHomes(): Location node is missing some SpawnPoints in zone '{Zone.ScriptName}'");
                    continue;
                }

                // Add home to collection
                homes.Add(home);
            }

            // Did we extract anything?
            if (homes.Count == 0)
            {
                Log.Warning($"ZoneInfo.ExtractHomes(): No residences to extract in '{Zone.ScriptName}'");
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
                        Log.Warning($"ZoneInfo.ExtractRoadLocations(): Unable to parse Location[coordinates] attribute value in zone '{Zone.ScriptName}'");
                        continue;
                    }

                    // Try and extract heading value
                    if (!n.TryGetAttribute("heading", out float heading))
                    {
                        Log.Warning($"ZoneInfo.ExtractRoadLocations(): Unable to extract Location[heading] value for '{n.GetFullPath()}'");
                        continue;
                    }

                    // Create instance
                    var sp = new RoadShoulder(Zone, vector, heading);

                    // Extract street name
                    var val = n.SelectSingleNode("Street")?.InnerText;
                    if (String.IsNullOrWhiteSpace(val))
                    {
                        Log.Warning($"ZoneInfo.ExtractRoadLocations(): Unable to extract Street value for '{n.GetFullPath()}'");
                        continue;
                    }
                    sp.StreetName = val;

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

                    // Parse spawn points
                    var pointsNode = n.SelectSingleNode("Positions");
                    foreach (XmlNode point in pointsNode?.SelectNodes("SpawnPoint"))
                    {
                        var item = ParseSpawnPoint(LocationTypeCode.RoadShoulder, point);
                        if (item == null)
                            continue;

                        // Try and extract typ value
                        if (point.Attributes["id"]?.Value == null || !Enum.TryParse(point.Attributes["id"].Value, out RoadShoulderPosition key))
                        {
                            Log.Warning($"ZoneInfo.ParseSpawnPoint(): Unable to extract SpawnPoint id value for '{Zone.ScriptName}->RoadShoulders->Location->Positions'");
                            break;
                        }

                        sp.SpawnPoints[key] = item;
                    }

                    // Not ok?
                    if (!sp.IsValid())
                    {
                        Log.Warning($"ZoneInfo.ExtractRoadLocations(): Location node is missing some SpawnPoints in zone '{Zone.ScriptName}'");
                        continue;
                    }

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
                Log.Warning($"ZoneInfo.ParseSpawnPoint(): Unable to parse SpawnPoint[coordinates] attribute value in zone '{Zone.ScriptName}'");
                return null;
            }

            // Try and extract heading value
            if (!float.TryParse(n.Attributes["heading"]?.Value, out float heading))
            {
                Log.Warning($"ZoneInfo.ParseSpawnPoint(): Unable to extract SpawnPoint[heading] value for '{n.GetFullPath()}'");
                return null;
            }

            // Create the Vector3
            var sp = new SpawnPoint(vector, heading);
            return sp;
        }
    }
}
