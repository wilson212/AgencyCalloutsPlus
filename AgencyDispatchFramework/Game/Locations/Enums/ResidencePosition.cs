namespace AgencyDispatchFramework.Game.Locations
{
    /// <summary>
    /// 
    /// </summary>
    /// <remarks>
    /// Use <see cref="Rage.Entity.GetOffsetPosition(Rage.Vector3)"/> to spawn peds 
    /// around the groups
    /// </remarks>
    /// <seealso cref="https://docs.ragepluginhook.net/html/M_Rage_Entity_GetOffsetPosition.htm"/>
    public enum ResidencePosition
    {
        /// <summary>
        /// A ped standing at the front door, facing away from the door,
        /// talking to <see cref="FrontDoorPolicePed1"/> and <see cref="FrontDoorPolicePed2"/>
        /// </summary>
        FrontDoorPed1,

        /// <summary>
        /// A ped standing at the front door, facing away from the door,
        /// talking to <see cref="FrontDoorPolicePed1"/> and <see cref="FrontDoorPolicePed2"/>
        /// </summary>
        FrontDoorPed2,

        /// <summary>
        /// Talking to <see cref="FrontDoorPed1"/>
        /// </summary>
        FrontDoorPolicePed1,

        /// <summary>
        /// Talking to <see cref="FrontDoorPed1"/>
        /// </summary>
        FrontDoorPolicePed2,

        /// <summary>
        /// Talking to <see cref="FrontDoorPed1"/>, but standing back providing
        /// backup if needed
        /// </summary>
        FrontDoorPolicePed3,

        /// <summary>
        /// A ped group standing in the front yard, away from the front door. 
        /// Position will have clearance all around to spawn a Ped Group
        /// </summary>
        FrontYardPedGroup,

        /// <summary>
        /// A ped standing at the backdoor facing away from the door
        /// </summary>
        BackDoorPed,

        /// <summary>
        /// Talking to <see cref="BackDoorPed"/>
        /// </summary>
        BackDoorPolicePed,

        /// <summary>
        /// A ped group standing in the back yard, away from the back door. 
        /// Position will have clearance all around to spawn a Ped Group
        /// </summary>
        BackYardPedGroup,

        /// <summary>
        /// A ped group standing in the front yard, away from the front door. 
        /// Position will have clearance all around to spawn a Ped Group
        /// </summary>
        SideYardPedGroup,

        /// <summary>
        /// A ped standing on the sidewalk in front of the home,
        /// talking to <see cref="SideWalkPolicePed1"/>
        /// </summary>
        SidewalkPed,

        /// <summary>
        /// Talking to <see cref="SidewalkPed"/>
        /// </summary>
        SideWalkPolicePed1,

        /// <summary>
        /// Talking to <see cref="SidewalkPed"/>
        /// </summary>
        SideWalkPolicePed2,

        /// <summary>
        /// A place for a ped to hide
        /// </summary>
        HidingSpot1,

        /// <summary>
        /// A place for a ped to hide
        /// </summary>
        HidingSpot2,

        /// <summary>
        /// A place for police car to park
        /// </summary>
        PoliceParking1,

        /// <summary>
        /// A place for police car to park
        /// </summary>
        PoliceParking2,

        /// <summary>
        /// A place for police car to park
        /// </summary>
        PoliceParking3,

        /// <summary>
        /// A place for police car to park
        /// </summary>
        PoliceParking4,

        /// <summary>
        /// A place for a resident car to park
        /// </summary>
        ResidentParking1,

        /// <summary>
        /// A second place for a resident car to park
        /// </summary>
        ResidentParking2,
    }
}
