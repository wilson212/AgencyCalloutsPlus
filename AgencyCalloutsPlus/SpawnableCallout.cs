﻿using AgencyCalloutsPlus.Extensions;
using LSPD_First_Response.Mod.Callouts;
using System;

namespace AgencyCalloutsPlus
{
    /// <summary>
    /// Represents a spawnable entity, containing Callout information
    /// </summary>
    internal class SpawnableCallout : ISpawnable
    {
        public string Name { get; set; }

        /// <summary>
        /// The <see cref="LSPD_First_Response.Mod.Callouts.Callout"/> object
        /// </summary>
        public Type CalloutSystemType { get; set; }

        /// <summary>
        /// The probability of this callout being spawned in comparison to other callouts
        /// </summary>
        public int Probability { get; set; }

        public SpawnableCallout(Type calloutType, int probability)
        {
            Name = calloutType.GetAttributeValue((CalloutInfoAttribute attr) => attr.Name);
            CalloutSystemType = calloutType;
            Probability = probability;
        }
    }
}
