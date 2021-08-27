using AgencyDispatchFramework.Conversation;
using AgencyDispatchFramework.Dispatching;
using AgencyDispatchFramework.Game.Locations;
using AgencyDispatchFramework.Simulation;

namespace AgencyDispatchFramework.Scripting
{
    /// <summary>
    /// Contains Scenario info for a <see cref="Callout"/>
    /// </summary>
    public class CalloutScenarioInfo
    {
        /// <summary>
        /// Gets the <see cref="WorldStateMultipliers"/> for this scenario
        /// </summary>
        public WorldStateMultipliers  ProbabilityMultipliers { get; set; }

        /// <summary>
        /// Gets the name of the Scenario
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets the name of the <see cref="Callout"/> script that runs this scenario
        /// </summary>
        public string CalloutName { get; set; }

        /// <summary>
        /// Indicates whether the responding unit should respond Code 3
        /// </summary>
        public ResponseCode ResponseCode { get; set; }

        /// <summary>
        /// Gets the priority level of the call
        /// </summary>
        public CallPriority Priority { get; set; }

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
        /// Gets the <see cref="Dispatching.CallCategory"/> of this scenario
        /// </summary>
        public CallCategory Category { get; internal set; }

        /// <summary>
        /// Gets the <see cref="Dispatching.CallTarget"/> of this scenario
        /// </summary>
        public CallTarget Targets { get; internal set; }

        /// <summary>
        /// Contains an array of <see cref="Agency"/>s that can handle this scenario
        /// </summary>
        public AgencyType[] AgencyTypes { get; internal set; }

        /// <summary>
        /// Gets the ideal <see cref="OfficerUnit"/> count to handle this call.
        /// </summary>
        public int UnitCount { get; internal set; } = 1;

        /// <summary>
        /// Contains a list of <see cref="Circumstance"/> from the CalloutMeta.xml
        /// </summary>
        public Circumstance[] Circumstances { get; internal set; }

        /// <summary>
        /// Selects a random <see cref="Circumstance"/>, using an <see cref="ExpressionParser"/>
        /// to evaluate acceptable <see cref="Circumstance"/>s based on the conditions set for
        /// each <see cref="Circumstance"/>
        /// </summary>
        /// <param name="parser"></param>
        public bool GetRandomCircumstance(ExpressionParser parser, out Circumstance selected)
        {
            var gen = new ProbabilityGenerator<Circumstance>();
            foreach (var scenario in Circumstances)
            {
                if (scenario.Evaluate(parser))
                {
                    gen.Add(scenario);
                }
            }

            return gen.TrySpawn(out selected);
        }
    }
}