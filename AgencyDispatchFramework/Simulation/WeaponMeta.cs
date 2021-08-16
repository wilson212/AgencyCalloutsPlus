using System.Collections.Generic;

namespace AgencyDispatchFramework.Simulation
{
    public class WeaponMeta : ISpawnable
    {
        /// <summary>
        /// Gets the chance that this meta will be used in a <see cref="ProbabilityGenerator{T}"/>
        /// </summary>
        public int Probability { get; private set; }

        /// <summary>
        /// Gets or sets the name of the weapon
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets a list of weapon compenents to attach to this weapon
        /// </summary>
        public List<string> Components { get; set; }

        /// <summary>
        /// Creates a new instance of <see cref="WeaponMeta"/>
        /// </summary>
        /// <param name="probability"></param>
        public WeaponMeta(int probability)
        {
            Probability = probability;
            Components = new List<string>();
        }
    }
}
