using AgencyDispatchFramework.Game;
using Rage;
using System;
using System.Collections.Generic;

namespace AgencyDispatchFramework.Simulation
{
    public class OfficerModelMeta : ISpawnable
    {
        /// <summary>
        /// Gets the chance that this meta will be used in a <see cref="ProbabilityGenerator{T}"/>
        /// </summary>
        public int Probability { get; private set; }

        /// <summary>
        /// Gets or sets the officer <see cref="Rage.Ped"/>s <see cref="Rage.Model"/>
        /// </summary>
        public Model Model { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public Dictionary<PedComponent, Tuple<int, int>> Components { get; internal set; }

        /// <summary>
        /// 
        /// </summary>
        public Dictionary<PedPropIndex, Tuple<int, int>> Props { get; internal set; }

        /// <summary>
        /// Creates a new instance
        /// </summary>
        public OfficerModelMeta(int probability)
        {
            Probability = probability;
            Components = new Dictionary<PedComponent, Tuple<int, int>>();
            Props = new Dictionary<PedPropIndex, Tuple<int, int>>();
        }
    }
}
