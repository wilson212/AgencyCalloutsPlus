using AgencyCalloutsPlus.Extensions;
using LSPD_First_Response.Mod.Callouts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgencyCalloutsPlus.API
{
    internal class SpawnableAgencyCallout : ISpawnable
    {
        public string Name { get; set; }

        public CalloutType CrimeType { get; set; }

        /// <summary>
        /// The <see cref="LSPD_First_Response.Mod.Callouts.Callout"/> object
        /// </summary>
        public Type CalloutSystemType { get; set; }

        /// <summary>
        /// The probability of this callout being spawned in comparison to other callouts
        /// </summary>
        public int Probability { get; set; }

        public SpawnableAgencyCallout(Type classType, CalloutType calloutType, int probability)
        {
            Name = classType.GetAttributeValue((CalloutInfoAttribute attr) => attr.Name);
            CalloutSystemType = classType;
            Probability = probability;
        }
    }
}
