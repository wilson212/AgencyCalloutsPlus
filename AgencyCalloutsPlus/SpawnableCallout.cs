using System;

namespace AgencyCalloutsPlus
{
    /// <summary>
    /// Represents a spawnable entity, containing Callout information
    /// </summary>
    internal class SpawnableCallout : ISpawnable
    {
        /// <summary>
        /// The <see cref="LSPD_First_Response.Mod.Callouts.Callout"/> object
        /// </summary>
        public Type CalloutType { get; set; }

        /// <summary>
        /// The probability of this callout being spawned in comparison to other callouts
        /// </summary>
        public int Probability { get; set; }

        public SpawnableCallout(Type calloutType, int probability)
        {
            CalloutType = calloutType;
            Probability = probability;
        }
    }
}
