using AgencyDispatchFramework.Game;
using Rage;
using System;
using System.Collections.Generic;

namespace AgencyDispatchFramework.Simulation
{
    /// <summary>
    /// Provides meta data used to spawn an officer <see cref="Ped"/> in the game world
    /// </summary>
    public class OfficerModelMeta : ISpawnable
    {
        /// <summary>
        /// Gets the chance that this meta will be used in a <see cref="ProbabilityGenerator{T}"/>
        /// </summary>
        public int Probability { get; protected set; }

        /// <summary>
        /// Gets or sets the officer <see cref="Rage.Ped"/>s <see cref="Rage.Model"/>
        /// </summary>
        public Model Model { get; set; }

        /// <summary>
        /// Gets a hash table of <see cref="PedComponent"/>s to spawn this <see cref="Ped"/> with.
        /// </summary>
        /// <remarks>Tuple{DrawableId, TextureId}</remarks>
        public Dictionary<PedComponent, Tuple<int, int>> Components { get; internal set; }

        /// <summary>
        /// Gets a hash table of <see cref="PedPropIndex"/>s to spawn this <see cref="Ped"/> with.
        /// </summary>
        /// <remarks>Tuple{DrawableId, TextureId}</remarks>
        public Dictionary<PedPropIndex, Tuple<int, int>> Props { get; internal set; }

        /// <summary>
        /// Creates a new instance
        /// </summary>
        public OfficerModelMeta(int probability, Model model)
        {
            Probability = probability;
            Model = model;
            Components = new Dictionary<PedComponent, Tuple<int, int>>();
            Props = new Dictionary<PedPropIndex, Tuple<int, int>>();
        }
    }
}
