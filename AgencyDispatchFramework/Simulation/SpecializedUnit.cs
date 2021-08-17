using AgencyDispatchFramework.Dispatching;

namespace AgencyDispatchFramework.Simulation
{
    /// <summary>
    /// An object that provides an interface to spawn <see cref="VehicleSet"/>s based on the
    /// <see cref="UnitType"/>
    /// </summary>
    public class SpecializedUnit
    {
        /// <summary>
        /// Gets the <see cref="UnitType"/> of this <see cref="SpecializedUnit"/>
        /// </summary>
        public UnitType UnitType { get; private set; }

        /// <summary>
        /// Containts a <see cref="ProbabilityGenerator{T}"/> list of <see cref="VehicleSet"/> for this agency
        /// </summary>
        public ProbabilityGenerator<VehicleSet> OfficerSets { get; internal set; }

        /// <summary>
        /// Containts a <see cref="ProbabilityGenerator{T}"/> list of <see cref="VehicleSet"/> for this agency
        /// </summary>
        public ProbabilityGenerator<VehicleSet> SupervisorSets { get; internal set; }

        /// <summary>
        /// Creates a new instance of <see cref="SpecializedUnit"/>
        /// </summary>
        /// <param name="type"></param>
        public SpecializedUnit(UnitType type)
        {
            UnitType = type;
            OfficerSets = new ProbabilityGenerator<VehicleSet>();
            SupervisorSets = new ProbabilityGenerator<VehicleSet>();
        }
    }
}
