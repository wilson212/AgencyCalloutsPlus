using LSPD_First_Response;
using System;
using System.IO;
using System.Linq;
using System.Xml;

namespace AgencyDispatchFramework
{
    /// <summary>
    /// A class used to generate random first and last names from the Names.xml file
    /// </summary>
    public static class RandomNameGenerator
    {
        /// <summary>
        /// Indicates whether Stop The Ped is running
        /// </summary>
        public static bool IsLoaded { get; private set; } = false;

        /// <summary>
        /// Contains a list of first names for male <see cref="Rage.Ped"/>s
        /// </summary>
        private static string[] MaleFirstNames { get; set; }

        /// <summary>
        /// Contains a list of first names for female <see cref="Rage.Ped"/>s
        /// </summary>
        private static string[] FemaleFirstNames { get; set; }

        /// <summary>
        /// Contains a list of last names for <see cref="Rage.Ped"/>s
        /// </summary>
        private static string[] LastNames { get; set; }

        /// <summary>
        /// Our randomizer
        /// </summary>
        private static CryptoRandom Random { get; set; }

        /// <summary>
        /// Private constructor
        /// </summary>
        static RandomNameGenerator()
        {
            if (!IsLoaded)
                Initialize();
        }

        /// <summary>
        /// Loads all names from the XML file
        /// </summary>
        internal static void Initialize()
        {
            if (!IsLoaded)
            {
                string path = Path.Combine(Main.FrameworkFolderPath, "Names.xml");
                if (!File.Exists(path))
                {
                    throw new FileNotFoundException("RandomNameGenerator: Names.xml file not found!");
                }

                // Load file into an XmlDocument
                XmlDocument doc = new XmlDocument();
                using (var stream = new FileStream(path, FileMode.Open))
                {
                    doc.Load(stream);
                }

                // Ensure we have first names
                XmlNode first = doc.SelectSingleNode("First");
                XmlNodeList names = doc?.SelectSingleNode("Male")?.SelectNodes("Name");
                if (names == null || names.Count == 0)
                {
                    throw new Exception("RandomNameGenerator: There are no male first names in the Names.xml file!");
                }

                // Extract names
                MaleFirstNames = (from XmlNode x in names select x.InnerText).ToArray();

                // Extrac female first names
                names = first?.SelectSingleNode("Female")?.SelectNodes("Name");
                if (names == null || names.Count == 0)
                {
                    throw new Exception("RandomNameGenerator: There are no female first names in the Names.xml file!");
                }

                // Extract names
                FemaleFirstNames = (from XmlNode x in names select x.InnerText).ToArray();

                // Ensure we have Last names
                names = doc?.SelectSingleNode("Last")?.SelectNodes("Name");
                if (names == null || names.Count == 0)
                {
                    throw new Exception("RandomNameGenerator: There are no last names in the Names.xml file!");
                }

                // Extract names
                LastNames = (from XmlNode x in names select x.InnerText).ToArray();

                // Flag
                IsLoaded = true;
            }
        }

        /// <summary>
        /// Returns a new random name
        /// </summary>
        /// <param name="gender"></param>
        /// <returns></returns>
        public static RandomName Generate(Gender gender)
        {
            var nameGen = new RandomName()
            {
                Forename = (gender == Gender.Male) ? Random.PickOne(MaleFirstNames) : Random.PickOne(FemaleFirstNames),
                Surname = Random.PickOne(LastNames)
            };

            return nameGen;
        }
    }
}
