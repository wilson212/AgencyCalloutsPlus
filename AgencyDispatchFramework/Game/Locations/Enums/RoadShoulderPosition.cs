namespace AgencyDispatchFramework.Game.Locations
{
    /// <summary>
    /// Provides indentifiers for 2 safe locations to spawn <see cref="Rage.Ped"/>s 
    /// on a <see cref="RoadShoulder"/>. There are 2 groups, each having enough
    /// space to spawn peds around the location using relative positions
    /// </summary>
    /// <remarks>
    /// Use <see cref="Rage.Entity.GetOffsetPosition(Rage.Vector3)"/> to spawn peds 
    /// around the groups
    /// </remarks>
    /// <seealso cref="https://docs.ragepluginhook.net/html/M_Rage_Entity_GetOffsetPosition.htm"/>
    public enum RoadShoulderPosition
    {
        /// <summary>
        /// A <see cref="Rage.Ped"/> that is facing opposite the
        /// heading of the car
        /// </summary>
        SidewalkGroup1,

        /// <summary>
        /// A <see cref="Rage.Ped"/> that is facing the same
        /// heading of the car, but behind it a ways, where a second
        /// car would be parked
        /// </summary>
        SidewalkGroup2,
    }
}
