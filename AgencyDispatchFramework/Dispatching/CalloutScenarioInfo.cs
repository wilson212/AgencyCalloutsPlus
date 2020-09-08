using AgencyDispatchFramework.Game.Locations;
using System.Reflection;
using System.Xml;

namespace AgencyDispatchFramework.Dispatching
{
    /// <summary>
    /// Contains Scenario info for an <see cref="AgencyCallout"/>
    /// </summary>
    public class CalloutScenarioInfo
    {
        /// <summary>
        /// Gets the base probability based on the players current <see cref="Agency"/>
        /// </summary>
        public int Probability { get; set; }

        public WorldStateMultipliers  ProbabilityMultipliers { get; set; }

        /// <summary>
        /// Gets the name of the Scenario
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets the name of the <see cref="AgencyCallout"/> that runs this scenario
        /// </summary>
        public string CalloutName { get; set; }

        /// <summary>
        /// Indicates whether the responding unit should respond Code 3
        /// </summary>
        public int ResponseCode { get; set; }

        /// <summary>
        /// Gets the priority level of the call
        /// </summary>
        public int Priority { get; set; }

        /// <summary>
        /// Defines the location type of this call
        /// </summary>
        public LocationType LocationType { get; set; }

        /// <summary>
        /// Gets the scanner text played
        /// </summary>
        public string ScannerText { get; set; }

        /// <summary>
        /// Gets the incident text
        /// </summary>
        public string IncidentText { get; set; }

        /// <summary>
        /// Gets the incident abbreviation text
        /// </summary>
        public string IncidentAbbreviation { get; set; }

        /// <summary>
        /// Gets an array of call descriptions
        /// </summary>
        public ProbabilityGenerator<PriorityCallDescription> Descriptions { get; set; }

        public string SpriteName { get; internal set; }

        public string SpriteTextureDict { get; internal set; }

        public Range<int> SimulationTime { get; internal set; }

        /// <summary>
        /// 
        /// </summary>
        public static void FromXml(XmlNode element, Assembly assembly)
        {

        }
    }
}