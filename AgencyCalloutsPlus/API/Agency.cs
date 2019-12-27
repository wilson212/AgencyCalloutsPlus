using LSPD_First_Response.Mod.API;
using Rage;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;

namespace AgencyCalloutsPlus.API
{
    public static class Agency
    {
        /// <summary>
        /// Indicates whether the the Agency data has been loaded into memory
        /// </summary>
        private static bool IsInitialized { get; set; } = false;

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
        private static IReadOnlyDictionary<string, AgencyType> AgencyTypes { get; set; } = new Dictionary<string, AgencyType>()
        {
            { "fib", AgencyType.SpecialAgent },
            { "noose", AgencyType.SpecialAgent },

            { "sahp", AgencyType.HighwayPatrol }, // San Andreas Highway Partol
            { "sast", AgencyType.HighwayPatrol }, // San Andreas State Trooper
            { "nysp", AgencyType.HighwayPatrol }, // North Yankton State Patrol

            { "lspd", AgencyType.CityPolice }, // Los Santos Police Department
            { "dppd", AgencyType.CityPolice }, // Del Parro Police Department
            { "vppd", AgencyType.CityPolice }, // Vespucci Police Department
            { "vwpd", AgencyType.CityPolice }, // Vinewood Police Department
            { "lmpd", AgencyType.CityPolice }, // La Messa Police Department
            { "rhpd", AgencyType.CityPolice }, // Rockford Hills Police Department
            { "pap", AgencyType.CityPolice }, // Port Authority Police (Los Santos)
            { "gspd", AgencyType.CityPolice }, // Grape Seed Police Department
            { "sspd", AgencyType.CityPolice }, // Sandy Shore Police Department
            { "pbpd", AgencyType.CityPolice }, // Paleto Bay Police Department

            { "lssd", AgencyType.CountySheriff }, // Los Santos Sheriff Department
            { "bcso", AgencyType.CountySheriff }, // Blaine County Sheriff Office
            { "bcsd", AgencyType.CountySheriff }, // Blaine County Sheriff Department

            { "lspd_swat", AgencyType.SWAT }, // Los Santos Police Department Swat Team
            { "lssd_swat", AgencyType.SWAT }, // Los Santos Sheriff Department Swat Team

            { "doa", AgencyType.DrugUnit },

            { "sapr", AgencyType.ParkRanger }, // San Andreas Park Rangers
        };

        /// <summary>
        /// Loads the XML data that defines all police agencies, and thier jurisdiction
        /// </summary>
        internal static void Initialize()
        {
            if (IsInitialized) return;

            // Set internal flag to initialize just once
            IsInitialized = true;

            // Create collections
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
            

            // Load each custom agency XML to get police car names!
            GameFiber.Yield();

            // Clean up!
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
        public static string[] GetZonesByAgencyName(string name)
        {
            name = name.ToLowerInvariant();
            if (AgencyZones.ContainsKey(name))
            {
                return AgencyZones[name].ToArray();
            }

            return null;
        }

        /// <summary>
        /// Gets a random <see cref="Location"/> on the side of the road within current jurisdiction
        /// </summary>
        /// <remarks>
        /// This method is best NOT used by state troopers with full map jurisdiction
        /// </remarks>
        /// <param name="type">The location type to get a random position for</param>
        /// <param name="range">
        /// if not null, sets the distance requirement for the position using 
        /// <see cref="Vector3.TravelDistanceTo(Vector3)"/>. If the player is outside this range from all
        /// positions in his jurisdiction, this method with return null.
        /// </param>
        /// <returns>returns a Vector3 location on success, or null on failure</returns>
        public static Location GetRandomLocationInJurisdiction(LocationType type, Range<float> range = null)
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
                Location[] locations = Location.GetZoneLocationsByName(zoneName, type);

                // No positions?
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
            return default(Location);
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

    }
}
