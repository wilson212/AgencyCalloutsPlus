using System;
using System.Collections.Generic;

namespace AgencyDispatchFramework.Simulation
{
    /// <summary>
    /// An object that provides an interface to spawn meta data used to create <see cref="AIOfficerUnit"/>s
    /// </summary>
    public class VehicleSet : ISpawnable, ICloneable
    {
        /// <summary>
        /// Gets the chance that this meta will be used in a <see cref="ProbabilityGenerator{T}"/>
        /// </summary>
        public int Probability { get; private set; }

        /// <summary>
        /// Contains a probable list of vehicle metas specific to this <see cref="VehicleSet"/>
        /// </summary>
        public ProbabilityGenerator<VehicleModelMeta> VehicleMetas { get; set; }

        /// <summary>
        /// Contains a probable list of officer metas specific to this <see cref="VehicleSet"/>
        /// </summary>
        public ProbabilityGenerator<OfficerModelMeta> OfficerMetas { get; set; }

        /// <summary>
        /// Gets the names of the non-lethal weapons this officer unit will spawn with in thier inventory.
        /// </summary>
        public HashSet<string> NonLethalWeapons { get; internal set; }

        /// <summary>
        /// Gets the handgun metadata this officer unit will spawn with in thier inventory.
        /// </summary>
        public ProbabilityGenerator<WeaponMeta> HandGunMetas { get; internal set; }

        /// <summary>
        /// Gets the longgun metadata this officer unit will spawn with in thier inventory. 
        /// </summary>
        public ProbabilityGenerator<WeaponMeta> LongGunMetas { get; internal set; }

        /// <summary>
        /// Creates a new instance of <see cref="VehicleSet"/>
        /// </summary>
        public VehicleSet(int probability)
        {
            Probability = probability;
            VehicleMetas = new ProbabilityGenerator<VehicleModelMeta>();
            OfficerMetas = new ProbabilityGenerator<OfficerModelMeta>();
            HandGunMetas = new ProbabilityGenerator<WeaponMeta>();
            LongGunMetas = new ProbabilityGenerator<WeaponMeta>();
            NonLethalWeapons = new HashSet<string>();
        }

        public object Clone()
        {
            var clone = new VehicleSet(Probability);
            clone.VehicleMetas.AddRange(VehicleMetas.GetItems());
            clone.OfficerMetas.AddRange(OfficerMetas.GetItems());
            clone.HandGunMetas.AddRange(HandGunMetas.GetItems());
            clone.LongGunMetas.AddRange(LongGunMetas.GetItems());
            clone.NonLethalWeapons = new HashSet<string>(NonLethalWeapons);
            return clone;
        }
    }
}
