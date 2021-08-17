﻿using Rage;
using System.Collections.Generic;
using System.Drawing;

namespace AgencyDispatchFramework.Simulation
{
    /// <summary>
    /// Provides meta data used to spawn a <see cref="Vehicle"/> in the game world
    /// </summary>
    public class VehicleModelMeta : ISpawnable
    {
        /// <summary>
        /// Gets the chance that this meta will be used in a <see cref="ProbabilityGenerator{T}"/>
        /// </summary>
        public int Probability { get; protected set; }

        /// <summary>
        /// Gets or sets the <see cref="Rage.Model"/> of this <see cref="Vehicle"/>
        /// </summary>
        public Model Model { get; set; }

        /// <summary>
        /// Gets or Sets the livery index of the <see cref="Vehicle"/>
        /// </summary>
        public int LiveryIndex { get; set; }

        /// <summary>
        /// Gets or Sets the color of the <see cref="Vehicle"/>
        /// </summary>
        public Color SpawnColor { get; set; }

        /// <summary>
        /// Gets or Sets the extras values of the <see cref="Vehicle"/>
        /// </summary>
        public Dictionary<int, bool> Extras { get; set; }

        /// <summary>
        /// Creates a new instance of <see cref="VehicleModelMeta"/>
        /// </summary>
        public VehicleModelMeta(int probability, Model model)
        {
            Probability = probability;
            Model = model;
            Extras = new Dictionary<int, bool>();
        }
    }
}
