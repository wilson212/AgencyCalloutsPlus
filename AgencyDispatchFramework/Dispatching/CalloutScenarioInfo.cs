using AgencyDispatchFramework.Game.Locations;

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

        /// <summary>
        /// Gets the <see cref="WorldStateMultipliers"/> for this scenario
        /// </summary>
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
        public LocationTypeCode LocationTypeCode { get; set; }

        /// <summary>
        /// Indicates the location flags required for this scenario, if any
        /// </summary>
        public FlagFilterGroup LocationFilters { get; set; }

        /// <summary>
        /// Gets the scanner audio to be played
        /// </summary>
        public string ScannerAudioString { get; set; }

        /// <summary>
        /// Indicates whether prefix the ScannerAudioString with the player's CallSign
        /// </summary>
        public bool ScannerPrefixCallSign { get; set; }

        /// <summary>
        /// Indicates whether to suffix the ScannerAudioString with the Callout Location
        /// </summary>
        public bool ScannerUsePosition { get; set; }

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

        /// <summary>
        /// Gets the texture sprite name to display in the CAD
        /// </summary>
        public string CADSpriteName { get; internal set; }

        /// <summary>
        /// Gets the texture sprite dictionary name to display in the CAD
        /// </summary>
        public string CADSpriteTextureDict { get; internal set; }

        /// <summary>
        /// Contains a time range of how long this scenario could take when the AI is on scene
        /// </summary>
        public Range<int> SimulationTime { get; internal set; }

        /// <summary>
        /// Gets the <see cref="CalloutType"/> of this scenario
        /// </summary>
        public CalloutType CrimeType { get; internal set; }

        /// <summary>
        /// Contains an array of <see cref="Agency"/>s that can handle this scenario
        /// </summary>
        public AgencyType[] AgencyTypes { get; internal set; }
    }
}