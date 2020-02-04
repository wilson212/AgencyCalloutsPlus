using System;
using AgencyCalloutsPlus.API;

namespace AgencyCalloutsPlus
{
    /// <summary>
    /// Contains Scenario info for an <see cref="AgencyCallout"/>
    /// </summary>
    internal class CalloutScenarioInfo : ISpawnable
    {
        /// <summary>
        /// Gets the total probability of this scenario spawning from
        /// a <see cref="SpawnGenerator{T}"/>
        /// </summary>
        public int Probability { get; set; }

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
        public bool RespondCode3 { get; set; }

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
        public PriorityCallDescription[] Descriptions { get; set; }

        internal PriorityCallDescription GetRandomDescription()
        {
            if (Descriptions.Length == 0) return null;

            int count = Descriptions.Length - 1;
            int index = new Random().Next(0, count);
            return Descriptions[index];
        }
    }
}